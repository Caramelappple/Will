using System;
using UnityEngine;
using UnityEngine.UI;

namespace _Scripts.LSO.UI.Panel
{
    public class LSO_OptionSelection : MonoBehaviour
    {
        [SerializeField] private GameObject option1;
        private Toggle option1Toggle;
    
        [SerializeField] private GameObject option2;
        private Toggle option2Toggle;


        private void Awake()
        {
            option1Toggle = option1.GetComponentInChildren<Toggle>();
            option2Toggle = option2.GetComponentInChildren<Toggle>();
        
            option2Toggle.isOn = true;
            option1Toggle.isOn = !option2Toggle.isOn;
        }

        public void ChangeToWindow(bool isOn)
        {
            if (isOn)
                Screen.fullScreenMode = FullScreenMode.Windowed;
        }
    
        public void ChangeToFullScreen(bool isOn)
        {
           if (isOn)
               Screen.fullScreenMode = FullScreenMode.FullScreenWindow;
        }
    }
}
