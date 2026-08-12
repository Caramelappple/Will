using _Scripts.LSO.Deck.Data;
using DG.Tweening;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class KTH_DiscardCardUI : MonoBehaviour
{
    [Header("UI 연결")]
    public RectTransform discardCardTransform; // 버린 카드 더미 UI 위치
    public TMP_Text discardCountText;         // 버린 카드 수 표시 Text

    [Header("연출 설정")]
    public float countBumpDuration = 0.2f;   // 카드가 도착했을 때 숫자 바운스 시간

    private List<LSO_CardSO> _discardCardList = new List<LSO_CardSO>();

    public RectTransform DiscardCardTransform => discardCardTransform;
    public int Count => _discardCardList.Count;

    private void Awake()
    {
        UpdateUI();
    }

    /// <summary>
    /// 버린 카드 더미에 카드를 추가하고 UI를 갱신합니다.
    /// </summary>
    public void AddToDiscardPile(LSO_CardSO cardData)
    {
        _discardCardList.Add(cardData);
        UpdateUI();
        AnimateCountText();
    }

    /// <summary>
    /// UI 텍스트 업데이트
    /// </summary>
    public void UpdateUI()
    {
        if (discardCountText != null)
        {
            discardCountText.text = _discardCardList.Count.ToString();
        }
    }

    /// <summary>
    /// 카드가 버림 더미에 추가될 때 숫자 바운스 연출
    /// </summary>
    private void AnimateCountText()
    {
        if (discardCountText == null) return;

        discardCountText.transform.DOKill();
        discardCountText.transform.localScale = Vector3.one;

        discardCountText.transform.DOScale(1.3f, countBumpDuration)
            .SetLoops(2, LoopType.Yoyo)
            .SetEase(Ease.OutQuad);
    }

    /// <summary>
    /// 덱 리셔플 시 버린 카드 더미 비우기
    /// </summary>
    public List<LSO_CardSO> ClearAndGetList()
    {
        List<LSO_CardSO> currentPile = new List<LSO_CardSO>(_discardCardList);
        _discardCardList.Clear();
        UpdateUI();
        return currentPile;
    }
}