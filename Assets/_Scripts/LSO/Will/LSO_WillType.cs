namespace _Scripts.LSO.Will
{
    /// <summary>
    /// 기물이 죽을 때 발동하는 유언.
    ///
    /// None이 0인 것은 의도한 것이다. 유니티는 enum을 int로 저장하므로
    /// 값을 안 정한 필드와 새로 만든 에셋은 전부 0으로 시작한다.
    /// 그 자리에 실제 유언이 앉아 있으면 "안 정했는데 저주가 걸려 있는" 일이 생긴다.
    ///
    /// 값은 뒤에만 추가할 것. 중간에 끼워 넣으면 저장된 값이 통째로 밀린다.
    /// 실제로 한 번 밀어봤고, 씬 6개·프리팹 6개·에셋 8개를 손으로 맞춰야 했다.
    /// </summary>
    public enum LSO_WillType
    {
        None = 0,
        Curse = 1,
        Rage = 2,
        Succession = 3,
        Contract = 4,
        Sacrifice = 5
    }
}
