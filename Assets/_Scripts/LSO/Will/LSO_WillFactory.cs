using System;
using System.Collections.Generic;

namespace _Scripts.LSO.Will
{
    public class LSO_WillFactory
    {
        // 유언은 발동 여부·지속 턴처럼 개체별 상태를 가질 수 있으므로
        // 완성된 인스턴스가 아니라 "생성 방법"을 등록한다. 인스턴스를 공유하면 상태가 섞인다.
        private static readonly Dictionary<LSO_WillType, Func<LSO_IWill>> Creators =
            new ()
            {
                { LSO_WillType.Curse, () => new LSO_TestCurseWill() },
            };

        public static LSO_IWill Create(LSO_WillType type)
        {
            return Creators.TryGetValue(type, out Func<LSO_IWill> creator)
                ? creator()
                : null;
        }
    }
}
