using System.Collections.Generic;
using _Scripts.LSO.Deck.Data;

public class CardPageBinder
{
    // 카드 ID 기반으로 고정된 페이지 번호를 저장하는 딕셔너리
    private readonly Dictionary<string, int> _assignedPages = new Dictionary<string, int>();

    /// <summary>
    /// 전체 카드 리스트를 입력받아 페이지별로 카드를 고정 할당합니다.
    /// </summary>
    public void AssignPages(IReadOnlyList<LSO_CardSO> cards, int itemsPerPage)
    {
        _assignedPages.Clear();
        if (cards == null || itemsPerPage <= 0) return;

        for (int i = 0; i < cards.Count; i++)
        {
            if (cards[i] == null) continue;

            // 순서대로 계산하여 각 카드마다 변경 불가한 고정 페이지(1, 2, 3...) 지정
            int assignedPage = (i / itemsPerPage) + 1;
            _assignedPages[cards[i].Id] = assignedPage;
        }
    }

    /// <summary>
    /// 해당 카드가 현재 요청된 페이지에 속해있는지 검증합니다.
    /// </summary>
    public bool IsCardInPage(LSO_CardSO card, int targetPage)
    {
        if (card == null) return false;

        if (_assignedPages.TryGetValue(card.Id, out int assignedPage))
        {
            return assignedPage == targetPage;
        }
        return false;
    }
}