using System.Collections.Generic;
using _Scripts.LDY;
using UnityEngine;
using _Scripts.LSO.Interfaces;

namespace _Scripts.LSO.Manager
{
    public class GameEventDispatcher : MonoBehaviour
    {
        private readonly List<IOnTurnStart> _onTurnStart = new();
        private readonly List<LSO_IOnAnimalDead> _onAnimalDead = new();

        private LDY_TurnManager _boundTurnManager;

        private void Awake()
        {
            // GameManager의 자식/컴포넌트로 붙는 경우엔 부모가 이미 DontDestroyOnLoad 대상이다.
            if (transform.parent == null)
                DontDestroyOnLoad(gameObject);
        }

        private void OnEnable()
        {
            GameManager gameManager = GameManager.Instance;
            if (gameManager == null) return;

            gameManager.TurnManagerChanged += BindTurnManager;
            BindTurnManager(gameManager.TurnManager);
        }

        private void OnDisable()
        {
            if (GameManager.HasInstance)
                GameManager.Instance.TurnManagerChanged -= BindTurnManager;

            BindTurnManager(null);
        }

        /// <summary>씬마다 바뀌는 턴 매니저를 안전하게 갈아끼운다. 이전 구독은 반드시 해제한다.</summary>
        private void BindTurnManager(LDY_TurnManager turnManager)
        {
            if (_boundTurnManager == turnManager) return;

            if (_boundTurnManager != null)
                _boundTurnManager.OnTurnChanged -= RaiseTurnStart;

            _boundTurnManager = turnManager;

            if (_boundTurnManager != null)
                _boundTurnManager.OnTurnChanged += RaiseTurnStart;
        }

        public void Register(object obj)
        {
            if (obj is IOnTurnStart s && !_onTurnStart.Contains(s)) _onTurnStart.Add(s);
            if (obj is LSO_IOnAnimalDead d && !_onAnimalDead.Contains(d)) _onAnimalDead.Add(d);
        }

        public void Unregister(object obj)
        {
            if (obj is IOnTurnStart s) _onTurnStart.Remove(s);
            if (obj is LSO_IOnAnimalDead d) _onAnimalDead.Remove(d);
        }

        // 두 Raise 모두 매번 배열을 새로 만든다. 재사용 버퍼로 바꾸면 안 된다.
        // 알림을 받은 특성이 다시 죽음을 일으켜(복수 → 처치 → RaiseAnimalDead) 같은 메서드로
        // 되들어오는 경로가 있어서, 버퍼를 공유하면 바깥쪽 순회가 안쪽 호출에 덮인다.
        public void RaiseTurnStart(LDY_Team team)
        {
            foreach (var l in _onTurnStart.ToArray())
                l.OnTurnStart(team);
        }

        public void RaiseAnimalDead(LDY_Animal info)
        {
            foreach (var l in _onAnimalDead.ToArray())
                l.OnAnimalDead(info);
        }
    }
}
