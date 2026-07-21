using UnityEngine;

namespace _Scripts.LSO.Animal
{
    public class LSO_Animal  : MonoBehaviour
    {
        public LSO_AnimalSO animal;
        
        public LSO_AnimalLoc animalLoc;

        public void Init(LSO_AnimalLoc loc)
        {
            animalLoc.loc.x = loc.loc.x; 
            animalLoc.loc.y = loc.loc.y;
        }
    }
}