using System;
using _Scripts.LSO.UI.Text;
using _Scripts.LSO.Will;
using UnityEngine;

namespace _Scripts.LSO.Reward
{
    /// <summary>
    /// 유언 보상을 보여주는 작은 메모장.
    ///
    /// 기획서(새로운 UI · 보상)의 마지막 단계다 —
    /// "카드 선택 후, 만약 유언 해금이 있다면 클릭 시 유언의 설명, 아이콘 그리고
    /// 설명이 적힌 작은 메모장이 나오게 된다."
    ///
    /// 보여주는 것은 셋뿐이다. 이름 · 아이콘 · 설명.
    /// LSO_RewardCard가 이미 그 셋을 들고 있으므로 여기서는 무엇을 넣을지만 정한다.
    ///
    /// ── 도장이 아니다 ──────────────────────────────────────────
    /// 한때 유언마다 다른 3D 도장 모델을 켜고 끄는 방식이었다. 기획이 메모장으로
    /// 돌아오면서 모델 목록이 통째로 필요 없어졌다.
    ///
    /// 도장은 유언 "선택"에만 남아 있다 — LSO_StampRack · LSO_StampSlot ·
    /// LSO_WillStampView. 손패 카드에 찍는 그쪽과 헷갈리지 말 것.
    /// ─────────────────────────────────────────────────────────
    /// </summary>
    public class LSO_WillNote : LSO_RewardCard
    {
        /// <summary>
        /// 보상 없이 유언만 그린다. 고른 뒤 보여줄 때 부른다.
        ///
        /// onDismiss는 "다 읽었다"는 뜻으로 눌렀을 때 불린다. 고르는 것이 아니다 —
        /// 이미 받은 뒤라 다시 고를 것이 없고, 치우기만 한다.
        /// </summary>
        public void Bind(DLJ_WillDataSO will, Action<LSO_RewardCard> onDismiss)
        {
            SetClickCallback(onDismiss);

            DrawWill(will);
        }

        /// <summary>
        /// 고를 수 있는 카드로는 쓰이지 않는다.
        ///
        /// 상자에서 나오는 세 장은 전부 기물 카드다. 메모장은 고른 뒤에
        /// 무엇이 풀렸는지 보여주는 것뿐이라 Bind(will) 쪽으로만 들어온다.
        ///
        /// LSO_RewardCard가 요구해서 남겨두지만, 여기로 들어왔다면 배선이 잘못된 것이다.
        /// </summary>
        protected override void Draw(LSO_RewardOption option)
        {
            Debug.LogWarning(
                $"{name}: 메모장은 고르는 카드가 아닙니다. " +
                "상자가 기물 카드 대신 메모장을 꺼내고 있는지 확인하세요.", this);

            DrawWill(option.will);
        }

        private void DrawWill(DLJ_WillDataSO will)
        {
            if (will == null)
            {
                Debug.LogWarning($"{name}: 유언 데이터가 없어 메모장을 채우지 못했습니다.", this);
                Clear();
                return;
            }

            // 에셋을 이미 손에 쥐고 있으므로 창구(LSO_DisplayNames)를 거치지 않는다.
            // 그쪽은 enum만 아는 곳을 위해 유언 데이터베이스를 한 번 더 뒤진다.
            // 이름·설명·아이콘이 전부 이 에셋에서 나오므로 세 줄이 같은 자리를 본다.
            SetName(will.DisplayName);

            SetDescription(will.description);
            SetIcon(will.icon);
        }

        protected override void Clear()
        {
            ClearCommon();
        }
    }
}
