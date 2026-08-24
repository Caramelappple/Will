using System;
using System.Collections.Generic;

namespace _Scripts.LDY.Save
{
    /// <summary>
    /// 진행 중인 런 하나.
    ///
    /// 전투 중간 상태(보드 배치, 손패, 코스트, 기물 HP)는 담지 않는다.
    /// 저장은 스테이지 경계에서만 일어나고, 그 값들은 전투가 시작될 때 다시 세워지기 때문이다.
    ///
    /// JsonUtility로 직렬화하므로 Dictionary를 쓰지 않는다.
    /// </summary>
    [Serializable]
    public class LDY_RunSaveData
    {
        public int schemaVersion = 1;

        /// <summary>
        /// 런 재현용 시드. 지금은 보스 스테이지 추첨(LDY_StageRouter)이 이 값을 쓴다.
        /// 상점 진열이나 이벤트에 무작위가 들어오면 그쪽도 이 시드를 함께 쓰면 된다.
        ///
        /// 0은 "아직 정해지지 않음"이다. 시드가 생기기 전에 만들어진 세이브가 그렇다.
        /// 값을 채우고 되돌리는 일은 LDY_RunSeedGateway가 맡는다.
        /// </summary>
        public int runSeed;

        // ── 맵 진행도 ─────────────────────────────────────────
        // chapter/stage가 노드 뼈대를 세우고, 아래 인덱스 목록이 세부 상태를 덮어쓴다.
        // 둘 다 저장하는 것은 맵이 분기를 갖게 되면 stage 하나로는 어느 갈래를 지나왔는지
        // 알 수 없기 때문이다.

        public int chapter = 1;
        public int stage = 1;

        public int currentNodeIndex = -1;
        public int battleEntryCount;

        public List<int> clearedNodeIndices = new();
        public List<int> unlockedNodeIndices = new();

        // ── 덱 ────────────────────────────────────────────────
        // 덱은 같은 카드가 여러 장 들어가는 목록이므로 수량으로 접지 않고 그대로 나열한다.
        // 정본인 KTH_DeckDataPersistent.savedInventory가 같은 형태다.

        public List<string> deckCardIds = new();

        // ── 해금 (런 스코프) ──────────────────────────────────
        // KTH_Reward는 기물과 유언을 서로 다른 타입으로 들고 있어서
        // 한 리스트에 섞으면 되돌릴 때 어느 쪽인지 알 수 없다.

        public List<string> unlockedPieceNames = new();
        public List<string> unlockedWillIds = new();

        /// <summary>이어할 런이 실제로 담겨 있는지. 빈 파일만 남은 경우를 걸러낸다.</summary>
        public bool HasRun => deckCardIds.Count > 0 || clearedNodeIndices.Count > 0;
    }
}
