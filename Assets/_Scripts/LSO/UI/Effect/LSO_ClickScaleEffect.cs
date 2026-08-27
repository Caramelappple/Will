using _Scripts.LSO.UI.Input;
namespace _Scripts.LSO.UI.Effect
{
    /// <summary>
    /// 클릭하면 잠깐 축소했다가 원래 크기로 되돌린다.
    /// 연출 자체는 LSO_ScalePunchEffectBase가 담당하고, 여기서는 클릭 트리거만 연결한다.
    /// </summary>
    public class LSO_ClickScaleEffect : LSO_ScalePunchEffectBase, LSO_IClickEffect
    {
        public void OnClick()
        {
            Play();
        }
    }
}
