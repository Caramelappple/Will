using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEngine.SceneManagement;
using _Scripts.LDY;

public class KTH_ChapterClearManager : MonoBehaviour
{
    public static KTH_ChapterClearManager Instance { get; private set; }

    [Header("스테이지 선택 씬")]
    [SerializeField] private string mapSceneName = "MapScene";

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    /// <summary>
    /// 보스 클리어 처리.
    ///
    /// 다음 챕터가 존재하면:
    /// 현재 보스 노드를 클리어하고 → 다음 챕터로 이동 → MapScene
    ///
    /// 다음 챕터가 없다면:
    /// 기존 보스 클리어 처리를 통해 완전 클리어 씬으로 이동
    /// </summary>
    public void HandleBossClear()
    {
        LDY_MapManager mapManager = LDY_MapManager.Instance;

        if (mapManager == null)
        {
            Debug.LogError(
                "[KTH_ChapterClearManager] LDY_MapManager.Instance를 찾을 수 없습니다."
            );

            return;
        }

        int currentChapter = mapManager.CurrentChapter;
        int nextChapter = currentChapter + 1;

        Debug.Log(
            $"[KTH_ChapterClearManager] " +
            $"현재 챕터: {currentChapter} / " +
            $"다음 챕터: {nextChapter}"
        );

        // 실제 ChapterMapData에 다음 챕터가 존재하는지 확인
        bool hasNextChapter = HasChapter(mapManager, nextChapter);

        if (hasNextChapter)
        {
            Debug.Log(
                $"[KTH_ChapterClearManager] ★ {nextChapter}챕터가 존재합니다. " +
                "다음 챕터로 이동합니다."
            );

            // Boss CompleteNode()가 내부에서
            // currentChapter++
            // currentStage = 1
            // LoadChapterToEditor()
            // BuildNodes()
            // 까지 처리함
            mapManager.CompleteActiveNode();

            Debug.Log(
                $"[KTH_ChapterClearManager] " +
                $"다음 챕터 준비 완료: Chapter {mapManager.CurrentChapter}"
            );

            // 마지막 타격의 복귀 애니메이션이 아직 돌고 있으면 기다렸다가 씬을 넘긴다.
            // LDY_MapManager.LoadSceneAfterAnimations와 같은 이유.
            StartCoroutine(Co_LoadMapSceneAfterAnimations());
        }
        else
        {
            Debug.Log(
                "[KTH_ChapterClearManager] ★ 더 이상 챕터가 없습니다. " +
                "완전 클리어 처리합니다."
            );

            // 마지막 챕터이므로 기존 보스 클리어 처리
            // (LDY_MapManager.CompleteActiveNodeAndReturnToMap 내부에서
            //  이미 씬 전환 전 연출 대기를 처리한다)
            mapManager.CompleteActiveNodeAndReturnToMap();
        }
    }

    private IEnumerator Co_LoadMapSceneAfterAnimations()
    {
        LDY_TurnManager turnManager = FindFirstObjectByType<LDY_TurnManager>();

        if (turnManager != null)
        {
            while (turnManager.IsAnimating())
                yield return null;
        }

        SceneManager.LoadScene(mapSceneName);
    }

    /// <summary>
    /// MapManager 내부의 chapterMaps에
    /// 해당 챕터 데이터가 실제로 존재하는지 확인한다.
    ///
    /// MapManager 코드는 수정하지 않는다.
    /// </summary>
    private bool HasChapter(
        LDY_MapManager mapManager,
        int chapter
    )
    {
        if (mapManager == null)
            return false;

        FieldInfo field = typeof(LDY_MapManager).GetField(
            "chapterMaps",
            BindingFlags.Instance | BindingFlags.NonPublic
        );

        if (field == null)
        {
            Debug.LogError(
                "[KTH_ChapterClearManager] " +
                "LDY_MapManager의 chapterMaps를 찾을 수 없습니다."
            );

            return false;
        }

        object value = field.GetValue(mapManager);

        if (value is not List<LDY_ChapterMapData> chapterMaps)
        {
            Debug.LogError(
                "[KTH_ChapterClearManager] " +
                "chapterMaps 데이터를 읽을 수 없습니다."
            );

            return false;
        }

        foreach (LDY_ChapterMapData chapterData in chapterMaps)
        {
            if (chapterData == null)
                continue;

            if (chapterData.chapter == chapter)
            {
                Debug.Log(
                    $"[KTH_ChapterClearManager] " +
                    $"{chapter}챕터 데이터 발견"
                );

                return true;
            }
        }

        Debug.Log(
            $"[KTH_ChapterClearManager] " +
            $"{chapter}챕터 데이터 없음 → 마지막 챕터"
        );

        return false;
    }
}