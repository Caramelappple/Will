using UnityEngine;

namespace _Scripts.LSO.Deck
{
    /// <summary>
    /// 덱 구성 규칙.
    ///
    /// 값을 코드에 박지 않고 에셋으로 두는 이유는 밸런싱에서 바뀔 수 있기 때문이다.
    /// 규칙이 늘어나면 여기에 필드를 더하고 LSO_DeckDraft가 그것을 보게 하면 된다.
    /// </summary>
    [CreateAssetMenu(fileName = "LSO_DeckRules", menuName = "LSO/Deck/Rules")]
    public class LSO_DeckRulesSO : ScriptableObject
    {
        [Tooltip("덱에 넣을 수 있는 최대 장수.")]
        [SerializeField, Min(1)] private int maxCards = 8;

        [Tooltip("확정에 필요한 최소 장수. 최대치와 같게 두면 다 채워야 시작할 수 있다.")]
        [SerializeField, Min(0)] private int minCards = 1;

        public int MaxCards => Mathf.Max(1, maxCards);

        public int MinCards => Mathf.Clamp(minCards, 0, MaxCards);
    }
}
