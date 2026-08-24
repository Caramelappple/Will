namespace _Scripts.LSO.Deck
{
    public enum LSO_DeckRejectReason
    {
        None,

        /// <summary>덱이 최대 장수를 이미 채웠다.</summary>
        DeckFull,

        /// <summary>확정하기에 장수가 모자란다.</summary>
        TooFewCards,

        /// <summary>도감에 없는 칸 번호가 들어왔다.</summary>
        InvalidSlot,
    }
}
