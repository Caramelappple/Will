using UnityEngine;
using UnityEngine.UI;

namespace _Scripts.LSO.Sound
{
    [DisallowMultipleComponent, RequireComponent(typeof(Button))]
    public sealed class LSO_MenuButtonAudio : MonoBehaviour
    {
        private Button _button;
        private void OnEnable()
        {
            _button = GetComponent<Button>();
            _button.onClick.AddListener(Play);
        }
        private void OnDisable()
        {
            if (_button != null) _button.onClick.RemoveListener(Play);
        }
        // A preceding click listener may already have closed this menu panel.
        private void Play() { LSO_GameAudio.Play(LSO_SoundCue.MenuClick); }
    }
}
