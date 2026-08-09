using TMPro;
using UnityEngine;

namespace _Scripts.LSO.UI.Stat
{
    /// <summary>
    /// 공격력/체력 숫자 표시. 값만 받아서 그린다.
    ///
    /// LDY_Animal을 모른다. 그래서 기물 머리 위뿐 아니라 카드 미리보기나 도감에서도 같은 프리팹을 쓸 수 있다.
    ///
    /// 텍스트 슬롯은 셋 다 선택 사항이라 표시 방식이 정해지지 않아도 된다.
    ///   - combinedText 하나만 채우면   "3/5" 한 줄
    ///   - atkText / hpText를 채우면    좌우로 나눈 배치
    /// 나중에 마음이 바뀌면 프리팹에서 슬롯만 옮기면 되고 코드는 그대로다.
    /// </summary>
    public class LSO_StatLabel : MonoBehaviour
    {
        [Header("한 줄로 붙일 때")]
        [Tooltip("{0}=공격력, {1}=체력, {2}=최대 체력")]
        [SerializeField] private TMP_Text combinedText;

        [SerializeField] private string combinedFormat = "{0}/{1}";

        [Header("나눠 배치할 때")]
        [SerializeField] private TMP_Text atkText;
        [SerializeField] private TMP_Text hpText;

        [Header("공격력 강조")]
        [Tooltip("원본 공격력과 달라졌을 때 색을 바꾼다.\n" +
                 "atkText가 있을 때만 적용된다. 한 줄 표시에서는 체력까지 물들어 오해를 부른다.")]
        [SerializeField] private bool colorizeAtk = true;

        [SerializeField] private Color normalColor = Color.white;
        [SerializeField] private Color buffedColor = new(0.45f, 1f, 0.6f);
        [SerializeField] private Color debuffedColor = new(1f, 0.45f, 0.4f);

        // 값이 그대로인데 text에 다시 대입하면 TMP가 메시를 통째로 다시 만든다.
        // 기물 수 × 프레임 수만큼 쌓이면 이것만으로 눈에 띄게 느려지므로 여기서 한 번 거른다.
        // 걸러내는 책임을 뷰가 가지면 호출하는 쪽은 아무 때나 불러도 된다.
        private bool _hasValue;
        private int _atk;
        private int _originalAtk;
        private int _hp;
        private int _maxHp;

        /// <summary>
        /// 표시할 값을 넘긴다. 이전과 같으면 아무 일도 하지 않으므로 매 프레임 불러도 된다.
        /// </summary>
        /// <param name="atk">특성까지 적용된 실제 공격력.</param>
        /// <param name="originalAtk">강화 여부를 판단할 기준값. 색을 안 쓸 거면 atk와 같은 값을 넣으면 된다.</param>
        public void SetStats(int atk, int originalAtk, int hp, int maxHp)
        {
            if (_hasValue && atk == _atk && originalAtk == _originalAtk && hp == _hp && maxHp == _maxHp)
                return;

            _hasValue = true;
            _atk = atk;
            _originalAtk = originalAtk;
            _hp = hp;
            _maxHp = maxHp;

            Redraw();
        }

        private void Redraw()
        {
            if (combinedText != null)
                combinedText.text = string.Format(combinedFormat, _atk, _hp, _maxHp);

            if (atkText != null)
            {
                atkText.text = _atk.ToString();

                if (colorizeAtk)
                    atkText.color = ResolveAtkColor();
            }

            if (hpText != null)
                hpText.text = _hp.ToString();
        }

        private Color ResolveAtkColor()
        {
            if (_atk > _originalAtk) return buffedColor;
            if (_atk < _originalAtk) return debuffedColor;

            return normalColor;
        }
    }
}
