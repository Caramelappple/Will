using System.Collections.Generic;
using System.Text;
using _Scripts.LSO.Manager;
using _Scripts.LSO;
using UnityEngine;

namespace _Scripts.LDY
{
    /// <summary>
    /// 계승 대기가 시작되면 주체 팀을 판별해, 적 팀이면 대상을 자동으로 고른다.
    /// 플레이어 팀이면 아무것도 하지 않는다 — 사람이 LDY_SelectionController로 클릭해서 고른다.
    ///
    /// 씬 배선: BoardManager를 연결할 것. 비워두면 GameManager에 등록된 보드를 쓴다.
    /// </summary>
    public class LDY_SuccessionResolver : MonoBehaviour
    {
        [SerializeField] private LDY_BoardManager board;

        private LDY_ISuccessionTargetPicker _picker;
        private bool _wasWaiting;

        /// <summary>스테이지별로 선택 기준을 갈아끼울 때 쓴다.</summary>
        public LDY_ISuccessionTargetPicker Picker
        {
            get => _picker ??= new LDY_StrongestSuccessionPicker();
            set => _picker = value;
        }

        // 계승 Activate()는 코루틴 갱신 단계(Update 직후, LateUpdate 직전)에서 실행된다.
        // LateUpdate에서 잡아야 timeScale이 0인 채로 프레임이 넘어가지 않는다.
        private void LateUpdate()
        {
            bool waiting = DLJ_SuccessionSystem.IsWaitingForSuccessionTarget;

            if (!waiting)
            {
                // 계승이 끝났다. 기록이 다음 계승까지 넘어가 팀 판별을 흐리지 않도록 비운다.
                if (_wasWaiting)
                {
                    LDY_DeferredDeaths.Clear();
                    _wasWaiting = false;
                }
                return;
            }

            if (_wasWaiting) return;
            _wasWaiting = true;

            HandleSuccessionStarted();
        }

        private void HandleSuccessionStarted()
        {
            if (!LDY_DeferredDeaths.TryGetCommonTeam(out LDY_Team team))
            {
                Debug.LogError(
                    "[LDY_SuccessionResolver] 계승 주체 팀을 판별할 수 없어 자동 선택을 하지 않는다. " +
                    $"근거: {LDY_DeferredDeaths.Describe()}. " +
                    "팀이 섞여 있거나 기록이 비어 있다. 주체가 플레이어 팀이면 클릭으로 진행할 수 있지만, " +
                    "적 팀이면 정지 상태가 그대로 유지된다.", this);
                return;
            }

            if (team == LDY_Team.Player) return;

            AutoSelect(team);
        }

        private void AutoSelect(LDY_Team team)
        {
            LDY_BoardManager targetBoard = ResolveBoard();
            if (targetBoard == null)
            {
                Debug.LogError($"{name}: BoardManager를 찾을 수 없어 계승 대상을 고를 수 없다.", this);
                return;
            }

            LDY_Animal dying = FirstPending();
            List<LDY_Animal> candidates = targetBoard.GetAllByTeam(team);

            var remaining = new List<LDY_Animal>(candidates);
            var rejected = new List<LDY_Animal>();

            while (remaining.Count > 0)
            {
                LDY_Animal picked = Picker.Pick(dying, remaining);
                if (picked == null) break;

                // 유효 대상인지는 DLJ_SuccessionSystem이 판단한다. 여기서는 후보를 넘기기만 한다.
                if (DLJ_SuccessionSystem.TrySelectSuccessionTarget(picked))
                    return;

                rejected.Add(picked);
                if (!remaining.Remove(picked)) break;
            }

            ReportUnresolvable(team, candidates, rejected);
        }

        // 이 로그는 그대로 DLJ 담당자에게 전달할 수 있어야 한다.
        private void ReportUnresolvable(LDY_Team team, List<LDY_Animal> candidates, List<LDY_Animal> rejected)
        {
            var builder = new StringBuilder();
            builder.Append("[LDY_SuccessionResolver] 계승 대상을 고르지 못해 게임이 정지 상태로 남는다.\n");
            builder.Append("- 계승 주체 팀: ").Append(team)
                   .Append(" (판별 근거: ").Append(LDY_DeferredDeaths.Describe()).Append(")\n");
            builder.Append("- 검토한 ").Append(team).Append(" 기물 ").Append(candidates.Count).Append("개");

            if (candidates.Count == 0)
            {
                builder.Append(" — 보드에 남은 기물이 없다.\n");
            }
            else
            {
                builder.Append(":\n");
                foreach (LDY_Animal candidate in candidates)
                {
                    builder.Append("    ").Append(DescribeCandidate(candidate))
                           .Append(rejected.Contains(candidate) ? " → DLJ가 거부함" : " → 선택 시도 안 함")
                           .Append('\n');
                }
            }

            builder.Append("- DLJ_SuccessionSystem에는 취소나 타임아웃 경로가 없다. ");
            builder.Append("Time.timeScale은 TrySelectSuccessionTarget이 성공해야만 복구되므로, ");
            builder.Append("이 상태에서는 플레이 모드를 종료하는 것 외에 복구 방법이 없다.");

            Debug.LogError(builder.ToString(), this);
        }

        private static string DescribeCandidate(LDY_Animal candidate)
        {
            if (candidate == null) return "이미 파괴된 기물";
            if (candidate.health == null) return $"{candidate.name} (Health 컴포넌트 없음)";

            return $"{candidate.name} (HP {candidate.health.GetValue()}/{candidate.health.MaxValue}, " +
                   $"team {candidate.team}, z {candidate.pos.z}, destroyed {candidate.health.IsDestroyed})";
        }

        private static LDY_Animal FirstPending()
        {
            foreach (LDY_Animal victim in LDY_DeferredDeaths.Pending)
            {
                if (victim != null) return victim;
            }
            return null;
        }

        private LDY_BoardManager ResolveBoard()
        {
            if (board != null) return board;

            return GameManager.HasInstance ? GameManager.Instance.Board : null;
        }
    }
}
