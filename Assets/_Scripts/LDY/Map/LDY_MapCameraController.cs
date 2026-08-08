using System;
using System.Collections.Generic;
using UnityEngine;

public class LDY_MapCameraController : MonoBehaviour
{
    [Serializable]
    public class ChapterUISetting
    {
        [Tooltip("챕터 번호 (예: 1, 2, 3...)")]
        public int chapter = 1;

        [Tooltip("해당 챕터에서 적용할 UI 확대/축소 배율")]
        public float zoomScale = 1.0f;

        [Tooltip("해당 챕터에서 화면 중앙을 맞추기 위한 UI 위치 오프셋")]
        public Vector2 positionOffset = Vector2.zero;

        [Header("챕터별 카메라 개별 설정")]
        [Tooltip("체크 시 아래의 챕터 전용 카메라 설정을 사용 (체크 해제 시 통합 카메라 설정 사용)")]
        public bool useCustomCameraSetting = false;

        [Tooltip("카메라 Orthographic Size (원근감이 없는 2D 카메라일 때 사용)")]
        public float cameraOrthographicSize = 5f;

        [Tooltip("카메라 Field of View (3D 원근 카메라일 때 사용)")]
        public float cameraFieldOfView = 60f;

        [Tooltip("챕터별 카메라 위치 오프셋")]
        public Vector3 cameraPositionOffset = new Vector3(0f, 0f, -10f);
    }

    [Header("카메라 참조")]
    [Tooltip("제어할 씬의 카메라 (미지정 시 Camera.main 자동 할당)")]
    [SerializeField] private Camera targetCamera;

    [Header("회전 대상 UI (선택한 UI들만 공통 회전)")]
    [Tooltip("회전을 적용할 Target UI 목록 (예: NodeContainer, LineContainer 등)")]
    [SerializeField] private List<RectTransform> targetUIs = new List<RectTransform>();

    [Header("통합 UI 회전 설정 (모든 챕터 공통)")]
    [Tooltip("지정한 Target UI들에 공통으로 적용될 X, Y, Z 3D 회전 각도")]
    [SerializeField] private Vector3 globalRotation = Vector3.zero;

    [Header("통합 카메라 기본 설정 (모든 챕터 공통)")]
    [Tooltip("공통 카메라 Orthographic Size")]
    [SerializeField] private float globalCameraOrthoSize = 5f;

    [Tooltip("공통 카메라 Field of View")]
    [SerializeField] private float globalCameraFOV = 60f;

    [Tooltip("공통 카메라 위치")]
    [SerializeField] private Vector3 globalCameraPosition = new Vector3(0f, 0f, -10f);

    [Tooltip("공통 카메라 회전")]
    [SerializeField] private Vector3 globalCameraRotation = Vector3.zero;

    [Header("챕터별 개별 설정")]
    [SerializeField]
    private List<ChapterUISetting> chapterSettings = new List<ChapterUISetting>()
    {
        new ChapterUISetting { chapter = 1, zoomScale = 1.0f, positionOffset = Vector2.zero },
        new ChapterUISetting { chapter = 2, zoomScale = 0.8f, positionOffset = Vector2.zero }
    };

    private void Awake()
    {
        if (targetCamera == null)
        {
            targetCamera = Camera.main;
        }
    }

    private void Start()
    {
        ApplyAllSettings();
    }

    private void OnValidate()
    {
        if (targetCamera == null)
        {
            targetCamera = Camera.main;
        }
        ApplyAllSettings();
    }

    /// <summary>
    /// UI 설정과 카메라 설정을 통합하여 적용합니다.
    /// </summary>
    public void ApplyAllSettings()
    {
        int currentChapter = 1;

        if (LDY_MapManager.Instance != null)
        {
            currentChapter = LDY_MapManager.Instance.CurrentChapter;
        }

        ChapterUISetting setting = chapterSettings.Find(s => s.chapter == currentChapter);

        ApplyUISetting(setting);
        ApplyCameraSetting(setting);
    }

    /// <summary>
    /// Target UI들의 Position, Scale, Rotation을 적용합니다.
    /// </summary>
    private void ApplyUISetting(ChapterUISetting setting)
    {
        if (targetUIs == null || targetUIs.Count == 0) return;

        float targetScale = setting != null ? setting.zoomScale : 1.0f;
        Vector2 targetPos = setting != null ? setting.positionOffset : Vector2.zero;

        foreach (RectTransform targetUI in targetUIs)
        {
            if (targetUI == null) continue;

            targetUI.anchoredPosition = targetPos;
            targetUI.localScale = Vector3.one * targetScale;
            targetUI.localEulerAngles = globalRotation;
        }
    }

    /// <summary>
    /// 카메라의 Size, FOV, Position, Rotation을 적용합니다.
    /// </summary>
    private void ApplyCameraSetting(ChapterUISetting setting)
    {
        if (targetCamera == null) return;

        bool useCustom = setting != null && setting.useCustomCameraSetting;

        // 카메라 Position / Rotation 설정
        targetCamera.transform.localPosition = useCustom ? setting.cameraPositionOffset : globalCameraPosition;
        targetCamera.transform.localEulerAngles = globalCameraRotation;

        // 카메라 타입(Orthographic / Perspective)에 맞는 Zoom 설정
        if (targetCamera.orthographic)
        {
            targetCamera.orthographicSize = useCustom ? setting.cameraOrthographicSize : globalCameraOrthoSize;
        }
        else
        {
            targetCamera.fieldOfView = useCustom ? setting.cameraFieldOfView : globalCameraFOV;
        }
    }
}