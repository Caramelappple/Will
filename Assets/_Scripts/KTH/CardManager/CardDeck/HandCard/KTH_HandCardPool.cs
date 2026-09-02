using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// KTH_HandCard 오브젝트 풀.
///
/// 드로우 / 버림 / 리셔플 시마다 카드를 Instantiate / Destroy 하는 대신
/// 이미 만들어둔 카드 오브젝트를 재사용한다.
/// (매 턴 카드가 여러 번 생성/파괴되는 구조라 GC 스파이크와
///  Instantiate 비용을 줄이기 위해 도입)
///
/// 사용법:
///  - 카드가 필요할 때: KTH_HandCardPool.Instance.Get(parent)
///  - 카드를 더 이상 안 쓸 때: KTH_HandCardPool.Instance.Release(card)
///  (Destroy(card.gameObject) 대신 반드시 Release를 호출할 것)
/// </summary>
public class KTH_HandCardPool : MonoBehaviour
{
    public static KTH_HandCardPool Instance { get; private set; }

    [Header("Pool 설정")]
    [SerializeField] private KTH_HandCard cardPrefab;

    [Tooltip("씬 시작 시 미리 만들어둘 카드 개수")]
    [SerializeField] private int prewarmCount = 10;

    [Tooltip("비활성화된 풀 카드들을 담아둘 부모. 비워두면 이 오브젝트 밑에 둔다.")]
    [SerializeField] private Transform poolParent;

    private readonly Stack<KTH_HandCard> _pool =
        new Stack<KTH_HandCard>();

    public int PooledCount =>
        _pool.Count;

    private void Awake()
    {
        if (Instance != null &&
            Instance != this)
        {
            Debug.LogWarning(
                "[KTH_HandCardPool] 이미 인스턴스가 존재합니다. 중복 오브젝트를 파괴합니다.",
                this
            );

            Destroy(gameObject);
            return;
        }

        Instance = this;

        if (poolParent == null)
        {
            poolParent = transform;
        }

        Prewarm(prewarmCount);
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }

    private void Prewarm(int count)
    {
        if (cardPrefab == null)
        {
            Debug.LogWarning(
                "[KTH_HandCardPool] Card Prefab이 연결되지 않아 프리워밍을 건너뜁니다.",
                this
            );

            return;
        }

        for (int i = 0; i < count; i++)
        {
            KTH_HandCard card = CreateNewInstance();
            ReturnToPoolInternal(card);
        }
    }

    private KTH_HandCard CreateNewInstance()
    {
        KTH_HandCard card =
            Instantiate(
                cardPrefab,
                poolParent
            );

        return card;
    }

    /// <summary>
    /// 풀에서 카드를 하나 꺼내 parent 아래에 배치하고 활성화한 뒤 반환한다.
    /// 풀이 비어있으면 새로 생성한다.
    /// 꺼낸 카드는 선택/호버/트윈 등 이전 상태가 모두 초기화되어 있다.
    /// </summary>
    public KTH_HandCard Get(Transform parent)
    {
        KTH_HandCard card =
            _pool.Count > 0
                ? _pool.Pop()
                : CreateNewInstance();

        Transform cardTransform =
            card.transform;

        cardTransform.SetParent(
            parent,
            false
        );

        card.ResetForPool();

        cardTransform.localPosition = Vector3.zero;
        cardTransform.localRotation = Quaternion.identity;
        cardTransform.localScale = Vector3.one;

        card.gameObject.SetActive(true);

        return card;
    }

    /// <summary>
    /// 카드를 비활성화하고 풀로 되돌린다.
    /// Destroy(card.gameObject) 대신 반드시 이걸 호출할 것.
    /// </summary>
    public void Release(KTH_HandCard card)
    {
        if (card == null)
        {
            return;
        }

        ReturnToPoolInternal(card);
    }

    private void ReturnToPoolInternal(KTH_HandCard card)
    {
        card.ResetForPool();

        card.transform.SetParent(
            poolParent,
            false
        );

        card.gameObject.SetActive(false);

        _pool.Push(card);
    }
}