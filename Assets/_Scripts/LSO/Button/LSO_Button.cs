using UnityEngine;

namespace _Scripts.LSO.Button
{
    public class LSO_Button : MonoBehaviour
    {
        public LSO_ButtonSO buttonSO;

        public void InitButton(LSO_AnimalLoc loc, LSO_ButtonSO targetAnimal, LSO_)
        {
        }
        
        public void SendButtonData()
        {
            LSO_ButtonManager.Instance.GiveButtonData(LSO_AnimalLoc.Create(buttonSO.pos.x, buttonSO.pos.y), buttonSO.buttonType,buttonSO.targetAnimal,buttonSO.animal);
        }
    }
}