namespace _Scripts.LDY.Save
{
    /// <summary>
    /// 런 시드를 <see cref="LDY_RunSeed"/>와 세이브 사이에서 옮긴다.
    ///
    /// Capture에서 EnsureAssigned를 부르는 것은 의도된 것이다.
    /// 시드를 처음 쓰는 시점(보스 노드 진입)보다 저장이 먼저 오는 경우가 대부분이라,
    /// 여기서 확정해두지 않으면 파일에 0이 적힌다. 그러면 다음에 이어하기로 들어왔을 때
    /// 복원할 시드가 없어 새로 뽑히고, 같은 런인데 보스가 바뀐다.
    /// </summary>
    public sealed class LDY_RunSeedGateway
    {
        public void Capture(LDY_RunSaveData data)
        {
            data.runSeed = LDY_RunSeed.EnsureAssigned();
        }

        public void Restore(LDY_RunSaveData data)
        {
            LDY_RunSeed.Restore(data.runSeed);
        }
    }
}
