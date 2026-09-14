using UnityEngine;

// KTH_HandCardLayout.ApplyFanAroundFocalCard에서 하던 순수 계산만 뽑아왔다.
// 트윈을 걸거나 카드 상태를 건드리는 건 여전히 KTH_HandCardLayout이 한다 - 여긴 숫자만 낸다.
public static class KTH_CardFanLayoutCalculator
{
    public struct FanSlot
    {
        public Vector3 LocalPosition;
        public float ZRotation;
    }

    /// <summary>
    /// 포커스 카드를 뺀 나머지가 좌/우에 몇 장씩 갈지 정한다.
    ///
    /// 기본은 절반씩 균등 분배. 나머지가 딱 1장일 때만 예외 -
    /// 균등분배 공식이 항상 오른쪽으로 밀어버려서, 원래 왼쪽에 있던 카드를
    /// 선택해도 반대편으로 튀어 보인다. 그 경우만 손패 상 실제 위치(singleCardIsOnLeft)를 따른다.
    /// </summary>
    public static void CalculateSideCounts(
        int otherCount,
        bool singleCardIsOnLeft,
        out int leftCount,
        out int rightCount)
    {
        if (otherCount == 1)
        {
            leftCount = singleCardIsOnLeft ? 1 : 0;
            rightCount = singleCardIsOnLeft ? 0 : 1;
            return;
        }

        leftCount = otherCount / 2;
        rightCount = otherCount - leftCount;
    }

    public static float CalculateSpacing(
        int count,
        float maxSpacing,
        float minSpacing,
        float maxWidth)
    {
        if (count <= 1)
        {
            return maxSpacing;
        }

        float spacing =
            Mathf.Min(
                maxSpacing,
                maxWidth / (count - 1)
            );

        return Mathf.Max(minSpacing, spacing);
    }

    /// <summary>
    /// otherIndex: 포커스 카드를 뺀 나머지 리스트에서의 순번(0-based, 손패 순서 그대로).
    /// leftCount/rightCount로 좌우 어느 쪽에 몇 번째로 들어가는지 정해서 자리를 낸다.
    /// </summary>
    public static FanSlot CalculateSlot(
        int otherIndex,
        int leftCount,
        int rightCount,
        float spacing,
        float centerGap,
        float anchorX,
        float arcHeight,
        float maxRotation)
    {
        int relativeIndex =
            otherIndex < leftCount
                ? otherIndex - leftCount
                : otherIndex - leftCount + 1;

        float targetX =
            relativeIndex * spacing;

        targetX +=
            relativeIndex < 0
                ? -centerGap
                : centerGap;

        targetX += anchorX;

        int sideCount =
            relativeIndex < 0
                ? leftCount
                : rightCount;

        float normalized =
            Mathf.Clamp01(
                Mathf.Abs(relativeIndex) /
                (float)Mathf.Max(1, sideCount)
            );

        float targetY =
            -normalized * normalized * arcHeight;

        float targetRotationZ =
            relativeIndex < 0
                ? normalized * maxRotation
                : -normalized * maxRotation;

        return new FanSlot
        {
            LocalPosition = new Vector3(targetX, targetY, 0f),
            ZRotation = targetRotationZ
        };
    }
}
