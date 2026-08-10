using _Scripts.LDY;
using UnityEngine;

namespace _Scripts.LSO.UI
{
    [RequireComponent(typeof (LDY_Animal))]
    public class LSO_StatBillBoardSpawner : MonoBehaviour
    {
        [SerializeField] public GameObject prefab;
        private GameObject _textObject;
        private LSO_StatBillboard _billboard;
        
        private LDY_Animal _animal;
        
        private void Awake()
        {
            _animal = GetComponent<LDY_Animal>();
            _textObject = Instantiate(prefab, transform);
            _billboard = _textObject.GetComponent<LSO_StatBillboard>();
        }

        private void Start()
        {
            _textObject.transform.localPosition = new Vector3(0f, 0.225f, -0.4f);
            _textObject.transform.localScale = new Vector3(1.6f, 1.6f, 1.6f);
            
            //_billboard = _textObject.GetComponent<LSO_StatBillboard>();
        }

        private void OnEnable()
        {
            GameManager.Instance.Board.OnBoardChanged += () => _billboard.SetAtkText(_animal.GetAtk());
            _animal.health.OnRecover += (_) => _billboard.SetAtkText(_animal.health.Value);
            _animal.health.OnDamage += (_) => _billboard.SetAtkText(_animal.health.Value);
        }

        private void OnDisable()
        {
            if (GameManager.Instance.Board != null)
                GameManager.Instance.Board.OnBoardChanged -= () => _billboard.SetAtkText(_animal.GetAtk());
            if (_animal.health == null || _animal == null) return;
            _animal.health.OnRecover -= (_)  => _billboard.SetAtkText(_animal.health.Value);
            _animal.health.OnDamage -= (_) => _billboard.SetAtkText(_animal.health.Value);
        }
    }
}