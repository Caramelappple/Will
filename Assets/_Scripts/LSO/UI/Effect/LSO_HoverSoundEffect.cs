using UnityEngine;
using _Scripts.LSO.UI.Input;

namespace _Scripts.LSO.UI.Effect
{
    /// <summary>
    /// 커서가 올라갈 때(그리고 원하면 벗어날 때) 효과음을 낸다.
    ///
    /// 소리를 어떻게 재생하는지는 KTH_SoundManager가 안다.
    /// 여기서는 "언제" 낼지만 정하므로, 사운드 구현이 바뀌어도 이 파일은 그대로다.
    ///
    /// 사운드 매니저가 씬에 없으면 조용히 넘어간다. 연출이 없다고 UI가 멈추면 안 된다.
    /// </summary>
    [RequireComponent(typeof(LSO_ButtonHoverHandler))]
    public class LSO_HoverSoundEffect : MonoBehaviour, LSO_IHoverEffect
    {
        [Tooltip("커서가 올라갈 때 재생할 효과음.")]
        [SerializeField] private SfxID enterSfx = SfxID.UIHover;

        [Tooltip("커서가 벗어날 때도 소리를 낼지. 보통은 꺼두는 편이 덜 시끄럽다.")]
        [SerializeField] private bool playOnExit;

        [SerializeField] private SfxID exitSfx = SfxID.UIHover;

        [Tooltip("이 간격 안에는 다시 재생하지 않는다.\n" +
                 "버튼 경계에서 커서가 떨릴 때 소리가 드르륵 이어지는 걸 막는다.")]
        [SerializeField, Min(0f)] private float cooldown = 0.05f;

        private float _lastPlayTime = float.NegativeInfinity;

        public void OnHoverEnter()
        {
            Play(enterSfx);
        }

        public void OnHoverExit()
        {
            if (!playOnExit) return;

            Play(exitSfx);
        }

        private void Play(SfxID id)
        {
            if (Time.unscaledTime - _lastPlayTime < cooldown) return;

            KTH_SoundManager manager = KTH_SoundManager.Instance;
            if (manager == null) return;

            _lastPlayTime = Time.unscaledTime;
            manager.PlaySfx(id);
        }
    }
}
