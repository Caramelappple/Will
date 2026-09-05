using System.Collections.Generic;
using _Scripts.LSO.UI.Input;
using UnityEngine;

namespace _Scripts.LSO.Reward
{
    /// <summary>
    /// 지금 무엇이 눌려도 되는지에 따라 클릭 핸들러를 여닫는다.
    ///
    /// 단계를 모른다. 부르는 쪽이 판단해서 결과만 넘긴다 —
    /// 여기까지 단계를 알면 "지금 눌려도 되나"를 두 곳에서 판단하게 된다.
    ///
    /// 콜라이더는 건드리지 않는다. 끄면 뒤에 있는 것이 대신 눌린다 —
    /// 눌러도 아무 일이 없는 편이 예측 가능하다. LSO_TurnClickGate와 같은 규칙이다.
    ///
    /// 핸들러를 끄면 LSO_HoverCursorEffect가 그 값을 보고 커서를 Blocked로 바꾼다.
    /// 그래서 "못 누른다"가 화면에 드러난다.
    /// </summary>
    public sealed class LSO_RewardClickGate
    {
        private readonly LSO_ButtonClickHandler _box;

        public LSO_RewardClickGate(LSO_ButtonClickHandler box)
        {
            _box = box;
        }

        /// <summary>상자를 눌러도 되는지.</summary>
        public void SetBox(bool open)
        {
            Apply(_box, open);
        }

        /// <summary>꺼내둔 카드를 눌러도 되는지.</summary>
        public void SetCards(IReadOnlyList<LSO_RewardCard> cards, bool open)
        {
            if (cards == null) return;

            foreach (LSO_RewardCard card in cards)
            {
                if (card == null) continue;

                Apply(card.GetComponent<LSO_ButtonClickHandler>(), open);
            }
        }

        /// <summary>메모장을 눌러도 되는지. 없으면 아무 일도 하지 않는다.</summary>
        public void SetNote(LSO_RewardCard note, bool open)
        {
            if (note == null) return;

            Apply(note.GetComponent<LSO_ButtonClickHandler>(), open);
        }

        /// <summary>
        /// 값이 그대로면 건드리지 않는다. 매 프레임 껐다 켜면
        /// OnEnable/OnDisable이 돌아 다른 것들이 함께 반응한다.
        /// </summary>
        private static void Apply(LSO_ButtonClickHandler handler, bool open)
        {
            if (handler == null) return;

            if (handler.enabled != open) handler.enabled = open;
        }

        /// <summary>
        /// 상자 클릭을 나눠 갖는 것이 또 있는지 본다.
        ///
        /// 클릭 핸들러는 그 오브젝트의 LSO_IClickEffect를 전부 부른다.
        /// 상자에 LSO_ClickRelay 같은 것이 함께 붙어 뚜껑을 여닫게 걸어두면,
        /// 한 번의 클릭이 절차와 뚜껑을 따로 움직여 "메모장이 올라오는데 뚜껑이 닫히는"
        /// 어긋난 화면이 나온다. 뚜껑을 여닫는 주체는 상자 하나여야 한다.
        /// </summary>
        public static void WarnIfShared(MonoBehaviour owner)
        {
            if (owner == null) return;

            LSO_IClickEffect[] effects = owner.GetComponents<LSO_IClickEffect>();

            foreach (LSO_IClickEffect effect in effects)
            {
                if (ReferenceEquals(effect, owner)) continue;

                Debug.LogWarning(
                    $"{owner.name}: 상자에 클릭 반응이 하나 더 붙어 있습니다 ({effect.GetType().Name}). " +
                    "한 번의 클릭이 두 곳으로 갑니다 — 뚜껑을 따로 여닫게 걸어뒀다면 떼세요.",
                    owner);
            }
        }
    }
}
