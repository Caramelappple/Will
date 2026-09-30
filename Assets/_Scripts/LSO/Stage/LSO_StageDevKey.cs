using _Scripts.LDY.Stage;
using UnityEngine;
using UnityEngine.InputSystem;

namespace _Scripts.LSO.Stage
{
    /// <summary>
    /// 개발자 키(기본 <b>Shift+F7</b>). 다음에 나올 스테이지를 지정한 것으로 바꾼다.
    ///
    /// 보스 연출이나 보스 전투를 확인할 때, 앞 스테이지를 전부 깨지 않아도 되게 한다.
    ///
    /// ── 바로 보내지 않는 이유 ─────────────────────────────────
    /// 자리만 바꿔서 그 스테이지로 순간이동시키면 클리어 처리와 판 뒤집기,
    /// 보상 단계가 통째로 건너뛰어진다. <b>실제 플레이와 다른 경로로 도착하므로</b>
    /// 거기서 보이는 것이 진짜 그 스테이지의 모습인지 믿을 수 없다.
    ///
    /// 그래서 "지금 판을 깨면 다음은 저것"으로만 맞춘다.
    /// 전투를 건너뛰고 싶으면 KTH_TestClearButton 과 같이 쓰면 된다.
    /// ─────────────────────────────────────────────────────────
    ///
    /// ── 빌드에도 들어간다 ─────────────────────────────────────
    /// 다른 개발자 키(LDY_SaveDebugHotkeys · LDY_BoardFlipDirector 의 F10·F11)는
    /// 전부 UNITY_EDITOR 로 묶여 빌드에서 빠진다. 이것만 예외다 —
    /// <b>테스터가 앞 스테이지를 다 깨지 않고 보스를 확인해야 하기 때문이다.</b>
    ///
    /// 그래서 배포 빌드에서도 이 키가 살아 있다. 플레이어에게 나갈 빌드라면
    /// 이 컴포넌트를 씬에서 빼거나 Jump Key 를 None 으로 두면 된다.
    /// ─────────────────────────────────────────────────────────
    ///
    /// 씬 배선: 아무 오브젝트에나 하나 붙이고 Next Stage 에 보고 싶은 스테이지를 넣는다.
    /// LSO_StageProgression 은 스스로 찾으므로 연결할 것이 없다.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class LSO_StageDevKey : MonoBehaviour
    {
        [Header("무엇을 다음으로")]
        [Tooltip("다음에 나올 스테이지. 황소왕을 보려면 그 스테이지 에셋을 넣는다.\n" +
                 "\n" +
                 "LSO_StageProgression 의 Chapters 안에 들어 있는 것이어야 한다.\n" +
                 "목록에 없으면 누르는 순간 경고가 뜬다.")]
        [SerializeField] private LDY_StageSO nextStage;

        [Header("키")]
        [Tooltip("누를 키. None 으로 두면 키로는 안 되고 인스펙터 우클릭으로만 된다.\n" +
                 "\n" +
                 "에디터에서는 F5·F7·F8·F9 를 LDY_SaveDebugHotkeys 가,\n" +
                 "F10·F11 을 LDY_BoardFlipDirector 가 쓴다.\n" +
                 "그 둘이 같은 씬에 있으면 F7 하나로 둘 다 도니 그때는 키를 바꿀 것.\n" +
                 "빌드에서는 그 스크립트들이 빠지므로 겹치지 않는다.")]
        [SerializeField] private Key jumpKey = Key.F7;

        [Tooltip("Shift 를 같이 누르고 있어야 먹는다.\n" +
                 "\n" +
                 "이 키는 배포 빌드에도 들어간다. 한 키로 두면 테스터가 실수로 눌러\n" +
                 "진행이 건너뛰어지고, 본인도 뭘 눌렀는지 모른 채 넘어간다.\n" +
                 "끄면 F7 만으로 동작한다.")]
        [SerializeField] private bool requireShift = true;

        [Tooltip("켜면 눌렀을 때 어디로 잡혔는지 콘솔에 찍는다.")]
        [SerializeField] private bool logSteps = true;

        private void Update()
        {
            if (jumpKey == Key.None || Keyboard.current == null) return;
            if (!Keyboard.current[jumpKey].wasPressedThisFrame) return;

            // 왼쪽·오른쪽 어느 Shift 든 받는다. shiftKey 가 둘을 묶어준다.
            //
            // isPressed 로 보는 것은 "누르고 있는 중"이면 되기 때문이다.
            // wasPressedThisFrame 으로 보면 두 키를 같은 프레임에 눌러야 해서 거의 안 먹는다.
            if (requireShift && !Keyboard.current.shiftKey.isPressed) return;

            Jump();
        }

        /// <summary>인스펙터 우클릭으로도 부를 수 있게 열어둔다. 키가 겹칠 때 쓴다.</summary>
        [ContextMenu("다음 스테이지로 잡기")]
        public void Jump()
        {
            if (!Application.isPlaying)
            {
                Debug.LogWarning($"{name}: 플레이 중에만 됩니다.", this);
                return;
            }

            if (!LSO_StageProgression.HasInstance)
            {
                Debug.LogWarning(
                    $"{name}: LSO_StageProgression 이 씬에 없어 다음 스테이지를 잡지 못합니다.", this);
                return;
            }

            LSO_StageProgression progression = LSO_StageProgression.Instance;

            if (!progression.TrySetNext(nextStage)) return;

            if (!logSteps) return;

            // 지금 어디에 섰는지까지 찍는다. "잡았다"만 보면 제대로 됐는지 알 수 없다.
            Debug.Log(
                $"[{name}] 다음 스테이지를 '{nextStage.name}' 로 잡았다. " +
                $"지금 자리는 {progression.ChapterNumber}-{progression.StageNumber} " +
                $"({(progression.Current != null ? progression.Current.stageName : "없음")}) — " +
                "이 판을 깨면 그리로 간다.", this);
        }
    }
}
