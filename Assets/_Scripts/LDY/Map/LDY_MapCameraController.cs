using System;
using System.Collections.Generic;
using UnityEngine;

public class LDY_MapCameraController : MonoBehaviour
{
    [Serializable]
    public class ChapterCameraSetting
    {
        [Tooltip("챕터 번호 (예: 1, 2, 3...)")]
        public int chapter = 1;

        [Tooltip("해당 챕터에서 적용할 카메라 확대/축소 배율 (1 = 기본)")]
        public float zoomScale = 1.0f;

        [Tooltip("해당 챕터에서 화면 중앙을 맞추기 위한 위치 오프셋")]
        public Vector2 positionOffset = Vector2.zero;
    }

    [Header("References")]
    [SerializeField] private RectTransform nodeContainer;
    [SerializeField] private RectTransform lineContainer;

    [Header("챕터별 카메라 스케일/위치 설정")]
    [SerializeField]
    private List<ChapterCameraSetting> chapterCameraSettings = new List<ChapterCameraSetting>()
    {
        new ChapterCameraSetting { chapter = 1, zoomScale = 1.0f, positionOffset = Vector2.zero },
        new ChapterCameraSetting { chapter = 2, zoomScale = 0.8f, positionOffset = Vector2.zero }
    };

    private void Start()
    {
        ApplyChapterCameraSetting();
    }

    /// <summary>
    /// 현재 챕터 번호에 맞춰 카메라 Zoom과 Position을 적용합니다.
    /// </summary>
    public void ApplyChapterCameraSetting()
    {
        int currentChapter = 1;

        if (LDY_MapManager.Instance != null)
        {
            currentChapter = LDY_MapManager.Instance.CurrentChapter;
        }

        // 현재 챕터에 맞는 설정 찾기 (없으면 기본 1.0f, Vector2.zero 적용)
        ChapterCameraSetting setting = chapterCameraSettings.Find(s => s.chapter == currentChapter);

        float targetScale = setting != null ? setting.zoomScale : 1.0f;
        Vector2 targetPos = setting != null ? setting.positionOffset : Vector2.zero;

        SetCameraTransform(targetPos, targetScale);
    }

    private void SetCameraTransform(Vector2 pos, float scale)
    {
        if (nodeContainer != null)
        {
            nodeContainer.anchoredPosition = pos;
            nodeContainer.localScale = Vector3.one * scale;
        }

        if (lineContainer != null)
        {
            lineContainer.anchoredPosition = pos;
            lineContainer.localScale = Vector3.one * scale;
        }
    }
}