using _Scripts.LDY;
using _Scripts.LSO.CoreLib;
using _Scripts.LSO.HealthSystem;
using _Scripts.LSO.HealthSystem.Data;
using UnityEngine;

namespace _Scripts.LSO.Ability
{
    /// <summary>
    /// 가시: 맞닿은 적에게 공격받으면 그 공격자에게 고정 피해를 되돌린다.
    ///
    /// ── 무엇으로 "닿았다"를 재는가 ────────────────────────────
    /// 예전에는 공격의 종류(LSO_DamageSource.Melee)로 갈랐다. 공격하는 쪽이
    /// 스스로 "나는 근접이다"라고 말한 값을 믿은 셈이다.
    ///
    /// 그래서 <b>실제로 어디 서 있는지와 어긋날 수 있었다.</b> 멀리서 근접으로
    /// 분류된 공격이 오면 반사됐고, 바로 옆에 붙어서 원거리로 쏘면 반사되지 않았다.
    ///
    /// 지금은 때린 기물의 격자 좌표를 직접 본다. 여덟 방향 어디든 한 칸 안에
    /// 붙어 있으면 찔린다 — 체비쇼프 거리다.
    /// ─────────────────────────────────────────────────────────
    ///
    /// 공격이 아닌 피해(저주·분노 장판, 상어왕 영역, 다른 특성)에는 반응하지 않는다.
    /// 그 판단만은 여전히 LSO_DamageSource 가 한다. 아래 IsAttack 주석 참고.
    /// </summary>
    public sealed class LSO_Thorns : LSO_IAbility, LSO_IOnHit, LSO_IAbilityInitializable
    {
        private const int DefaultReflectDamage = 1;

        /// <summary>맞닿음으로 치는 거리. 1이면 자기 칸에 인접한 여덟 칸이다.</summary>
        private const int DefaultReflectRange = 1;

        public int ReflectDamage { get; private set; } = DefaultReflectDamage;

        /// <summary>이 거리(체비쇼프) 안에서 때렸을 때만 반사한다.</summary>
        public int ReflectRange { get; private set; } = DefaultReflectRange;

        private LSO_AbilityContext _context;

        public LSO_Thorns() { }

        public LSO_Thorns(int reflectDamage, int reflectRange = DefaultReflectRange)
        {
            ReflectDamage = Mathf.Max(0, reflectDamage);
            ReflectRange = Mathf.Max(0, reflectRange);
        }

        public void Initialize(LSO_AbilityContext context)
        {
            _context = context;
        }

        public void OnHit(LDY_Animal self, DamageData data)
        {
            if (ReflectDamage <= 0) return;
            if (!IsAttack(data.source)) return;
            if (data.giver == null) return;
            if (self == null || self.health == null || self.health.IsDestroyed) return;

            LDY_Animal attacker = data.giver.GetComponent<LDY_Animal>();
            if (attacker == null || attacker == self) return;
            if (attacker.health == null || attacker.health.IsDestroyed) return;

            // 실제로 붙어 있는지는 좌표가 정한다. 공격이 스스로 밝힌 종류가 아니라
            // 맞은 순간 둘이 어디 서 있었는지를 본다.
            if (ChebyshevDistance(self.pos, attacker.pos) > ReflectRange) return;

            attacker.health.GetDamage(
                DamageData.Create(self.health, ReflectDamage, LSO_DamageSource.Ability));

            // 이펙트는 **때린 쪽**에 뜬다. 찔린 것은 공격자이므로 가시가 고슴도치 자리에
            // 돋으면 누가 피해를 입었는지 화면과 숫자가 어긋난다.
            //
            // 자리는 이 순간에 잡힌다. 바로 아래에서 공격자가 죽어 사라져도 남는다.
            LSO_AbilitySignal.Raise(LSO_AbilityType.Thorns, self, attacker,
                $"<color=green>{self.name}의 가시: {attacker.name}에게 {ReflectDamage} 반사</color>");

            // 반사로 상대가 죽었다면 사망 처리까지 이어줘야 보드에서 사라진다.
            if (attacker.health.IsDestroyed)
                _context?.Deaths?.Kill(attacker, self);
        }

        /// <summary>
        /// 가시가 반응할 "공격"인지.
        ///
        /// 거리를 좌표로 재게 된 뒤에도 이 판단만은 LSO_DamageSource 가 한다.
        /// 저주·분노 장판이나 상어왕 영역은 누가 때린 것이 아니라서 되돌릴 상대가 없고,
        /// 붙어 있다는 이유만으로 반사하면 장판 위에 선 것이 곧 반격이 된다.
        ///
        /// <b>Ability 를 빼두는 것이 특히 중요하다.</b> 가시가 주는 피해가 Ability 인데
        /// 그것까지 반사 대상으로 삼으면, 고슴도치 둘이 붙어 있을 때
        /// 서로의 반사가 서로를 다시 찔러 그 자리에서 무한히 되먹임된다.
        /// </summary>
        private static bool IsAttack(LSO_DamageSource source)
        {
            return source == LSO_DamageSource.Melee ||
                   source == LSO_DamageSource.Ranged ||
                   source == LSO_DamageSource.Jump;
        }

        /// <summary>
        /// 두 칸 사이의 체비쇼프 거리. 대각선도 한 칸으로 센다.
        ///
        /// 보드는 x·z 평면이고 y 는 항상 0이라(LDY_BoardManager 가 Vector3Int(x, 0, z) 로 쓴다)
        /// 두 축만 본다. y 를 섞으면 쓰지도 않는 축 때문에 거리가 부풀 수 있다.
        ///
        /// 이 거리를 쓰면 인접한 여덟 칸이 모두 1이 된다 — 대각선으로 붙은 적도
        /// 옆에 붙은 적과 똑같이 찔린다.
        /// </summary>
        private static int ChebyshevDistance(Vector3Int a, Vector3Int b)
        {
            return Mathf.Max(Mathf.Abs(a.x - b.x), Mathf.Abs(a.z - b.z));
        }
    }
}
