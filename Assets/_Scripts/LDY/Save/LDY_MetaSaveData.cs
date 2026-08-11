using System;
using System.Collections.Generic;

namespace _Scripts.LDY.Save
{
    /// <summary>유언 하나의 누적 사용 횟수.</summary>
    [Serializable]
    public class LDY_WillUsageEntry
    {
        public string willId;
        public int count;
    }

    /// <summary>
    /// 런을 넘어 영구히 쌓이는 기록. 런이 끝나도 지워지지 않는다.
    /// </summary>
    [Serializable]
    public class LDY_MetaSaveData
    {
        public int schemaVersion = 1;

        /// <summary>
        /// 유언별 사용 횟수.
        /// KTH_WillRecord가 아직 유언을 구분하지 않고 총합만 세므로 지금은 항상 비어 있다.
        /// 그쪽이 유언별로 세기 시작하면 이 목록이 채워지고 totalWillUseCount는 파생값이 된다.
        /// </summary>
        public List<LDY_WillUsageEntry> willUsage = new();

        /// <summary>유언 사용 횟수 총합. 지금은 이쪽이 원본이다.</summary>
        public int totalWillUseCount;

        public List<string> foundComboWillIds = new();

        /// <summary>채울 공급자가 아직 없다. 항상 빈 채로 저장된다.</summary>
        public List<string> unlockedCardIds = new();

        /// <summary>
        /// 처치한 보스 id 목록.
        /// 개수만 저장하면 어느 보스를 잡았는지 되돌릴 수 없어서 목록으로 둔다.
        /// </summary>
        public List<string> defeatedBossIds = new();

        public int totalRuns;
    }
}
