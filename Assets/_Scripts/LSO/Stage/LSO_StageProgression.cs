using System;
using System.Collections.Generic;
using _Scripts.LDY.Stage;
using _Scripts.LSO.CoreLib;
using UnityEngine;

namespace _Scripts.LSO.Stage
{
    /// <summary>
    /// 지금 몇 챕터 몇 번째인지. 그리고 다음이 무엇인지.
    ///
    /// 고르는 것이 없어졌으므로 진행은 한 방향뿐이다.
    /// 클리어하면 한 칸 내려가고, 챕터 끝이면 다음 챕터의 첫 칸으로 넘어간다.
    ///
    /// 챕터·스테이지를 아는 곳은 여기 하나뿐이다.
    /// 예전에는 맵 매니저도 같은 값을 들고 있었지만 노드와 함께 걷어냈다.
    ///
    /// 씬 배선: 씬 아무 곳에나 하나. 씬을 넘어가도 살아남는다.
    /// </summary>
    [DisallowMultipleComponent]
    public class LSO_StageProgression : MonoSingleton<LSO_StageProgression>
    {
        [Header("챕터")]
        [Tooltip("순서대로. 위에서부터 1챕터다.\n" +
                 "챕터 번호는 에셋이 들고 있지만, 진행 순서는 이 목록이 정한다.")]
        [SerializeField] private List<LSO_ChapterSO> chapters = new List<LSO_ChapterSO>();

        [Header("시작 지점")]
        [Tooltip("몇 챕터부터 시작할지. 0부터 센다. 테스트할 때 건너뛰는 용도다.")]
        [SerializeField, Min(0)] private int startChapterIndex;

        [Tooltip("그 챕터의 몇 번째부터 시작할지. 0부터 센다.")]
        [SerializeField, Min(0)] private int startStageIndex;

        [Header("진단")]
        [SerializeField] private bool logSteps;

        private int _chapterIndex;
        private int _stageIndex;

        /// <summary>지금 챕터. 없으면 null.</summary>
        public LSO_ChapterSO Chapter =>
            _chapterIndex >= 0 && _chapterIndex < chapters.Count ? chapters[_chapterIndex] : null;

        /// <summary>지금 스테이지. 없으면 null.</summary>
        public LDY_StageSO Current => Chapter != null ? Chapter.At(_stageIndex) : null;

        /// <summary>지금 자리가 보스인지. 챕터 목록의 마지막 칸이다.</summary>
        public bool IsBoss => Chapter != null && Chapter.IsBossAt(_stageIndex);

        /// <summary>화면에 띄울 챕터 번호. 1부터.</summary>
        public int ChapterNumber => Chapter != null ? Chapter.chapter : _chapterIndex + 1;

        /// <summary>화면에 띄울 스테이지 번호. 1부터.</summary>
        public int StageNumber => _stageIndex + 1;

        /// <summary>지금 자리. 세이브가 이 값을 담는다. 0부터.</summary>
        public int ChapterIndex => _chapterIndex;

        /// <summary>지금 자리. 세이브가 이 값을 담는다. 0부터.</summary>
        public int StageIndex => _stageIndex;

        /// <summary>런을 다 돌았는지. 마지막 챕터의 마지막을 깬 뒤다.</summary>
        public bool IsRunFinished => Chapter == null;

        /// <summary>한 칸 넘어갔을 때. 인자는 새로 시작할 스테이지다.</summary>
        public event Action<LDY_StageSO> Advanced;

        /// <summary>챕터가 바뀌었을 때. 지역 이름을 띄우는 쪽이 듣는다.</summary>
        public event Action<LSO_ChapterSO> ChapterChanged;

        protected override void Awake()
        {
            base.Awake();

            if (Instance != this) return;

            DontDestroyOnLoad(gameObject);

            _chapterIndex = startChapterIndex;
            _stageIndex = startStageIndex;

            WarnIfEmpty();
        }

        /// <summary>
        /// 다음 칸으로 넘어간다. 클리어했을 때 부른다.
        ///
        /// 챕터의 끝이면 다음 챕터의 첫 칸으로 간다.
        /// 마지막 챕터까지 끝나면 Current가 null이 되고 IsRunFinished가 true가 된다 —
        /// 그때 무엇을 보여줄지는 부르는 쪽이 정한다.
        /// </summary>
        public LDY_StageSO Advance()
        {
            if (Chapter == null)
            {
                Log("이미 런이 끝나 더 갈 곳이 없다");
                return null;
            }

            bool wasLast = _stageIndex >= Chapter.Count - 1;

            if (wasLast)
            {
                _chapterIndex++;
                _stageIndex = 0;

                Log($"챕터 넘김 → {ChapterNumber}");

                // 다음 챕터가 없으면 Chapter가 null이라 여기서 알린다.
                if (Chapter != null) ChapterChanged?.Invoke(Chapter);
            }
            else
            {
                _stageIndex++;
            }

            LDY_StageSO next = Current;

            Log(next != null
                ? $"다음 → {ChapterNumber}-{StageNumber} ({next.stageName})"
                : "런 종료");

            Advanced?.Invoke(next);

            return next;
        }

        /// <summary>
        /// 처음으로 되돌린다. 새 게임을 시작할 때 부른다.
        /// </summary>
        public void Restart()
        {
            _chapterIndex = startChapterIndex;
            _stageIndex = startStageIndex;

            Log($"처음으로 → {ChapterNumber}-{StageNumber}");
        }

        /// <summary>
        /// 자리를 직접 정한다. 세이브를 되돌릴 때 쓴다.
        ///
        /// 번호가 아니라 인덱스다. 화면의 "1-1"은 (0, 0)이다.
        /// </summary>
        public void SetPosition(int chapterIndex, int stageIndex)
        {
            _chapterIndex = Mathf.Max(0, chapterIndex);
            _stageIndex = Mathf.Max(0, stageIndex);

            Log($"자리 지정 → {ChapterNumber}-{StageNumber}");
        }

        private void WarnIfEmpty()
        {
            if (chapters.Count == 0)
            {
                Debug.LogWarning($"{name}: 챕터가 하나도 없어 진행할 수 없습니다.", this);
                return;
            }

            for (int i = 0; i < chapters.Count; i++)
            {
                if (chapters[i] == null)
                    Debug.LogWarning($"{name}: 챕터 목록 {i}번이 비어 있습니다.", this);
                else if (chapters[i].Count == 0)
                    Debug.LogWarning($"{name}: '{chapters[i].name}'에 스테이지가 없습니다.", chapters[i]);
            }

            if (Current == null)
            {
                Debug.LogWarning(
                    $"{name}: 시작 지점({startChapterIndex}, {startStageIndex})에 스테이지가 없습니다.", this);
            }
        }

        private void Log(string message)
        {
            if (logSteps) Debug.Log($"[{name}] {message}", this);
        }

#if UNITY_EDITOR
        [ContextMenu("테스트: 다음 칸으로")]
        private void TestAdvance()
        {
            if (!Application.isPlaying)
            {
                Debug.LogWarning($"{name}: 플레이 중에만 됩니다.", this);
                return;
            }

            Advance();
        }

        [ContextMenu("테스트: 지금 자리")]
        private void TestDump()
        {
            Debug.Log(
                $"{name}\n" +
                $"  자리   : {ChapterNumber}-{StageNumber}\n" +
                $"  지역   : {(Chapter != null ? Chapter.regionName : "-")}\n" +
                $"  스테이지: {(Current != null ? Current.stageName : "없음")}\n" +
                $"  보스   : {IsBoss}\n" +
                $"  런 종료 : {IsRunFinished}",
                this);
        }
#endif
    }
}
