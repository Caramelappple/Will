using System;
using System.Collections.Generic;
using System.Linq;
using _Scripts.LSO.Ability;
using _Scripts.LSO.Animal.Data;
using _Scripts.LSO.CoreLib;
using _Scripts.LSO.Deck.Data;
using _Scripts.LSO.Will;
using TMPro;
using UnityEngine;
using _Scripts.LSO.UI.Input;

namespace _Scripts.LSO.Reward
{
    /// <summary>
    /// 보상 카드 한 장. 받은 내용을 그리고, 눌리면 알려준다.
    ///
    /// 자기 자리를 정하지 않는다. 어디에 놓일지는 LSO_RewardBox만 안다.
    /// 어느 카드가 골라졌는지도 기억하지 않는다. 그건 상자의 몫이다.
    ///
    /// 카드가 스스로 상태를 갖기 시작하면 상자가 아는 것과 어긋난다.
    /// 턴 레버에서 같은 실수를 한 적이 있다 — 자리를 정하는 주체가 셋이었다.
    ///
    /// 텍스트 슬롯은 전부 선택 사항이다. 비워두면 그 칸만 건너뛴다.
    /// 기물과 유언이 같은 다섯 칸을 나눠 쓴다.
    ///   기물  ATK / HP / 이동 / 사거리 / 특성
    ///   유언  피해량 / 범위 / 지속시간 / 버프 / 디버프
    ///
    /// 씬 배선: Collider + LSO_ButtonClickHandler 와 함께 붙일 것.
    /// 호버 반응이 필요하면 LSO_HoverMoveEffect 등을 따로 얹으면 된다.
    /// </summary>
    [RequireComponent(typeof(LSO_ButtonClickHandler))]
    public class LSO_RewardCard : MonoBehaviour, LSO_IClickEffect, LSO_IPoolable
    {
        [Header("공통")]
        [SerializeField] private SpriteRenderer iconImage;
        [SerializeField] private TMP_Text nameText;
        [SerializeField] private TMP_Text descriptionText;

        [Header("스탯 (기물·유언 공용)")]
        [Tooltip("둘 다 항목이 다섯 개라 같은 칸을 나눠 쓴다.\n" +
                 "기물: ATK / HP / 이동 / 사거리 / 특성\n" +
                 "유언: 피해량 / 범위 / 지속시간 / 버프 / 디버프")]
        [SerializeField] private TMP_Text[] statTexts = new TMP_Text[5];

        private LSO_RewardOption _option;
        private Action<LSO_RewardCard> _onClick;

        /// <summary>이 카드가 들고 있는 보상. 상자가 지급할 때 읽는다.</summary>
        public LSO_RewardOption Option => _option;

        /// <summary>
        /// 보여줄 내용과 눌렸을 때 알릴 곳을 받는다.
        ///
        /// 콜백을 인스펙터가 아니라 인자로 받는 이유는, 풀에서 재사용되기 때문이다.
        /// 인스펙터에 걸어두면 지난번 상자에 계속 연결된 채로 돌아온다.
        /// </summary>
        public void Bind(LSO_RewardOption option, Action<LSO_RewardCard> onClick)
        {
            _option = option;
            _onClick = onClick;

            if (option == null)
            {
                Debug.LogWarning($"{name}: 보상이 비어 있어 빈 카드로 둡니다.", this);
                Clear();
                return;
            }

            if (option.type == LSO_RewardType.Piece)
                DrawPiece(option.piece);
            else
                DrawWill(option.will);
        }

        public void OnClick()
        {
            // 이미 넘긴 뒤라면 아무것도 하지 않는다.
            // 한 번 클릭으로 확정되므로 두 번째 클릭이 들어올 틈이 짧게 있다.
            if (_onClick == null) return;

            Action<LSO_RewardCard> callback = _onClick;
            _onClick = null;

            callback(this);
        }

        private void DrawPiece(LSO_CardSO card)
        {
            LSO_AnimalSO animal = card != null ? card.Animal : null;

            if (animal == null)
            {
                SetText(nameText, "알 수 없는 기물");
                SetText(descriptionText, string.Empty);
                SetIcon(null);
                SetStats("ATK -", "HP -", "이동 -", "사거리 -", "특성 -");
                return;
            }

            SetText(nameText, animal.animalName);
            SetText(descriptionText, animal.description);
            SetIcon(card.Image);

            SetStats(
                $"ATK {animal.damage}",
                $"HP {animal.maxHealth}",
                $"이동 {animal.MoveRange}",
                $"사거리 {animal.range}",
                BuildTraitText(animal.AbilityTypes));
        }

        private void DrawWill(DLJ_WillDataSO will)
        {
            if (will == null)
            {
                SetText(nameText, "알 수 없는 유언");
                SetText(descriptionText, string.Empty);
                SetIcon(null);
                SetStats("피해량 -", "범위 -", "지속시간 -", string.Empty, string.Empty);
                return;
            }

            SetText(nameText, will.WillType.ToString());
            SetText(descriptionText, will.description);
            SetIcon(will.icon);

            // 버프·디버프는 0이면 칸을 비운다. "버프 : 0"은 효과가 있는 것처럼 읽힌다.
            SetStats(
                $"피해량 {will.DisplayDamage}",
                $"범위 {will.DisplayRange}",
                $"지속시간 {will.DisplayDuration}",
                will.DisplayBuffAmount != 0 ? $"버프 {will.DisplayBuffAmount}" : string.Empty,
                will.DisplayDebuffAmount != 0 ? $"디버프 {will.DisplayDebuffAmount}" : string.Empty);
        }

        private static string BuildTraitText(IReadOnlyList<LSO_AbilityType> abilities)
        {
            if (abilities == null || abilities.Count == 0) return "특성 없음";

            return "특성 " + string.Join(", ", abilities.Select(a => a.ToString()));
        }

        private void SetStats(params string[] values)
        {
            if (statTexts == null) return;

            for (int i = 0; i < statTexts.Length; i++)
                SetText(statTexts[i], i < values.Length ? values[i] : string.Empty);
        }

        private static void SetText(TMP_Text label, string value)
        {
            if (label != null) label.text = value;
        }

        private void SetIcon(Sprite sprite)
        {
            if (iconImage == null) return;

            iconImage.sprite = sprite;

            // 스프라이트가 없는데 켜두면 흰 사각형이 남는다.
            iconImage.enabled = sprite != null;
        }

        private void Clear()
        {
            SetText(nameText, string.Empty);
            SetText(descriptionText, string.Empty);
            SetIcon(null);
            SetStats(string.Empty, string.Empty, string.Empty, string.Empty, string.Empty);
        }

        // 풀에서 되살아난 카드다. 지난번 보상이 남아 있으면 엉뚱한 것이 지급된다.
        public void OnSpawned()
        {
            _option = null;
            _onClick = null;
        }

        public void OnDespawned()
        {
            _option = null;
            _onClick = null;
        }
    }
}
