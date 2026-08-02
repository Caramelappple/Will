using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.UI;

// 노드 글로우를 URP Bloom으로 실제로 "번지게" 만들고 싶을 때 쓰는 선택적 설정 툴.
// 기본 글로우(LDY_MapNodeView의 알파 펄스)는 이 툴 없이도 항상 동작함 — 이건 그 위에 실제 후처리를 얹는 추가 옵션.
// 주의: Canvas가 Screen Space - Overlay면 카메라 후처리가 UI에 적용되지 않으므로,
// 이 툴은 MapCanvas를 Screen Space - Camera로 전환하는 것까지 함께 처리함.
public static class LDY_MapBloomSetup
{
    private const string ProfileAssetPath = "Assets/01.Scripts/LDY/Map/LDY_MapPostProcess.asset";

    [MenuItem("LDY/Map/Bloom 후처리 설정 (URP, 선택)")]
    private static void Setup()
    {
        if (!(GraphicsSettings.currentRenderPipeline is UniversalRenderPipelineAsset))
        {
            EditorUtility.DisplayDialog("Bloom 설정", "현재 프로젝트의 Render Pipeline이 URP가 아니라 적용할 수 없습니다.", "확인");
            return;
        }

        Camera cam = Camera.main;
        if (cam == null)
        {
            EditorUtility.DisplayDialog("Bloom 설정", "씬에 MainCamera 태그가 붙은 카메라가 없습니다. 먼저 카메라를 배치해주세요.", "확인");
            return;
        }

        bool proceed = EditorUtility.DisplayDialog("Bloom 후처리 설정",
            "다음을 자동으로 처리합니다:\n\n" +
            "1) MainCamera의 HDR / Post Processing 켜기\n" +
            "2) Bloom이 적용된 Global Volume 생성\n" +
            "3) 'MapCanvas'를 Screen Space - Camera로 전환하고 MainCamera 연결\n\n" +
            "Canvas 렌더 모드가 바뀌면 UI가 카메라 위치/거리 영향을 받게 됩니다. 계속할까요?",
            "계속", "취소");
        if (!proceed) return;

        cam.allowHDR = true;
        UniversalAdditionalCameraData camData = cam.GetUniversalAdditionalCameraData();
        camData.renderPostProcessing = true;

        SetupVolume();
        SetupCanvas(cam);

        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());

        EditorUtility.DisplayDialog("Bloom 설정 완료",
            "이제 LDY_MapTheme 애셋의 'Glow Hdr Intensity'를 1보다 크게(2~3 권장) 올리면 " +
            "노드 글로우가 Bloom으로 실제로 번집니다.\n\n" +
            "번짐이 너무 세거나 약하면 Global Volume(MapPostProcessVolume)의 Bloom Threshold/Intensity도 함께 조절하세요.",
            "확인");
    }

    private static void SetupVolume()
    {
        Volume existing = Object.FindFirstObjectByType<Volume>();
        if (existing != null && existing.profile != null && existing.profile.Has<Bloom>()) return;

        GameObject go = existing != null ? existing.gameObject : new GameObject("MapPostProcessVolume");
        if (existing == null) Undo.RegisterCreatedObjectUndo(go, "Setup Bloom");

        Volume volume = existing != null ? existing : go.AddComponent<Volume>();
        volume.isGlobal = true;

        VolumeProfile profile = volume.profile;
        if (profile == null)
        {
            profile = ScriptableObject.CreateInstance<VolumeProfile>();
            AssetDatabase.CreateAsset(profile, ProfileAssetPath);
            volume.profile = profile;
        }

        if (!profile.Has<Bloom>())
        {
            Bloom bloom = profile.Add<Bloom>(true);
            bloom.threshold.overrideState = true;
            bloom.threshold.value = 1.0f;
            bloom.intensity.overrideState = true;
            bloom.intensity.value = 0.6f;
            bloom.tint.overrideState = true;
            bloom.tint.value = new Color(1f, 0.85f, 0.55f);
        }

        AssetDatabase.SaveAssets();
    }

    private static void SetupCanvas(Camera cam)
    {
        GameObject canvasGO = GameObject.Find("MapCanvas");
        if (canvasGO == null) return;

        Canvas canvas = canvasGO.GetComponent<Canvas>();
        if (canvas == null) return;

        Undo.RecordObject(canvas, "Setup Bloom Canvas");
        canvas.renderMode = RenderMode.ScreenSpaceCamera;
        canvas.worldCamera = cam;
        canvas.planeDistance = 1f;
    }
}
