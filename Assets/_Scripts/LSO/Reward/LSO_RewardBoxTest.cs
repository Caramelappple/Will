#if UNITY_EDITOR
using System.Collections;
using UnityEngine;

namespace _Scripts.LSO.Reward
{
    /// <summary>
    /// 맵도 전투도 거치지 않고 상자만 돌려보는 도구.
    ///
    /// 런타임 코드와 한 파일에 두면 본체를 읽을 때마다 테스트용 필드를 지나쳐야 한다.
    /// partial로 갈라두면 인스펙터에는 그대로 붙어 나오면서 파일은 따로 본다.
    ///
    /// Editor 폴더에 두지 않는다. 거기 두면 런타임 클래스의 partial이 될 수 없다.
    /// 대신 파일 전체를 #if UNITY_EDITOR로 감싸 빌드에서 빠지게 한다.
    /// </summary>
    public partial class LSO_RewardBox
    {
        [Header("테스트용")]
        [Tooltip("컨텍스트 메뉴로 Begin을 부를 때 쓸 챕터·스테이지. 빌드에는 들어가지 않는다.")]
        [SerializeField] private int testChapter = 1;

        [SerializeField] private int testStage = 1;

        [Tooltip("켜면 플레이를 누르는 순간 스스로 시작한다.\n" +
                 "맵도 전투도 거치지 않으므로 상자 연출만 볼 때 쓴다.")]
        [SerializeField] private bool testAutoBegin;

        private void Start()
        {
            if (!testAutoBegin) return;

            StartCoroutine(Co_TestAuto());
        }

        /// <summary>
        /// 클릭 없이 시작한다. 뚜껑과 카드는 Begin이 알아서 이어간다.
        ///
        /// 한 프레임 기다리는 이유: 다른 컴포넌트의 Start가 끝나야
        /// LSO_ItemLibraryManager 같은 것들이 자리를 잡는다.
        /// </summary>
        private IEnumerator Co_TestAuto()
        {
            yield return null;

            TestBegin();
        }

        /// <summary>
        /// 맵을 거치지 않고 보상을 시작한다. 컴포넌트 톱니바퀴에서 부른다.
        ///
        /// 플레이 중에만 쓸 것. 정지 상태에서는 풀이 아직 없어 아무 일도 일어나지 않는다.
        /// </summary>
        [ContextMenu("테스트: 보상 시작")]
        private void TestBegin()
        {
            if (!Application.isPlaying)
            {
                Debug.LogWarning($"{name}: 플레이 중에만 됩니다.", this);
                return;
            }

            Debug.Log($"{name}: 테스트 시작 (챕터 {testChapter} 스테이지 {testStage})", this);

            Begin(testChapter, testStage);
        }

        /// <summary>
        /// 인스펙터에서 값을 만지면 꺼내둔 카드를 그 자리에서 다시 늘어놓는다.
        ///
        /// 플레이 중에만 한다. 정지 상태에서는 꺼내둔 카드가 없다.
        /// </summary>
        private void OnValidate()
        {
            if (!Application.isPlaying) return;

            Relayout();
        }

        /// <summary>지금 어느 단계인지 콘솔에 찍는다. 눌러도 반응이 없을 때 본다.</summary>
        [ContextMenu("테스트: 지금 상태")]
        private void TestDumpState()
        {
            Debug.Log(
                $"{name}\n" +
                $"  단계    : {_phase}\n" +
                $"  바쁨    : {IsBusy}\n" +
                $"  카드    : {_cards.Count}장\n" +
                $"  풀      : {(_pool == null ? "없음" : _pool.Describe())}\n" +
                $"  뚜껑    : {(lid == null ? "없음" : lid.IsOpened ? "열림" : "닫힘")}\n" +
                $"  뜸      : Lift {pickLiftDuration}s / Pick {pickHold}s / Claim {claimHold}s",
                this);
        }
    }
}
#endif
