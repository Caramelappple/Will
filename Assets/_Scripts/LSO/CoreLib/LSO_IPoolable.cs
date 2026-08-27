namespace _Scripts.LSO.CoreLib
{
    /// <summary>
    /// 풀에서 꺼내지고 돌아갈 때 스스로를 정리해야 하는 것에 붙인다.
    ///
    /// 풀은 오브젝트가 무엇인지 모른다. 남은 체력이나 재생 중인 트윈처럼
    /// 지난번 사용의 흔적을 지우는 일은 오브젝트 자신만 할 수 있다.
    ///
    /// 이 흔적을 지우지 않으면 재사용했을 때 "가끔 이상한 값으로 나온다"는 형태로 드러난다.
    /// 새로 만든 것이 아니라 되살린 것이기 때문인데, 겉으로는 원인이 전혀 보이지 않는다.
    /// </summary>
    public interface LSO_IPoolable
    {
        /// <summary>풀에서 꺼내져 켜진 직후. 지난번 상태를 여기서 되돌린다.</summary>
        void OnSpawned();

        /// <summary>풀로 돌아가 꺼지기 직전. 트윈·코루틴·구독을 여기서 끊는다.</summary>
        void OnDespawned();
    }
}
