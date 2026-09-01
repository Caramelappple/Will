using UnityEngine;

public static class KTH_CardLayoutCalculator
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
        float farCardPushMultiplier = 0.5f)
    {
        if (totalCount <= 0)
        {
            return default;
        }

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

        float centerIndex =
            (totalCount - 1) * 0.5f;

        float offset =
            index - centerIndex;

        float posX =
            offset * cardSpacing;

        float cardAmountRatio =
            Mathf.Clamp01(totalCount / 10f);

        float dynamicArcHeight =
            arcHeight * cardAmountRatio;

        float dynamicMaxRotation =
            maxRotation * cardAmountRatio;

        float normalizedPos = 0f;

        if (totalCount > 1 &&
            centerIndex > 0f)
        {
            normalizedPos =
                offset / centerIndex;
        }

        float posY =
            (1f -
             normalizedPos *
             normalizedPos) *
            dynamicArcHeight -
            dynamicArcHeight;

        float zRotation =
            -normalizedPos *
            dynamicMaxRotation;

        if (selectedIndex >= 0 &&
            selectedIndex < totalCount &&
            index != selectedIndex)
        {
            int distance =
                Mathf.Abs(index - selectedIndex);

            float push =
                CalculatePush(
                    distance,
                    pushAmount,
                    farCardPushMultiplier
                );

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

    private static float CalculatePush(
        int distance,
        float pushAmount,
        float farCardPushMultiplier)
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
