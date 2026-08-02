using System;
using System.Collections;
using TMPro;
using UnityEngine;

// 씬 전환(아이리스)이 끝난 직후 재생되는 "3, 2, 1, START!" 카운트다운.
// dimBackground가 raycastTarget=true라서 켜져 있는 동안 보드 클릭이 막힘 - 별도 입력 차단 코드가 필요 없음.
// LDY_SceneTransition이 씬 로드 직후 Play(null)로 호출함.
public class LDY_BattleCountdown : MonoBehaviour
{
    [SerializeField] private GameObject dimBackground;
    [SerializeField] private TextMeshProUGUI countdownText;
    [SerializeField] private float numberHoldDuration = 0.7f;
    [SerializeField] private float startHoldDuration = 0.5f;

    private Coroutine playing;

    public void Play(Action onComplete)
    {
        if (playing != null) StopCoroutine(playing);
        playing = StartCoroutine(PlayRoutine(onComplete));
    }

    private IEnumerator PlayRoutine(Action onComplete)
    {
        if (dimBackground != null) dimBackground.SetActive(true);

        yield return ShowNumber("3", numberHoldDuration);
        yield return ShowNumber("2", numberHoldDuration);
        yield return ShowNumber("1", numberHoldDuration);
        yield return ShowNumber("START!", startHoldDuration);

        if (dimBackground != null) dimBackground.SetActive(false);
        playing = null;
        onComplete?.Invoke();
    }

    private IEnumerator ShowNumber(string text, float holdDuration)
    {
        if (countdownText != null) countdownText.text = text;
        yield return new WaitForSecondsRealtime(holdDuration);
    }
}
