using DevLib.ServiceLocator;
using DevLib.SoundSystem.Runtime;
using UnityEngine;
using UnityEngine.InputSystem;

namespace _Scripts.LSO.Stage
{
    public class LSO_SoundTest : MonoBehaviour
    {
        public SoundClipSO clip;

        private void Update()
        {
            if (Keyboard.current.tKey.wasPressedThisFrame)
            {
                ServiceLocator.Get<IAudioService>()?.PlaySfx(clip);
            }
        }
    }
}
