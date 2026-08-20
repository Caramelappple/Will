using UnityEngine;

namespace _Scripts.LDY.Save
{
    /// <summary>
    /// 세이브 조립 담당.
    ///
    /// 각 시스템에서 값을 모으고 되돌리는 실제 작업은 게이트웨이들이 맡는다.
    /// 이 클래스가 하는 일은 순서를 정하고 저장소에 넘기는 것뿐이다.
    ///
    /// MonoBehaviour가 아니라서 씬에 배치할 필요가 없다.
    /// 카드 카탈로그만 Resources에서 찾아온다.
    /// </summary>
    public sealed class LDY_SaveService
    {
        public const string RunKey = "run";
        public const string MetaKey = "meta";

        private const int CurrentSchemaVersion = 1;

        private static LDY_SaveService _instance;
        public static LDY_SaveService Instance => _instance ??= new LDY_SaveService();

        /// <summary>
        /// 플레이 모드에 들어갈 때 인스턴스를 버린다. 다음 Instance 호출이 새로 만든다.
        ///
        /// Reload Domain을 끈 에디터에서는 이 인스턴스가 세션 내내 살아남는데,
        /// 여기에는 파일에서 한 번만 읽는 Meta와 그 읽음 여부(_metaLoaded)가 들어 있다.
        /// 즉 지난 플레이의 기록이 캐시된 채로 남아, 파일을 지우거나 밖에서 고쳐도 반영되지 않고
        /// SaveMeta가 그 낡은 값을 그대로 다시 써버린다.
        ///
        /// 필드를 하나씩 되돌리지 않고 인스턴스째 버리는 이유는, 나중에 캐시가 늘어나도
        /// 여기를 고칠 필요가 없기 때문이다.
        /// </summary>
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            _instance = null;
        }

        private readonly LDY_ISaveRepository _repository;
        private readonly LDY_DeckSaveGateway _deck;
        private readonly LDY_UnlockSaveGateway _unlocks;
        private readonly LDY_MapProgressGateway _map;
        private readonly LDY_MetaRecordGateway _records;

        private bool _metaLoaded;

        /// <summary>영구 기록. 파일에서 읽기 전이면 기본값이다.</summary>
        public LDY_MetaSaveData Meta { get; private set; } = new();

        private LDY_SaveService()
            : this(new LDY_FileSaveRepository(), FindCatalog())
        {
        }

        /// <summary>저장소와 카탈로그를 갈아끼울 수 있게 열어둔다(테스트용).</summary>
        public LDY_SaveService(LDY_ISaveRepository repository, LDY_CardCatalogSO catalog)
        {
            _repository = repository;
            _deck = new LDY_DeckSaveGateway(catalog);
            _unlocks = new LDY_UnlockSaveGateway();
            _map = new LDY_MapProgressGateway();
            _records = new LDY_MetaRecordGateway();
        }

        /// <summary>이어할 런이 있는지. 파일만 있고 내용이 비어 있으면 false다.</summary>
        public bool HasRun
        {
            get
            {
                if (!_repository.Exists(RunKey)) return false;

                LDY_RunSaveData data = _repository.Load<LDY_RunSaveData>(RunKey);

                return data != null && data.HasRun;
            }
        }

        public void SaveRun()
        {
            var data = new LDY_RunSaveData { schemaVersion = CurrentSchemaVersion };

            _map.Capture(data);
            _deck.Capture(data);
            _unlocks.Capture(data);

            _repository.Save(RunKey, data);
        }

        public void LoadRun()
        {
            LDY_RunSaveData data = _repository.Load<LDY_RunSaveData>(RunKey);
            if (data == null)
            {
                Debug.LogWarning("[LDY_SaveService] 불러올 런이 없습니다.");
                return;
            }

            if (data.schemaVersion != CurrentSchemaVersion)
            {
                Debug.LogWarning(
                    $"[LDY_SaveService] 런 세이브 버전이 다릅니다(파일 {data.schemaVersion} / 현재 {CurrentSchemaVersion}). " +
                    "그대로 읽어보지만 값이 어긋날 수 있습니다.");
            }

            // 맵을 먼저 되돌린다. 진행도가 서야 덱과 해금이 어느 시점의 것인지 뜻이 통한다.
            _map.Restore(data);
            _deck.Restore(data);
            _unlocks.Restore(data);
        }

        public void ClearRun()
        {
            _repository.Delete(RunKey);

            // 임시 조치다. 런이 끝날 때 해금을 비우는 곳이 지금은 여기뿐이라 여기서 부른다.
            // 팀이 "런 시작" 흐름을 만들면 그쪽으로 옮길 것.
            //if (KTH_Reward.Instance != null)
                //KTH_Reward.Instance.ResetUnlocks();

            EnsureMetaLoaded();
            Meta.totalRuns++;
            SaveMeta();
        }

        /// <summary>
        /// 런과 메타를 모두 지운다. 되돌릴 수 없다.
        ///
        /// ClearRun과 다르다. 저쪽은 "런 하나가 끝났다"라서 totalRuns를 올리고 메타를 다시 쓴다.
        /// 이쪽은 "기록 자체를 없앤다"이므로 메타 파일도 지우고 아무것도 되쓰지 않는다.
        ///
        /// 파일만 지우면 이 인스턴스에 남은 Meta가 다음 SaveMeta에서 그대로 되살아난다.
        /// 그래서 캐시도 같이 비운다.
        /// </summary>
        public void ClearAll()
        {
            _repository.Delete(RunKey);
            _repository.Delete(MetaKey);

            Meta = new LDY_MetaSaveData();
            _metaLoaded = false;
        }

        public void SaveMeta()
        {
            // 먼저 읽어두지 않으면 totalRuns 같은, 공급자가 없어 메모리에만 있는 값이 0으로 덮인다.
            EnsureMetaLoaded();

            _records.Capture(Meta);
            Meta.schemaVersion = CurrentSchemaVersion;

            _repository.Save(MetaKey, Meta);
        }

        public void LoadMeta()
        {
            EnsureMetaLoaded();

            _records.Restore(Meta);
        }

        /// <summary>파일에서 Meta를 한 번만 읽어온다. 각 시스템에 되돌리지는 않는다.</summary>
        private void EnsureMetaLoaded()
        {
            if (_metaLoaded) return;

            LDY_MetaSaveData data = _repository.Load<LDY_MetaSaveData>(MetaKey);
            if (data != null) Meta = data;

            _metaLoaded = true;
        }

        private static LDY_CardCatalogSO FindCatalog()
        {
            var catalog = Resources.Load<LDY_CardCatalogSO>(LDY_CardCatalogSO.ResourcePath);

            if (catalog == null)
            {
                Debug.LogError(
                    $"[LDY_SaveService] Resources/{LDY_CardCatalogSO.ResourcePath} 에서 카드 카탈로그를 찾지 못했습니다. " +
                    "덱은 저장되지만 불러오기에서 복원되지 않습니다.");
            }

            return catalog;
        }
    }
}
