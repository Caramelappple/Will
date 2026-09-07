#if UNITY_EDITOR
using System.IO;
using System.Text;
using _Scripts.LSO.Stage;
using UnityEngine;

namespace _Scripts.LDY.Save.Debugging
{
    /// <summary>
    /// ⚠ 임시 디버그 코드다. 게임 로직이 여기에 기대게 만들지 말 것.
    ///
    /// ── 제거 예정 ─────────────────────────────────────────────
    /// 정식 자동저장 훅이 전투 승리 지점에 연결되면 이 컴포넌트와 씬 배치를 제거할 것.
    /// 그 전까지는 검증용으로 유지한다.
    ///
    /// 지울 때 함께 치울 것:
    ///   - 이 파일 (LDY_SaveDebugHotkeys.cs, .meta 포함)
    ///   - LDY_TestScene에 붙여둔 오브젝트 (현재 유일한 배치처)
    /// ─────────────────────────────────────────────────────────
    ///
    /// SaveRun()을 부르는 곳이 아직 없어서, 실제 게임 상태로 왕복이 되는지 확인할 방법이 없다.
    /// 그 자리를 잠시 메우는 손잡이다. 아무 씬의 빈 오브젝트에 붙여서 쓴다.
    ///
    /// 파일 전체가 UNITY_EDITOR로 묶여 있어 빌드에는 들어가지 않는다.
    /// 그래서 이 컴포넌트를 붙여둔 씬은 빌드에서 "missing script" 경고를 낸다.
    /// </summary>
    [DisallowMultipleComponent]
    public class LDY_SaveDebugHotkeys : MonoBehaviour
    {
        [Header("단축키")]
        [SerializeField] private KeyCode saveKey = KeyCode.F5;
        [SerializeField] private KeyCode loadKey = KeyCode.F9;

        [Tooltip("새 런 진입 플래그를 켠 뒤 맵 진입 처리를 다시 태운다. 건너뛰기 경로를 보기 위한 것.")]
        [SerializeField] private KeyCode newRunKey = KeyCode.F8;

        [Tooltip("플래그 없이 맵 진입 처리만 다시 태운다. 이어하기 경로를 보기 위한 것.")]
        [SerializeField] private KeyCode restoreKey = KeyCode.F7;

        [Header("화면 표시")]
        [Tooltip("화면 구석에 뜬 요약이 남아 있는 시간(초).")]
        [SerializeField] private float toastSeconds = 5f;

        private string _toast;
        private float _toastUntil;
        private bool _toastIsError;

        /// <summary>
        /// 저장소가 키에 .json을 붙여 persistentDataPath 아래에 두는 규약을 그대로 따라 짚는다.
        /// 디버그 표시용이므로 규약이 바뀌면 여기도 같이 고칠 것.
        /// </summary>
        private static string RunFilePath =>
            Path.Combine(Application.persistentDataPath, LDY_SaveService.RunKey + ".json");

        private void Update()
        {
            if (Input.GetKeyDown(saveKey)) DoSave();
            else if (Input.GetKeyDown(loadKey)) DoLoad();
            else if (Input.GetKeyDown(newRunKey)) DoMarkNewRunAndRestore();
            else if (Input.GetKeyDown(restoreKey)) DoRestore();
        }

        /// <summary>
        /// 새 런 진입 신호를 켠 뒤 맵의 진입 처리를 그 자리에서 다시 태운다.
        /// 맵이 세이브를 건너뛰는지 보는 용도다.
        /// </summary>
        private void DoMarkNewRunAndRestore()
        {
            LDY_RunEntryState.IsStartingNewRun = true;

            Debug.Log("[LDY_SaveDebugHotkeys] IsStartingNewRun = true 로 설정함");

            InvokeRestore($"{newRunKey} (새 런 플래그 켬)");
        }

        /// <summary>플래그를 켜지 않고 맵의 진입 처리만 다시 태운다. 이어하기 경로를 보는 용도다.</summary>
        private void DoRestore()
        {
            InvokeRestore($"{restoreKey} (플래그 없이)");
        }

        /// <summary>
        /// 세이브 진입 처리를 다시 태우고 어느 갈래로 갔는지 남긴다.
        ///
        /// Start는 세션당 한 번뿐이라 그대로 두면 분기를 다시 태워볼 수 없다.
        /// 같은 플레이 안에서 되풀이해 부를 수 있게 여기서 직접 호출한다.
        /// </summary>
        private void InvokeRestore(string label)
        {
            // 맵 매니저가 하던 진입 처리를 여기서 그대로 편다.
            // 조건이 바뀌면 여기도 같이 고칠 것.
            bool wasNewRun = LDY_RunEntryState.IsStartingNewRun;
            bool hadRun = LDY_SaveService.Instance.HasRun;

            string before = DescribeGameState();

            if (!LDY_RunEntryState.Consume() && hadRun)
                LDY_SaveService.Instance.LoadRun();

            string branch =
                wasNewRun ? "새 런이라 LoadRun 을 건너뜀" :
                hadRun ? "이어할 런이 있어 LoadRun 호출" :
                "세이브가 없어 아무것도 하지 않음";

            Debug.Log(
                $"[SaveDebug] {label} → {branch}\n" +
                $"  이전: {before}\n" +
                $"  이후: {DescribeGameState()}");

            ShowToast($"{label} → {branch}", false);
        }

        private void DoSave()
        {
            var file = new FileInfo(RunFilePath);
            bool existedBefore = file.Exists;
            long sizeBefore = existedBefore ? file.Length : -1;
            System.DateTime timeBefore = existedBefore ? file.LastWriteTimeUtc : default;

            LDY_SaveService.Instance.SaveRun();

            file.Refresh();

            // SaveRun은 값을 돌려주지 않는다. 파일이 실제로 갱신됐는지로 성공을 판단한다.
            bool wrote = file.Exists && (!existedBefore
                                         || file.LastWriteTimeUtc != timeBefore
                                         || file.Length != sizeBefore);

            string state = DescribeGameState();

            if (wrote)
            {
                Debug.Log(
                    $"[SaveDebug] 저장 성공\n" +
                    $"  경로: {RunFilePath}\n" +
                    $"  크기: {file.Length} bytes ({(existedBefore ? "갱신" : "새로 만듦")})\n" +
                    $"  내용: {state}");

                ShowToast($"저장 완료 — {state}", false);
                return;
            }

            Debug.LogError(
                $"[SaveDebug] 저장 실패 — 파일이 갱신되지 않았습니다.\n" +
                $"  경로: {RunFilePath}\n" +
                $"  파일 존재: {file.Exists}\n" +
                "  위쪽 LDY_FileSaveRepository 로그를 확인하세요.");

            ShowToast("저장 실패 — 콘솔 확인", true);
        }

        private void DoLoad()
        {
            if (!File.Exists(RunFilePath))
            {
                Debug.LogWarning($"[SaveDebug] 불러올 파일이 없습니다: {RunFilePath}");
                ShowToast("불러오기 실패 — run.json 없음", true);
                return;
            }

            string before = DescribeGameState();

            LDY_SaveService.Instance.LoadRun();

            string after = DescribeGameState();

            Debug.Log(
                $"[SaveDebug] 불러오기 완료\n" +
                $"  경로: {RunFilePath}\n" +
                $"  이전: {before}\n" +
                $"  이후: {after}");

            ShowToast($"불러오기 완료 — {after}", false);
        }

        /// <summary>
        /// 세이브에 담기는 값들을 그 원본에서 직접 읽어 요약한다.
        /// 저장 코드가 읽는 곳과 같은 자리를 보므로, 저장된 내용과 어긋나지 않는다.
        /// </summary>
        private static string DescribeGameState()
        {
            // 블록 주석이 겹쳐 있어 본문 전체가 주석 처리돼 있었다.
            // 에러도 경고도 없이 늘 빈 문자열이 나와, 어느 갈래로 갔는지 볼 수 없었다.
            var text = new StringBuilder();

            if (LSO_StageProgression.HasInstance)
            {
                LSO_StageProgression p = LSO_StageProgression.Instance;

                text.Append($"{p.ChapterNumber}-{p.StageNumber}");
                text.Append($" / {(p.Current != null ? p.Current.stageName : "스테이지 없음")}");
                text.Append(p.IsBoss ? " / 보스" : string.Empty);
            }
            else
            {
                text.Append("진행 (매니저 없음)");
            }

            // 덱과 해금 목록은 누가 들고 있을지가 합의되기 전까지 빼둔다.

            return text.ToString();
        }

        private void ShowToast(string message, bool isError)
        {
            _toast = message;
            _toastIsError = isError;
            _toastUntil = Time.unscaledTime + toastSeconds;
        }

        private void OnGUI()
        {
            if (string.IsNullOrEmpty(_toast) || Time.unscaledTime > _toastUntil) return;

            var style = new GUIStyle(GUI.skin.box)
            {
                fontSize = 14,
                alignment = TextAnchor.MiddleLeft,
                wordWrap = true,
            };
            style.normal.textColor = _toastIsError ? Color.red : Color.white;

            const float margin = 10f;
            float width = Mathf.Min(Screen.width - margin * 2f, 720f);

            GUI.Box(new Rect(margin, margin, width, 52f),
                $"[{saveKey}=저장 {loadKey}=불러오기 {restoreKey}=진입처리 {newRunKey}=새 런+진입처리]  {_toast}", style);
        }
    }
}
#endif
