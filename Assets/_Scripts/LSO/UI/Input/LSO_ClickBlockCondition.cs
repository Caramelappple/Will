namespace _Scripts.LSO.UI.Input
{
    /// <summary>
    /// 클릭을 막을 상황.
    ///
    /// 값은 뒤에만 추가할 것. 중간에 끼워 넣으면 씬에 저장된 목록이 어긋난다.
    /// </summary>
    public enum LSO_ClickBlockCondition
    {
        /// <summary>내 턴이 아닐 때. Allowed Turn 으로 어느 턴인지 정한다.</summary>
        NotMyTurn = 0,

        /// <summary>이동·공격 연출이 도는 동안.</summary>
        Animating = 1,

        // 2 = PieceSelected · 3 = CardPlacing 이 있던 자리다.
        //
        // 기물을 골랐다고, 카드를 들고 있다고 해서 막을 일이 아니었다.
        // 둘 다 "고른 것을 바꾸려고" 누르는 경우가 더 많은데 그때 문이 닫혀
        // 아무 반응이 없었다.
        //
        // **번호는 비워 둔다.** 아래 것들을 당겨 쓰면 씬·프리팹에 저장된 숫자가
        // 통째로 한 칸씩 어긋난다. 유니티는 enum 을 이름이 아니라 숫자로 저장한다.

        /// <summary>유언을 고르는 중.</summary>
        WillSelecting = 4,

        /// <summary>계승 대상을 지정하는 중.</summary>
        SuccessionWaiting = 5,

        /// <summary>
        /// 위의 조건을 전부 합친 것. 하나라도 해당하면 막는다.
        ///
        /// 일일이 넣는 대신 쓴다. 나중에 조건이 늘어나도 저절로 따라간다.
        /// 반대로, 새 조건이 생기면 이걸 쓰는 곳이 모르는 사이에 더 자주 막히게 된다.
        /// 몇 개만 골라 막고 싶으면 이것 대신 그 조건들을 직접 넣을 것.
        /// </summary>
        All = 6
    }
}
