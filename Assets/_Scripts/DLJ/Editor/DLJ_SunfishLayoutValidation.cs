using System;
using System.IO;
using System.Reflection;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>편집 가능한 배치와 독립 재생 사본의 동일성을 검사한다. 원본 에셋/씬은 저장하지 않는다.</summary>
public static class DLJ_SunfishLayoutValidation
{
    private const string ResultPath = "tmp/DLJ_SunfishLayoutValidation.txt";

    [MenuItem("Tools/DLJ/Validate Sunfish Layout")]
    public static void Run()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) return;
        Scene scene = default;
        try
        {
            var asset = AssetDatabase.LoadAssetAtPath<GameObject>(
                "Assets/_Prefabs/DLJ/SunfishFateLayout.prefab");
            var prefab = asset != null ? asset.GetComponent<DLJ_SunfishFateLayout>() : null;
            Require(prefab != null, "Layout prefab did not import.");
            foreach (string path in new[] { "Background", "Top Wave", "Bottom Wave", "Left Arrow", "Right Arrow", "Text Group/Text", "Text Group/Text Shadow" })
                Require(prefab.transform.Find(path) != null, "Missing editable element: " + path);

            scene = EditorSceneManager.NewPreviewScene();
            var layout = UnityEngine.Object.Instantiate(prefab);
            SceneManager.MoveGameObjectToScene(layout.gameObject, scene);
            // 임의로 위치/크기/회전을 바꿔도 재생 시 다시 계산해 덮지 않아야 한다.
            var textRect = (RectTransform)layout.transform.Find("Text Group");
            textRect.anchoredPosition = new Vector2(37, -23);
            textRect.sizeDelta = new Vector2(480, 140);
            textRect.localRotation = Quaternion.Euler(0, 0, 9);
            var wave = (RectTransform)layout.transform.Find("Top Wave");
            wave.anchoredPosition += new Vector2(11, 19);
            wave.sizeDelta = new Vector2(510, 96);

            var sourceObject = new GameObject("Sunfish Layout Validation Source");
            sourceObject.SetActive(false);
            SceneManager.MoveGameObjectToScene(sourceObject, scene);
            var source = sourceObject.AddComponent<DLJ_SunfishFateEffect>();
            var serialized = new SerializedObject(source);
            serialized.FindProperty("layoutPrefab").objectReferenceValue = layout;
            serialized.FindProperty("worldScale").floatValue = 0.002f;
            serialized.FindProperty("worldOffset").vector3Value = new Vector3(0, 0.87f, 0);
            serialized.FindProperty("deathText").stringValue = "돌연사";
            serialized.FindProperty("survivalText").stringValue = "살았다!";
            serialized.ApplyModifiedPropertiesWithoutUndo();
            source.transform.localScale = Vector3.one * 0.64f;
            source.Play();
            var preview = source.GetComponentInChildren<DLJ_SunfishFateLayout>(true);
            Require(preview != null, "Preview did not instantiate layout.");

            for (int variant = 0; variant <= 1; variant++)
            {
                var playback = source.CreateDetachedPlayback(variant);
                playback.Play();
                var actual = playback.GetComponentInChildren<DLJ_SunfishFateLayout>(true);
                Require(actual != null, "Playback did not instantiate layout.");
                CompareRect(textRect, (RectTransform)actual.transform.Find("Text Group"));
                CompareRect(wave, (RectTransform)actual.transform.Find("Top Wave"));
                string expected = variant == 0 ? "돌연사" : "살았다!";
                foreach (TMP_Text label in actual.GetComponentsInChildren<TMP_Text>(true))
                    Require(label.text == expected, "Wrong result text: " + label.text);
                Require(Vector3.Distance(preview.transform.lossyScale, actual.transform.lossyScale) < 0.00001f,
                    "Preview and playback scales differ.");
                Require(Vector3.Distance(preview.transform.position, actual.transform.position) < 0.00001f,
                    "Preview and playback offsets differ.");
                // 실제 애니메이션이 호출하는 배치 갱신 뒤에도 자식 레이아웃을 유지한다.
                typeof(DLJ_SunfishFateEffect).GetMethod("UpdatePose", BindingFlags.NonPublic | BindingFlags.Instance)
                    .Invoke(playback, null);
                CompareRect(textRect, (RectTransform)actual.transform.Find("Text Group"));
                UnityEngine.Object.DestroyImmediate(playback.gameObject);
            }
            Directory.CreateDirectory("tmp");
            File.WriteAllText(ResultPath, "PASS: prefab import; editable elements; moved/resized/rotated layout retained; death/survival text; preview/playback world scale and offset match; UpdatePose preserves child layout.");
            Debug.Log("DLJ Sunfish layout validation passed.");
        }
        catch (Exception e)
        {
            Directory.CreateDirectory("tmp");
            File.WriteAllText(ResultPath, "FAIL: " + e);
            Debug.LogException(e);
        }
        finally
        {
            if (scene.IsValid()) EditorSceneManager.ClosePreviewScene(scene);
        }
    }

    private static void CompareRect(RectTransform expected, RectTransform actual)
    {
        Require(actual != null, "Missing rectangle.");
        Require(expected.anchoredPosition == actual.anchoredPosition, "Position overwritten.");
        Require(expected.sizeDelta == actual.sizeDelta, "Size overwritten.");
        Require(Quaternion.Angle(expected.localRotation, actual.localRotation) < 0.001f, "Rotation overwritten.");
        Require(expected.anchorMin == actual.anchorMin && expected.anchorMax == actual.anchorMax, "Anchors overwritten.");
        Require(expected.localScale == actual.localScale, "Local scale overwritten.");
    }

    private static void Require(bool value, string message)
    {
        if (!value) throw new InvalidOperationException(message);
    }
}
