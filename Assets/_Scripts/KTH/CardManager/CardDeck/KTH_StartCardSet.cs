using System.Collections;
using UnityEngine;

public class KTH_StartCardSet : MonoBehaviour
{
    [SerializeField] private int startingHandCount = 5;
    [Tooltip("초기 카드가 뽑히는 간격(초)")]
    [SerializeField] private float drawInterval = 0.12f;

    private KTH_SpawnCard spawnCard;

    private void Awake()
    {
        spawnCard = FindAnyObjectByType<KTH_SpawnCard>();
    }

    private void Start()
    {
        if (spawnCard != null)
        {
            StartCoroutine(Co_StartDraw());
        }
        else
        {
            Debug.LogError("[KTH_StartCardSet] KTH_SpawnCard를 찾을 수 없습니다.");
        }
    }

    private IEnumerator Co_StartDraw()
    {
        // UI 및 Layout 생성이 완료되도록 1프레임 대기
        yield return null;

        for (int i = 0; i < startingHandCount; i++)
        {
            bool success = spawnCard.SpawnOneCardPublic();
            if (!success) break;

            yield return new WaitForSeconds(drawInterval);
        }
    }
}