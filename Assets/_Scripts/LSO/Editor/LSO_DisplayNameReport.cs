#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using _Scripts.LDY;
using _Scripts.LSO.Ability;
using _Scripts.LSO.Ability.Catalog;
using _Scripts.LSO.UI.Text;
using _Scripts.LSO.Will;
using UnityEditor;
using UnityEngine;

namespace _Scripts.LSO.Editor
{
    /// <summary>
    /// 영문 ↔ 한글 대응표를 뽑는다. 사람이 보라고 만드는 것이고 게임은 이것을 읽지 않는다.
    ///
    /// 문구는 세 곳에 나뉘어 산다(LSO_DisplayNames 주석 참고). 나뉘어 있는 편이
    /// 고칠 때 편하지만, 전체를 한눈에 보려면 어딘가에서 모아줘야 한다.
    /// 그 "모아 보기"가 이 도구다. 여기서 새 문구를 만들지 않고 있는 것만 읽는다.
    ///
    /// 쓰는 법: 메뉴 LSO &gt; 대응표 뽑기
    /// 결과는 Assets 옆(프로젝트 루트)의 대응표.md 로 떨어진다.
    /// 기획서와 코드가 어긋났는지 볼 때, 팀에 표기를 공유할 때 쓴다.
    /// </summary>
    public static class LSO_DisplayNameReport
    {
        private const string OutputFileName = "대응표.md";

        [MenuItem("LSO/대응표 뽑기")]
        private static void Export()
        {
            string markdown = Build();

            // Application.dataPath는 Assets 폴더다. 그 한 칸 위가 프로젝트 루트다.
            string root = Directory.GetParent(Application.dataPath)?.FullName ?? Application.dataPath;
            string path = Path.Combine(root, OutputFileName);

            File.WriteAllText(path, markdown, new UTF8Encoding(true));

            Debug.Log($"대응표를 뽑았습니다: {path}");

            EditorUtility.RevealInFinder(path);
        }

        [MenuItem("LSO/대응표 콘솔에 찍기")]
        private static void Dump()
        {
            Debug.Log(Build());
        }

        private static string Build()
        {
            var text = new StringBuilder();

            text.AppendLine("# 영문 ↔ 한글 대응표");
            text.AppendLine();
            text.AppendLine($"뽑은 때: {DateTime.Now:yyyy-MM-dd HH:mm}");
            text.AppendLine();
            text.AppendLine("이 파일은 `LSO > 대응표 뽑기`가 만든다. 손으로 고치지 말 것 —");
            text.AppendLine("다시 뽑으면 덮어쓴다. 문구를 바꾸려면 아래 각 절이 가리키는 자리를 고친다.");
            text.AppendLine();

            AppendAbilities(text);
            AppendWills(text);
            AppendRanges(text);

            return text.ToString();
        }

        private static void AppendAbilities(StringBuilder text)
        {
            text.AppendLine("## 특성");
            text.AppendLine();
            text.AppendLine("문구가 사는 곳: `Assets/Resources/LSO_AbilityCatalog.asset`");
            text.AppendLine();
            text.AppendLine("| 영문 (enum) | 한글 | 설명 |");
            text.AppendLine("|---|---|---|");

            var missing = new List<string>();

            foreach (LSO_AbilityType type in Enum.GetValues(typeof(LSO_AbilityType)))
            {
                if (type == LSO_AbilityType.None) continue;

                string korean = LSO_AbilityText.NameOf(type);
                string description = LSO_AbilityText.DescriptionOf(type);

                // 한글을 못 찾으면 창구가 enum 이름을 그대로 돌려준다. 그게 빠졌다는 신호다.
                if (korean == type.ToString()) missing.Add(type.ToString());

                text.AppendLine($"| {type} | {korean} | {Cell(description)} |");
            }

            AppendMissing(text, missing, "특성 사전에 이름이 없는 것");
        }

        private static void AppendWills(StringBuilder text)
        {
            text.AppendLine("## 유언");
            text.AppendLine();
            text.AppendLine("문구가 사는 곳: `Assets/_SO/DLJ/WillData/*.asset` 각각의 Display Name");
            text.AppendLine();
            text.AppendLine("| 영문 (enum) | 한글 | 설명 |");
            text.AppendLine("|---|---|---|");

            var missing = new List<string>();

            foreach (LSO_WillType type in Enum.GetValues(typeof(LSO_WillType)))
            {
                if (type == LSO_WillType.None) continue;

                string korean = LSO_WillText.NameOf(type);
                string description = LSO_WillText.DescriptionOf(type);

                if (korean == type.ToString()) missing.Add(type.ToString());

                text.AppendLine($"| {type} | {korean} | {Cell(description)} |");
            }

            AppendMissing(text, missing, "유언 에셋에 이름이 없는 것");
        }

        private static void AppendRanges(StringBuilder text)
        {
            text.AppendLine("## 사거리");
            text.AppendLine();
            text.AppendLine("문구가 사는 곳: `LSO_DisplayNames.Of(LDY_RangeType)` — 코드 안의 switch");
            text.AppendLine();
            text.AppendLine("| 영문 (enum) | 한글 |");
            text.AppendLine("|---|---|");

            foreach (LDY_RangeType type in Enum.GetValues(typeof(LDY_RangeType)))
                text.AppendLine($"| {type} | {LSO_DisplayNames.Of(type)} |");

            text.AppendLine();
        }

        private static void AppendMissing(StringBuilder text, List<string> missing, string label)
        {
            text.AppendLine();

            if (missing.Count > 0)
                text.AppendLine($"> **{label} ({missing.Count}개):** {string.Join(", ", missing)}");

            text.AppendLine();
        }

        /// <summary>
        /// 표 한 칸에 넣을 수 있게 다듬는다.
        /// 줄바꿈은 칸을 깨고, 파이프는 칸을 하나 더 만든다.
        /// </summary>
        private static string Cell(string value)
        {
            if (string.IsNullOrWhiteSpace(value)) return "—";

            return value
                .Replace("\r", string.Empty)
                .Replace("\n", " ")
                .Replace("|", "\\|");
        }
    }
}
#endif
