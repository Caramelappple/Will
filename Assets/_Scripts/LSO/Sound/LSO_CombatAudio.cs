using _Scripts.LDY;
using _Scripts.LDY.Boss.BullKing;
using _Scripts.LDY.Stage;
using _Scripts.LSO.Boss;
using UnityEngine;

namespace _Scripts.LSO.Sound
{
    /// <summary>전투 대상·스테이지 데이터에 맞는 음원을 고른다.</summary>
    public static class LSO_CombatAudio
    {
        public static void Death(LDY_Animal animal)
        {
            if (animal == null) return;
            if (animal.GetComponent<LDY_BullKingBoss>() != null)
                LSO_GameAudio.Play(LSO_SoundCue.BullDeath);
            else if (animal.GetComponent<DLJ_SharkKing>() != null)
                LSO_GameAudio.Play(LSO_SoundCue.SharkDeath);
        }

        public static void Stage(LDY_StageSO stage)
        {
            bool boss = false;
            if (stage != null && stage.enemies != null)
            foreach (LDY_StageEnemyEntry entry in stage.enemies)
            {
                GameObject prefab = entry?.card != null && entry.card.Animal != null
                    ? entry.card.Animal.unitPrefab : null;
                if (prefab != null && prefab.GetComponentInChildren<LSO_BossPhase>(true) != null)
                { boss = true; break; }
            }
            LSO_GameAudio.Music(boss ? LSO_SoundCue.BossMusic : LSO_SoundCue.BattleMusic);
        }
    }
}
