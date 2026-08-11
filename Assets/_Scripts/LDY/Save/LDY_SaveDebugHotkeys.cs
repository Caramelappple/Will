#if UNITY_EDITOR
using System.IO;
using System.Text;
using UnityEngine;

namespace _Scripts.LDY.Save.Debugging
{
    /// <summary>
    /// ⚠ 임시 디버그 코드다. 정식 자동저장 훅(전투 승리 + 보상 선택 완료 지점)이 생기면
    /// 이 파일은 통째로 지운다. 게임 로직이 여기에 기대게 만들지 말 것.
    ///
    /// SaveRun()을 부르는 곳이 아직 없어서, 실제 게임 상태로 왕복이 되는지 확인할 방법이 없다.
    /// 그 자리를 잠시 메우는 손잡이다. 아무 씬의 빈 오브젝트에 붙여서 쓴다.
    ///
    /// 파일 전체가 UNITY_EDITOR로 묶여 있어 빌드에는 들어가지 않는다.
    /// 그래서 이 컴포넌트를 붙여둔 씬은 빌드에서 "missing script" 경고를 낸다.
    /// 지울 때 씬에서 오브젝트도 함께 치울 것.
    /// </summary>
    [DisallowMultipleComponent]
    public class LDY_SaveDebugHotkeys : MonoBehaviour
    {
        [Header("단축키")]
        [SerializeField] private KeyCode saveKey = KeyCode.F5;
        [SerializeField] private KeyCode loadKey = KeyCode.F9;

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
            var text = new StringBuilder();

            KTH_DeckDataPersistent deck = KTH_DeckDataPersistent.Instance;
            text.Append(deck != null ? $"덱 {deck.savedInventory.Count}장" : "덱 (매니저 없음)");

            LDY_MapManager map = LDY_MapManager.Instance;
            if (map != null)
            {
                int cleared = 0;
                foreach (LDY_MapNode node in map.Nodes)
                    if (node != null && node.isCleared) cleared++;

                text.Append($" / 클리어 노드 {cleared}개");
                text.Append($" / 현재 노드 {map.CurrentNodeIndex}");
                text.Append($" / {map.CurrentChapter}-{map.CurrentStage}");
            }
            else
            {
                text.Append(" / 맵 (매니저 없음)");
            }

            KTH_Reward reward = KTH_Reward.Instance;
            text.Append(reward != null
                ? $" / 해금 기물 {reward.Unlocks.Pieces.Count}·유언 {reward.Unlocks.Wills.Count}"
                : " / 해금 (매니저 없음)");

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

            GUI.Box(new Rect(margin, margin, width, 52f), $"[{saveKey}=저장 {loadKey}=불러오기]  {_toast}", style);
        }
    }
}
#endif
