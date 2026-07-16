using System;
using _Scripts.LSO;
using _Scripts.LSO.HealthSystem;
using UnityEngine;

[RequireComponent(typeof(LSO_AnimalBase))]
public class LSO_HealthSystem : MonoBehaviour
{
    private LSO_AnimalBase _animalBase;
    
    
    [field: SerializeField]
   public int Health { get; private set; }
    [Header("Health는 SO에 있는거 가져옴")]
   [SerializeField]private int maxHealth;
   
   private bool _isDead;
   public event Action<LSO_DamageData> OnDamage;
   
   private void Start()
   {
       _animalBase = GetComponent<LSO_AnimalBase>();
       maxHealth = _animalBase.animal.maxHealth;
       Health = maxHealth;
   }
   
   public event Action<LSO_AnimalBase> OnDeath;

   public void Dead()
   {
       if (_isDead) return;
       _isDead = true;
       OnDeath?.Invoke(_animalBase);
   }

   private void OnEnable()
   {
       OnDamage += data => Debug.Log($"{gameObject.name} Damaged {data.damage} by {data.giver.gameObject.name}");
   }

   public void GetDamage(LSO_DamageData data)
   {
       if (_isDead) return;
       
       Health -= data.damage;
       OnDamage?.Invoke(data);
       Health = Mathf.Clamp(Health, 0, maxHealth);

       if (Health <= 0)
       {
          Dead();
       }
   }
    
   [ContextMenu("Damage")]
   private void GetDamage()
   {
       GetDamage(LSO_DamageData.Create(this,1));
   }

   public void Heal(int heal)
   {
       if (_isDead) return;
       
       Health += heal;
       Health = Mathf.Clamp(Health, 0, maxHealth);
   }
}
