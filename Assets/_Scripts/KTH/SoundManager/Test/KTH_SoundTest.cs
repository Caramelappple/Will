using UnityEngine;

public class KTH_SoundTest : MonoBehaviour
{
    private void Start()
    {
        // SoundManager 인스턴스가 완전히 세팅된 후 호출되도록 보장
        if (KTH_SoundManager.Instance != null)
        {
            KTH_SoundManager.Instance.PlayBgm(BgmID.Stage1);
        }
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Space))
        {
            if (KTH_SoundManager.Instance != null)
            {
                KTH_SoundManager.Instance.PlaySfx(SfxID.Hit);
            }
        }
    }
}