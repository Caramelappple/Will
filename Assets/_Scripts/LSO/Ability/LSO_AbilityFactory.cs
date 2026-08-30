using System;
using System.Collections.Generic;
using UnityEngine;

namespace _Scripts.LSO.Ability
{
    /// <summary>
    /// 특성 종류(enum)로 특성 인스턴스를 만드는 곳.
    ///
    /// 예전에는 이 파일이 구체 특성 26개를 직접 new 했다. 그래서 특성 하나를 쓰려는
    /// 코드가 까마귀왕·황소왕·여우왕·상어왕 구현을 전부 끌고 들어왔고,
    /// 결국 "순수 인터페이스 -> LDY_Animal -> 이 팩토리 -> 특성 전체 -> 다시 인터페이스"라는
    /// 고리가 생겨 어셈블리를 나눌 수 없었다.
    ///
    /// 지금은 표를 비워 두고 구현 쪽이 자기를 등록한다(LSO_AbilityRegistry).
    /// 이 파일은 어떤 특성이 존재하는지 몰라도 된다.
    /// </summary>
    public static class LSO_AbilityFactory
    {
        // 특성은 개체마다 상태(발동 여부, 누적 수치 등)를 가질 수 있으므로
        // 완성된 인스턴스가 아니라 "생성 방법"을 등록한다. 인스턴스를 공유하면 상태가 섞인다.
        private static readonly Dictionary<LSO_AbilityType, Func<LSO_IAbility>> Creators = new();

        /// <summary>
        /// 특성 하나의 생성 방법을 등록한다. 같은 종류를 다시 등록하면 덮어쓴다.
        ///
        /// 덮어쓰기로 둔 이유는 Reload Domain을 끈 에디터 때문이다.
        /// static인 이 표가 플레이를 멈춰도 살아남으므로, 두 번째 실행에서 다시 등록될 때
        /// Add였다면 중복 키로 터진다.
        /// </summary>
        public static void Register(LSO_AbilityType type, Func<LSO_IAbility> creator)
        {
            if (type == LSO_AbilityType.None)
            {
                Debug.LogWarning("LSO_AbilityFactory: None은 등록할 수 없습니다.");
                return;
            }

            if (creator == null)
            {
                Debug.LogWarning($"LSO_AbilityFactory: '{type}'의 생성기가 null입니다.");
                return;
            }

            Creators[type] = creator;
        }

        /// <summary>등록표를 비운다. 등록 담당이 다시 채우기 직전에만 부른다.</summary>
        public static void Clear()
        {
            Creators.Clear();
        }

        /// <summary>
        /// 특성 하나를 만든다. None이면 조용히 null을 돌려준다.
        ///
        /// 등록되지 않은 값을 고르면 경고를 낸다.
        /// enum에는 있는데 표에 없는 항목이 있어서, 예전에는 인스펙터에서 고르고도
        /// 아무 일이 일어나지 않는 이유를 알 방법이 없었다.
        /// </summary>
        public static LSO_IAbility Create(LSO_AbilityType type)
        {
            if (type == LSO_AbilityType.None) return null;

            if (Creators.TryGetValue(type, out Func<LSO_IAbility> creator))
                return creator();

            // 표가 통째로 비어 있으면 개별 특성이 빠진 게 아니라 등록 자체가 안 돈 것이다.
            // 두 경우의 원인이 전혀 다르므로 메시지를 나눈다.
            if (Creators.Count == 0)
            {
                Debug.LogWarning(
                    $"LSO_AbilityFactory: 등록표가 비어 있어 '{type}'을(를) 만들지 못했습니다. " +
                    $"LSO_AbilityRegistry가 실행되지 않았습니다.");

                return null;
            }

            Debug.LogWarning(
                $"LSO_AbilityFactory: '{type}'은(는) 구현체가 등록되지 않아 붙지 않습니다. " +
                $"에셋에서 다른 특성으로 바꾸거나 LSO_AbilityRegistry에 등록하세요.");

            return null;
        }
    }
}
