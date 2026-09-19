using _Scripts.LSO.Animal.Data;
using _Scripts.LSO.Interfaces;
using UnityEngine;

/// <summary>기존 공격 훅에 타격 순간의 월드 위치를 추가한다.</summary>
public interface DLJ_IOnAttackImpact : IOnAnimalAttack
{
    void OnAttack(LSO_AnimalSO animal, Vector3 impactPosition);
}
