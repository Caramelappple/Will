namespace _Scripts.LSO.Deck
{
    /// <summary>
    /// 덱 조작이 받아들여졌는지와 그 이유.
    ///
    /// bool만 돌려주면 화면이 "왜 안 됐는지"를 다시 계산하게 된다.
    /// Value에 숫자를 함께 실어서 "8장까지" "2장 더 필요" 같은 문구를 바로 만들 수 있게 한다.
    /// </summary>
    public readonly struct LSO_DeckValidation
    {
        public bool IsValid { get; }
        public LSO_DeckRejectReason Reason { get; }
        public int Value { get; }

        private LSO_DeckValidation(bool isValid, LSO_DeckRejectReason reason, int value)
        {
            IsValid = isValid;
            Reason = reason;
            Value = value;
        }

        public static LSO_DeckValidation Ok() =>
            new(true, LSO_DeckRejectReason.None, 0);

        public static LSO_DeckValidation Fail(LSO_DeckRejectReason reason, int value = 0) =>
            new(false, reason, value);

        /// <summary>화면에 그대로 띄울 수 있는 문구. 비어 있으면 띄울 것이 없다는 뜻이다.</summary>
        public string Message =>
            Reason switch
            {
                LSO_DeckRejectReason.DeckFull => $"덱은 {Value}장까지 넣을 수 있습니다.",
                LSO_DeckRejectReason.TooFewCards => $"{Value}장 이상 골라야 합니다.",
                LSO_DeckRejectReason.InvalidSlot => "고를 수 없는 카드입니다.",
                _ => string.Empty
            };
    }
}
