namespace _Scripts.LDY.Save
{
    /// <summary>
    /// 세이브 데이터를 어디에 어떻게 담아두는지를 감춘다.
    /// 조립하는 쪽(LDY_SaveService)은 파일인지 무엇인지 알 필요가 없다.
    /// </summary>
    public interface LDY_ISaveRepository
    {
        bool Exists(string key);

        /// <summary>없거나 읽지 못하면 null.</summary>
        T Load<T>(string key) where T : class;

        void Save<T>(string key, T data);

        void Delete(string key);
    }
}
