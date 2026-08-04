using UnityEngine;
using UnityEngine.InputSystem;

public class KTH_SoundTest : MonoBehaviour
{
    private void Start()
    {
        KTH_SoundManager.Instance.PlayBgm(BgmID.Stage1);
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Space))
        {
            KTH_SoundManager.Instance.PlaySfx(SfxID.Hit);
        }
    }
}