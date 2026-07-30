using _Scripts.LDY;
using _Scripts.LSO.CoreLib;
using _Scripts.LSO.Manager;

namespace _Scripts.LSO
{
    public class GameManager : MonoSingleton<GameManager>
    {
        public GameSaveData SaveData { get; private set; }
        public DeckModule Deck {get; private set;}
        public GameEventDispatcher EventDispatcher {get; private set;}
        public LDY_TurnManager TurnManager {get; private set;}

        protected override void Awake()
        {
            base.Awake();
           
            if (Instance != this) return;
            
            DontDestroyOnLoad(gameObject);
        }
    }
}