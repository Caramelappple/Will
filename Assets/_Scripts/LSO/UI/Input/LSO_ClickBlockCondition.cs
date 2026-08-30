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
        NotMyTurn,

        /// <summary>이동·공격 연출이 도는 동안.</summary>
        Animating,

        /// <summary>보드에서 기물을 고른 상태.</summary>
        PieceSelected,

        /// <summary>카드를 들고 놓을 자리를 고르는 중.</summary>
        CardPlacing,

        /// <summary>유언을 고르는 중.</summary>
        WillSelecting,

        /// <summary>계승 대상을 지정하는 중.</summary>
        SuccessionWaiting,

        /// <summary>
        /// 위의 조건을 전부 합친 것. 하나라도 해당하면 막는다.
        ///
        /// 여섯 개를 일일이 넣는 대신 쓴다. 나중에 조건이 늘어나도 저절로 따라간다.
        /// 반대로, 새 조건이 생기면 이걸 쓰는 곳이 모르는 사이에 더 자주 막히게 된다.
        /// 몇 개만 골라 막고 싶으면 이것 대신 그 조건들을 직접 넣을 것.
        /// </summary>
        All
    }
}
