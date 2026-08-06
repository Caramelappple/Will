using System;
using _Scripts.LSO.Deck.Data;
using UnityEngine;

/// <summary>
/// 보드에 배치된 기물의 표시. 클릭되면 콜백으로 알리기만 한다.
/// </summary>
[RequireComponent(typeof(BoxCollider))]
public class KTH_PlacedUnitView : MonoBehaviour
{
    [Tooltip("3D 모델이 생성될 위치. 비워두면 이 오브젝트 자체를 부모로 사용")]
    [SerializeField] private Transform modelSpawnPoint;

    private LSO_CardSO _data;
    private Action<LSO_CardSO> _onClicked;
    private GameObject _modelInstance;

    public LSO_CardSO Data => _data;

    /// <param name="onClicked">클릭됐을 때 알릴 대상. 뷰는 누가 듣는지 알 필요가 없다.</param>
    public void Setup(LSO_CardSO cardData, Action<LSO_CardSO> onClicked)
    {
        _data = cardData;
        _onClicked = onClicked;

        if (cardData == null || !cardData.IsValid)
        {
            Debug.LogWarning("[KTH_PlacedUnitView] 카드에 동물 데이터가 없습니다.", cardData);
            return;
        }

        if (cardData.UnitPrefab == null)
        {
            Debug.LogWarning(
                $"[KTH_PlacedUnitView] {cardData.AnimalName}의 AnimalSO에 unitPrefab이 비어있음", cardData);
            return;
        }

        Transform parent = modelSpawnPoint != null ? modelSpawnPoint : transform;
        _modelInstance = Instantiate(cardData.UnitPrefab, parent);
        _modelInstance.transform.localPosition = Vector3.zero;
        _modelInstance.transform.localRotation = Quaternion.identity;
    }

    private void OnMouseDown()
    {
        _onClicked?.Invoke(_data);
    }
}
