using UnityEngine;

namespace _Scripts.LSO.Animal
{
    public class LSO_Animal : MonoBehaviour
    {
        public LSO_AnimalSO animal;

        public LSO_AnimalLoc animalLoc;

        /// <summary>
        /// 동물의 보드 좌표를 설정한다. (불변 구조체이므로 통째로 대입)
        /// </summary>
        public void Init(LSO_AnimalLoc loc)
        {
            animalLoc = loc;
        }
    }
}