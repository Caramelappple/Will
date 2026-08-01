namespace _Scripts.LSO.UI
{
    /// <summary>
    /// 커서가 올라가면 잠깐 축소했다가 원래 크기로 되돌린다.
    /// 연출 자체는 LSO_ScalePunchEffectBase가 담당하고, 여기서는 호버 트리거만 연결한다.
    /// </summary>
    public class LSO_HoverScaleEffect : LSO_ScalePunchEffectBase, LSO_IHoverEffect
    {
        public void OnHoverEnter()
        {
            Play();
        }

        // 축소 → 복귀가 한 번에 끝나는 연출이라 이탈 시 할 일이 없다.
        public void OnHoverExit() { }
    }
}
