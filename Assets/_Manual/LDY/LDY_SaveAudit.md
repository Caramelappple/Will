# LDY_SaveAudit — 세이브 대상 코드베이스 조사

조사일: 2026-08-06 / 브랜치: `LDY` / 커밋: `9a7caf4`
조사 범위: `Assets/_Scripts/**` (152개 .cs, Plugins·TutorialInfo 제외), `Packages/`, `ProjectSettings/`

> 이 문서는 조사 결과만 담는다. 조사 중 어떤 파일도 수정·생성·삭제하지 않았다(이 파일 제외).
> 확인하지 못한 것은 **불명확**으로 표시하고 이유를 적었다.

---

## 요약 (먼저 읽을 것)

1. **런타임 세이브 코드는 0줄이다.** `PlayerPrefs` / `JsonUtility` / `File.` / `Application.persistentDataPath` 사용처가 런타임 코드에 하나도 없다.
2. **`GameSaveData` 라는 이름의 구조체가 이미 있지만 아무도 저장하지 않는다.** `GameManager.Awake()`에서 `CreateDefault()`로 한 번 만들고 끝. 게다가 필드가 `LSO_CardSO`(ScriptableObject) 참조라서 **그대로는 직렬화 불가**.
3. **저장 가능한 문자열 ID가 기물·유언·스테이지 어디에도 없다.** 전부 SO 에셋 참조 / 배열 인덱스 / enum 으로 구분한다.
4. **런 상태가 4개 매니저에 흩어져 있고, 서로 모른다.** `GameManager`, `KTH_DeckDataPersistent`, `LDY_MapManager`, `KTH_Reward`(+`KTH_WillRecord`). 전부 각자 `DontDestroyOnLoad` 싱글톤.
5. **랜덤은 전역 `UnityEngine.Random`.** 시드 재현 불가.
6. **Newtonsoft.Json 미설치.** asmdef 0개.
7. **Build Settings에 `SampleScene` 1개만 등록돼 있다.** 이름으로 씬을 로드하는 코드 전부가 빌드에서 실패한다(세이브와 직접 관련은 없으나 로드 후 복귀 흐름을 만들 때 걸린다).

---

## A. 런 단위 상태를 들고 있는 클래스

### A-1. 표

| 클래스명 | 파일 경로 | 보관 중인 런 상태 필드 | 접근 방식 | 외부에서 읽기 가능? | 외부에서 복원 가능? |
|---|---|---|---|---|---|
| `GameManager` | [GameManager.cs](Assets/_Scripts/LSO/Manager/GameManager.cs) | `SaveData`(GameSaveData), `Cards`(LSO_CardCollection) | `MonoSingleton<T>` + `DontDestroyOnLoad` | ✅ `SaveData`, `Cards` 둘 다 public getter | ❌ **불가 — 둘 다 `{ get; private set; }`.** `SaveData`는 struct라 `SaveData.stage = n` 같은 부분 대입조차 컴파일 에러. `Cards`는 인스턴스 교체 불가(단 내용물은 `Cards.AddItem()`으로 채울 수 있음) |
| `GameSaveData` (struct) | [GameSaveData.cs](Assets/_Scripts/LSO/Manager/GameSaveData.cs#L20-L35) | `stage`(int), `maxCost`(int), `inventoryItems`(DeckCardsSaveData[]) | 값 타입, `[Serializable]` | ✅ public 필드 | ⚠️ 필드 자체는 public이지만 **담고 있는 값이 직렬화 불가**(아래 A-2 참조). 또한 `GameManager.SaveData`의 setter가 private이라 완성된 struct를 되돌려 넣을 수 없다 |
| `DeckCardsSaveData` (struct) | [GameSaveData.cs](Assets/_Scripts/LSO/Manager/GameSaveData.cs#L7-L17) | `cardId`(**`LSO_CardSO` 타입**), `amount`(int) | 값 타입, `[Serializable]` | ✅ public 필드 | ⚠️ 이름은 `cardId`인데 실제 타입이 SO 참조다. **JSON으로 나가지 않는다** |
| `LSO_CardCollection` | [LSO_CardCollection.cs](Assets/_Scripts/LSO/Deck/LSO_CardCollection.cs) | `_items`(`Dictionary<LSO_CardSO,int>`) — 플레이어 보유 카드+수량 | `GameManager.Cards`를 통해서만 | ✅ `ToSaveData()`, `ToCardList()`, `GetItemAmount()` | ✅ **가능** — `AddItem(card,amount)` public, `Clear()` public. 단 `LSO_CardSO` 인스턴스가 필요하므로 ID→SO 해석기가 선행돼야 함 |
| `KTH_DeckDataPersistent` | [KTH_DeckDataPersistent.cs](Assets/_Scripts/KTH/CardManager/CardBuild/KTH_DeckDataPersistent.cs) | `savedInventory`(`List<LSO_CardSO>`) — 덱빌드 씬에서 확정한 최종 카드 목록 | `public static Instance` + `DontDestroyOnLoad` | ✅ public 필드 | ✅ **가능** — 필드가 public이고 `SaveInventory(List<LSO_CardSO>)`도 public |
| `KTH_DeckBuilderManager` | [KTH_DeckBuilderManager.cs](Assets/_Scripts/KTH/CardManager/CardBuild/KTH_DeckBuilderManager.cs) | `_inventoryIndices`(`List<int>`) — **cardDatabase 배열 인덱스** 목록 | `public static Instance` (**DontDestroyOnLoad 아님** — 덱빌드 씬 로컬) | ✅ `GetCurrentInventoryIndices()`, `GetCurrentInventoryCardData()` | ❌ **불가** — 목록을 채우는 `BuildInitialInventory()`가 private이고, `Start()`에서 인스펙터의 `initialInventoryCards`만 보고 매번 다시 만든다 |
| `LDY_MapManager` | [LDY_MapManager.cs](Assets/_Scripts/LDY/Map/LDY_MapManager.cs) | `Nodes`(`List<LDY_MapNode>` — 각 노드의 `isCleared`/`isUnlocked`), `activeNodeIndex`, `CurrentNodeIndex`, `BattleEntryCount` | `public static Instance` + `DontDestroyOnLoad` | ✅ `Nodes`, `ActiveNodeIndex`, `CurrentNodeIndex`, `BattleEntryCount` 전부 public getter | ⚠️ **부분만 가능** (A-3 상세) |
| `LDY_MapNode` | [LDY_MapNode.cs](Assets/_Scripts/LDY/Map/LDY_MapNode.cs) | `isCleared`, `isUnlocked`, `type`, `position`, `nextIndices` | `LDY_MapManager.Nodes[i]` 로 접근 | ✅ 전부 public 필드 | ✅ **가능** — `Nodes` 리스트 자체는 교체 못 하지만 요소의 public 필드는 직접 대입 가능 |
| `KTH_Reward` | [KTH_Reward.cs](Assets/_Scripts/KTH/Reward/KTH_Reward.cs) | `unlockedPieces`(`List<string>`), `unlockedWills`(`List<string>`) — 해금된 기물/유언 ID | `public static Instance` + `DontDestroyOnLoad` | ✅ `GetUnlockedPieces()`, `GetUnlockedWills()` (IReadOnlyList) | ✅ **가능** — `UnlockPiece(id)` / `UnlockWill(id)` / `ResetUnlocks()` 전부 public. 로드 시 `ResetUnlocks()` → ID 루프로 복원 |
| `KTH_WillRecord` | [KTH_WillRecord.cs](Assets/_Scripts/KTH/Record/KTH_WillRecord.cs) | `totalWillUseCount`(int), `discoveredCombos`(`List<string>`), `defeatedBosses`(`List<string>`) | `public static Instance` + `DontDestroyOnLoad` | ✅ `GetWillUseCount()`, `GetDiscoveredCombos()`, `GetDefeatedBosses()` | ⚠️ **부분만 가능** — 리스트 2개는 `DiscoverCombo(id)`/`RecordBossDefeat(id)`로 복원 가능. **`totalWillUseCount`는 `AddWillUse()`(+1) 밖에 없어서 N번 호출해야 한다 — 사실상 불가** |
| `LDY_StageSelection` | [LDY_StageSelection.cs](Assets/_Scripts/LDY/Stage/LDY_StageSelection.cs) | `Pending`(`LDY_StageSO`) — 다음에 시작할 스테이지 | **`static class`** (씬 전환 1회성 전달) | ✅ `Pending` public getter | ✅ `Select(stage)` public. 단 SO 참조이므로 ID→SO 해석 필요 |

### A-2. `GameSaveData`가 지금 상태로는 직렬화되지 않는 이유

```csharp
// Assets/_Scripts/LSO/Manager/GameSaveData.cs:9
public LSO_CardSO cardId;   // 이름은 "Id"인데 타입은 ScriptableObject 참조
```

`JsonUtility`로 이걸 직렬화하면 `LSO_CardSO`는 `UnityEngine.Object`이므로 `{"instanceID":0}` 같은 값만 남고 로드 시 전부 null이 된다. Newtonsoft를 쓰면 SO의 모든 public 필드를 통째로 덤프하다가 순환 참조(`unitPrefab` → GameObject)로 터진다.
→ **`cardId`를 `string`으로 바꾸거나, 별도의 직렬화용 DTO를 새로 만들어야 한다.**

### A-3. `LDY_MapManager` 복원 경로 상세

읽기는 전부 열려 있으나 되돌려 넣는 쪽이 막혀 있다.

| 값 | 복원 가능? | 근거 |
|---|---|---|
| `Nodes[i].isCleared` / `isUnlocked` | ✅ 가능 | `LDY_MapNode`의 public 필드에 직접 대입 |
| `CurrentNodeIndex` (플레이어가 서 있는 노드) | ❌ **불가 — private setter** ([L52](Assets/_Scripts/LDY/Map/LDY_MapManager.cs#L52)). `OnNodeClicked()`가 유일한 변경 경로인데 씬 전환·이벤트를 다 유발한다 |
| `activeNodeIndex` | ❌ **불가 — `[SerializeField] private`, getter만 공개** ([L46-47](Assets/_Scripts/LDY/Map/LDY_MapManager.cs#L46-L47)) |
| `BattleEntryCount` | ❌ **불가 — private setter** ([L59](Assets/_Scripts/LDY/Map/LDY_MapManager.cs#L59)). `OnNodeClicked()`에서 Battle 노드일 때만 `++` |
| 맵 구조(`nodePositions`/`nodeTypes`/`connections`) | ❌ 불가 (`[SerializeField] private`) — 다만 **씬 에셋에 하드코딩된 고정 데이터라 저장 대상이 아니다** |

**추가 함정:** `Awake()`가 항상 `BuildNodes()`를 호출해 `activeNodeIndex = -1`, 시작 노드만 unlock 으로 초기화한다([L94-124](Assets/_Scripts/LDY/Map/LDY_MapManager.cs#L94-L124)). 즉 **로드는 반드시 `Awake()` 이후 시점에 덮어써야 한다.**
`CompleteNode(index)`는 public이라 복원에 쓸 수는 있지만 `onMapChanged` 이벤트를 쏘고 `activeNodeIndex`를 리셋하는 부작용이 있어 일괄 복원용으로는 부적합하다.

### A-4. 저장 대상이 **아닌** 것 (전투 중간 상태 — 정책상 제외)

| 클래스 | 파일 | 성격 |
|---|---|---|
| `LSO_Deck` | [LSO_Deck.cs](Assets/_Scripts/LSO/Deck/LSO_Deck.cs) | 뽑을더미/손패/버린더미. 클래스 주석에 "세이브 대상이 아니다"라고 명시됨. **참고: 이 클래스를 `new` 하는 곳이 코드베이스에 하나도 없다 — 현재 미사용** |
| `LDY_ActionPointManager` | [LDY_ActionPointManager.cs](Assets/_Scripts/LDY/LDY_ActionPointManager.cs) | 턴 단위 행동력. 씬 로컬 `public static instance` (DontDestroyOnLoad 아님) |
| `LDY_CardPlacer` | [LDY_CardPlacer.cs](Assets/_Scripts/LDY/LDY_CardPlacer.cs) | `CurrentCost`/`maxCost`. 스테이지SO가 `SetMaxCost()`로 매번 세팅 |
| `LDY_BoardManager` / `LDY_TurnManager` / `LDY_DeathHandler` | Assets/_Scripts/LDY/ | 씬마다 새로 생겨 `GameManager`에 자기를 등록. 전투 종료와 함께 소멸 |

### A-5. **존재하지 않는 것**

- **재화(골드/코인) 시스템 없음.** `gold`/`currency`/`coin` 전수 검색 결과, `LDY_MapTheme.cs`의 색상 이름(`gold`, `goldDim`)만 나온다.
- **유물(Relic/Artifact) 시스템 없음.** 관련 클래스·필드·enum 전부 0건.
- **전투 승리 판정 없음.** `victory`/`GameOver`/`BattleEnd` 등 전수 검색 결과 0건. `LDY_MapManager.CompleteActiveNode()`를 호출하는 곳은 **테스트 버튼 [KTH_TestClearButton.cs:36](Assets/_Scripts/KTH/Map/KTH_TestClearButton.cs#L36) 하나뿐**이다.
- **보상 선택 UI 없음.** `KTH_Reward.UnlockByStage()`를 호출하는 곳이 코드베이스에 없다(정의만 있고 호출처 0건).

> ⚠️ **정책상의 주 저장 트리거인 "전투 승리 + 보상 선택 완료" 시점이 코드에 아직 존재하지 않는다.** 세이브를 붙이려면 그 시점을 만드는 작업이 선행되거나 병행되어야 한다.

### A-6. 런/메타 구분이 불명확한 항목

- `KTH_Reward.unlockedPieces` / `unlockedWills`: 스테이지 클리어 시 해금되며 `DontDestroyOnLoad`로 유지된다. 하지만 **런이 끝날 때 초기화하는 코드가 없다**(`ResetUnlocks()` 호출처 0건). 런 단위인지 메타 단위인지 **코드만으로는 불명확** — 기획 확인 필요.
- `KTH_WillRecord`: 클래스 주석에 "메타 진행"이라 적혀 있으므로 meta.json 대상으로 판단.
- `GameSaveData.stage`: `CreateDefault()`에서 0으로 두고 **이후 아무도 읽거나 쓰지 않는다.** `LDY_MapManager`의 노드 진행도와 중복 개념인지 **불명확**.

---

## B. 식별자(ID) 상태

### B-1. 데이터 SO 목록과 ID 필드 유무

| 데이터 | 타입 | 파일 | 저장 가능한 문자열 ID | 현재 구분 수단 |
|---|---|---|---|---|
| 카드 | ScriptableObject | [LSO_CardSO.cs](Assets/_Scripts/LSO/Deck/Data/LSO_CardSO.cs) | ❌ **없음** | **에셋 참조 그 자체**. 표시용 `AnimalName`은 `animal.animalName`을 그대로 넘긴 것 |
| 기물(동물) | ScriptableObject | [LSO_AnimalSO.cs](Assets/_Scripts/LSO/Animal/Data/LSO_AnimalSO.cs) | ❌ **없음** | `animalName`(string)이 있으나 `[Header("Tool Tip")]` 아래의 **표시용**이며 유일성 보장 없음 |
| 유언 | ScriptableObject | [DLJ_WillDataSO.cs](Assets/_Scripts/DLJ/Will/DLJ_WillDataSO.cs) | ⚠️ **enum이 사실상 키** | `willType`(`LSO_WillType`). `DLJ_WillDatabaseSO.Get(willType)`로 조회 가능 → **유일하게 ID 해석 경로가 갖춰진 데이터** |
| 유언 타입 | enum | [LSO_WillType.cs](Assets/_Scripts/LSO/Will/LSO_WillType.cs) | — | `Curse, Rage, Succession, Contract, Sacrifice` (5종) |
| 스테이지 | ScriptableObject | [LDY_StageSO.cs](Assets/_Scripts/LDY/Stage/LDY_StageSO.cs) | ❌ **없음** | `stageName`(표시용), `sceneName`(씬 이름). 노드↔스테이지 매핑은 [LDY_StageRouter](Assets/_Scripts/LDY/Stage/LDY_StageRouter.cs)의 `nodeIndex`(int) + `nodeType`(enum) |
| 유물 | — | — | — | **시스템 자체가 없음** |

### B-2. 에셋 이름 예시 (실제 파일)

```
Assets/_SO/LSO/CorvoCard.asset          ← LSO_CardSO
Assets/_SO/LSO/Corvo.asset              ← LSO_AnimalSO
Assets/_SO/LSO/AbilityTest.asset
Assets/_SO/LDY/NewStage.asset           ← LDY_StageSO
Assets/_SO/DLJ/WillData/CurseWillData.asset      ← DLJ_WillDataSO
Assets/_SO/DLJ/WillData/RageWillData.asset
Assets/_SO/DLJ/DLJ_WillDatabase.asset            ← DLJ_WillDatabaseSO
Assets/Resources/DLJ/DLJ_WillDatabase.asset      ← 같은 이름이 Resources에도 하나 더 있음(동일 에셋인지 불명확)
```

### B-3. 🔴 깨지기 쉬운 지점 (별도 표시)

| # | 위치 | 무엇에 의존 | 무엇이 깨지나 |
|---|---|---|---|
| **B-3-1** | [KTH_DeckBuilderManager.cs](Assets/_Scripts/KTH/CardManager/CardBuild/KTH_DeckBuilderManager.cs#L46) `_inventoryIndices` | **`cardDatabase` 배열 인덱스** | 인스펙터에서 카드 순서를 바꾸거나 중간에 카드를 추가/삭제하면 **저장된 덱이 전혀 다른 카드로 바뀐다.** `ResolveUnclaimedIndex()`([L197](Assets/_Scripts/KTH/CardManager/CardBuild/KTH_DeckBuilderManager.cs#L197))가 중복 카드를 "몇 번째 슬롯인가"로 구분하고 있어 더 취약 |
| **B-3-2** | [LDY_StageRouter.NodeEntry.nodeIndex](Assets/_Scripts/LDY/Stage/LDY_StageRouter.cs#L18-L19) | **`LDY_MapManager.nodePositions` 배열 순서** | 맵 노드를 하나 끼워 넣으면 이후 모든 노드의 스테이지 배정이 밀린다 |
| **B-3-3** | `LDY_MapManager.Nodes[i]` 의 인덱스 | **`nodePositions` 배열 순서** | 세이브에 노드 인덱스를 그대로 적으면, 맵 씬 에셋을 편집하는 순간 기존 세이브의 진행도가 엉뚱한 노드에 들어간다 |
| **B-3-4** | [KTH_Reward](Assets/_Scripts/KTH/Reward/KTH_Reward.cs#L14-L18) / [KTH_WillRecord](Assets/_Scripts/KTH/Record/KTH_WillRecord.cs#L15-L19) 의 `List<string>` ID | **인스펙터에 손으로 적는 자유 문자열** | ID → 실제 SO 에셋을 찾아주는 조회 테이블이 **어디에도 없다.** 오타를 잡을 방법이 없고, 저장은 되지만 로드 후 그 ID로 무엇을 열어줄지 결정할 수 없다 |
| **B-3-5** | `LSO_CardSO` / `LSO_AnimalSO` | **에셋 참조** | 저장할 문자열이 아예 없다. 에셋 이름(`CorvoCard`)으로 대신하면 파일명 변경 = 세이브 파괴 |

---

## C. 랜덤 사용 지점

### C-1. 게임플레이에 영향 (재현성 문제 있음)

| 파일:줄 | API | 용도 | 시드 |
|---|---|---|---|
| [KTH_DeckManager.cs:146](Assets/_Scripts/KTH/CardManager/CardDeck/KTH_DeckManager.cs#L146) | `UnityEngine.Random.Range` | **드로우할 카드 선택** | ❌ 전역 |
| [LSO_Dodge.cs:33](Assets/_Scripts/LSO/Ability/LSO_Dodge.cs#L33) | `UnityEngine.Random.value` | 회피 판정 | ❌ 전역 |
| [LSO_Frail.cs:39](Assets/_Scripts/LSO/Ability/LSO_Frail.cs#L39) | `Random.value` | 즉사(취약) 판정 | ❌ 전역 |
| [LSO_Deck.cs:18,36,139](Assets/_Scripts/LSO/Deck/LSO_Deck.cs#L34-L45) | `System.Random` | 덱 셔플 (`ShuffleInPlace`, Fisher-Yates) | ✅ **생성자가 `int? randomSeed`를 받는다** |

### C-2. 연출 전용 (저장 무관)

| 파일 | 용도 |
|---|---|
| [LDY_MapNodeView.cs:49](Assets/_Scripts/LDY/Map/LDY_MapNodeView.cs#L49) | 노드 반짝임 위상 오프셋 |
| [LDY_UIShootingStars.cs](Assets/_Scripts/LDY/Map/LDY_UIShootingStars.cs) (6곳) | 유성 연출 |
| [LDY_UIStarfield.cs](Assets/_Scripts/LDY/Map/LDY_UIStarfield.cs) (8곳) | 별밭 배경 |

### C-3. 판정

**시드 기반 재현 불가 — 전역 랜덤을 그냥 쓰고 있다.**

근거:
1. 시드를 받을 수 있는 유일한 클래스 `LSO_Deck`이 **코드베이스 전체에서 한 번도 `new` 되지 않는다.** 즉 시드 가능한 구조는 만들어졌으나 **사용되지 않는 죽은 코드**다.
2. 실제로 카드를 뽑는 [KTH_DeckManager.DrawCards()](Assets/_Scripts/KTH/CardManager/CardDeck/KTH_DeckManager.cs#L112-L148)는 `LSO_Deck`을 거치지 않고 `UnityEngine.Random.Range`를 직접 부른다. 덱에서 카드를 제거하지도 않아 **같은 카드가 중복해서 뽑힐 수 있다**(셔플 개념 자체가 없음).
3. `UnityEngine.Random.InitState()` 호출처 0건.

**세이브 관점 영향:**
- 스테이지 단위 저장이므로 **전투 중간 랜덤 상태는 저장하지 않아도 된다**(정책상 전투 중간 저장 안 함).
- 다만 맵 생성은 랜덤이 아니라 **씬 인스펙터에 하드코딩된 고정 배열**이므로, 맵 시드를 저장할 필요는 없다. 대신 B-3-3의 인덱스 취약성이 그대로 남는다.
- 향후 상점 진열·이벤트 결과에 랜덤을 넣으면 그 시점에 런 시드 저장이 필요해진다. 지금은 해당 없음.

---

## D. 씬 구성과 전환 흐름

### D-1. 씬 목록 (15개)

| 씬 파일 | 성격 |
|---|---|
| `Assets/Scenes/SampleScene.unity` | Unity 기본 씬. **Build Settings에 등록된 유일한 씬** |
| `Assets/_Scenes/LDY/LDY_MapScene.unity` | 맵(별자리) 씬 |
| `Assets/_Scenes/LDY/LDY_TestScene.unity` | 전투 테스트 |
| `Assets/_Scenes/KTH/KTH_BuildDeckScene.unity` | 덱 빌드 |
| `Assets/_Scenes/KTH/KTH_StageScene.unity` | 스테이지 |
| `Assets/_Scenes/KTH/KTH_TestScene.unity` | 테스트 |
| `Assets/_Scenes/KTH/KTH_SoundManagerScene.unity` | 사운드 테스트 |
| `Assets/_Scenes/KTH/DLJ/DLJ_TestScene.unity` | 테스트 |
| `Assets/_Scenes/DLJ/DLJ_TestScene.unity` | 테스트 |
| `Assets/_Scenes/JSJ/JSJ_TestScene.unity` | 테스트 |
| `Assets/_Scenes/LSO/LSO_TestScene.unity` | 테스트 |
| `Assets/_Scenes/LSO/LSO_UI Scene.unity` | UI |
| `Assets/_Scenes/Shared_TestScene.unity` | 공용 테스트 |
| `Assets/_Recovery/0.unity`, `0 (1).unity` | 복구 잔여물로 보임 |

### D-2. 🔴 Build Settings

```yaml
# ProjectSettings/EditorBuildSettings.asset
m_Scenes:
- enabled: 1
  path: Assets/Scenes/SampleScene.unity
```

**등록된 씬이 `SampleScene` 하나뿐이다.** 코드는 씬을 전부 **문자열 이름**으로 로드한다:
- `LDY_MapManager.mapSceneName` / `battleSceneName` / `bossSceneName` (기본값 `"MapScene"`, `"BattleScene"`, `"BossScene"` — 실제 씬 파일명과도 불일치)
- `LDY_StageSO.sceneName`
- `KTH_DeckBuilderManager.nextSceneName` (기본값 `"KTH_BattleScene"` — **그런 이름의 씬 파일이 없다**)

→ 에디터에서 열려 있는 씬끼리는 동작할 수 있으나, **빌드에서는 전부 실패한다.** 세이브 로드 후 "저장된 지점의 씬으로 복귀" 를 구현하려면 이 등록이 선행되어야 한다.

### D-3. 씬 간 데이터 전달 방식

| 방식 | 대상 |
|---|---|
| **`DontDestroyOnLoad` 싱글톤** | `GameManager`([L36](Assets/_Scripts/LSO/Manager/GameManager.cs#L36)), `LDY_MapManager`([L70](Assets/_Scripts/LDY/Map/LDY_MapManager.cs#L70)), `KTH_DeckDataPersistent`([L17](Assets/_Scripts/KTH/CardManager/CardBuild/KTH_DeckDataPersistent.cs#L17)), `KTH_Reward`([L42](Assets/_Scripts/KTH/Reward/KTH_Reward.cs#L42)), `KTH_WillRecord`([L26](Assets/_Scripts/KTH/Record/KTH_WillRecord.cs#L26)), `LDY_SceneTransition`, `KTH_DontDestroy`(사운드) |
| **`static class`** | `LDY_StageSelection.Pending` — 맵→전투 씬으로 스테이지SO 1개 전달 |
| **씬 로컬 static** | `KTH_DeckBuilderManager.Instance`, `LDY_ActionPointManager.instance` (DontDestroyOnLoad 없음 → 씬 전환 시 소멸) |
| **ScriptableObject** | 데이터 정의용으로만 쓰고, **런타임 상태를 SO에 써 넣는 패턴은 없다** |

### D-4. 런 상태가 씬 전환을 살아남는 방식 (서술)

```
[KTH_BuildDeckScene]
  KTH_DeckBuilderManager (씬 로컬)
    └ 완료 버튼 → GetCurrentInventoryCardData()
                → KTH_DeckDataPersistent.Instance.SaveInventory(list)   ★여기서 DontDestroyOnLoad로 넘어감
                → SceneManager.LoadScene(nextSceneName)

[LDY_MapScene]
  LDY_MapManager (DontDestroyOnLoad)
    └ Awake: BuildNodes() — 인스펙터 배열로 노드 재구성, 시작 노드만 unlock
    └ 노드 클릭 → OnNodeClicked(index, screenUV)
        ├ Battle/Boss: BattleEntryCount++ (Battle만)
        │              → _stageRouter.Resolve(index, type) → LDY_StageSO
        │              → LDY_StageSelection.Select(stage)   ★static으로 다음 씬에 전달
        │              → LDY_SceneTransition → SceneManager.LoadScene(stage.SceneName)
        └ Shop/Event:  씬 전환 없이 UnityEvent로 팝업만 열고 즉시 CompleteActiveNode()

[전투 씬]
  LDY_StageDirector.Start()
    └ LDY_StageSelection.Consume() ?? defaultStage
    └ LDY_IStageSetupStep 들을 순서대로 Setup(stage)
  KTH_DeckManager.Awake()
    └ KTH_DeckDataPersistent.Instance.savedInventory → cardDatabase 로 복사   ★덱이 여기로 들어옴
  ...전투...
  KTH_TestClearButton (테스트용 버튼)
    └ LDY_MapManager.Instance.CompleteActiveNodeAndReturnToMap()
        → CompleteNode(activeNodeIndex): isCleared=true, 다음 노드들 isUnlocked=true
        → SceneManager.LoadScene(mapSceneName)

[LDY_MapScene 재진입]
  LDY_MapUIController.Start() (씬 로컬 — 매번 새로 생성)
    └ LDY_MapManager.Instance (살아남은 인스턴스) 를 우선 사용
    └ CurrentNodeIndex 로 플레이어 토큰 위치 복원
```

**요점:** 런 상태는 씬에 저장되지 않고 **전적으로 `DontDestroyOnLoad` 인스턴스의 메모리에만 존재한다.** 게임을 끄면 전부 사라진다. 저장 지점도, 복원 지점도 아직 없다.

**⚠️ 맵 씬을 다시 로드할 때의 함정:** 맵 씬에는 `LDY_MapManager` 컴포넌트가 씬 오브젝트로 배치돼 있으므로, 복귀 시 두 번째 인스턴스가 생겼다가 `Awake()`에서 `Destroy` 된다([L63-67](Assets/_Scripts/LDY/Map/LDY_MapManager.cs#L63-L67)). `LDY_MapUIController`가 [L30](Assets/_Scripts/LDY/Map/LDY_MapUIController.cs#L30)에서 굳이 `Instance`를 다시 잡는 이유가 이것이다. **세이브 로드 코드도 같은 타이밍 문제를 겪는다.**

---

## E. 기존 저장 코드

### E-1. 런타임: **없음**

`Assets/_Scripts/**` 전수 검색 결과:

| 검색어 | 런타임 히트 |
|---|---|
| `PlayerPrefs` | **0건** |
| `JsonUtility` | **0건** |
| `Application.persistentDataPath` | **0건** |
| `File.` / `StreamWriter` | **0건** |
| `Newtonsoft` | **0건** |
| `System.IO` | 1건 — **에디터 전용** |

### E-2. 유일한 `System.IO` 사용처 (에디터 전용, 세이브와 무관)

[LDY_MapSceneBuilder.cs:1](Assets/_Scripts/LDY/Map/Editor/LDY_MapSceneBuilder.cs#L1) — `using System.IO;`. `Assets/_Scripts/LDY/Map/Editor/` 아래의 씬 자동 생성 에디터 툴이다.

### E-3. 설정 저장도 없음

[KTH_SoundSettingManager.cs](Assets/_Scripts/KTH/SoundManager/KTH_SoundSettingManager.cs#L16-L18) 는 `Start()`에서 슬라이더 3개를 **매번 `1f`로 하드코딩**한다. 볼륨은 `KTH_SoundManager` → `KTH_SfxPlayer`/`KTH_BgmPlayer`로 흘러갈 뿐 어디에도 저장되지 않는다.

→ **settings.json 은 완전히 새로 만들어야 한다. 재사용할 기존 코드가 없다.**

---

## F. 환경

| 항목 | 값 |
|---|---|
| **Unity 버전** | `6000.3.6f1` (revision `bbb010bdb8a3`) — `ProjectSettings/ProjectVersion.txt` |
| **렌더 파이프라인** | URP 17.3.0 |
| **Newtonsoft.Json** | ❌ **미설치** |
| **어셈블리 정의(.asmdef)** | ❌ **0개** |

### F-1. Newtonsoft 미설치 근거

1. `Packages/manifest.json` — `com.unity.nuget.newtonsoft-json` 없음
2. `Packages/packages-lock.json` — `newtonsoft` 문자열 0건 (전이 의존성으로도 안 들어옴)
3. `Assembly-CSharp.csproj` — `Newtonsoft` 참조 0건
4. `Library/PackageCache/` — newtonsoft 폴더 없음

> 참고: `com.unity.visualscripting` 1.9.9 가 설치돼 있어 흔히 Newtonsoft를 끌고 오지만, 이 버전의 lock 파일상 의존성은 `com.unity.ugui` + `com.unity.modules.jsonserialize` 뿐이라 **끌려오지 않았다.**

**선택지:**
- (a) Package Manager에서 `com.unity.nuget.newtonsoft-json` 추가 → `Dictionary` / 다형성 / null 처리가 필요하면 사실상 필수
- (b) `JsonUtility`로 버틴다 → `Dictionary` 직렬화 불가, `null` 필드 처리 불가, 최상위 배열 불가. `LSO_CardCollection`의 `Dictionary<LSO_CardSO,int>`를 배열로 평탄화해야 함(이미 `ToSaveData()`가 그 일을 하고 있으므로 불가능하진 않음)

### F-2. 어셈블리 구성

asmdef가 하나도 없어 모든 런타임 스크립트가 **`Assembly-CSharp` 단일 어셈블리**에 들어간다.
- `Assembly-CSharp.csproj` — 런타임 전부 (DLJ / KTH / LDY / LSO 폴더 전체)
- `Assembly-CSharp-Editor.csproj` — `Editor/` 폴더들
- `Assembly-CSharp-firstpass.csproj` — `Assets/Plugins/` (DOTween 등)

**세이브 관점:** 새 세이브 코드를 어느 폴더에 두든 컴파일 순환 문제는 없다. 다만 **컴파일 시간이 전부 한 덩어리**이고, 팀원 코드에 대한 의존을 어셈블리 경계로 막을 수 없다.

### F-3. 기타

- **`Assets/Resources/`가 존재한다** — `Assets/Resources/DLJ/DLJ_WillDatabase.asset`, `DOTweenSettings.asset`. ID→SO 해석기를 만들 때 `Resources.Load` 경로를 쓸 여지가 있다. 다만 `Assets/_SO/DLJ/DLJ_WillDatabase.asset` 에도 동명 에셋이 있어 **어느 쪽이 실사용인지 불명확**(에셋 GUID를 대조하지 않음).
- 입력: `com.unity.inputsystem` 1.18.0 (`LDY_CardPlacer`) 과 레거시 `Input` (`KTH_DeckBuilderManager`, `KTH_SoundSettingManager`) **혼용 중**. 세이브와 무관하나 참고.

---

## 1. 저장해야 할 항목 최종 목록

> 표기: ✅ 지금 값이 존재함 / ⚠️ 시스템은 있으나 런/메타 구분이 불명확 / 🔲 시스템 자체가 아직 없음(자리만 잡아둠)

### 1-1. `settings.json` — 설정

| 필드 | 타입 | 출처 | 상태 |
|---|---|---|---|
| `masterVolume` | float | `KTH_SoundManager.SetMasterVolume` | 🔲 현재 저장·복원 코드 전무. 값을 보관하는 필드조차 없음 |
| `bgmVolume` | float | `KTH_SoundManager.SetBgmVolume` | 🔲 동일 |
| `sfxVolume` | float | `KTH_SoundManager.SetSfxVolume` | 🔲 동일 |
| `schemaVersion` | int | (신규) | 🔲 |

> `KTH_SoundSettingManager`가 `Start()`에서 무조건 `1f`를 넣으므로, 로드 값을 슬라이더에 반영하는 경로를 새로 만들어야 한다.

### 1-2. `meta.json` — 런을 넘어 영구 누적

| 필드 | 타입 | 출처 | 상태 |
|---|---|---|---|
| `totalWillUseCount` | int | `KTH_WillRecord.GetWillUseCount()` | ✅ 읽기 가능 / ❌ **복원 불가** (`AddWillUse()` +1 밖에 없음) |
| `discoveredCombos` | string[] | `KTH_WillRecord.GetDiscoveredCombos()` | ✅ 읽기·복원 가능 |
| `defeatedBosses` | string[] | `KTH_WillRecord.GetDefeatedBosses()` | ✅ 읽기·복원 가능 |
| `unlockedPieces` | string[] | `KTH_Reward.GetUnlockedPieces()` | ⚠️ 읽기·복원 가능하나 **런/메타 구분 불명확** |
| `unlockedWills` | string[] | `KTH_Reward.GetUnlockedWills()` | ⚠️ 동일 |
| `schemaVersion` | int | (신규) | 🔲 |

### 1-3. `run.json` — 진행 중인 런 1개

| 필드 | 타입 | 출처 | 상태 |
|---|---|---|---|
| `deck` | `{cardId:string, amount:int}[]` | `GameManager.Cards.ToSaveData()` 또는 `KTH_DeckDataPersistent.savedInventory` | ⚠️ **두 곳에 덱이 이중으로 존재한다.** 어느 쪽이 정본인지 결정 필요. 어느 쪽이든 `cardId`가 문자열이 아니라 SO 참조 |
| `mapNodeStates` | `{cleared:bool, unlocked:bool}[]` | `LDY_MapManager.Nodes[i]` | ✅ 읽기·복원 가능 (요소 public 필드) |
| `currentNodeIndex` | int | `LDY_MapManager.CurrentNodeIndex` | ✅ 읽기 / ❌ **복원 불가 — private setter** |
| `activeNodeIndex` | int | `LDY_MapManager.ActiveNodeIndex` | ✅ 읽기 / ❌ **복원 불가 — private setter** |
| `battleEntryCount` | int | `LDY_MapManager.BattleEntryCount` | ✅ 읽기 / ❌ **복원 불가 — private setter** |
| `mapLayoutHash` | string | (신규 — `nodePositions`/`nodeTypes`/`connections` 해시) | 🔲 **B-3-3 방어용.** 맵 에셋이 바뀌면 세이브를 무효 처리하기 위함 |
| `unlockedPieces` / `unlockedWills` | string[] | `KTH_Reward` | ⚠️ meta 인지 run 인지 결정되면 한쪽에만 넣을 것 |
| `stage` | int | `GameSaveData.stage` | ⚠️ 필드는 있으나 **아무도 읽고 쓰지 않는다.** `mapNodeStates`와 중복 개념인지 확인 필요 |
| `maxCost` | int | `GameSaveData.maxCost` | ⚠️ 필드는 있으나 미사용. 실제 코스트는 `LDY_StageSO.summonCostPerTurn` → `LDY_CardPlacer.SetMaxCost()` 로 흐름 |
| `gold` / `relics` | — | — | 🔲 **시스템 자체가 없음.** 스키마 자리만 비워둘지 결정 필요 |
| `randomSeed` | int | — | 🔲 현재 전역 랜덤이라 저장할 시드가 없음. 상점/이벤트에 랜덤이 들어가면 그때 추가 |
| `schemaVersion` | int | (신규) | 🔲 |

---

## 2. 차단 요인 — 남의 파일을 고쳐야 하는 지점

> 팀 요청 목록. 우선순위 순.

### 🔴 P0 — 이게 없으면 세이브가 성립하지 않음

**① `LSO_CardSO` / `LSO_AnimalSO` 에 문자열 ID 필드 추가**
- 파일: [Assets/_Scripts/LSO/Deck/Data/LSO_CardSO.cs](Assets/_Scripts/LSO/Deck/Data/LSO_CardSO.cs), [Assets/_Scripts/LSO/Animal/Data/LSO_AnimalSO.cs](Assets/_Scripts/LSO/Animal/Data/LSO_AnimalSO.cs) (담당: LSO)
- 무엇: `[SerializeField] private string id;` + `public string Id => id;` (또는 동등한 것)
- 왜: **JSON에 적을 수 있는 값이 현재 하나도 없다.** 에셋 참조는 직렬화되지 않고, 에셋 이름에 의존하면 파일명 변경이 세이브를 파괴한다(B-3-5).
- 부수 요청: ID → SO 를 되찾는 조회 테이블(예: `LSO_CardDatabaseSO` 또는 `Resources` 규약). 유언은 `DLJ_WillDatabaseSO.Get(enum)` 이 이미 그 역할을 하므로 **그 패턴을 그대로 따르면 된다.**

**② `GameSaveData.DeckCardsSaveData.cardId` 의 타입 변경**
- 파일: [Assets/_Scripts/LSO/Manager/GameSaveData.cs:9](Assets/_Scripts/LSO/Manager/GameSaveData.cs#L9) (담당: LSO)
- 무엇: `public LSO_CardSO cardId;` → `public string cardId;` (또는 이 struct를 세이브 전용 DTO와 분리)
- 왜: 이름은 `cardId`인데 타입이 `ScriptableObject`다. 현재 상태로는 `JsonUtility`·Newtonsoft 어느 쪽으로도 나가지 않는다(A-2).

**③ `GameManager.SaveData` / `Cards` 에 복원 경로 추가**
- 파일: [Assets/_Scripts/LSO/Manager/GameManager.cs:16-18](Assets/_Scripts/LSO/Manager/GameManager.cs#L16-L18) (담당: LSO)
- 무엇: `{ get; private set; }` 인 두 프로퍼티에 로드용 진입점 추가 (예: `public void LoadFrom(GameSaveData data)`)
- 왜: 값을 되돌려 넣을 public 경로가 전혀 없다. 특히 `SaveData`는 struct라 `GameManager.Instance.SaveData.stage = n` 이 **컴파일조차 되지 않는다.**

**④ `LDY_MapManager` 의 진행도 3개에 복원 경로 추가**
- 파일: [Assets/_Scripts/LDY/Map/LDY_MapManager.cs](Assets/_Scripts/LDY/Map/LDY_MapManager.cs#L46-L59) (담당: LDY — **내 파일**)
- 무엇: `CurrentNodeIndex`(L52), `activeNodeIndex`(L46), `BattleEntryCount`(L59) 를 세이브에서 되돌려 넣을 수 있는 메서드. 그리고 **`Awake()`의 `BuildNodes()`가 초기화한 뒤에 덮어쓸 수 있는 타이밍 훅**(A-3의 함정).
- 왜: 셋 다 private setter라 로드 불가. `Nodes[i]`의 `isCleared`/`isUnlocked`만 복원하면 진행도는 맞지만 **플레이어 토큰은 시작 노드로 돌아가고 난이도 카운터는 0이 된다.**
- 비고: 내 담당 파일이므로 남에게 요청할 필요는 없지만, 세이브 작업의 선행 조건이라 여기 적어둔다.

### 🟠 P1 — 지금 결정하지 않으면 스키마를 고정할 수 없음

**⑤ 덱의 정본을 하나로 정할 것**
- 파일: [KTH_DeckDataPersistent.cs](Assets/_Scripts/KTH/CardManager/CardBuild/KTH_DeckDataPersistent.cs) (KTH) vs [GameManager.Cards](Assets/_Scripts/LSO/Manager/GameManager.cs#L18) / [LSO_CardCollection.cs](Assets/_Scripts/LSO/Deck/LSO_CardCollection.cs) (LSO)
- 무엇: 플레이어 덱이 **두 군데에 따로 산다.** `KTH_DeckDataPersistent.savedInventory`(List, 중복 허용)와 `GameManager.Cards`(Dictionary, 수량 카운트). 둘은 서로를 전혀 모른다.
- 왜: 세이브가 어느 쪽을 적어야 하는지 결정할 수 없고, 한쪽만 복원하면 다른 쪽이 어긋난다. `LSO_CardCollection`은 클래스 주석에 "세이브/로드 대상이다"라고 명시돼 있지만, **실제로 게임이 쓰는 덱은 `KTH_DeckDataPersistent` 쪽이다**([KTH_DeckManager.cs:64](Assets/_Scripts/KTH/CardManager/CardDeck/KTH_DeckManager.cs#L64)).

**⑥ `KTH_Reward` 의 해금이 런 단위인지 메타 단위인지 확정**
- 파일: [Assets/_Scripts/KTH/Reward/KTH_Reward.cs](Assets/_Scripts/KTH/Reward/KTH_Reward.cs) (담당: KTH)
- 무엇: `unlockedPieces` / `unlockedWills` 가 run.json 인지 meta.json 인지
- 왜: 런이 끝날 때 초기화하는 코드가 없어(`ResetUnlocks()` 호출처 0건) **코드만으로 판단이 불가능하다.** 잘못 넣으면 런 리셋 시 영구 해금이 날아가거나, 그 반대가 된다.

**⑦ `KTH_Reward` / `KTH_WillRecord` 의 string ID 규약 정의**
- 파일: [KTH_Reward.cs:14-18](Assets/_Scripts/KTH/Reward/KTH_Reward.cs#L14-L18), [KTH_WillRecord.cs:15-19](Assets/_Scripts/KTH/Record/KTH_WillRecord.cs#L15-L19) (담당: KTH)
- 무엇: 이 문자열들이 무엇을 가리키는지, ①의 카드 ID와 같은 체계인지
- 왜: **ID → 실제 SO 를 찾아주는 테이블이 어디에도 없다.** 저장은 되지만 로드 후 그 ID로 무엇을 열어줄지 결정할 수 없고, 인스펙터 오타를 잡을 수단이 없다(B-3-4).

**⑧ `KTH_WillRecord.totalWillUseCount` 에 setter 추가**
- 파일: [Assets/_Scripts/KTH/Record/KTH_WillRecord.cs:13](Assets/_Scripts/KTH/Record/KTH_WillRecord.cs#L13) (담당: KTH)
- 무엇: 값을 직접 넣는 경로 (`SetWillUseCount(int)` 등)
- 왜: 현재 `AddWillUse()`(+1)뿐이라 **누적 12,000회를 복원하려면 12,000번 호출해야 한다.**

### 🟡 P2 — 세이브 품질에 영향

**⑨ `KTH_DeckBuilderManager._inventoryIndices` 의 배열 인덱스 의존 제거**
- 파일: [Assets/_Scripts/KTH/CardManager/CardBuild/KTH_DeckBuilderManager.cs:46,197](Assets/_Scripts/KTH/CardManager/CardBuild/KTH_DeckBuilderManager.cs#L46) (담당: KTH)
- 무엇: `cardDatabase` 배열 인덱스 대신 ①의 카드 ID로 전환. 덱 목록을 외부에서 주입하는 경로도 필요(현재 `BuildInitialInventory()`가 private).
- 왜: **인스펙터에서 카드 순서를 한 번만 바꿔도 저장된 덱이 전혀 다른 카드로 바뀐다**(B-3-1). 로드 시 덱을 복원해 넣을 방법도 없다.

**⑩ `KTH_SoundManager` 에 볼륨 조회 + 적용 경로 추가**
- 파일: [KTH_SoundManager.cs:69-86](Assets/_Scripts/KTH/SoundManager/KTH_SoundManager.cs#L69-L86), [KTH_SoundSettingManager.cs:16-18](Assets/_Scripts/KTH/SoundManager/KTH_SoundSettingManager.cs#L16-L18) (담당: KTH)
- 무엇: `Set*Volume()` 만 있고 **`Get*Volume()`이 없다.** 그리고 `KTH_SoundSettingManager.Start()`가 슬라이더를 무조건 `1f`로 덮어쓴다.
- 왜: 저장할 값을 읽어올 수 없고, 로드한 값을 넣어도 다음 씬에서 `1f`로 리셋된다.

**⑪ 전투 승리 판정 + 보상 선택 완료 지점 신설**
- 파일: 해당 클래스 없음 (담당: **불명확** — 논의 필요)
- 무엇: **세이브 정책이 정한 주 트리거("전투 승리 + 보상 선택 완료")가 코드에 존재하지 않는다.** `LDY_MapManager.CompleteActiveNode()` 를 부르는 곳은 테스트 버튼([KTH_TestClearButton.cs:36](Assets/_Scripts/KTH/Map/KTH_TestClearButton.cs#L36)) 하나뿐이고, `KTH_Reward.UnlockByStage()` 는 호출처가 0건이다.
- 왜: 자동저장을 걸 훅이 없다. **이 지점이 생기기 전까지는 세이브를 "언제" 부를지 정할 수 없다.**

**⑫ Build Settings 에 씬 등록**
- 파일: `ProjectSettings/EditorBuildSettings.asset` (담당: 공용)
- 무엇: 현재 `SampleScene` 1개만 등록. 코드가 이름으로 로드하는 씬(`MapScene`, `BattleScene`, `BossScene`, `KTH_BattleScene` 등)이 전부 미등록이며, 일부는 **실제 씬 파일명과도 다르다**.
- 왜: "저장된 지점의 씬으로 복귀"를 구현할 수 없다. 에디터에서만 되고 빌드에서 깨진다.

**⑬ Newtonsoft.Json 설치 여부 결정**
- 파일: `Packages/manifest.json` (담당: 공용)
- 무엇: `com.unity.nuget.newtonsoft-json` 추가 여부
- 왜: 미설치 상태(F-1). `JsonUtility`만으로 갈 경우 `Dictionary` 직렬화 불가·`null` 처리 불가·최상위 배열 불가라는 제약을 스키마 설계에 반영해야 한다. **스키마를 확정하기 전에 결정되어야 한다.**

---

## 부록: 조사 방법

- 전수 검색은 `Assets/_Scripts/**` 기준. `Assets/Plugins/`(DOTween), `Assets/TutorialInfo/`, `Library/`, `Temp/`, `obj/` 제외.
- 씬(`.unity`)과 프리팹의 인스펙터 배선은 **열어보지 않았다.** 따라서 "어느 씬에 어떤 매니저가 실제로 배치돼 있는지", "인스펙터에 어떤 SO가 꽂혀 있는지"는 이 문서의 범위 밖이다. 코드에서 읽어낸 구조만 담았다.
- `.asset` 파일의 내부 값(예: `NewStage.asset`의 `stageName`)도 열어보지 않았다.
