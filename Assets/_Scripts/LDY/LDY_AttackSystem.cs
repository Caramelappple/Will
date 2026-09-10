using System;
using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using _Scripts.LSO.Ability;
using _Scripts.LSO.DeathSystem;
using _Scripts.LSO.HealthSystem.Data;
using _Scripts.LSO.Manager;
using _Scripts.LSO.Will;
using _Scripts.LSO.UI.Effect;
using UnityEngine;
using _Scripts.LSO.Interfaces;

namespace _Scripts.LDY
{
    // 씬 배선: BoardManager를 연결할 것.
    public class LDY_AttackSystem : MonoBehaviour
    {
        [SerializeField] private LDY_BoardManager board;
        [SerializeField] private LDY_ActionPointManager actionPoints;
        [SerializeField] private float attackDuration = 0.3f;
        [SerializeField] private float lungeRatio = 0.4f;
        [Tooltip("공격 돌진이 그리는 포물선의 높이. 너무 높으면 점프처럼 보인다.")]
        [SerializeField] private float lungeArcHeight = 0.12f;
        [Tooltip("달려드는 동안 대상 쪽으로 기울어지는 각도(도). 축은 공격자->대상 방향으로 자동 계산하므로\n" +
                 "앞/뒤/양옆 어느 쪽을 공격하든 그 방향으로 수그러진다. 복귀할 때는 원래 각도로 돌아온다.")]
        [SerializeField] private float lungeTiltAngle = 15f;

        // 특성이 공격 횟수를 잘못 계산했을 때 연출이 끝나지 않는 것을 막는 상한.
        // 기획상 필요한 값이 아니라 폭주 방지선이므로 인스펙터에 열지 않는다.
        private const int MaxAttackCount = 8;

        // 공격 연출(코루틴)이 하나라도 재생 중이면 true. 턴 전환이 이 애니메이션 도중에 끼어들지 않도록 막는 용도.
        // 다단 타격은 한 번의 공격 행동이므로 횟수와 무관하게 1로 센다.
        public bool IsBusy => _activeCount > 0;
        public LDY_ActionPointManager ActionPoints => actionPoints;
        private int _activeCount;
        private readonly HashSet<LDY_Animal> _attackingAnimals = new();

        public bool IsAttacking(LDY_Animal animal) => animal != null && _attackingAnimals.Contains(animal);

        public List<Vector3Int> GetAttackableTiles(LDY_Animal attacker)
        {
            if (attacker == null || IsAttacking(attacker)) return new List<Vector3Int>();
            if (actionPoints != null && !actionPoints.HasActionPoints) return new List<Vector3Int>();

            return AttackableTilesFrom(attacker, attacker.pos, board);
        }

        /// <summary>
        /// 아직 가보지 않은 좌표에서 공격할 대상이 있는지 묻는다.
        /// 행동을 고르는 평가 단계용이라 행동력을 보지 않는다 — 실행이 아니므로 소모 여부와 무관하다.
        /// 인스턴스 상태가 필요 없는 순수 판정이라 보드를 인자로 받는다.
        /// </summary>
        public static bool HasTargetFrom(LDY_Animal attacker, Vector3Int from, LDY_BoardManager board)
        {
            if (attacker == null || board == null) return false;

            foreach (var tile in AttackableTilesFrom(attacker, from, board))
            {
                var occupant = board.Get(tile);
                if (occupant != null && occupant.team != attacker.team)
                    return true;
            }
            return false;
        }

        /// <summary>연결된 보드를 기준으로 한 HasTargetFrom.</summary>
        public bool HasTargetFrom(LDY_Animal attacker, Vector3Int from)
        {
            return HasTargetFrom(attacker, from, board);
        }

        // 순수 사거리 판정. 판정 자체는 항상 LDY_IAttackRange 구현에 위임한다.
        private static List<Vector3Int> AttackableTilesFrom(LDY_Animal attacker, Vector3Int from, LDY_BoardManager board)
        {
            var strategy = LDY_AttackRangeFactory.Create(attacker.RangeType);
            return strategy != null
                ? strategy.GetAttackableTiles(from, board)
                : new List<Vector3Int>();
        }

        public List<LDY_Animal> GetAttackTargets(LDY_Animal attacker)
        {
            var targets = new List<LDY_Animal>();
            if (attacker == null) return targets;

            foreach (var tile in GetAttackableTiles(attacker))
            {
                var occupant = board.Get(tile);
                if (occupant != null && occupant.team != attacker.team)
                    targets.Add(occupant);
            }
            return targets;
        }

        /// <summary>
        /// onComplete는 공격 연출이 완전히 끝난 뒤(성공하든 검증에 막히든) 반드시 호출된다.
        /// 호출자가 공격 전에 호버를 뜬 채로 남겨뒀다면(Deselect의 lowerHover: false 등),
        /// 그 자리를 내려놓는 책임은 이 콜백을 받는 쪽에 있다 — 여기서 실패해도 안 부르면
        /// 기물이 영영 뜬 채로 남는다.
        /// </summary>
        public void Attack(LDY_Animal attacker, LDY_Animal target, Action onComplete = null)
        {
            if (attacker == null || target == null ||
                !GetAttackTargets(attacker).Contains(target) ||
                (actionPoints != null && !actionPoints.TryConsume()))
            {
                onComplete?.Invoke();
                return;
            }

            StartCoroutine(AttackRoutine(attacker, target, onComplete));
        }

        /// <summary>
        /// 한 번의 공격 행동을 처리한다. 특성에 따라 여러 번 때릴 수 있다.
        /// 행동력은 Attack에서 이미 한 번만 소모했으므로 여기서는 건드리지 않는다.
        /// </summary>
        private IEnumerator AttackRoutine(LDY_Animal attacker, LDY_Animal target, Action onComplete)
        {
            _attackingAnimals.Add(attacker);
            _activeCount++;
            var hoverEffects = attacker.GetComponentsInChildren<LSO_HoverMoveEffect>(true);
            try
            {
                // 자리를 되돌리지 않고 트윈만 멈춘다. 공격은 떠 있는 상태에서 그대로 재생돼야
                // 하고("공격할때는 떠있는 상태에서 포물선으로"), 내려놓는 건 onComplete를 받는
                // 쪽(선택 컨트롤러)이 공격이 끝난 뒤에 한다.
                foreach (var effect in hoverEffects)
                    if (effect != null) effect.SetSuspended(true, restore: false);

                int count = ResolveAttackCount(attacker, target);

                for (int i = 0; i < count; i++)
                {
                    // 앞선 타격으로 대상이 죽었거나 공격자가 사라졌으면 남은 횟수는 버린다.
                    if (attacker == null || target == null) break;
                    if (target.health == null || target.health.IsDestroyed) break;

                    yield return StrikeOnce(attacker, target);
                }
            }
            finally
            {
                _activeCount--;
                _attackingAnimals.Remove(attacker);
                foreach (var effect in hoverEffects)
                    if (effect != null) effect.SetSuspended(false);
                onComplete?.Invoke();
            }
        }

        /// <summary>
        /// 특성들에게 몇 번 때릴지 물어본다. 아무도 손대지 않으면 1이다.
        /// </summary>
        private static int ResolveAttackCount(LDY_Animal attacker, LDY_Animal target)
        {
            if (attacker == null) return 1;

            int count = LSO_AbilityNotify.Accumulate<LSO_IAbilityCountModifier>(
                attacker.Abilities, 1, (mod, value) => mod.ModifyAttackCount(attacker, target, value));

            // 0 이하가 나오면 행동력만 쓰고 아무 일도 안 일어난다.
            // 위쪽 한계는 특성이 잘못 계산했을 때 연출이 끝나지 않는 것을 막는 안전장치다.
            return Mathf.Clamp(count, 1, MaxAttackCount);
        }

        // 공격 대상 쪽으로 살짝 달려들었다가 원위치로 돌아오는 연출. 데미지는 달려든 시점(절반 지점)에 적용한다.
        private IEnumerator StrikeOnce(LDY_Animal attacker, LDY_Animal target)
        {
            Transform t = attacker.modelTransform;
            Vector3 startPos = t.position;
            Quaternion startRot = t.localRotation;
            Vector3 lungePos = Vector3.Lerp(startPos, target.modelTransform.position, lungeRatio);
            Quaternion lungeRot = startRot * Quaternion.AngleAxis(lungeTiltAngle, LungeTiltAxis(t, startPos, target.modelTransform.position));
            float half = attackDuration * 0.5f;

            yield return LungeTo(t, lungePos, lungeRot, half, attacker.gameObject);

            // 연출이 재생되는 동안 다른 공격이 같은 대상을 먼저 처치했을 수 있으므로 다시 확인한다.
            if (target != null)
            {
                if (target.health != null && attacker.health != null)
                {
                    // 피해량은 때리는 쪽의 공격력이다. 출처를 함께 실어 보내면
                    // "근접 공격을 받으면 반격" 같은 특성이 판단할 수 있다.
                    DamageData data = DamageData.Create(
                        attacker.health,
                        attacker.GetAtk(),
                        ToDamageSource(attacker.RangeType));

                    NotifyAttackAbilities(attacker);
                    target.health.GetDamage(data);
                    if (target.health.IsDestroyed)
                        HandleDeath(target, attacker);
                }
                else
                {
                    Debug.Log("체력이 존재하지 않습니다");
                }
            }

            if (attacker != null && t != null)
                yield return LungeTo(t, startPos, startRot, half, attacker.gameObject);
        }

        /// <summary>
        /// 공격자 -> 대상 방향(수평)을 향해 기울어지도록, 그 방향에 수직인 축을 구한다.
        /// 축이 로컬 회전(t.localRotation) 기준이라야 하므로 부모의 회전을 걷어낸다.
        /// 방향이 거의 수직(같은 칸을 찌르는 등 수평 성분이 0에 가까움)이면 원래 축(X)으로 대체한다.
        /// </summary>
        private static Vector3 LungeTiltAxis(Transform t, Vector3 fromWorldPos, Vector3 toWorldPos)
        {
            Vector3 dir = toWorldPos - fromWorldPos;
            dir.y = 0f;

            if (dir.sqrMagnitude < 0.0001f)
                dir = Vector3.forward;
            else
                dir.Normalize();

            Vector3 axis = Vector3.Cross(Vector3.up, dir);
            if (t.parent != null)
                axis = t.parent.InverseTransformDirection(axis);

            return axis.sqrMagnitude > 0.0001f ? axis.normalized : Vector3.right;
        }

        /// <summary>
        /// 두트윈으로 toPos까지 포물선(DOJump)을 그리며 이동하는 동시에 toRot까지 기울인다.
        /// linkTarget이 파괴되면(유언 등으로 기물 사망) 트윈도 같이 끊긴다.
        /// </summary>
        private IEnumerator LungeTo(Transform t, Vector3 toPos, Quaternion toRot, float duration, GameObject linkTarget)
        {
            if (t == null) yield break;

            Sequence seq = DOTween.Sequence()
                .Append(t.DOJump(toPos, lungeArcHeight, 1, duration))
                .Join(t.DOLocalRotateQuaternion(toRot, duration))
                .SetLink(linkTarget);

            yield return seq.WaitForCompletion();
        }

        public void HandleDeath(LDY_Animal target)
        {
            HandleDeath(target, null);
        }

        /// <summary>
        /// 사망 처리는 LDY_DeathHandler에 위임한다.
        /// 씬에 핸들러가 없으면 예전 방식으로 직접 처리해서 기존 씬이 깨지지 않게 한다.
        /// </summary>
        public void HandleDeath(LDY_Animal target, LDY_Animal killer)
        {
            if (target == null) return;

            LSO_IDeathService deathService = GameManager.HasInstance
                ? GameManager.Instance.DeathService
                : null;

            if (deathService != null)
            {
                deathService.Kill(target, killer);
                return;
            }

            board.Remove(target);
            DLJ_CombatKillEvents.Raise(target, killer);
            RaiseAnimalDead(target);

            LSO_IWill will = DLJ_WillRuntime.Invoke(target, board);

            if (!(will is DLJ_IDeferredDestruction deferred) ||
                !deferred.ShouldDeferDestruction)
            {
                // LDY_DeathHandler와 같게 유지할 것. 파괴는 디졸브가 끝난 뒤에 일어난다.
                LDY_DissolveEffect.PlayOn(target.gameObject);
                return;
            }

            // LDY_DeathHandler와 같은 이유로 기록한다. 이쪽은 핸들러가 없을 때 쓰이는 예전 경로다.
            LDY_DeferredDeaths.Record(target);
        }

        private static LSO_DamageSource ToDamageSource(LDY_RangeType rangeType)
        {
            switch (rangeType)
            {
                case LDY_RangeType.Melee: return LSO_DamageSource.Melee;
                case LDY_RangeType.Ranged: return LSO_DamageSource.Ranged;
                case LDY_RangeType.Jump: return LSO_DamageSource.Jump;
                default: return LSO_DamageSource.Unknown;
            }
        }

        private static void NotifyAttackAbilities(LDY_Animal attacker)
        {
            if (attacker == null) return;

            LSO_AbilityNotify.Notify<IOnAnimalAttack>(
                attacker.Abilities, a => a.OnAttack(attacker.data));
        }

        // 팀을 가리지 않고 모든 죽음을 알린다. 누가 적인지는 받는 특성이 판단한다.
        // LDY_DeathHandler.RaiseAnimalDead와 반드시 같게 유지할 것.
        private static void RaiseAnimalDead(LDY_Animal target)
        {
            if (target == null) return;
            if (!GameManager.HasInstance) return;

            GameEventDispatcher dispatcher = GameManager.Instance.EventDispatcher;
            if (dispatcher == null) return;

            dispatcher.RaiseAnimalDead(target);
        }
    }
}
