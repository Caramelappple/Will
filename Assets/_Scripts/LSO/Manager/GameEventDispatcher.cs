using System;
using System.Collections.Generic;
using _Scripts.LDY;
using _Scripts.LSO;
using UnityEngine;

public class GameEventDispatcher : MonoBehaviour
{
    private readonly List<IOnTurnStart> _onTurnStart = new();
    private readonly List<IOnEnemyDead> _onEnemyDead = new();

    private void Awake()
    {
        DontDestroyOnLoad(gameObject);
    }

    private void OnEnable()
    {
        GameManager.Instance.TurnManager.OnTurnChanged += RaiseTurnStart;
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

    public void RaiseEnemyDead(LSO_AnimalSO info)
    {
        foreach (var l in _onEnemyDead.ToArray())
            l.OnEnemyDead(info);
    }
}