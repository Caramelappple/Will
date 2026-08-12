using System.Collections.Generic;
using _Scripts.LDY;
using UnityEngine;

namespace _Scripts.LSO.Manager
{
    public class GameEventDispatcher : MonoBehaviour
    {
        private readonly List<IOnTurnStart> _onTurnStart = new();
        private readonly List<IOnEnemyDead> _onEnemyDead = new();

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
            if (obj is IOnEnemyDead d && !_onEnemyDead.Contains(d)) _onEnemyDead.Add(d);
        }

        public void Unregister(object obj)
        {
            if (obj is IOnTurnStart s) _onTurnStart.Remove(s);
            if (obj is IOnEnemyDead d) _onEnemyDead.Remove(d);
        }

        public void RaiseTurnStart(LDY_Team team)
        {
            // 순회 중 리스트가 바뀔 수 있으므로 복사본으로 순회
            foreach (var l in _onTurnStart.ToArray())
                l.OnTurnStart(team);
        }

        public void RaiseEnemyDead(LDY_Animal info)
        {
            foreach (var l in _onEnemyDead.ToArray())
                l.OnEnemyDead(info);
        }
    }
}
