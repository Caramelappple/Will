namespace _Scripts.LSO.Cost
{
    /// <summary>
    /// 코인이 늘어설 방향. 케이스의 로컬 축 기준이다.
    ///
    /// 값은 뒤에만 추가할 것. 중간에 끼워 넣으면 프리팹에 저장된 선택이 어긋난다.
    /// </summary>
    public enum LSO_CostAxis
    {
        /// <summary>옆으로. 케이스가 가로로 누워 있을 때.</summary>
        X,

        /// <summary>위로. UI나 세로 케이스일 때.</summary>
        Y,

        /// <summary>안쪽으로. 3D에서 케이스가 깊이 방향으로 놓였을 때.</summary>
        Z,

        /// <summary>축을 섞어 비스듬히 놓고 싶을 때. Custom Step을 쓴다.</summary>
        Custom,

        // 아래 셋은 뒤에 붙였다. 값이 저장된 뒤에 중간으로 옮기면
        // 프리팹에 남은 숫자가 다른 축을 가리키게 된다.

        /// <summary>왼쪽으로.</summary>
        MinusX,

        /// <summary>아래로.</summary>
        MinusY,

        /// <summary>바깥쪽으로.</summary>
        MinusZ
    }
}
