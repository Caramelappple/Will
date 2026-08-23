using UnityEngine;

namespace _Scripts.LDY.UI
{
    /// <summary>
    /// "내 직계 자식이 바뀌었다"만 주인에게 알린다.
    ///
    /// OnTransformChildrenChanged는 직계 자식이 바뀐 그 오브젝트에서만 불린다.
    /// 캔버스 루트에만 달아두면 Starfield처럼 "중간 컨테이너 밑에" 런타임 생성되는
    /// 그래픽을 놓치므로, 픽셀라이저가 훑는 모든 트랜스폼에 이 중계기를 심는다.
    /// Update가 없어서 프레임 비용은 사실상 0이다.
    /// </summary>
    [AddComponentMenu("")]
    [DisallowMultipleComponent]
    internal sealed class LDY_UIPixelizerRelay : MonoBehaviour
    {
        private LDY_UIPixelizer owner;

        internal static LDY_UIPixelizerRelay Attach(Transform target, LDY_UIPixelizer owner)
        {
            if (!target.TryGetComponent(out LDY_UIPixelizerRelay relay))
            {
                relay = target.gameObject.AddComponent<LDY_UIPixelizerRelay>();
                relay.hideFlags = HideFlags.HideInInspector | HideFlags.DontSave;
            }

            relay.owner = owner;
            return relay;
        }

        internal void Detach(LDY_UIPixelizer requester)
        {
            if (owner != requester) return;
            owner = null;
            Destroy(this);
        }

        private void OnTransformChildrenChanged()
        {
            if (owner != null) owner.MarkDirty();
        }
    }
}
