using System;
using System.Collections.Generic;
using _Scripts.LSO.Deck.Data;
using UnityEngine;

namespace _Scripts.LSO.Will
{
    /// <summary>
    /// 유언 선택 창을 찾아 쓰는 지점.
    ///
    /// 선택 UI는 전투 씬에만 있고, 스테이지 초기 배치나 적 소환처럼
    /// UI를 띄우면 안 되는 경로도 같은 소환 코드를 지나간다.
    /// 그래서 "UI가 없으면 즉시 기본값으로 진행"을 여기서 한 번에 처리한다.
    ///
    /// 이게 없으면 호출부마다 null 검사를 반복하게 되고,
    /// 한 곳이라도 빠뜨리면 소환이 영영 멈춘 채로 남는다.
    /// </summary>
    public static class LSO_WillSelection
    {
        /// <summary>씬의 선택 UI. 구현체가 켜질 때 스스로 등록한다.</summary>
        public static LSO_IWillSelector Current { get; private set; }

        public static bool HasSelector => Current != null;

        /// <summary>
        /// 지금 플레이어의 답을 기다리는 중인지.
        ///
        /// 창이 떠 있는 동안 보드를 만지면 뒤에서 이동·공격·소환이 일어난다.
        /// 조작을 받는 쪽은 이 값을 보고 클릭을 흘려보낼 것.
        /// </summary>
        public static bool IsSelecting => Current is { IsSelecting: true };

        /// <summary>
        /// 해금된 유언을 돌려주는 함수. 해금 시스템을 가진 쪽이 등록한다.
        ///
        /// 이 자리에 함수를 두는 이유는 소환 코드가 해금 시스템을 직접 참조하지 않게 하기 위해서다.
        /// 등록되지 않으면 호출부가 자기 기본 목록을 쓴다.
        /// </summary>
        public static Func<IReadOnlyList<LSO_WillType>> UnlockedWillsProvider { get; set; }

        /// <summary>해금된 유언. 공급자가 없으면 null.</summary>
        public static IReadOnlyList<LSO_WillType> UnlockedWills =>
            UnlockedWillsProvider?.Invoke();

        public static void Register(LSO_IWillSelector selector)
        {
            if (selector == null) return;

            Current = selector;
        }

        public static void Unregister(LSO_IWillSelector selector)
        {
            if (!ReferenceEquals(Current, selector)) return;

            Current = null;
        }

        /// <summary>
        /// 유언을 고르게 한다. 고를 수 없는 상황이면 fallback으로 즉시 진행한다.
        /// </summary>
        /// <param name="fallback">
        /// UI가 없거나 선택지가 없을 때 쓸 유언.
        /// 적 기물이나 스테이지 초기 배치처럼 플레이어가 개입하지 않는 경로에서 쓰인다.
        /// </param>
        public static void Request(
            LSO_CardSO card,
            IReadOnlyList<LSO_WillType> options,
            LSO_WillType fallback,
            Action<LSO_WillType> onSelected,
            Action onCancelled = null)
        {
            if (onSelected == null) return;

            // 선택지가 하나뿐이면 물어볼 이유가 없다. 창이 뜨는 것 자체가 방해다.
            if (options != null && options.Count == 1)
            {
                onSelected(options[0]);
                return;
            }

            if (Current == null || options == null || options.Count == 0)
            {
                onSelected(fallback);
                return;
            }

            Current.Request(card, options, onSelected, onCancelled ?? DoNothing);
        }

        /// <summary>진행 중인 선택을 중단한다. 턴이 넘어가거나 씬이 바뀔 때 부른다.</summary>
        public static void Abort()
        {
            if (Current is { IsSelecting: true })
                Current.Abort();
        }

        private static void DoNothing()
        {
        }

        /// <summary>
        /// 정적 필드는 씬을 다시 로드해도 남는다.
        /// 파괴된 UI가 그대로 꽂혀 있으면 다음 씬에서 소환이 멈춘다.
        /// </summary>
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetOnLoad()
        {
            Current = null;
            UnlockedWillsProvider = null;
        }
    }
}
