using UnityEngine;

public static class CardLayoutCalculator
{
    /// <summary>
    /// 부채꼴로 겹칠 때 어느 카드가 앞에 오는지.
    ///
    /// 카드가 불투명 메쉬라 sortingOrder가 먹지 않는다. 앞뒤는 오직 카메라와의
    /// 거리(Z-버퍼)로 정해지므로, 앞에 둘 카드를 실제로 카메라 쪽으로 당긴다.
    /// KTH_CardSorting이 선택된 카드 하나에 쓰는 방법과 같다.
    ///
    /// 값은 뒤에만 추가할 것. 중간에 끼워 넣으면 씬에 저장된 값이 어긋난다.
    /// </summary>
    public enum DepthOrder
    {
        /// <summary>왼쪽 카드가 앞. 손패를 왼쪽부터 훑어 읽는 모양이다.</summary>
        LeftFirst,

        /// <summary>오른쪽 카드가 앞.</summary>
        RightFirst,

        /// <summary>가운데가 제일 앞이고 양끝으로 갈수록 뒤로 물러난다.</summary>
        CenterFirst
    }

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
        float farCardPushMultiplier = 0.5f,
        float depthStep = 0f,
        DepthOrder depthOrder = DepthOrder.LeftFirst,
        Vector3? depthAxis = null)
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

        // 앞에 올 카드일수록 더 당긴다. depthStep이 0이면 전부 같은 깊이에 놓인다.
        //
        // ── 어느 방향으로 당기는가 ────────────────────────────────
        // 예전에는 언제나 로컬 -Z 였다. 손패가 카메라를 정면으로 볼 때는 그것이
        // 곧 시선 방향이라 뗀 거리가 화면에 안 드러난다. 그런데 손패를 눕혀 놓으면
        // 시선과 어긋나서, 뗀 만큼이 그대로 **카드 사이의 틈**으로 보인다.
        //
        // 그래서 방향을 밖에서 받는다. 부르는 쪽이 카메라를 보고 정해 넘기면
        // 깊이는 살아 있고 틈만 사라진다.
        //
        // 안 넘기면 예전 그대로다(-Z).
        // ─────────────────────────────────────────────────────────
        Vector3 axis = depthAxis ?? Vector3.back;

        Vector3 depthOffset =
            axis *
            (DepthRank(index, totalCount, depthOrder) * depthStep);

        return new CardTransformData
        {
            LocalPosition =
                new Vector3(
                    posX,
                    posY,
                    0f
                ) + depthOffset,

            ZRotation =
                zRotation
        };
    }

    /// <summary>
    /// 몇 번째로 앞인지. 클수록 앞이다.
    ///
    /// 실제 거리로 바꾸는 것은 부르는 쪽이 depthStep을 곱해서 한다.
    /// 여기서는 순서만 정한다.
    ///
    /// 밖에 열어두는 이유는 배치 모드처럼 좌표를 따로 계산하는 곳도
    /// **같은 규칙**을 써야 하기 때문이다. 두 곳이 따로 정하면
    /// 손패와 배치 모드에서 앞뒤가 뒤바뀐다.
    /// </summary>
    public static float DepthRank(
        int index,
        int totalCount,
        DepthOrder order)
    {
        if (totalCount <= 1)
        {
            return 0f;
        }

        switch (order)
        {
            case DepthOrder.RightFirst:
                return index;

            case DepthOrder.CenterFirst:
            {
                float center =
                    (totalCount - 1) * 0.5f;

                return center -
                       Mathf.Abs(index - center);
            }

            default:
                return (totalCount - 1) - index;
        }
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