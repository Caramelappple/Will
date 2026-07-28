using System;
using _Scripts.LDY;
using _Scripts.LSO.HealthSystem;
using UnityEngine;

public class LSO_HealthSystem : MonoBehaviour
{
    private LDY_Animal _animal;

    [field: SerializeField]
    public int Health { get; private set; }
    
    private int _maxHealth;

    private bool _isDead;

    public event Action<LSO_DamageData> OnDamage;
    public event Action<LDY_Animal> OnDeath;

    private void Awake()
    {
        _animal = GetComponent<LDY_Animal>();
    }

    private void Start()
    {
        /*if (_animal.animal == null)
        {
            Debug.LogError($"{name}: LSO_AnimalSO가 할당되지 않았습니다.", this);
            return;
        }*/

       // _maxHealth = _animal.animal.maxHealth;
        Health = _maxHealth;
    }

    public void GetDamage(LSO_DamageData data)
    {
        if (_isDead) return;
        
        Health = Mathf.Clamp(Health - data.damage, 0, _maxHealth);
        OnDamage?.Invoke(data);

        if (Health <= 0) Dead();
    }

    public void Heal(int heal)
    {
        if (_isDead) return;
        Health = Mathf.Clamp(Health + heal, 0, _maxHealth);
    }

    public void Dead()
    {
        if (_isDead) return;
        _isDead = true;
        OnDeath?.Invoke(_animal);
    }
    
    [ContextMenu("Damage")]
    private void DebugDamage()
    {
        GetDamage(LSO_DamageData.Create(_animal, 1));
    }
}