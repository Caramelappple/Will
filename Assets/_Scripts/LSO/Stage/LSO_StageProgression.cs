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
    /// 씬 배선: 전투 씬에 하나. 런 진행은 세이브 데이터로 복원하며 씬 오브젝트 자체는 남기지 않는다.
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

        /// <summary>
        /// 전달받은 스테이지가 속한 챕터를 찾는다.
        ///
        /// 다음 스테이지를 세우는 연출은 진행도를 한 칸 넘긴 뒤에 시작된다.
        /// 그 순간에도 화면 연출은 이벤트로 전달받은 스테이지를 기준으로 해야
        /// 하므로, 현재 커서에 기대지 않고 목록에서 소속을 직접 찾는다.
        /// </summary>
        public LSO_ChapterSO FindChapter(LDY_StageSO stage)
        {
            return TryFindStageContext(stage, out LSO_ChapterSO chapter, out _) ? chapter : null;
        }

        /// <summary>전달받은 스테이지가 그 챕터의 마지막 칸인지 반환한다.</summary>
        public bool IsBossStage(LDY_StageSO stage)
        {
            return TryFindStageContext(stage, out LSO_ChapterSO chapter, out int stageIndex) &&
                   chapter.IsBossAt(stageIndex);
        }

        /// <summary>전달받은 스테이지의 화면용 챕터 번호를 반환한다.</summary>
        public int GetChapterNumber(LDY_StageSO stage)
        {
            LSO_ChapterSO chapter = FindChapter(stage);
            return chapter != null ? chapter.chapter : 0;
        }

        /// <summary>전달받은 스테이지의 화면용 순번을 반환한다.</summary>
        public int GetStageNumber(LDY_StageSO stage)
        {
            return TryFindStageContext(stage, out _, out int stageIndex) ? stageIndex + 1 : 0;
        }

        private bool TryFindStageContext(
            LDY_StageSO stage, out LSO_ChapterSO chapter, out int stageIndex)
        {
            chapter = null;
            stageIndex = -1;
            if (stage == null) return false;

            // 챕터 사이에서 같은 StageSO를 후보로 재사용할 수 있다.
            // 진행 중인 칸은 목록을 처음부터 훑지 않고 현재 커서를 우선해야
            // 튜토리얼의 후보가 정식 Chapter1의 지역명으로 잘못 해석되지 않는다.
            if (Chapter != null && Current == stage)
            {
                chapter = Chapter;
                stageIndex = _stageIndex;
                return true;
            }

            for (int chapterIndex = 0; chapterIndex < chapters.Count; chapterIndex++)
            {
                LSO_ChapterSO candidate = chapters[chapterIndex];
                if (candidate == null) continue;

                for (int candidateStageIndex = 0; candidateStageIndex < candidate.Count; candidateStageIndex++)
                {
                    if (candidate.At(candidateStageIndex) != stage) continue;

                    chapter = candidate;
                    stageIndex = candidateStageIndex;
                    return true;
                }
            }

            return false;
        }

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

        /// <summary>
        /// **다음에 갈 칸**을 그 스테이지로 맞춘다. 지금 칸은 그대로 둔다.
        ///
        /// 개발자 키가 쓴다. 지금 판을 깨면 그 스테이지가 나온다.
        ///
        /// ── 왜 여기 있나 ─────────────────────────────────────────
        /// 챕터 목록을 아는 곳은 이 클래스 하나다. 찾는 일을 밖에 두면
        /// chapters 를 밖으로 열어야 하고, 그러면 "몇 챕터 몇 번째인가"를
        /// 아는 곳이 둘이 된다.
        /// ─────────────────────────────────────────────────────────
        ///
        /// 바로 그 칸으로 보내는 것이 아니라 <b>한 칸 앞에 세운다.</b>
        /// Advance 를 거치지 않고 자리만 바꾸면 클리어 처리와 연출이 건너뛰어져,
        /// 실제 플레이와 다른 경로로 그 스테이지에 도착한다.
        /// </summary>
        /// <returns>맞췄으면 참. 목록에 없거나 앞에 설 자리가 없으면 거짓.</returns>
        public bool TrySetNext(LDY_StageSO stage)
        {
            if (stage == null)
            {
                Debug.LogWarning($"{name}: 다음으로 잡을 스테이지가 비어 있습니다.", this);
                return false;
            }

            if (!TryFind(stage, out int chapterIndex, out int stageIndex))
            {
                Debug.LogWarning(
                    $"{name}: '{stage.name}' 을 챕터 목록에서 찾지 못했습니다. " +
                    "Chapters 에 그 스테이지가 들어 있는지 확인하세요.", this);
                return false;
            }

            // 그 칸의 바로 앞에 선다. 깨서 Advance 하면 그 칸이 나온다.
            if (stageIndex > 0)
            {
                SetPosition(chapterIndex, stageIndex - 1);
                return true;
            }

            // 챕터의 첫 칸이면 앞 챕터의 마지막에 선다.
            // Advance 가 챕터를 넘기면서 그 칸으로 간다.
            for (int prev = chapterIndex - 1; prev >= 0; prev--)
            {
                if (chapters[prev] == null || chapters[prev].Count == 0) continue;

                SetPosition(prev, chapters[prev].Count - 1);
                return true;
            }

            Debug.LogWarning(
                $"{name}: '{stage.name}' 이 맨 첫 칸이라 그 앞에 설 자리가 없습니다.", this);

            return false;
        }

        /// <summary>
        /// 그 스테이지가 몇 챕터 몇 번째인지 찾는다.
        ///
        /// 적어둔 칸(stages)과 실제로 고른 칸(At) 둘 다 본다.
        /// 갈래가 걸린 자리는 둘이 다를 수 있어서, 한쪽만 보면 못 찾는다.
        /// </summary>
        private bool TryFind(LDY_StageSO stage, out int chapterIndex, out int stageIndex)
        {
            for (int c = 0; c < chapters.Count; c++)
            {
                LSO_ChapterSO chapter = chapters[c];

                if (chapter == null) continue;

                for (int s = 0; s < chapter.Count; s++)
                {
                    bool authored = chapter.stages != null &&
                                    s < chapter.stages.Count &&
                                    chapter.stages[s] == stage;

                    if (!authored && chapter.At(s) != stage) continue;

                    chapterIndex = c;
                    stageIndex = s;
                    return true;
                }
            }

            chapterIndex = -1;
            stageIndex = -1;
            return false;
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
