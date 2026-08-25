using UnityEngine;

public static class CardLayoutCalculator
{
    public struct CardTransformData
    {
        public Vector3 LocalPosition;
        public float ZRotation;
    }

    public static CardTransformData CalculateCardTransform(
        int index,
        int totalCount,
        float maxSpacing,
        float minSpacing,
        float maxWidth,
        float arcHeight,
        float maxRotation,
        int selectedIndex = -1,
        float pushAmount = 0f,
        float farCardPushMultiplier = 0.5f
    )
    {
        if (totalCount <= 0)
        {
            return default;
        }

        // =====================================================
        // 카드 간격
        // =====================================================

        float cardSpacing = maxSpacing;

        if (totalCount > 1)
        {
            cardSpacing = Mathf.Min(
                maxSpacing,
                maxWidth / (totalCount - 1)
            );

            cardSpacing = Mathf.Max(
                minSpacing,
                cardSpacing
            );
        }

        // =====================================================
        // 중앙 기준 위치
        // =====================================================

        float centerIndex =
            (totalCount - 1) * 0.5f;

        // 기존 카드 방향 유지
        int reversedIndex =
            totalCount - 1 - index;

        float offset =
            reversedIndex - centerIndex;

        float posX =
            offset * cardSpacing;

        // =====================================================
        // 카드 수에 따른 곡률
        // =====================================================

        float cardAmountRatio =
            Mathf.Clamp01(
                totalCount / 10f
            );

        float dynamicArcHeight =
            arcHeight * cardAmountRatio;

        float dynamicMaxRotation =
            maxRotation * cardAmountRatio;

        // =====================================================
        // Y 곡선
        // =====================================================

        float normalizedPos = 0f;

        if (totalCount > 1 &&
            centerIndex > 0f)
        {
            normalizedPos =
                offset / centerIndex;
        }

        float posY =
            (1f -
             normalizedPos * normalizedPos)
            * dynamicArcHeight
            - dynamicArcHeight;

        // =====================================================
        // 회전
        // =====================================================

        float zRotation =
            -normalizedPos *
            dynamicMaxRotation;

        // =====================================================
        // 선택 카드 주변 밀기
        // =====================================================

        if (selectedIndex >= 0 &&
            selectedIndex < totalCount &&
            index != selectedIndex)
        {
            int distance =
                Mathf.Abs(
                    index -
                    selectedIndex
                );

            float push =
                CalculatePush(
                    distance,
                    pushAmount,
                    farCardPushMultiplier
                );

            // index 기준이 아니라 실제 화면 방향 기준으로 밀기
            if (index < selectedIndex)
            {
                posX -= push;
            }
            else
            {
                posX += push;
            }
        }

        return new CardTransformData
        {
            LocalPosition =
                new Vector3(
                    posX,
                    posY,
                    0f
                ),

            ZRotation =
                zRotation
        };
    }

    // =========================================================
    // Push
    // =========================================================

    private static float CalculatePush(
        int distance,
        float pushAmount,
        float farCardPushMultiplier
    )
    {
        if (distance <= 0)
        {
            return 0f;
        }

        return pushAmount *
               Mathf.Pow(
                   farCardPushMultiplier,
                   distance - 1
               );
    }
}