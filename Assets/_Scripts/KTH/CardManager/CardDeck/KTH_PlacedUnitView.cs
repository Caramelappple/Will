using _Scripts.LSO.Deck.Data;
using TMPro;
using UnityEngine;

[RequireComponent(typeof(BoxCollider))]
public class KTH_PlacedUnitView : MonoBehaviour
{
    [Tooltip("3D 모델이 생성될 위치. 비워두면 이 오브젝트 자체를 부모로 사용")]
    public Transform modelSpawnPoint;

    private LSO_CardSO data;
    private KTH_DeckManager manager;
    private GameObject modelInstance;

    public void Setup(LSO_CardSO cardData, KTH_DeckManager deckManager)
    {
        data = cardData;
        manager = deckManager;

        if (cardData == null || !cardData.IsValid)
        {
            Debug.LogWarning("[KTH_PlacedUnitView] 카드에 동물 데이터가 없습니다.", cardData);
            return;
        }

        if (cardData.UnitPrefab != null)
        {
            Transform parent = modelSpawnPoint != null ? modelSpawnPoint : transform;
            modelInstance = Instantiate(cardData.UnitPrefab, parent);
            modelInstance.transform.localPosition = Vector3.zero;
            modelInstance.transform.localRotation = Quaternion.identity;
        }
        else
        {
            Debug.LogWarning(
                $"[KTH_PlacedUnitView] {cardData.AnimalName}의 AnimalSO에 unitPrefab이 비어있음", cardData);
        }
    }

    public LSO_CardSO GetData() => data;

    private void OnMouseDown()
    {
        if (manager != null) manager.ShowPlacedUnitInfo(data);
    }
}
