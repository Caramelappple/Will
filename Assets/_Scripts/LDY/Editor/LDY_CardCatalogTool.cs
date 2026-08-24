using System.Collections.Generic;
using System.IO;
using System.Linq;
using _Scripts.LDY.Save;
using _Scripts.LSO.Deck.Data;
using UnityEditor;
using UnityEngine;

namespace _Scripts.LDY.EditorTools
{
    /// <summary>
    /// 카드 카탈로그를 만들고 채우는 에디터 도구.
    ///
    /// 카탈로그는 손으로 관리하면 카드가 늘어날 때마다 빠뜨리기 쉽고,
    /// 빠뜨린 카드는 불러오기에서 조용히 사라진다. 그래서 프로젝트를 훑어 자동으로 채운다.
    /// </summary>
    public static class LDY_CardCatalogTool
    {
        /// <summary>Resources.Load가 찾을 수 있는 유일한 경로.</summary>
        private static string ExpectedAssetPath =>
            $"Assets/Resources/{LDY_CardCatalogSO.ResourcePath}.asset";

        [MenuItem("LDY/Save/Card Catalog 자동 채우기")]
        public static void FillCatalog()
        {
            LDY_CardCatalogSO catalog = FindOrCreateCatalog();
            if (catalog == null) return;

            LSO_CardSO[] cards = FindAllCards();
            if (cards.Length == 0)
            {
                Debug.LogWarning("[LDY_CardCatalogTool] 프로젝트에서 LSO_CardSO 에셋을 하나도 찾지 못했습니다.");
                return;
            }

            WriteCards(catalog, cards);
            ReportDuplicateIds(cards);

            Debug.Log(
                $"[LDY_CardCatalogTool] 카드 {cards.Length}장을 채웠습니다.\n" +
                $"  경로: {AssetDatabase.GetAssetPath(catalog)}\n" +
                $"  카드: {string.Join(", ", cards.Select(c => c.Id))}", catalog);

            Selection.activeObject = catalog;
        }

        /// <summary>
        /// 카탈로그 에셋을 찾는다. 없으면 Resources 아래에 만들고,
        /// Resources 밖에 있으면 옮길지 물어본다. 그 자리에 두면 런타임에 찾지 못하기 때문이다.
        /// </summary>
        private static LDY_CardCatalogSO FindOrCreateCatalog()
        {
            string[] guids = AssetDatabase.FindAssets($"t:{nameof(LDY_CardCatalogSO)}");

            if (guids.Length == 0)
                return CreateCatalogAtExpectedPath();

            if (guids.Length > 1)
            {
                Debug.LogWarning(
                    $"[LDY_CardCatalogTool] 카탈로그가 {guids.Length}개 있습니다. " +
                    "런타임은 Resources 아래의 것만 읽으므로 나머지는 지우는 편이 좋습니다.");
            }

            // 이미 올바른 자리에 있는 것이 있으면 그걸 쓴다.
            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                if (path == ExpectedAssetPath)
                    return AssetDatabase.LoadAssetAtPath<LDY_CardCatalogSO>(path);
            }

            string currentPath = AssetDatabase.GUIDToAssetPath(guids[0]);
            return MoveCatalogIntoResources(currentPath);
        }

        private static LDY_CardCatalogSO CreateCatalogAtExpectedPath()
        {
            EnsureResourcesFolder();

            var catalog = ScriptableObject.CreateInstance<LDY_CardCatalogSO>();
            AssetDatabase.CreateAsset(catalog, ExpectedAssetPath);
            AssetDatabase.SaveAssets();

            Debug.Log($"[LDY_CardCatalogTool] 카탈로그를 새로 만들었습니다: {ExpectedAssetPath}");

            return catalog;
        }

        private static LDY_CardCatalogSO MoveCatalogIntoResources(string currentPath)
        {
            bool move = EditorUtility.DisplayDialog(
                "카드 카탈로그 위치",
                $"카탈로그가 Resources 폴더 밖에 있습니다.\n\n" +
                $"현재: {currentPath}\n" +
                $"필요: {ExpectedAssetPath}\n\n" +
                "이 자리에 두면 게임 실행 중에 찾지 못해 덱이 복원되지 않습니다.\n옮길까요?",
                "옮긴다", "그대로 둔다");

            if (!move)
            {
                Debug.LogWarning(
                    $"[LDY_CardCatalogTool] 카탈로그가 {currentPath} 에 남았습니다. " +
                    "런타임에서 찾지 못하므로 덱 불러오기가 동작하지 않습니다.");

                return AssetDatabase.LoadAssetAtPath<LDY_CardCatalogSO>(currentPath);
            }

            EnsureResourcesFolder();

            string error = AssetDatabase.MoveAsset(currentPath, ExpectedAssetPath);
            if (!string.IsNullOrEmpty(error))
            {
                Debug.LogError($"[LDY_CardCatalogTool] 옮기지 못했습니다: {error}");
                return AssetDatabase.LoadAssetAtPath<LDY_CardCatalogSO>(currentPath);
            }

            Debug.Log($"[LDY_CardCatalogTool] 카탈로그를 옮겼습니다: {currentPath} → {ExpectedAssetPath}");

            return AssetDatabase.LoadAssetAtPath<LDY_CardCatalogSO>(ExpectedAssetPath);
        }

        private static void EnsureResourcesFolder()
        {
            if (!AssetDatabase.IsValidFolder("Assets/Resources"))
                AssetDatabase.CreateFolder("Assets", "Resources");

            string directory = Path.GetDirectoryName(ExpectedAssetPath);
            if (!string.IsNullOrEmpty(directory) && !AssetDatabase.IsValidFolder(directory))
            {
                Debug.LogError($"[LDY_CardCatalogTool] 폴더가 없습니다: {directory}");
            }
        }

        private static LSO_CardSO[] FindAllCards()
        {
            // 개수를 박아두지 않고 타입으로 훑는다. 카드가 늘거나 폴더가 바뀌어도 그대로 동작한다.
            return AssetDatabase.FindAssets($"t:{nameof(LSO_CardSO)}")
                .Select(AssetDatabase.GUIDToAssetPath)
                .Select(AssetDatabase.LoadAssetAtPath<LSO_CardSO>)
                .Where(card => card != null)
                .OrderBy(card => card.name)
                .ToArray();
        }

        /// <summary>
        /// cards는 private [SerializeField]다. SerializedObject를 거치면
        /// 카탈로그 클래스를 고치지 않고도 에디터에서 채울 수 있다.
        /// </summary>
        private static void WriteCards(LDY_CardCatalogSO catalog, LSO_CardSO[] cards)
        {
            var serialized = new SerializedObject(catalog);
            SerializedProperty property = serialized.FindProperty("cards");

            property.arraySize = cards.Length;
            for (int i = 0; i < cards.Length; i++)
                property.GetArrayElementAtIndex(i).objectReferenceValue = cards[i];

            serialized.ApplyModifiedProperties();

            EditorUtility.SetDirty(catalog);
            AssetDatabase.SaveAssets();
        }

        /// <summary>
        /// id가 겹치면 불러올 때 어느 카드인지 가릴 수 없다.
        /// 런타임에도 카탈로그가 에러를 내지만, 넣는 시점에 알려주는 편이 고치기 쉽다.
        /// </summary>
        private static void ReportDuplicateIds(IEnumerable<LSO_CardSO> cards)
        {
            var duplicates = cards
                .GroupBy(card => card.Id)
                .Where(group => group.Count() > 1);

            foreach (var group in duplicates)
            {
                Debug.LogError(
                    $"[LDY_CardCatalogTool] id '{group.Key}' 가 {group.Count()}장에서 겹칩니다: " +
                    string.Join(", ", group.Select(c => c.name)));
            }
        }
    }
}
