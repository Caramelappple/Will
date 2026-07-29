using _Scripts.LSO.HealthSystem;

namespace _Scripts.HealthSystem
{
    public readonly struct RecoverData
    {
        public readonly Health giver;
        public readonly int recoverValue;

        public RecoverData(Health giver, int recoverValue)
        {
            this.giver = giver;
            this.recoverValue = recoverValue;
        }

        public static RecoverData Create(Health giver, int recoverValue)
        {
            return new RecoverData(giver, recoverValue);
        }
    }

    public readonly struct RecoverResultData
    {
        public readonly Health giver;
        public readonly int recoverValue;
        public readonly int resourceData;

        public RecoverResultData(Health giver, int recoverValue, int resourceData)
        {
            this.giver = giver;
            this.recoverValue = recoverValue;
            this.resourceData = resourceData;
        }

        public static RecoverResultData Create(Health giver, int recoverValue, int resourceData)
        {
            return new RecoverResultData(giver, recoverValue, resourceData);
        }
    }
}