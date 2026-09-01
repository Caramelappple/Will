using UnityEngine;

/// <summary>코인 하나와 원래 슬롯 Transform 정보를 한 단위로 보관한다.</summary>
public sealed class DLJ_CostCoinSlot
{
    public Transform Coin { get; }
    public Transform HomeParent { get; }
    public Vector3 RestLocalPosition { get; }
    public Quaternion RestLocalRotation { get; }
    public Vector3 RestLocalScale { get; }

    public bool IsValid => Coin != null;

    public DLJ_CostCoinSlot(Transform coin)
    {
        Coin = coin;
        HomeParent = coin != null ? coin.parent : null;
        RestLocalPosition = coin != null ? coin.localPosition : Vector3.zero;
        RestLocalRotation = coin != null ? coin.localRotation : Quaternion.identity;
        RestLocalScale = coin != null ? coin.localScale : Vector3.one;
    }

    public Vector3 GetRestWorldPosition()
    {
        return HomeParent != null
            ? HomeParent.TransformPoint(RestLocalPosition)
            : RestLocalPosition;
    }

    public void DetachTo(Transform parent)
    {
        if (!IsValid || parent == null || Coin.parent == parent) return;
        Coin.SetParent(parent, true);
    }

    public void Restore(bool active)
    {
        if (!IsValid) return;

        Coin.SetParent(HomeParent, false);
        Coin.localPosition = RestLocalPosition;
        Coin.localRotation = RestLocalRotation;
        Coin.localScale = RestLocalScale;
        Coin.gameObject.SetActive(active);
    }
}
