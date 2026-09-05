using _Scripts.LDY;
using _Scripts.LSO.Ability;
using _Scripts.LSO.Ability.Catalog;
using _Scripts.LSO.Will;

namespace _Scripts.LSO.UI.Text
{
    /// <summary>
    /// 영문 enum을 화면에 띄울 한글로 바꾸는 유일한 창구.
    ///
    /// 화면에 한글을 뿌리는 곳은 전부 여기로 온다. 정보창·카드창·유언 메모장이
    /// 같은 창구를 보므로 두 화면의 표기가 갈리지 않는다.
    ///
    /// ── 대응표는 파일 하나가 아니라 창구 하나다 ────────────────
    /// 한글 문구를 한 파일에 몰아넣지 않는다. 그러면 특성을 고칠 때
    /// 구현과 문구가 서로 다른 파일에 있어 늘 두 곳을 열게 된다.
    /// 대신 "그 대상의 데이터가 이미 사는 곳"에 문구를 둔다.
    ///
    ///   특성   LSO_AbilityCatalog.asset   구현이 순수 C# 클래스라 데이터 자리가
    ///                                     없었다. 그래서 사전을 새로 만들었다.
    ///   유언   각 DLJ_WillDataSO 에셋      이미 설명·아이콘이 거기 있다.
    ///                                     이름만 딴 데 두면 문구가 두 파일로 갈린다.
    ///   사거리 아래 switch                 데이터 에셋이 없고 값이 다섯 개로 고정이다.
    ///                                     이것 때문에 에셋을 만들면 관리할 것만 는다.
    ///
    /// 어느 쪽이든 부르는 쪽은 이 창구만 알면 된다.
    /// ─────────────────────────────────────────────────────────
    ///
    /// 못 찾은 값은 영문 enum 이름이 그대로 나온다. 화면이 비지 않고,
    /// 영문이 보이면 그 자리에 문구를 안 적었다는 신호가 된다.
    ///
    /// 에셋 파일 이름은 영문으로 둘 것. 윈도우와 맥이 한글을 다르게 저장해서
    /// (NFC / NFD) Git이 같은 파일을 두 벌로 본다. 한글은 파일 이름이 아니라
    /// 파일 내용에만 넣는다.
    /// </summary>
    public static class LSO_DisplayNames
    {
        /// <summary>
        /// 사거리 이름. 다섯 개로 고정이고 딸린 데이터 에셋이 없어 여기 그대로 둔다.
        ///
        /// 사거리에 설명·아이콘이 붙기 시작하면 그때 특성처럼 사전으로 뺄 것.
        /// 지금 빼면 다섯 줄을 위해 에셋 하나를 더 챙겨야 한다.
        /// </summary>
        public static string Of(LDY_RangeType value)
        {
            return value switch
            {
                LDY_RangeType.Melee => "근접",
                LDY_RangeType.MeleeOrthogonal => "직선 근접",
                LDY_RangeType.Ranged => "원거리",
                LDY_RangeType.Jump => "도약",
                LDY_RangeType.None => "없음",
                _ => value.ToString()
            };
        }

        /// <summary>
        /// 유언 이름. 문구는 각 DLJ_WillDataSO 에셋이 들고 있다.
        ///
        /// 유언 에셋을 이미 손에 쥐고 있다면 will.DisplayName을 바로 읽는 편이 낫다.
        /// 이 창구는 enum만 아는 곳을 위해 데이터베이스를 한 번 거친다.
        /// </summary>
        public static string Of(LSO_WillType value) => LSO_WillText.NameOf(value);

        /// <summary>
        /// 특성 이름. 문구는 특성 사전(LSO_AbilityCatalog.asset)이 들고 있다.
        ///
        /// 예전에는 이 자리의 switch가 이름을, KTH 정보창의 인스펙터 리스트가
        /// 설명을 따로 들고 있었다. 특성 하나를 고치려면 두 곳을 맞춰야 했고,
        /// 어긋났을 때 어느 쪽이 맞는지 정할 방법이 없었다.
        ///
        /// 설명·아이콘까지 필요하면 LSO_AbilityText를 직접 쓸 것.
        /// </summary>
        public static string Of(LSO_AbilityType value) => LSO_AbilityText.NameOf(value);
    }
}
