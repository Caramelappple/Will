using System.Collections.Generic;
using _Scripts.LSO.HealthSystem;

namespace _Scripts.LSO.Ability
{
    /// <summary>
    /// 특성을 바깥 시스템에 등록하고 해제하는 유일한 지점.
    ///
    /// 예전에는 훅마다 등록처가 달라서(체력 / 이벤트 디스패처 / 호출부 직접 순회)
    /// 새 훅을 만들 때마다 "이건 어디에 붙이지"를 매번 정해야 했고,
    /// 등록·해제 코드가 LDY_Animal과 Health, 디스패처에 흩어져 있었다.
    ///
    /// 밀어 넣는 쪽(등록형) 훅은 전부 여기서 처리한다.
    /// 꺼내 쓰는 쪽(순회형) 훅은 LSO_AbilityNotify가 맡는다.
    ///
    /// 새 등록형 훅을 추가할 때 고칠 곳은 이 파일 하나다.
    /// </summary>
    public static class LSO_AbilityWiring
    {
        /// <param name="health">특성이 피해 계산에 끼어들 대상. null이면 그 부분만 건너뛴다.</param>
        /// <param name="dispatcher">턴·사망 같은 전역 이벤트 통로. null이면 그 부분만 건너뛴다.</param>
        public static void Bind(
            IReadOnlyList<LSO_IAbility> abilities,
            DamageableResources health,
            GameEventDispatcher dispatcher)
        {
            if (abilities == null) return;

            for (int i = 0; i < abilities.Count; i++)
            {
                LSO_IAbility ability = abilities[i];
                if (ability == null) continue;

                if (health != null && ability is LSO_IDamageModifier modifier)
                    health.AddDamageModifier(modifier);

                // 디스패처는 자기가 아는 인터페이스만 걸러 담는다.
                dispatcher?.Register(ability);
            }
        }

        public static void Unbind(
            IReadOnlyList<LSO_IAbility> abilities,
            DamageableResources health,
            GameEventDispatcher dispatcher)
        {
            if (abilities == null) return;

            for (int i = 0; i < abilities.Count; i++)
            {
                LSO_IAbility ability = abilities[i];
                if (ability == null) continue;

                if (health != null && ability is LSO_IDamageModifier modifier)
                    health.RemoveDamageModifier(modifier);

                dispatcher?.Unregister(ability);
            }
        }
    }
}
