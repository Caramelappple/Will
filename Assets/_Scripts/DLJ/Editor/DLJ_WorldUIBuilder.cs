using _Scripts.DLJ.UI.WorldUI;
using UnityEditor;
using UnityEngine;

/// <summary>효과 발생 시에만 나타나는 공통 팝업을 선택한 기물에 연결한다.</summary>
public static class DLJ_WorldUIBuilder
{
    // 기존 메뉴 경로를 유지해 이전 안내대로 실행해도 순간 팝업만 설정한다.
    private const string MenuPath = "GameObject/DLJ/Add World UI to Piece";

    [MenuItem(MenuPath, false, 20)]
    private static void AddToSelectedPieces()
    {
        foreach (Transform selected in Selection.transforms)
        {
            if (selected == null || EditorUtility.IsPersistent(selected.gameObject))
                continue;

            GameObject piece = selected.gameObject;
            DLJ_PieceFeedback feedback = piece.GetComponent<DLJ_PieceFeedback>();
            if (feedback == null)
                feedback = Undo.AddComponent<DLJ_PieceFeedback>(piece);

            Undo.RecordObject(feedback, "Enable DLJ Feedback");
            feedback.enabled = true;

            if (piece.TryGetComponent<global::DLJ_FoxKingBoss>(out _) &&
                piece.GetComponent<DLJ_FoxKingWorldUIBinder>() == null)
                Undo.AddComponent<DLJ_FoxKingWorldUIBinder>(piece);

            // 이전 버전의 상시 표시만 비활성화한다. 배치/아이콘 설정은 복구할 수 있게 보존한다.
            DLJ_WorldHealthUIBinder legacyHealth = piece.GetComponent<DLJ_WorldHealthUIBinder>();
            if (legacyHealth != null)
            {
                Undo.RecordObject(legacyHealth, "Disable Persistent Health UI");
                legacyHealth.enabled = false;
                PrefabUtility.RecordPrefabInstancePropertyModifications(legacyHealth);
            }

            for (int i = 0; i < selected.childCount; i++)
            {
                GameObject child = selected.GetChild(i).gameObject;
                if (child.GetComponent<DLJ_WorldUIController>() == null) continue;
                Undo.RecordObject(child, "Disable Persistent World UI");
                child.SetActive(false);
                PrefabUtility.RecordPrefabInstancePropertyModifications(child);
            }

            PrefabUtility.RecordPrefabInstancePropertyModifications(feedback);
            EditorUtility.SetDirty(piece);
            Debug.Log($"{piece.name}: 효과 발생 시 잠깐 표시하는 공통 팝업을 연결했습니다.", piece);
        }
    }

    [MenuItem(MenuPath, true)]
    private static bool ValidateAddToSelectedPieces()
    {
        return !EditorApplication.isPlaying && Selection.transforms.Length > 0;
    }
}
