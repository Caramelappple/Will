using System.IO;
using _Scripts.LDY.Save;
using UnityEditor;
using UnityEngine;

namespace _Scripts.LDY.EditorTools
{
    /// <summary>
    /// 세이브 계층이 실제로 도는지 확인하는 수동 점검.
    ///
    /// JsonUtility는 에디터/플레이어 안에서만 동작하므로 코드 리뷰만으로는
    /// 직렬화가 되는지 알 수 없다. 이 메뉴가 그걸 눈으로 확인해준다.
    ///
    /// 게임 상태와 실제 세이브 파일은 건드리지 않는다. 임시 폴더에서만 돈다.
    /// </summary>
    public static class LDY_SaveSmokeTest
    {
        [MenuItem("LDY/Save/세이브 왕복 점검")]
        public static void Run()
        {
            string root = Path.Combine(Path.GetTempPath(), "LDY_SaveSmokeTest");

            if (Directory.Exists(root)) Directory.Delete(root, true);

            var repository = new LDY_FileSaveRepository(root);
            int failed = 0;

            failed += CheckRunRoundTrip(repository);
            failed += CheckOverwrite(repository, root);
            failed += CheckMetaRoundTrip(repository);
            failed += CheckDelete(repository);

            if (failed == 0)
                Debug.Log($"[LDY_SaveSmokeTest] 전부 통과했습니다. (임시 폴더: {root})");
            else
                Debug.LogError($"[LDY_SaveSmokeTest] {failed}건 실패했습니다. 위 로그를 확인하세요.");
        }

        private static int CheckRunRoundTrip(LDY_ISaveRepository repository)
        {
            var saved = new LDY_RunSaveData
            {
                chapter = 2,
                stage = 3,
                currentNodeIndex = 4,
                battleEntryCount = 7,
            };
            saved.clearedNodeIndices.AddRange(new[] { 0, 1, 2 });
            saved.unlockedNodeIndices.AddRange(new[] { 0, 1, 2, 3 });
            saved.deckCardIds.AddRange(new[] { "corvo", "corvo", "goat" });
            saved.unlockedPieceNames.Add("Crow");
            saved.unlockedWillIds.Add("Rage");

            repository.Save(LDY_SaveService.RunKey, saved);
            LDY_RunSaveData loaded = repository.Load<LDY_RunSaveData>(LDY_SaveService.RunKey);

            if (loaded == null) return Fail("run 을 다시 읽지 못했습니다.");

            int failed = 0;
            failed += Expect(loaded.chapter == 2, "chapter");
            failed += Expect(loaded.stage == 3, "stage");
            failed += Expect(loaded.currentNodeIndex == 4, "currentNodeIndex");
            failed += Expect(loaded.battleEntryCount == 7, "battleEntryCount");
            failed += Expect(loaded.clearedNodeIndices.Count == 3, "clearedNodeIndices");
            failed += Expect(loaded.unlockedNodeIndices.Count == 4, "unlockedNodeIndices");

            // 덱은 중복을 허용하는 목록이다. 접히거나 순서가 바뀌면 안 된다.
            failed += Expect(loaded.deckCardIds.Count == 3, "deckCardIds 개수");
            failed += Expect(loaded.deckCardIds[0] == "corvo" && loaded.deckCardIds[2] == "goat", "deckCardIds 순서");

            failed += Expect(loaded.unlockedWillIds.Count == 1 && loaded.unlockedWillIds[0] == "Rage", "unlockedWillIds");
            failed += Expect(loaded.HasRun, "HasRun");

            return failed;
        }

        private static int CheckOverwrite(LDY_ISaveRepository repository, string root)
        {
            var second = new LDY_RunSaveData { chapter = 9 };
            second.deckCardIds.Add("only");

            repository.Save(LDY_SaveService.RunKey, second);
            LDY_RunSaveData loaded = repository.Load<LDY_RunSaveData>(LDY_SaveService.RunKey);

            int failed = 0;
            failed += Expect(loaded != null && loaded.chapter == 9, "덮어쓰기 후 값");
            failed += Expect(loaded != null && loaded.deckCardIds.Count == 1, "덮어쓰기 후 덱");

            // 교체가 끝났으면 임시 파일은 남아 있으면 안 된다.
            string[] leftovers = Directory.GetFiles(root, "*.tmp");
            failed += Expect(leftovers.Length == 0, $"임시 파일 잔여 없음 (발견 {leftovers.Length}개)");

            return failed;
        }

        private static int CheckMetaRoundTrip(LDY_ISaveRepository repository)
        {
            var meta = new LDY_MetaSaveData { totalWillUseCount = 12, totalRuns = 3 };
            meta.foundComboWillIds.Add("combo_a");
            meta.defeatedBossIds.Add("corvo_king");
            meta.willUsage.Add(new LDY_WillUsageEntry { willId = "Rage", count = 5 });

            repository.Save(LDY_SaveService.MetaKey, meta);
            LDY_MetaSaveData loaded = repository.Load<LDY_MetaSaveData>(LDY_SaveService.MetaKey);

            if (loaded == null) return Fail("meta 를 다시 읽지 못했습니다.");

            int failed = 0;
            failed += Expect(loaded.totalWillUseCount == 12, "totalWillUseCount");
            failed += Expect(loaded.totalRuns == 3, "totalRuns");
            failed += Expect(loaded.foundComboWillIds.Count == 1, "foundComboWillIds");
            failed += Expect(loaded.defeatedBossIds.Count == 1, "defeatedBossIds");

            // 중첩 클래스 목록이 JsonUtility로 나갔다 들어오는지가 핵심이다.
            failed += Expect(loaded.willUsage.Count == 1, "willUsage 개수");
            failed += Expect(loaded.willUsage.Count == 1 && loaded.willUsage[0].count == 5, "willUsage 내용");

            return failed;
        }

        private static int CheckDelete(LDY_ISaveRepository repository)
        {
            repository.Delete(LDY_SaveService.RunKey);

            return Expect(!repository.Exists(LDY_SaveService.RunKey), "삭제 후 Exists=false");
        }

        private static int Expect(bool condition, string what)
        {
            if (condition) return 0;

            Debug.LogError($"[LDY_SaveSmokeTest] 실패: {what}");
            return 1;
        }

        private static int Fail(string message)
        {
            Debug.LogError($"[LDY_SaveSmokeTest] 실패: {message}");
            return 1;
        }
    }
}
