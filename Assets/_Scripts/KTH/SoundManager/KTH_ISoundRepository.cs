using UnityEngine;

public interface KTH_ISoundRepository
{
    KTH_SfxData GetSfx(SfxID id);

    KTH_BgmData GetBgm(BgmID id);
}