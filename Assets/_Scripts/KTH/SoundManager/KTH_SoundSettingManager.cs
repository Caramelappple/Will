using UnityEngine;
using UnityEngine.UI;

public class KTH_SoundSettingManager : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private GameObject settingPanel;
    [SerializeField] private Slider masterSlider;
    [SerializeField] private Slider bgmSlider;
    [SerializeField] private Slider sfxSlider;

    private void Start()
    {
        settingPanel.SetActive(false);

        masterSlider.value = 1f;
        bgmSlider.value = 1f;
        sfxSlider.value = 1f;

        masterSlider.onValueChanged.AddListener(OnMasterVolumeChanged);
        bgmSlider.onValueChanged.AddListener(OnBGMVolumeChanged);
        sfxSlider.onValueChanged.AddListener(OnSFXVolumeChanged);
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            settingPanel.SetActive(!settingPanel.activeSelf);
        }
    }

    private void OnMasterVolumeChanged(float value)
    {
        KTH_SoundManager.Instance.SetMasterVolume(value);
    }

    private void OnBGMVolumeChanged(float value)
    {
        KTH_SoundManager.Instance.SetBgmVolume(value);
    }

    private void OnSFXVolumeChanged(float value)
    {
        KTH_SoundManager.Instance.SetSfxVolume(value);
    }
}