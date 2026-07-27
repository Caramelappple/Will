using TMPro;
using UnityEngine;

[RequireComponent(typeof(BoxCollider))]
public class KTH_PlacedUnitView : MonoBehaviour
{
    [Tooltip("3D 모델이 생성될 위치. 비워두면 이 오브젝트 자체를 부모로 사용")]
    public Transform modelSpawnPoint;

    private KTH_CardData data;
    private KTH_DeckManager manager;
    private GameObject modelInstance;

    public void Setup(KTH_CardData cardData, KTH_DeckManager deckManager)
    {
        data = cardData;
        manager = deckManager;

        if (cardData.unitModelPrefab != null)
        {
            Transform parent = modelSpawnPoint != null ? modelSpawnPoint : transform;
            modelInstance = Instantiate(cardData.unitModelPrefab, parent);
            modelInstance.transform.localPosition = Vector3.zero;
            modelInstance.transform.localRotation = Quaternion.identity;
        }
        else
        {
            Debug.LogWarning($"[KTH_PlacedUnitView] {cardData.cardName} 카드에 unitModelPrefab이 비어있음");
        }
    }

    public KTH_CardData GetData() => data;

    private void OnMouseDown()
    {
        if (manager != null) manager.ShowPlacedUnitInfo(data);
    }
}
