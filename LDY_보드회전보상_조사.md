# 보드 회전 보상 연출 — 사전 조사

조사만 수행. 코드/씬 변경 없음.
기준 커밋: `c8cf1e2` (브랜치 LDY)

---

## 0. 먼저 알아야 할 것 — 전투 씬은 두 개고, 둘 다 같은 구조다

맵 씬은 `KTH_StageScene`이고, 거기 붙은 `LDY_StageRouter`가 노드 타입별로 전투 씬을 고른다.

| 노드 | StageSO | 실제 열리는 씬 |
|---|---|---|
| Battle (전 챕터) | `TestStage2.asset` | **`LSO_Test`** |
| Boss | `LSO_TestState.asset` / `BossStage_BullKing.asset` | **`LDY_TestScene`** |

두 씬은 `LDY_SceneBuilder`가 찍어낸 것이라 **보드·카메라 구조가 완전히 동일**하다.
따라서 연출 컴포넌트는 씬에 하드코딩된 좌표가 아니라 `LDY_BoardManager`에서 좌표를 유도하도록 만들면 한 벌로 양쪽을 커버한다.

> 씬 소유권이 갈린다. `LDY_TestScene`은 LDY(20/29 커밋), `LSO_Test`는 KTH(7)+LSO(5). 5번 항목 참고.

---

## 1. 보드의 3D 구조

```
LDY_AutoScene                    (빈 오브젝트, pos 0,0,0)
├── LDY_Board                    (빈 오브젝트, pos 0,0,0)  ← BoardManager.boardOrigin
│   └── Tile_0_0 … Tile_7_7      (64개, 각각 Cube 프리미티브)
│         localPos   = (x, -0.05, z)
│         localScale = (0.98, 0.1, 0.98)
│         layer      = 8 "LDY_Board",  BoxCollider + MeshRenderer(URP/Lit)
├── LDY_GameSystems              ← BoardManager·MoveSystem·AttackSystem·TileHighlighter
│                                  SelectionController·EnemyAI·TurnManager·ActionPointManager
├── LDY_Pieces
│   └── Player_* / Enemy_*       (Cube, scale 0.6, pos (x, 0.3, z))
└── LDY_UI                       (Canvas — Screen Space Overlay)
```

씬 루트에 따로: `Main Camera`, `Directional Light`, `Canvas`(손패/정보창), `CardPlacer`, `EndManager`, `GameManager`, `GameSystem`, `LDY_PauseCanvas`, `LDY_SuccessionResolver`.

측정값 (cellSize = 1, boardOrigin = 월드 원점):

| 항목 | 값 |
|---|---|
| 보드 풋프린트 | x, z ∈ [-0.49, 7.49] (8×8) |
| 타일 윗면 / 아랫면 | y = 0 / y = -0.1 |
| 보드 중심 | **(3.5, -0.05, 3.5)** |
| 기물 높이 | y = 0.3 |

### 보드 전체를 돌리면 무엇이 따라 도는가

**기물의 부모가 두 군데로 갈려 있다.**

- 씬에 미리 놓인 기물 → `LDY_Pieces` 아래
- 런타임 소환 기물 → `LDY_BoardUnitSpawner.cs:35`가 `LSO_AnimalFactory.Create(card, team, board.transform)`으로 만든다. `board`는 `LDY_BoardManager` 컴포넌트이므로 `board.transform`은 **`LDY_GameSystems`**다.

즉 `LDY_Board`만 돌리면 타일만 돌고 기물은 제자리에 남는다.
타일+기물을 한 번에 돌리려면 `LDY_AutoScene`(공통 조상)을 돌리거나, 회전 시점에 피벗을 만들어 필요한 트랜스폼을 모아 붙여야 한다.

### 회전에 대한 코드 쪽 함정

`LDY_BoardManager.GridToWorld` / `WorldToGrid`는 **boardOrigin의 position만 쓰고 rotation을 무시한다** ([LDY_BoardManager.cs:193-208](Assets/_Scripts/LDY/LDY_BoardManager.cs#L193-L208)).

```csharp
Vector3 origin = boardOrigin != null ? boardOrigin.position : Vector3.zero;
return origin + new Vector3(p.x * cellSize, p.y * heightStep, p.z * cellSize);
```

보드를 돌린 상태에서 격자↔월드 변환이 한 번이라도 일어나면 값이 전부 어긋난다. 같은 가정을 깔고 있는 곳:

- `LDY_TileHighlighter.Show` — 하이라이트를 `GridToWorld` 월드 좌표에 생성
- `LDY_BoardManager.Place` — `animal.modelTransform.position`에 월드 좌표 대입
- `DLJ_ObjectHovering` — `_hoveredTransform.position`을 월드로 저장/복원
- `LDY_SelectionController.TryRaycastToGrid` / `LDY_CardPlacer.TryRaycastToGrid` — 레이 히트점을 `WorldToGrid`로 환산

**승리 후에도 플레이어 입력이 안 잠긴다.** `LDY_SelectionController.Update`에는 "게임 끝" 게이트가 없고 턴 가드(`CurrentTurn != Player`)만 있다. 승리 시점에 턴은 보통 Player라 회전 중에도 기물 클릭·이동이 그대로 먹는다. 회전 전에 반드시 잠가야 한다 — 이미 있는 수단으로 충분하다:

- `LSO_WillSelection.BeginBoardInteractionLock()` → `LDY_SelectionController.cs:91`이 이미 존중한다
- `LDY_CardPlacer.SetBoardActive(false)` → `LDY_CardPlacer.cs:372`. CardPlacer는 위 잠금을 안 보므로 별도로 필요하다

---

## 2. 카메라

`Main Camera` (두 전투 씬 동일):

| 항목 | 값 |
|---|---|
| position | (3.5, 8, -4) |
| rotation | euler (55, 0, 0) |
| 투영 | **Perspective**, FOV 60 (vertical) |
| near / far | 0.3 / 1000 |
| Clear Flags | Skybox |
| 컴포넌트 | Camera, AudioListener, URP Additional Camera Data |

`LDY_SceneBuilder.FrameCamera`가 만든 값이다:

```csharp
float center = (Size - 1) * CellSize * 0.5f;              // 3.5
cam.transform.position = boardRoot.position + new Vector3(center, 8f, -4f);
cam.transform.rotation = Quaternion.Euler(55f, 0f, 0f);
```

파생 기저:

```
forward = (0, -0.8192,  0.5736)
up      = (0,  0.5736,  0.8192)
right   = (1,  0,       0     )
```

화면 안에서 보드는 세로로 **중앙 아래 35% ~ 중앙 위 64%** 구간을 차지한다 (근접 모서리가 아래). 화면 하단 1/3이 비어 있고 거기에 손패 UI가 있다.

### 전투 씬에서 카메라를 움직이는 기존 코드

| 위치 | 내용 |
|---|---|
| `LDY_CameraShake` | `Camera.main`을 찾아 **스스로 AddComponent**한 뒤 `transform.localPosition`을 흔든다. 시작할 때 `_baseLocalPos`를 캐시한다. `LDY_BullKingBoss`(돌진 충돌)가 호출 |
| `DLJ/TestCameraMove.cs` | DOTween으로 `Camera.main`의 position·rotation·FOV를 참조 카메라 두 개 사이로 트윈. `playOnSpace`로 스페이스바 트리거. 카메라 연출 선례 |
| `LDY_MapCameraController` | 맵 씬 전용 (전투 씬에는 없음) |

즉 **Camera.main은 이미 두 사람이 건드리는 공유 자원**이다. 특히 `LDY_CameraShake`는 자기가 시작할 때의 localPosition을 기준점으로 잡으므로, 카메라를 트윈하는 도중 셰이크가 들어오면 카메라가 회전 궤도 어딘가에 눌러앉는다.

한 가지 더: 전투 씬 루트의 `Canvas`(InfoPanel/HandCard/Button)는 **Screen Space - Camera**다. 카메라를 움직이면 이 캔버스의 월드 배치가 통째로 따라 움직인다. 화면상 위치는 유지되지만 3D 공간에서 보상 quad와 겹칠 여지가 생긴다.

---

## 3. 보드 뒷면

**보드 판이 없다.** 8×8 큐브 64개가 전부고, 그 아래를 덮는 판·베이스·테이블이 없다.

돌렸을 때 실제로 보이는 것:

1. **컬링으로 사라지진 않는다.** 타일이 Cube(닫힌 6면체)라 아랫면 폴리곤이 존재한다. URP/Lit의 기본 백페이스 컬링과 무관하게 정상적으로 그려진다.
2. **윗면과 똑같이 생겼다.** 같은 머티리얼(흑/백 체커 2종)이라 뒷면도 그냥 체커보드다. "뒷면이 드러났다"는 인상이 안 난다.
3. **타일 사이가 뚫려 보인다.** scale 0.98이라 칸마다 0.02 간격이 있고, Clear Flags가 Skybox라 그 틈으로 스카이박스가 비친다.
4. **거의 검게 나온다.** Directional Light가 euler (50,-30,0)으로 아래를 향하고, 앰비언트는 Skybox 모드에 `AmbientGroundColor = (0.047, 0.043, 0.035)`. 아랫면은 사실상 무광원이다.

→ **뒷판 에셋과 조명 대응이 필요하다.** (5번 항목의 팀 요청 범위)

---

## 4. 승리 → 보상 흐름의 타이밍

```
적 Health.OnDamage
  └─ KTH_GameEndManager.HandleEnemyDamaged        → 1프레임 뒤 재검사
       └─ CheckGameClear()  모든 적 IsDestroyed
            └─ ClearStage()  _isGameEnded = true
                 └─ Co_ClearStage()
                      ├─ while (turnManager.IsAnimating()) yield      ★A
                      ├─ ResolveCurrentStageType()
                      └─ ClearBattleStage()
                           ├─ onBattleClear?.Invoke()                 ★B  (UnityEvent, 현재 비어 있음)
                           └─ LDY_MapManager.CompleteActiveNodeAndReturnToMap()   ★C
                                ├─ willGiveReward 판정
                                ├─ rewardUI.OnRewardResolved 구독
                                ├─ CompleteActiveNode()
                                │    └─ CompleteNode(index)
                                │         └─ TriggerStageReward()
                                │              └─ KTH_GiveReward.GiveStageReward()
                                │                   └─ KTH_RewardChoiceUI.ShowRewards()   ★D  ← UI 등장
                                └─ (보상 대기 중이면 씬 전환 보류)

플레이어가 카드 선택 → 획득 버튼
  └─ KTH_RewardChoiceUI.PlayHideAnimation() → OnRewardResolved
       └─ LDY_MapManager.HandleRewardResolvedThenReturnToMap()
            └─ GoToPostClearScene() → Co_LoadSceneAfterAnimations()   ★E
                 └─ while (IsAnimating()) yield  →  SceneManager.LoadScene(맵)
```

### 끼워 넣을 틈

| 지점 | 파일 소유 | 평가 |
|---|---|---|
| ★A | KTH | 연출 대기가 이미 있다. 여기 더 붙이려면 KTH 파일 수정 |
| ★B `onBattleClear` | KTH (씬 인스펙터) | **코드 수정 없이 씬에서 배선 가능.** 단 fire-and-forget이라 이것만 쓰면 회전이 도는 동안 ★D의 보상 UI가 같은 프레임에 튀어나온다 |
| ★C `CompleteActiveNodeAndReturnToMap` | **LDY** | **최적.** 이 메서드 본문을 코루틴으로 감싸 회전을 먼저 기다린 뒤 `CompleteActiveNode()`를 부르면, ★D가 자동으로 회전 뒤로 밀린다. 패배 경로(`FailActiveNodeAndReturnToMap`)와 무관하고 보스 경로도 같은 함수를 지난다 |
| ★D | KTH | 보상 UI 자체. KTH가 3D quad로 바꿀 대상 |
| ★E | LDY | 이미 `IsAnimating()`을 기다린다. 되돌리기 연출을 넣는다면 여기 |

**추천: ★C.** LDY 소유 파일 안에서 끝나고, 승리 판정·보상 생성·씬 전환 순서를 하나도 안 바꾼다.

### 조사 중 발견한 배선 불일치 (참고)

`KTH_GameEndManager.turnManager`가 **`LDY_TestScene`(보스 씬)에서 비어 있다** (`fileID: 0`). `LSO_Test`에서는 연결돼 있다.
보스 씬에서는 ★A의 대기가 통째로 스킵되므로, 마지막 디졸브가 도는 중에 ★C가 들어온다. 회전 디렉터가 자체적으로 `IsAnimating()`을 기다려야 하는 이유다 (`LDY_TurnManager.IsAnimating()`은 이동·공격·`LDY_DissolveEffect.ActiveCount`를 합쳐서 본다).

또 `onBattleClear` / `onBossClear`는 양쪽 씬 모두 `m_Calls: []` — 아직 아무것도 안 걸려 있다.

---

## 5. 3D 오브젝트 클릭 판정 — KTH에게 넘길 참고 자료

프로젝트 전체에서 월드 공간 레이캐스트는 딱 세 군데다.

```csharp
// LDY_SelectionController.cs:198-208
private bool TryRaycastToGrid(out Vector3Int gridPos)
{
    gridPos = default;
    if (targetCamera == null) return false;

    var ray = targetCamera.ScreenPointToRay(Mouse.current.position.ReadValue());
    if (!Physics.Raycast(ray, out var hit, 100f, boardLayerMask)) return false;

    gridPos = board.WorldToGrid(hit.point);
    return board.IsInside(gridPos);
}
```

`LDY_CardPlacer.cs:332-342`도 완전히 같은 형태. (세 번째는 `DLJ_WillTest.cs:32`인데 마스크 없는 `RaycastAll`이라 참고 대상 아님.)

### 지켜야 할 규칙

1. **New Input System.** 프로젝트 Active Input Handling이 New 전용이다. `Input.mousePosition`은 예외를 던진다. `Mouse.current.position.ReadValue()`를 쓸 것. `Mouse.current` null 체크도 (`LDY_SelectionController.cs:72`).
2. **카메라는 `[SerializeField]` + Awake 폴백.** `LDY_SelectionController.cs:44-45`가 `targetCamera == null`이면 `Camera.main`을 넣는다.
3. **`LayerMask`로 거른다.** 거리는 100f 관례.
4. **quad에 Collider가 필요하다.** `Physics.Raycast`는 MeshRenderer를 안 본다. Quad 프리미티브는 MeshCollider가 붙어 나오지만, 보상 카드처럼 두께 없는 판은 BoxCollider(z 두께 0.05 정도)가 클릭이 안정적이다.
5. **일시적으로 끄는 패턴이 이미 있다.** `LDY_CardPlacer.SetBoardActive(bool)` (`LDY_CardPlacer.cs:372-388`) — 마스크를 잠깐 비워 두는 방식. 보상 quad도 등장 애니메이션 중에는 같은 식으로 막으면 된다.

### 레이어 구성

`ProjectSettings/TagManager.asset`:

| # | 이름 | 상태 |
|---|---|---|
| 0-2 | Default / TransparentFX / Ignore Raycast | 빌트인 |
| 3 | Board | **미사용** (전투 씬에 0개) |
| 4 | Water | 빌트인 |
| 5 | UI | 사용 중 |
| 8 | **LDY_Board** | 타일 64개. `boardLayerMask = m_Bits: 256` |
| 10 | WillTest | **미사용** |
| 6, 7, 9, 11–31 | — | 비어 있음 |

**보상용 레이어를 새로 만들어야 한다.** 레이어 8을 재사용하면 보상 quad가 `SelectionController`/`CardPlacer`의 보드 레이캐스트에 걸려서 `WorldToGrid`가 엉뚱한 칸을 뱉는다.

→ **9번 슬롯에 `LDY_Reward` 추가**를 제안한다 (LDY_Board 바로 옆이라 묶여 보인다). 3번 `Board`와 10번 `WillTest`는 잔재라 재사용하면 나중에 헷갈린다.

### KTH_RewardOptionUI를 3D로 옮길 때 깨지는 것

현재 구현이 uGUI에 강하게 묶여 있다:

| 현재 | 3D quad에서 |
|---|---|
| `[RequireComponent(typeof(Button))]`, `cardButton.onClick` (`:57`, `:331`) | Button이 없다. 위 레이캐스트로 대체 |
| `rectTransform.DOAnchorPos(...)` (`:306`, `:369`, `:378`) | `transform.DOLocalMove(...)` |
| `rectTransform.anchoredPosition` 기준점 저장 (`:298`) | `transform.localPosition` |
| `LayoutRebuilder.ForceRebuildLayoutImmediate` (`KTH_RewardChoiceUI.cs:218`) | LayoutGroup이 없으므로 직접 배치 |
| `iconImage.sprite` (Image) | Quad 머티리얼의 `_BaseMap` |

`Awake`의 `if (cardButton != null)`은 `cardButton`이 아직 대입 전이라 항상 null이다 — 리스너가 안 붙고, 실제 클릭은 인스펙터에서 Button의 OnClick에 직접 걸어둔 것으로 보인다. 3D 전환 때 어차피 다시 짜야 하는 부분이라 같이 정리하면 된다.

### 보상 Canvas의 현재 상태 (KTH 확인 필요)

`KTH_RewardChoiceUI`가 붙은 Canvas는 `KTH_StageScene`(맵 씬)에 있고 **Screen Space - Camera**, `m_Camera`가 맵 씬의 Main Camera, `planeDistance: 100`이다. 그런데 이 오브젝트는 `Awake`에서 `SetParent(null)` + `DontDestroyOnLoad`로 전투 씬까지 넘어간다 — 그때 맵 카메라는 파괴돼 있으므로 카메라 참조가 null이 되고, Unity가 Overlay처럼 그린다.

지금은 우연히 동작하지만 3D 전환 시 정리 대상이다.

---

## 6. 소유권

작성자 매핑 (git): `Leedoyun0117` = LDY, `taeho0919` = KTH, `Caramel_appple` = LSO, `L0GINC0DE` = DLJ, `정성진` = JSJ.

| 요소 | 소유 |
|---|---|
| `LDY_BoardManager`, `GridToWorld`/`WorldToGrid` | LDY |
| `LDY_SelectionController`, `LDY_CardPlacer` (레이캐스트) | LDY |
| `LDY_TileHighlighter`, `LDY_DissolveEffect`, `LDY_CameraShake` | LDY |
| `LDY_MapManager` (★C 지점) | LDY |
| `LDY_SceneBuilder` (보드·카메라 생성) | LDY |
| **`LDY_TestScene`** (보스 전투 씬) | LDY (20/29 커밋) |
| `KTH_GameEndManager` (★A/★B) | KTH |
| `KTH_GiveReward`, `KTH_Reward`, `KTH_RewardChoiceUI`, `KTH_RewardOptionUI` | KTH |
| **`KTH_StageScene`** (맵 씬, 보상 UI 원본) | KTH |
| **`LSO_Test`** (일반 전투 씬) | KTH(7) + LSO(5) |
| 보드 타일 머티리얼 / 조명 / 뒷판 에셋 | LSO (미존재 — 신규) |
| `TestCameraMove` (카메라 트윈 선례) | DLJ |
| `DLJ_ObjectHovering` (선택 기물 부양) | DLJ |

---

# 산출물

## A. 보드 회전 vs 카메라 회전 — **보드 회전을 추천한다**

### 근거

1. **원하는 그림을 카메라로는 못 만든다.** 보드는 지면 위에 놓여 있고 뒷면은 아래를 향한다. 카메라가 뒷면을 보려면 지면 아래로 내려가야 하는데, 그러면 "보드를 뒤집었다"가 아니라 "바닥을 파고 들어갔다"가 된다. 벅샷 룰렛의 탄창 확인도 총을 **기울여서** 보여주는 연출이지 카메라가 총 밑으로 들어가는 게 아니다.

2. **카메라는 이미 세 사람이 쥐고 있는 공유 자원이다.**
   - `LDY_CameraShake`가 `Camera.main`에 런타임으로 자기를 붙이고 `localPosition`을 기준점 캐시해서 흔든다
   - DLJ의 `TestCameraMove`가 같은 카메라를 DOTween으로 움직인다
   - 전투 씬 메인 `Canvas`가 Screen Space - Camera라 카메라를 돌리면 UI의 월드 배치가 통째로 따라 돈다

   보드 회전은 이 셋 중 무엇도 안 건드린다.

3. **회전을 읽는 게임플레이 코드가 없다.** `GridToWorld`/`WorldToGrid`가 rotation을 무시한다는 건 회전 중 격자 변환이 위험하다는 뜻이기도 하지만, 반대로 **트랜스폼을 돌려도 격자 상태는 한 톨도 안 망가진다**는 뜻이다. 입력만 잠그면 나머지는 순수 시각 효과다. 그리고 연출이 끝나면 곧장 맵 씬으로 넘어가므로 되돌릴 필요조차 없다.

4. **나중에 합칠 수 있다.** 보드 회전 위에 카메라 푸시인을 얹는 건 나중에 KTH/DLJ가 독립적으로 추가 가능하다. 반대는 안 된다.

### 회전 사양

```
피벗 위치 : 보드 중심 (3.5, -0.05, 3.5)
             = boardOrigin.position + (3.5, -0.05, 3.5) * cellSize
회전 축   : 월드 X
각도      : 180°
```

- 타일 중심이 전부 y = -0.05에 있으므로, 그 높이의 X축을 중심으로 180° 돌리면 **보드가 제자리에서 앞뒤 면만 맞바뀐다**. 화면 풋프린트가 그대로라 재프레이밍이 필요 없다.
- 8×8 체커라 좌우 반전이 눈에 안 띈다.
- 지속 0.9–1.2초, `Ease.InOutCubic` 또는 끝에 살짝 오버슛.

**대안 각도 145°** — 뒷면이 카메라를 정면으로 마주 본다.
공식: `θ = 90° + 카메라 피치 = 90 + 55 = 145`. 뒷면 법선 `(0,-1,0)`을 X축으로 θ 돌리면 `(0, -cosθ, -sinθ)`이고, 카메라 방향 `(0, 0.8192, -0.5736)`과 일치하는 해가 145°다.
보상 판을 보드 뒷면에 **눕혀서** 붙일 거라면 이쪽이 정면으로 보여 가독성이 좋다. 대신 보드가 이젤처럼 서므로 인상이 달라진다.

### 보드 회전을 택했을 때 반드시 처리해야 하는 것

| 문제 | 대응 |
|---|---|
| 기물 부모가 `LDY_Pieces` / `LDY_GameSystems` 둘로 갈림 | 런타임에 피벗을 만들어 `boardOrigin` + 생존 기물을 붙인다 (씬 수술 불필요, `LDY_SceneBuilder` 재생성에도 안 날아감, 두 전투 씬 공용) |
| 승리 후에도 보드 클릭이 먹음 | `LSO_WillSelection.BeginBoardInteractionLock()` + `LDY_CardPlacer.SetBoardActive(false)` |
| 생존 플레이어 기물이 보드와 같이 뒤집혀 아래로 감 | 회전 직전 Renderer 페이드아웃 (0.3–0.4초). `LDY_DissolveEffect.PlayOn`은 GameObject를 파괴하고 `IsAnimating()`에 잡히므로, 여기서는 Renderer만 끄는 쪽이 안전하다 |
| 보스 씬에서 마지막 디졸브가 아직 도는 중일 수 있음 | 디렉터가 시작 전에 `LDY_TurnManager.IsAnimating()`을 직접 기다린다 |
| 뒷면이 앞면과 똑같고 어둡고 틈이 뚫림 | 뒷판 에셋 + 조명 — 팀 요청 (아래 D) |

---

## B. 보상 배치 기준점 (앵커) 제안

### `LDY_RewardAnchor` — 빈 오브젝트 하나

| 항목 | 값 | 유도식 |
|---|---|---|
| position | **(3.5, 1.3, 1.8)** | `boardCenter + (0, 1.35, -1.7)` |
| rotation | **euler (55, 0, 0)** | `Camera.main.transform.rotation` |
| scale | (1, 1, 1) | — |
| 부모 | 씬 루트 또는 `LDY_AutoScene` — **회전 피벗의 자식으로 두지 않는다** | |

### 왜 이 좌표인가

카메라 (3.5, 8, -4) / 55° / FOV 60 기준으로 계산했다.

- 카메라까지 거리 8.86, 카메라 정면축에서 **화면 중앙보다 18% 위** → 카드가 중앙에 오고 아래쪽에 "획득" 버튼 자리가 남는다
- 그 거리에서 화면 세로 10.2 유닛 / 가로 18.1 유닛(16:9)
- 카드 1.6 × 2.3 이면 한 장이 화면 높이의 23%, 세 장 + 간격이 가로의 30%
- 뒤집힌 보드 뒷면(y ≈ 0) 위 1.3 유닛에 떠 있고, 보드 중앙선보다 앞쪽(z 1.8)이라 플레이어 쪽으로 나온다 → "드러난 공간에서 고른다"는 인상

### 왜 회전 피벗의 자식이 아닌가

앵커를 피벗에 붙이면 KTH의 quad가 **우리 회전 각도에 종속된다.** 우리가 180°를 145°로 바꾸는 순간 KTH 쪽 배치가 전부 틀어진다.
분리해 두면 KTH는 "카메라를 마주 보는 평면 하나"만 알면 되고, 우리는 회전 수치를 자유롭게 만질 수 있다.

### KTH에게 넘길 규격

> `LDY_RewardAnchor`의 로컬 축이 곧 화면 축이다.
>
> - **로컬 +X = 화면 오른쪽**
> - **로컬 +Y = 화면 위**
> - **로컬 +Z = 화면 안쪽 (카메라에서 멀어지는 방향)**
>
> quad를 로컬 z = 0 평면에 놓고, 보이는 면이 로컬 **-Z**(카메라 쪽)를 향하게 하면 된다.
> 앵커 회전이 카메라 회전과 같으므로 키스톤 왜곡 없이 정면으로 보인다.
>
> 권장 로컬 배치 (3지선다 기준):
>
> | 대상 | 로컬 위치 | 로컬 크기 |
> |---|---|---|
> | 카드 1 | (-1.95, 0, 0) | 1.6 × 2.3 |
> | 카드 2 | (0, 0, 0) | 1.6 × 2.3 |
> | 카드 3 | (+1.95, 0, 0) | 1.6 × 2.3 |
> | 획득 버튼 | (0, -1.9, 0) | 2.4 × 0.6 |
>
> 레이어는 **`LDY_Reward`(9번, 신설)**, Collider 필수.

> 만약 "보드 뒷면에 카드를 눕힌다"로 가면: 앵커 position `boardCenter + (0, 0.12, 0)` = (3.5, 0.07, 3.5), rotation euler **(-90, 0, 0)** (로컬 +Y가 월드 -Z = 플레이어 쪽). 회전 각도를 145°로 바꾸는 안과 짝이다.

---

## C. LDY 단독 가능 범위 / 팀 요청 필요 범위

### LDY 단독으로 지금 가능

| # | 작업 | 파일 |
|---|---|---|
| 1 | `LDY_BoardRevealDirector` 신규 — 런타임 피벗 생성, 기물 수집, DOTween 회전, `Play(Action onDone)` / `IsPlaying` | `Assets/_Scripts/LDY/Effect/` (신규) |
| 2 | 회전 전 입력 잠금 | `LSO_WillSelection.BeginBoardInteractionLock()` (public static, 기존) + `LDY_CardPlacer.SetBoardActive(false)` (public, LDY 소유) |
| 3 | 회전 전 생존 기물 Renderer 페이드 | 디렉터 내부 |
| 4 | 흐름 삽입 (★C) — `CompleteActiveNodeAndReturnToMap`을 코루틴화해 디렉터를 먼저 기다림. 디렉터가 없으면(맵/팝업 경로) 즉시 통과 | `LDY_MapManager.cs:748` |
| 5 | `LDY_Reward` 레이어 추가 (9번) | `ProjectSettings/TagManager.asset` |
| 6 | 앵커 + 더미 quad 배치 | `LDY_TestScene` (LDY 소유 씬) |
| 7 | 디버그 단축키로 회전만 단독 재생 | 디렉터 내부 (`TestCameraMove.playOnSpace` 선례) |

**4번이 핵심이다.** LDY 파일 하나만 만지고, 승리 판정·보상 생성·씬 전환 순서를 전혀 안 바꾼다. 보스 경로와 패배 경로도 안전하다(패배는 `FailActiveNodeAndReturnToMap`으로 갈라진다).

### 팀 요청 필요

| 대상 | 요청 |
|---|---|
| **KTH** | ① `LSO_Test`(일반 전투 씬)에 앵커·디렉터 배치 — 씬 소유가 KTH+LSO라 사전 공유 필요<br>② 보상 quad 제작 + `KTH_RewardOptionUI`를 RectTransform/Button → Transform/Physics.Raycast로 이관 (5번 항목 표 참고)<br>③ `KTH_RewardChoiceUI`의 Canvas를 어디까지 남길지 결정 (프레임·획득 버튼만 UI로 남길지, 전부 3D로 갈지)<br>④ ★C 대신 ★B(`onBattleClear`)를 쓰고 싶다면 알려줄 것 — 씬 배선만으로 되지만 보상 UI 타이밍은 별도 처리 필요<br>⑤ (참고) `LDY_TestScene`의 `KTH_GameEndManager.turnManager`가 비어 있음 |
| **LSO** | ① 보드 **뒷판** 메시/머티리얼 — 현재 뒷면이 앞면과 동일하고 타일 틈으로 스카이박스가 비침. 8.2×8.2 판 하나를 y ≈ -0.12에 깔면 해결<br>② 뒷면 조명 — Directional Light가 위에서만 비춰 뒤집힌 면이 거의 검게 나옴. Unlit/Emissive 머티리얼이나 필 라이트 중 택일<br>③ `LSO_Test` 씬 편집 공유 |
| **DLJ** | 나중에 카메라 푸시인을 얹는다면 `TestCameraMove`·`LDY_CameraShake`와의 충돌 조율. 지금 당장은 없음 |

---

## D. 임시 더미로 검증하는 방법

KTH의 3D quad를 기다리지 않고 우리 연출만 완결 검증하는 절차.

### 1단계 — 더미 앵커 만들기 (5분, 씬 작업)

`LDY_TestScene`에서:

1. 빈 오브젝트 `LDY_RewardAnchor` 생성 → position (3.5, 1.3, 1.8), rotation (55, 0, 0)
2. 그 아래 `GameObject > 3D Object > Quad` 3개
   - 로컬 위치 (-1.95, 0, 0) / (0, 0, 0) / (1.95, 0, 0)
   - 로컬 스케일 (1.6, 2.3, 1), **로컬 회전 (0, 0, 0)**
     — Unity의 Quad는 로컬 −Z를 향하고 앵커의 −Z가 카메라 쪽이라, 회전 0이 곧 정면이다
   - 레이어 `LDY_Reward`
   - 머티리얼: **URP/Unlit**. `LDY_SceneBuilder.CreateHighlightMaterial`이 쓰는 것과 같은 이유로 — Lit을 쓰면 조명 때문에 확인이 어렵다
3. 이 상태로 Play만 눌러도 "회전 뒤 카드가 어디에 어떻게 보이는지"를 즉시 판정할 수 있다

### 2단계 — 회전만 단독 재생 (전투 불필요)

디렉터에 디버그 키를 하나 둔다 (DLJ `TestCameraMove.playOnSpace` 선례):

```csharp
if (Keyboard.current != null && Keyboard.current.f9Key.wasPressedThisFrame)
    Play(null);
```

각도·지속·이징·피벗 높이를 이걸로 반복 조정한다. 승리 조건을 만들 필요가 없다.

### 3단계 — 실제 흐름 그대로 검증 (전투 불필요)

**`KTH_TestClearButton`이 정확히 우리가 노리는 진입점을 부른다:**

```csharp
LDY_MapManager.Instance.CompleteActiveNodeAndReturnToMap();
```

전투 씬 Canvas에 버튼 하나를 놓고 이 컴포넌트를 붙이면, 적을 한 마리도 안 죽이고
**회전 → 보상 UI → 카드 선택 → 맵 복귀** 전 구간을 한 번의 클릭으로 재현할 수 있다.

⚠️ **반드시 `KTH_StageScene`에서 Play를 시작해 노드를 눌러 전투 씬에 들어갈 것.**
전투 씬에서 바로 Play하면 `LDY_MapManager.activeNodeIndex`가 -1이라
`CompleteActiveNodeAndReturnToMap`이 보상 없이 `GoToMapScene()`으로 직행한다 (`LDY_MapManager.cs:750-754`).

### 4단계 — 실전 확인

`LDY_TestScene`은 보스 씬이고 적 3마리(`Enemy_Melee`는 BullKing)라 시간이 걸린다.
빠르게 보려면 `Enemy_*`의 `Health` maxHealth를 임시로 1로 낮추거나, 적을 한 마리만 남기고 나머지를 비활성화하면 된다.

### 5단계 — KTH 인수인계

KTH의 quad가 나오면 더미 3개를 지우고 그 자리에 붙이면 끝이다.
앵커가 회전 피벗과 분리돼 있으므로 **디렉터 코드는 손댈 필요가 없다.**

---

# 구현 (이후 작업)

## 만든 파일

| 파일 | 종류 | 책임 |
|---|---|---|
| `LDY/Effect/LDY_BoardFlipDirector.cs` | MonoBehaviour | 순서 진행 · 인스펙터 · 디버그 키 |
| `LDY/Effect/LDY_BoardFlipMotion.cs` | plain class | 월드 축 둘레 회전 (부모 변경 없음) |
| `LDY/Effect/LDY_BoardPieceHider.cs` | plain class | 남은 기물 정리 · 되돌리기 |
| `LDY/Effect/LDY_BoardInputGate.cs` | plain class | 보드 입력 차단 · 되돌리기 |
| `LDY/Effect/LDY_RewardAnchorDummy.cs` | MonoBehaviour (`#if UNITY_EDITOR`) | 더미 보상 판 3장 |

## 고친 파일

- `LDY_BoardManager.cs` — 읽기 전용 프로퍼티 두 개 추가 (`BoardRoot`, `BoardCenter`). 동작 변화 없음.
- `LDY_MapManager.cs` — `CompleteActiveNodeAndReturnToMap()`이 회전을 먼저 기다리도록.
  기존 본문은 `CompleteClearedNodeAndLeave()`로 그대로 옮겼고 **순서는 한 줄도 바꾸지 않았다.**

## 회전 방식 — 피벗을 만들지 않는다

부모를 바꿔 붙였다 떼는 대신, 시작 시점의 위치·회전만 기억해 두고 매 프레임 새로 계산한다.

```csharp
Quaternion step = Quaternion.AngleAxis(angle * progress, unitAxis);
target.SetPositionAndRotation(pivot + step * arm, step * _startRotation);
```

- 계층 구조를 건드리지 않는다 → 연출이 게임 상태를 바꾸지 않는다
- 누적(`RotateAround`)이 아니라 절대 계산이라 180°가 정확히 180°다
- 중단 시 되돌릴 것이 트랜스폼 하나뿐이다

## 기물 처리 — 스케일 축소 후 비활성

조사 시점에 놓쳤던 부분. `LDY_Board`만 돌리면 살아남은 플레이어 기물이 공중에 남는다.
`LDY_DissolveEffect` 대신 **스케일을 0으로 줄인 뒤 `SetActive(false)`** 를 골랐다.

1. 머티리얼을 안 건드린다 — URP Lit을 투명으로 바꿨다가 마젠타가 뜬 전례가 있다
   (`LDY_SceneBuilder.CreateHighlightMaterial` 주석)
2. 오브젝트를 안 지운다 — `LDY_DissolveEffect`는 끝나면 GameObject를 파괴하고 `ActiveCount`를 올려
   `IsAnimating()`을 true로 만든다. 우리는 그 값이 내려가기를 기다린 뒤 시작하므로 서로 물린다
3. 되돌릴 수 있다 — 연출이 끊겨도 원래 크기·활성 상태가 그대로 돌아온다

## ⚠ 지시와 다르게 간 곳 — 전역 잠금

`LSO_WillSelection.BeginBoardInteractionLock()`을 **기본으로 쓰지 않는다.**

`LSO_WillPanel`이 `BoardInteractionLockChanged`를 구독해서 **전체 화면 검은 디머를 페이드인**하고
(`LSO_WillPanel.HandleBoardInteractionLockChanged`), 그 패널이 **`LSO_Test`(일반 전투 씬)에 들어 있다.**
그대로 쓰면 보드가 뒤집히는 장면 위에 검은 막이 덮여 연출이 안 보인다.

대신 입력 차단은 컴포넌트를 꺼서 한다(`boardInputBehaviours`).
디머를 원하면 디렉터의 `useGlobalInteractionLock`을 켜면 된다 — 인스펙터 체크 하나다.

전역 잠금을 켜는 경우 **반드시 풀어야 한다.** static이라 씬을 넘어가도 남아서, 그대로 두면
다음 전투가 잠긴 채로 시작하고 ESC(`LDY_EscapeKeyHandler.cs:116`)까지 막힌다. `Seal()`이 그 일을 한다.

## 연출이 끝난 뒤 입력을 다시 열지 않는 이유

보드는 뒤집혀 있고 기물은 숨겨져 있는데 **격자에는 그 기물들이 그대로 등록돼 있다.**
클릭을 다시 받으면 보이지도 않는 기물이 선택되고 하이라이트가 뜬다.
이 뒤로는 보상 선택과 씬 전환만 남았으므로 보드 입력은 씬이 끝날 때까지 닫아둔다(`Seal()`).
컴포넌트 단위 차단이라 씬과 함께 사라지고, 전역 상태로는 새지 않는다.

## 씬 배선 체크리스트 (유저 작업)

두 전투 씬(`LSO_Test`, `LDY_TestScene`) 모두에 필요하다.

1. **레이어 추가** — Project Settings > Tags and Layers, 9번 슬롯에 `LDY_Reward`
2. **빈 오브젝트 `LDY_RewardAnchor`** — position (3.5, 1.3, 1.8), rotation (55, 0, 0), scale (1,1,1)
   - 검증용이면 `LDY_RewardAnchorDummy` 컴포넌트를 붙인다 (Play 시 판 3장 자동 생성)
   - **보드 루트(`LDY_Board`) 아래에 두지 말 것** — 디렉터가 Awake에서 검사하고 경고한다
3. **`LDY_BoardFlipDirector`** — 아무 오브젝트에나. 앵커에 같이 붙여도 된다.
   **`rewardAnchor`만 연결하면 되고** 나머지(`board`·`turnManager`·`cardPlacer`·`selectionController`)는
   비워두면 Awake에서 씬을 뒤져 채운다.

`KTH_GameEndManager`나 `onBattleClear`에는 아무것도 걸지 않는다. 호출은 `LDY_MapManager`가 한다.

## 1차 배선에서 터진 것 두 개

첫 배선에서 회전이 아예 안 돌고 보상만 떴다. 원인은 둘 다 코드 쪽이었다.

**① 디렉터가 자기 자신을 껐다.** 디렉터를 `LDY_RewardAnchor`에 붙였는데,
Awake에서 `rewardAnchor.gameObject.SetActive(false)`를 하니 곧 자기가 올라탄 오브젝트가 꺼졌다.
그 뒤 `LDY_MapManager`의 `FindFirstObjectByType`은 기본값이 비활성 제외라 디렉터를 못 찾고,
회전을 건너뛴 채 보상으로 직행했다.

→ 앵커 자신 대신 **자식만** 끈다. 어디에 붙이든 자멸하지 않는다.
   감추는 시점도 Awake에서 **Start로 옮겼다** — 더미 판이 Awake에서 자식을 만들기 때문에,
   Awake에서 감추면 아직 없는 자식을 감추고 뒤늦게 생긴 판이 그대로 보인다.

**② `Behaviour[]` 드래그가 엉뚱한 컴포넌트를 잡았다.**
`LDY_SelectionController`가 `LDY_BoardManager`와 같은 오브젝트(`LDY_GameSystems`)에 있어서,
GameObject를 끌어다 놓으니 첫 번째 Behaviour인 **BoardManager**가 들어갔다. 입력이 안 막혔다.

→ `boardInputBehaviours`를 없애고 **타입이 박힌 `selectionController` 필드**로 바꿨다.
   같은 오브젝트의 `DLJ_ObjectHovering`은 자동으로 함께 꺼진다. 잘못 넣을 수가 없다.

**③ 뒤집힌 상태에서 또 돌리면 360°가 됐다.**
`LDY_BoardFlipMotion.Rotate`는 **부를 때의 자세를 시작점으로 캡처**한다.
이미 뒤집힌 채로 다시 재생하면 180°가 한 번 더 얹혀 제자리로 돌아오고,
더 나쁜 건 처음 자세를 기억하던 `_startPosition`/`_startRotation`이 **뒤집힌 자세로 덮여서**
되돌리기(F11)까지 망가진다는 점이었다.

→ `Play()`가 시작 전에 `IsFlipped`를 보고, 뒤집혀 있으면 `ResetToStart()`로 먼저 원위치시킨다.
   언제 눌러도 같은 연출이 같은 자리에서 출발한다. `Abort()`도 같은 `ResetToStart()`를 쓴다.

## 검증 절차

- **연출만**: 전투 씬에서 Play → **F10** 재생 / **F11** 되돌리기 (F5·F7·F8·F9는 `LDY_SaveDebugHotkeys`가 쓴다)
- **전체 흐름**: `KTH_StageScene`에서 Play 시작 → 노드 진입 → `KTH_TestClearButton`
  (전투 씬 직접 Play는 `activeNodeIndex`가 −1이라 보상이 안 뜬다)

## 컴파일 검증

Roslyn으로 Assembly-CSharp 전체 컴파일: **error 0**, `probe.dll` 생성 확인.
새 파일에서 나온 경고는 CS0649 3개(인스펙터 주입 필드)로, 프로젝트 기존 206개와 같은 종류다.
