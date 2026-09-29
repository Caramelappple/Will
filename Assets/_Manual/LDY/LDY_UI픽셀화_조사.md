# UI 픽셀화 조사 보고서

작성: LDY / 2026-08-22 / **조사만 수행, 코드 및 에셋 무변경**

목표: 모든 UI에 픽셀 효과. 단 (1) 포스트 프로세싱·포스터라이즈 동반 금지, (2) 텍스트는 지정 폰트 그대로 선명.

---

## 0. 먼저 알아야 할 것 — 이미 픽셀화가 켜져 있다

`Assets/Settings/PC_Renderer.asset` 에 **Full Screen Pass Renderer Feature 3개**가 등록되어 있고, 그중 2개가 **활성 상태**다.

| # | Pass Material | 셰이더 | Injection | 상태 |
|---|---|---|---|---|
| 1 | `Assets/_Shaders/LSO/Pixelize.mat` | `LSO/Fullscreen/Pixelate` (`_TargetHeight=512`) | AfterRenderingPostProcessing (600) | **활성** |
| 2 | `Assets/_Shaders/LSO/LSO_Posterize.mat` | `LSO/Fullscreen/Posterize` (`_Levels=3`) | AfterRenderingPostProcessing (600) | **활성** |
| 3 | `Assets/Shaders/New Material.mat` | `Hidden/DebtPit/PixelDitherPostProcess` | AfterRenderingPostProcessing (600) | 비활성 |

추가로 `ScreenSpaceAmbientOcclusion` 피처도 활성(Intensity 0.4).

QualitySettings `m_CurrentQuality: 1` → **PC 레벨 → PC_RPAsset → PC_Renderer**. 즉 지금 에디터/빌드에서 도는 게 이 렌더러다. (Mobile_Renderer 는 피처 0개.)

이게 만드는 현재 상황:

- 렌더러 피처는 카메라의 `Render Post Processing` 토글과 **무관하게** 실행된다. 대부분 씬의 Main Camera 가 `m_RenderPostProcessing: 0` 이지만 Pixelize/Posterize 는 그대로 걸린다.
- Screen Space **Overlay** 캔버스는 모든 카메라 렌더 이후 합성되므로 이 패스를 **피해간다**.
- Screen Space **Camera** 캔버스는 카메라의 투명 큐에서 그려지므로 **픽셀화 + 포스터라이즈를 그대로 맞는다. 그 안의 TMP 텍스트까지 포함해서.**

→ 지금 UI 룩이 씬마다 다른 근본 원인이고, 동시에 **요구사항 "포스터라이즈 절대 금지"를 정면으로 위반하는 상태**다. 어떤 방식을 고르든 **이 3개 피처 정리가 1번 작업**이다. (소유: LSO)

---

## 1. Canvas 전수 조사

### 1-1. 빌드에 포함된 9개 씬 (EditorBuildSettings 기준) — 캔버스 17개

| 씬 | Canvas | RenderMode | Sort | CanvasScaler | 소유 |
|---|---|---|---|---|---|
| LSO_UI Scene | SceneFaderManager/Canvas | Overlay | 999 | ScaleWithScreen 1920x1080 m0 | LSO |
| LSO_UI Scene | Canvas *(비활성)* | **SS-Camera** (Main) | 0 | ScaleWithScreen 1920x1080 m0.5 | LSO |
| LSO_UI Scene | Canvas *(비활성)* | Overlay | 0 | ScaleWithScreen 1920x1080 m0.5 | LSO |
| LSO_UI Scene | Canvas (MainMenu 루트) | Overlay | 0 | **ConstantPixelSize** | LSO |
| LSO_Test | LDY_AutoScene/LDY_UI | Overlay | 0 | ScaleWithScreen 1920x1080 m0 | LDY |
| LSO_Test | Canvas (전투 HUD) | **SS-Camera** (Main) | 100 | **ConstantPixelSize** | LSO/LDY 혼재 |
| LDY_TestScene | LDY_AutoScene/LDY_UI | Overlay | 0 | ScaleWithScreen 1920x1080 m0 | LDY |
| LDY_TestScene | Canvas (전투 HUD) | **SS-Camera** (Main) | 100 | **ConstantPixelSize** | LSO/LDY 혼재 |
| LDY_TestScene | LDY_PauseCanvas | Overlay | 900 | ScaleWithScreen 1920x1080 m0.5 | LDY |
| KTH_StageScene | Canvas (RewardChoiceUI) | **SS-Camera** (Main) | 0 | **ConstantPixelSize** | KTH |
| KTH_StageScene | MapCanvas | **SS-Camera** (Main) | 0 | ScaleWithScreen 1920x1080 m0 | LDY |
| KTH_BuildDeckScene | Canvas | Overlay | 0 | ScaleWithScreen 1920x1080 m0 | KTH |
| KTH_LoadingScene | Canvas | Overlay | 0 | **ConstantPixelSize** | KTH |
| KTH_TestScene | CardDrowUI(1)/Canvas | Overlay | 0 | **ConstantPixelSize** | KTH |
| KTH_TestScene | CardDrowUI(1)/Canvas | **SS-Camera** (Main) | 0 | **ConstantPixelSize** | KTH |
| KTH_Death Scene | Canvas | Overlay | 0 | **ConstantPixelSize** | KTH(껍데기)/LSO(내용) |
| KTH_BossClearScene | Canvas | Overlay | 0 | **ConstantPixelSize** | KTH(껍데기)/LSO(내용) |

**Overlay 11개 / ScreenSpace-Camera 6개.** SS-Camera 6개는 전부 각 씬의 `Main Camera` 를 참조한다.

빌드 외 씬까지 합치면 `Assets/_Scenes` 전체에 캔버스 **32개** (DLJ 7, KTH 11, LDY 5, LSO 8, JSJ 0). 프리팹에도 3개 더 있다:

- `Assets/_Prefabs/KTH/HandCard.prefab` — **WorldSpace** 캔버스 (손패 카드가 월드에 그려진다)
- `Assets/_Prefabs/KTH/DrowCardUI/CardDrowUI.prefab` — Overlay
- `Assets/Dark - Complete Horror UI/Prefabs/UI Elements/Canvas.prefab` — Overlay (미사용 템플릿)

### 1-2. 런타임에 생기는 캔버스 (씬 파일로는 안 잡힘)

씬을 아무리 고쳐도 이건 코드로 처리해야 한다. **DontDestroyOnLoad 라 씬을 넘어 살아남는다.**

| 생성처 | 방식 | 비고 |
|---|---|---|
| `LDY/Effect/SceneAutoFader.cs:30` | `AddComponent<Canvas>()`, Overlay, sort 999, DDoL | 검은 페이드 |
| `LDY/Map/LDY_SceneTransition.cs:54` | DDoL 싱글톤 | 아이리스 전환 |
| `KTH/Reward/KTH_RewardChoiceUI.cs:95` | 씬의 SS-Camera 캔버스를 DDoL 로 승격 | ⚠ 아래 참조 |
| `LDY/Map/LDY_MapPlayerToken.cs:33` | 자식에 nested `Canvas` + overrideSorting | 맵 토큰 |
| `LDY/Map/LDY_UIStarfield.cs:44` | `new GameObject(..., typeof(Image))` 로 별 N개 생성 | MapCanvas 하위 |
| `LDY/Map/LDY_MapUIController.cs:146,169` | 노드/라인 Image 를 런타임 생성 | MapCanvas 하위 |

⚠ **기존 버그**: `KTH_RewardChoiceUI` 가 붙은 KTH_StageScene 의 Canvas 는 RenderMode = ScreenSpaceCamera 인데 DDoL 로 다음 씬에 넘어간다. `worldCamera` 재바인딩 코드가 없어서 다음 씬에서 카메라 참조가 죽고 **Unity 가 Overlay 처럼 취급**한다. 즉 이 캔버스는 씬에 따라 픽셀화를 맞기도 하고 피하기도 한다.

---

## 2. 이미지 / 텍스트 혼재 구조

**결론부터: 텍스트가 이미지의 자식으로, 그것도 LayoutGroup 안에 들어 있다. 레이어 분리가 이 프로젝트에서 가장 비싼 항목이다.**

전형적인 패턴 (KTH_BuildDeckScene):

```
Canvas [Canvas, CanvasScaler, GraphicRaycaster]
  Image            [Image, HorizontalLayoutGroup, KTH_DeckBuildManager]
  PageText (TMP)   [TextMeshProUGUI]
  Button           [Image, Button]
    Text (TMP)     [TextMeshProUGUI]        ← 이미지의 자식
  Panel            [Image]
  LeftRight Button []
    Button (1)     [Image, Button]
    Button         [Image, Button]
  Button (1)       [Image, Button]
    Text (TMP)     [TextMeshProUGUI]        ← 이미지의 자식
  Image (1)        [Image, KTH_Inventory, HorizontalLayoutGroup]
```

LDY_TestScene 의 InfoPanel — 레이아웃 그룹 3개가 텍스트 위치를 결정한다:

```
InfoPanel
  Border [Image]  Background [Image]  DescTxt [TMP]
  StatLayout  [HorizontalLayoutGroup]
    HpTxt [TMP]   AtkTxt [TMP]                 ← 위치를 레이아웃이 계산
  TypeLayout  [HorizontalLayoutGroup]
    Will  [Image] → Text (TMP)                 ← 이미지 자식
    Range [Image] → Text (TMP)
  BtnLayout   [HorizontalLayoutGroup]
    CancelBtn [Image, Button] → Text (TMP)
    SelectBtn [Image, Button] → Text (TMP)
```

KTH_StageScene / MapCanvas — 그나마 평평한 편:

```
MapCanvas
  Background     [Image, LDY_MapCameraController]
  NodeContainer  []  → 런타임 노드(Image+Button) N개
  Starfield      [LDY_UIStarfield, LDY_UIShootingStars]   → 런타임 Image N개
  CurStage       [TMP]
  ClearBanner    [CanvasGroup, LDY_ClearBanner] → MessageText [TMP]
  LineContainer  []  → 런타임 Image N개
```

가장 심한 곳은 `KTH_Death Scene` 의 Canvas — **노드 78개, 깊이 7, Image 30 / TMP 15**, 내용물은 LSO 의 `MainMenu.prefab`(Image 31 / TMP 17) 계열이다.

### 그래픽 물량 (빌드 씬 + 참조 프리팹 기준)

| | Image | TMP | 비고 |
|---|---|---|---|
| 빌드 씬 직접 배치 | 약 60 | 약 40 | |
| LSO/MainMenu.prefab | 31 | 17 | Death/BossClear/LSO_UI 3곳에서 사용 |
| LSO/AnimalInfo.prefab | 0 | 10 | |
| KTH/Reward.prefab | 3 | 7 | 런타임 인스턴스 |
| KTH/CardDrowUI.prefab | 6 | 7 | |
| KTH/HandCard.prefab | 3 | 3 | **WorldSpace** |
| 런타임 생성 (별/노드/라인) | 가변 (수십~수백) | 0 | |

**텍스트를 별도 캔버스로 빼려면**: 최소 40여 개 TMP 를 부모 이미지에서 떼어내고, 그중 상당수는 LayoutGroup 이 위치를 계산해 주던 것이므로 **원래 RectTransform 을 매 프레임 따라가는 미러 컴포넌트**를 붙여야 한다. 프리팹 6개 + 씬 9개를 동시에 손대야 하고, 네 명의 작업 영역을 전부 침범한다.

---

## 3. 렌더 파이프라인

- **URP 17.3.0 / Unity 6000.3.6f1**. HDRP·Built-in 아님.
- Graphics Settings 기본 RP = `PC_RPAsset`, Quality 기본 레벨 = PC → `PC_Renderer`.
- `PC_RPAsset` 에 `m_VolumeProfile = SampleSceneProfile.asset` 이 물려 있다 → Bloom / Vignette / ColorAdjustments / Tonemapping / MotionBlur / FilmGrain / ShadowsMidtonesHighlights 가 활성 오버라이드로 들어 있다.
- **씬 안에 Volume 컴포넌트는 하나도 없다.** 위 프로파일은 파이프라인 기본값으로 들어가며, 카메라의 `Render Post Processing` 이 켜진 씬에서만 실제 적용된다 → 현재 **`LSO_UI Scene` 의 Main Camera 만 `m_RenderPostProcessing: 1`**. 나머지 8개 씬은 0.
- 카메라는 전 씬 공통으로 `Main Camera` 하나, **Base** 타입, perspective, `targetTexture` 없음, 카메라 스택 없음.
- 프로젝트 전체에서 `RenderTexture` 를 쓰는 런타임 코드는 **0건**. 백지 상태다.
- `m_UpscalingFilter: 0`, renderScale 기본.

즉 **"포스트 프로세싱 볼륨"은 씬에 없지만, 파이프라인 레벨 프로파일과 렌더러 피처 두 갈래로 이미 화면에 개입하고 있다.**

---

## 4. UI 클릭 판정 경로

**빌드 9개 씬 전부 동일**: `EventSystem` + `InputSystemUIInputModule` (신 Input System) 1개, 모든 Canvas 에 `GraphicRaycaster` 1개씩. 커스텀 BaseRaycaster 없음, PhysicsRaycaster / Physics2DRaycaster 없음.

UI 입력을 하는 코드:

| 파일 | 방식 | RT 전환 시 안전한가 |
|---|---|---|
| `KTH_HandCard.cs:46` | `IPointerClickHandler` | 레이캐스터가 옳은 좌표를 주면 안전 |
| `LSO_ButtonClickHandler / HoverHandler` | `IPointer*Handler` | 동일 |
| `KTH_DeckBuildManager.cs:95` | `RectTransformUtility.RectangleContainsScreenPoint(rect, eventData.position, eventData.pressEventCamera)` | ✅ 카메라를 넘기므로 구조적으로 안전 |
| `LSO_DamagePopupSpawner.cs:108-118` | `cam.WorldToScreenPoint` → `ScreenPointToLocalPointInRectangle(..., canvas.worldCamera)` | ✅ 동일 |
| `LDY_MapNodeView.cs:63` | `Button.onClick` | 레이캐스터 의존 |

UI 밖(월드) 레이캐스트 — **UI 픽셀화와 무관하지만 카메라를 RT 로 돌릴 경우 같이 깨지는 곳**:

| 파일 | 방식 |
|---|---|
| `LDY_CardPlacer.cs:337` | `targetCamera.ScreenPointToRay(Mouse.current.position)` → `Physics.Raycast(boardLayerMask)` |
| `LDY_SelectionController.cs:203` | 동일 |
| `DLJ_WillTest.cs:31` | `Camera.ScreenPointToRay` → `Physics.RaycastAll` |

**핵심**: `GraphicRaycaster` 는 ScreenSpace-Camera 모드에서 `eventCamera.ScreenPointToRay(eventData.position)` 을 쓴다. 카메라에 `targetTexture` 를 붙이면 카메라의 pixelRect 가 RT 크기가 되는데 `eventData.position` 은 여전히 **화면 픽셀 좌표**다. RT 가 화면보다 작으면 클릭 지점이 `화면크기 / RT크기` 배만큼 어긋난다. `Mouse.current.position` 을 직접 읽는 위 3개 파일도 같은 이유로 어긋난다. → **RT 방식을 택하면 커스텀 레이캐스터 또는 좌표 리맵이 필수.**

---

## 5. Canvas 소유자 판정

| 소유 | 범위 |
|---|---|
| **KTH** | KTH_StageScene(Canvas), KTH_BuildDeckScene, KTH_LoadingScene, KTH_TestScene, KTH_Death Scene·KTH_BossClearScene(껍데기), HandCard.prefab(WorldSpace), CardDrowUI.prefab, Reward.prefab |
| **LSO** | LSO_UI Scene(캔버스 4), LSO_Test(HUD Canvas), MainMenu / WillPanel / AnimalInfo / Button / CreditTxt / WillOption prefab, **`PC_Renderer` 의 Pixelize·Posterize 피처와 그 셰이더·머티리얼** |
| **LDY** | LDY_TestScene(LDY_UI, LDY_PauseCanvas), MapCanvas 하위 전부(MapUIController, MapNodeView, MapLine, PlayerToken, UIStarfield, UIShootingStars, ClearBanner, SceneTransition, ProceduralSprite), SceneAutoFader |
| **DLJ** | DLJ_* 씬 4개(빌드 미포함), Will 관련 UI |
| **공용** | `Dark - Complete Horror UI` 텍스처 42장 — 거의 모든 UI 가 참조. TMP 폰트 에셋(MaruBuri / Larke Sans / LiberationSans) |

**"전체 UI 에 적용"은 정의상 KTH·LSO 씬 7개와 LSO 프리팹 6개를 건드려야 한다.** LDY 단독으로 끝나는 범위가 아니다. 다만 아래 3번 방식을 쓰면 남의 씬은 **캔버스 루트에 컴포넌트 1개 추가**로 끝난다.

---

## 6. 이미지 에셋 임포트 설정

UI/스프라이트로 실제 참조되는 텍스처 **45개**를 전수 확인했다.

| filterMode | 개수 |
|---|---|
| **Bilinear** | **39** |
| Point | 2 (`_Assets/LSO/zentonLogo.png`, `ChatGPT Image ....png`) |
| 미지정(기본 Bilinear) | 1 (`_Prefabs/KTH/Arrow_left_dark.png`) |
| Unity 빌트인 | 3 |

| compression | 개수 |
|---|---|
| **Normal (DXT)** | **42** (= 빌트인 제외 전부) |
| None | 0 |

`maxTextureSize` 는 전부 2048.

**예상대로 Bilinear + DXT 압축이다.** 픽셀 룩을 내려면:

- `filterMode: 0` (Point) — 필수
- `textureCompression: 0` (None/Uncompressed) — 이게 없으면 확대 시 DXT 블록 아티팩트(4x4 색 뭉개짐)가 도트가 아니라 **지저분한 노이즈**로 보인다. 픽셀 아트에서 압축은 특히 티가 난다.
- `mipmaps` off, `aniso` 0

추가로 **런타임 생성 스프라이트도 전부 Bilinear** 다 — `LDY_ProceduralSprite.cs` 의 6곳(`24, 53, 87, 110, 143, ...`)이 모두 `filterMode = FilterMode.Bilinear`. 맵 노드/라인/토큰이 여기서 나온다. 이건 LDY 소유라 즉시 고칠 수 있다.

---

## 7. 구현 방식 3안

### 방식 A — RenderTexture 2층 (요구사항 그대로)

UI 전용 카메라 + 저해상도 RT(예: 480x270) → Point 필터 RawImage 로 전체화면 확대. 텍스트는 별도 Overlay 캔버스에 원해상도로.

```
[UI_Image_Canvas]  (SS-Camera, layer=UI_Pixel) ──> UICamera ──> RT 480x270 (Point)
                                                                     │
[Top_Overlay_Canvas] ── RawImage(RT, Point, 전체화면) ◄───────────────┘
                    └─ [UI_Text_Canvas] TMP 원해상도
```

- ✅ 격자가 화면 전역으로 완벽히 정렬된다. 진짜 저해상도 화면 느낌.
- ✅ 텍스트가 물리적으로 다른 레이어라 어떤 픽셀 처리도 안 맞는다.
- ✅ 포스트 프로세싱과 완전 분리 (UI 카메라 전용 렌더러 사용).
- ❌ **텍스트 40여 개를 부모 Image 에서 떼어내야 한다.** 그중 상당수가 LayoutGroup 자식이라 위치 미러 컴포넌트가 필요하다 (§2).
- ❌ **정렬 파괴**: 텍스트가 항상 이미지 위에 그려진다. 패널이 라벨을 가려야 하는 연출(모달, 페이드, 슬라이드인)이 전부 깨진다. RectMask2D / Mask 도 캔버스를 넘어가지 못해 스크롤 영역 밖으로 글자가 삐져나온다.
- ❌ **클릭 좌표 리맵 필수** (§4). 커스텀 GraphicRaycaster 또는 InputModule 래핑.
- ❌ DDoL 런타임 캔버스 6종을 코드로 재배선해야 한다 (§1-2).
- ❌ 4명 전원의 씬·프리팹을 동시에 수정. 머지 충돌 위험 최대.
- 비용 감각: **씬 9 + 프리팹 6 + 신규 스크립트 3~4개 + 회귀 테스트**. 며칠 단위.

### 방식 B — 에셋 자체를 저해상도/Point 로 (임포트 설정)

텍스처 임포트 설정만 바꾼다. filterMode Point, compression None, maxTextureSize 를 128~256 으로 낮춰 소스를 실제로 저해상도화.

- ✅ 코드 0, 씬 0, 캔버스 구조 0, 클릭 판정 0. 텍스트는 손도 안 대므로 자동으로 선명.
- ✅ 포스트 프로세싱과 원천적으로 무관.
- ✅ LDY 혼자 30분. 되돌리기도 쉽다.
- ❌ **화면 전역 격자가 아니다.** 각 이미지가 자기 크기·자기 격자로 확대되므로 인접 요소끼리 픽셀 크기가 다르게 보인다. "저해상도 화면"이 아니라 "도트풍 에셋"이 된다.
- ❌ 사각 패널·라인 등 기하 경계는 여전히 매끈. 런타임 생성 도형(Starfield, MapLine)에는 아무 효과 없음.
- ❌ Dark Horror UI 팩 42장을 건드리므로 전 팀의 UI 외형이 바뀐다 → 합의 필요.

### 방식 C — UI 전용 픽셀 셰이더를 Image 머티리얼로 (권장)

`UI/Default` 를 베이스로, **스크린 좌표 기준으로 블록을 스냅해 샘플링**하는 UI 셰이더를 하나 만들고, 캔버스 루트에 붙는 주입 컴포넌트가 하위 `Image`/`RawImage` 에 공유 머티리얼을 일괄 할당한다. TMP 는 자기 기본 머티리얼을 그대로 쓰므로 **아무 작업 없이 선명하다.**

```
Canvas (Overlay 그대로)
 └ LDY_UIPixelizer  ← 이 한 줄만 추가
     └ 하위 Image/RawImage 전부에 sharedMaterial = PixelUI.mat
     └ TMP_Text 는 건드리지 않음  (= 텍스트 자동 제외)
```

셰이더 핵심 (스크린 좌표로 위상을 맞춰야 전역 정렬이 된다):

```hlsl
float2 px      = i.screenPos.xy / i.screenPos.w * _ScreenParams.xy; // 화면 픽셀
float2 snapped = (floor(px / _Block) + 0.5) * _Block;               // 블록 중앙
float2 uvPerPx = float2(ddx(i.uv).x, ddy(i.uv).y);                  // 화면픽셀당 UV
float2 uv      = i.uv + (snapped - px) * uvPerPx;                   // 위상 보정
```

- ✅ **RenderMode 를 바꾸지 않는다.** Overlay 11개 그대로 → 클릭 좌표계 무변화, 커스텀 레이캐스터 불필요, DDoL 캔버스도 컴포넌트만 붙으면 끝.
- ✅ **계층 재구성 0.** 텍스트를 부모에서 뗄 필요가 없다 → §2 의 가장 비싼 항목이 통째로 사라진다.
- ✅ 텍스트 정렬·마스킹·레이아웃이 전부 원래대로 동작한다.
- ✅ 픽셀 블록이 스크린 좌표 기준이라 **격자가 화면 전역으로 정렬**된다 (방식 B 의 약점 해결).
- ✅ 포스트 프로세싱 파이프라인을 아예 안 탄다 → "포스터라이즈 동반 금지" 요구를 구조적으로 보장.
- ✅ 런타임 생성 Image(Starfield 별, 맵 노드/라인)도 주입 컴포넌트가 `OnTransformChildrenChanged` 로 잡으면 자동 커버.
- ⚠ **직사각형 경계는 여전히 매끈하다.** 패널 모서리가 블록에 안 맞으면 "픽셀 화면"이 아니라 "픽셀 텍스처를 쓴 매끈한 UI"로 보인다. → RectTransform 위치·크기를 블록 배수로 스냅하는 보조 컴포넌트로 상당 부분 해소 가능.
- ⚠ `Mask` / `RectMask2D` 를 쓰는 곳은 셰이더에 `UNITY_UI_CLIP_RECT` 와 스텐실 처리를 넣어야 한다. (`UI/Default` 원본을 복사해 시작하면 이미 들어 있다.)
- ⚠ Sliced / Filled Image 는 UV 가 불연속이라 경계에서 튈 수 있다 → 초기에 검증 필요.

### 권장

**C 를 주력, B 를 보조로 병행. A 는 이 프로젝트에서 비추천.**

근거:

1. **A 의 비용이 이 프로젝트에서만 유독 크다.** 텍스트가 전부 Image 자식 + LayoutGroup 안에 있기 때문(§2). 텍스트를 뽑아내는 순간 버튼 라벨 위치, 모달 가림, 스크롤 마스킹이 동시에 깨지고, 그걸 미러 컴포넌트로 되살리는 게 본 작업보다 커진다.
2. **A 는 클릭 좌표 리맵이 필수인데, C 는 좌표계를 아예 안 건드린다.** 지금 UI 입력이 전부 EventSystem+GraphicRaycaster 단일 경로라(§4) C 는 회귀 위험이 거의 0 이다.
3. **DDoL 런타임 캔버스가 6종이나 된다**(§1-2). A 는 이걸 전부 코드로 재배선해야 하지만, C 는 캔버스 루트에 컴포넌트 하나 붙는 것으로 끝난다.
4. **남의 씬 침범 범위**: A 는 계층 재구성 → 리뷰·머지 지옥. C 는 캔버스 루트에 컴포넌트 1줄 → KTH/LSO 에게 "이 컴포넌트만 붙여 달라"로 요청 가능.
5. B 를 병행하는 이유: C 의 블록 샘플링이 Bilinear+DXT 소스를 그대로 확대하면 블록 안이 여전히 뿌옇다. Point + 무압축이 C 의 화질을 결정한다.

A 가 정말 필요한 경우는 **월드(보드/캐릭터)까지 통째로 저해상도 화면으로 만들고 UI 만 그 위에 얹는** 아트 디렉션으로 갈 때다. 그건 지금 요구사항("UI 만 픽셀화")과 다르다.

---

## 8. LDY 단독 범위 vs 팀 요청 범위

### LDY 혼자 가능 (충돌 위험 없음)

- 신규 셰이더 `LDY_UIPixelate.shader` + 머티리얼 + 주입 컴포넌트 `LDY_UIPixelizer.cs` 작성 (전부 신규 파일)
- `LDY_TestScene` 의 캔버스 3개에 적용해 프로토타입 검증
- `KTH_StageScene / MapCanvas` 하위 LDY 자산 전부 (MapNodeView, MapLine, PlayerToken, Starfield, ShootingStars, ClearBanner)
- `LDY_ProceduralSprite.cs` 의 `FilterMode.Bilinear` → `Point` 6곳
- `SceneAutoFader`, `LDY_SceneTransition` 의 런타임 캔버스 대응
- RectTransform 블록 스냅 보조 컴포넌트

### 팀 요청 필요

| 대상 | 요청 상대 | 내용 | 우선도 |
|---|---|---|---|
| `PC_Renderer` 피처 3개 | **LSO** | Pixelize·Posterize·PixelDither 비활성 또는 제거. 요구사항상 포스터라이즈는 절대 불가 | **최우선** |
| `KTH_RewardChoiceUI` DDoL 캔버스 | **KTH** | SS-Camera + DDoL 조합의 worldCamera 유실 (기존 버그) | 높음 |
| KTH 씬 5개 캔버스 | **KTH** | 캔버스 루트에 `LDY_UIPixelizer` 부착 | 중 |
| LSO 씬 2개 + 프리팹 6개 | **LSO** | 동일 | 중 |
| `Dark - Complete Horror UI` 텍스처 42장 | **전원 합의** | filterMode Point / compression None / mipmap off | 중 |
| CanvasScaler 모드 통일 | **전원 합의** | 현재 17개 중 9개가 ConstantPixelSize(800x600 기본값) → ScaleWithScreen 1920x1080 로 통일 | 높음 |
| DLJ 씬 4개 | **DLJ** | 빌드 미포함이라 후순위 | 낮음 |

---

## 9. 예상되는 함정

1. **이중 픽셀화 / 모아레** — 현재 Pixelize 피처(512p)가 살아 있는 상태에서 UI 픽셀화를 얹으면 두 격자가 간섭해 물결무늬가 생긴다. 반드시 먼저 끄고 시작할 것.
2. **CanvasScaler 가 씬마다 다르다** — 17개 중 9개가 Constant Pixel Size(레퍼런스 800x600 = Unity 기본값 방치). 창 크기를 바꿔도 UI 가 안 커진다. 픽셀 블록 크기를 화면 픽셀 기준으로 잡으면 **UI 요소 크기와 블록 크기가 따로 논다** — 1080p 에서 딱 맞춘 도트가 1440p 에서 절반 크기로 보인다. → 블록 크기를 "가상 세로 해상도"(예: 360) 기준으로 잡고 CanvasScaler 를 통일해야 한다. 이건 픽셀화보다 먼저 정리할 문제다.
3. **클릭 좌표 어긋남** — 방식 A 한정이지만 치명적. RT 크기 ≠ 화면 크기면 `GraphicRaycaster` 도, `Mouse.current.position` 을 직접 읽는 `LDY_CardPlacer` / `LDY_SelectionController` / `DLJ_WillTest` 도 동시에 어긋난다. 방식 C 는 해당 없음.
4. **텍스트 레이어 정렬** — 방식 A 한정. 텍스트 캔버스가 별도면 (a) 모달 패널이 뒤 화면 글자를 못 가림, (b) `RectMask2D` 가 캔버스를 못 넘어가 스크롤 밖으로 글자 노출, (c) LayoutGroup 이 계산한 위치를 매 프레임 복사해야 함 → 1프레임 지연으로 글자가 패널 뒤에 끌려다닌다.
5. **DXT 압축을 안 끄면 Point 필터가 무의미** — 4x4 블록 단위 색 뭉개짐이 도트가 아니라 노이즈로 보인다. `compression: None` 이 Point 필터보다 중요할 수 있다.
6. **Mask / RectMask2D** — 커스텀 UI 셰이더에 `UNITY_UI_CLIP_RECT`·`_Stencil*` 프로퍼티를 안 넣으면 마스킹이 통째로 깨진다. `UI/Default` 원본을 복사해서 시작할 것.
7. **Sliced / Tiled / Filled Image** — UV 가 불연속이라 블록 스냅 시 이음매에서 색이 튄다. `Rounded Filled 128px`, `Square.png`, `Panel Corner*` 등 나인슬라이스 패널이 많으므로 초기 검증 대상.
8. **WorldSpace 캔버스** — `HandCard.prefab` 은 WorldSpace 다. 화면 UI 규칙이 그대로 안 먹으므로 별도 판단 필요(월드 픽셀화 대상인지, 아니면 예외인지).
9. **DDoL 캔버스의 다중 인스턴스** — SceneAutoFader / SceneTransition / RewardChoiceUI 가 각각 자기 싱글톤 규칙으로 산다. 씬 전환 후 캔버스가 두 개 겹치거나 픽셀화 컴포넌트가 두 번 붙는 상황을 테스트해야 한다.
10. **TMP 폰트 톤** — 텍스트만 원해상도로 남기는 게 요구사항이지만, MaruBuri 같은 얇은 한글 SDF 폰트는 픽셀 배경 위에서 대비가 과하게 보인다. 픽셀 UI 위 벡터 폰트 조합은 아트 디렉션 확인이 필요하다 (요구사항이므로 그대로 가되, 스타일 확인은 받을 것).
11. **`_Recovery` 폴더** — 씬 24개가 들어 있다. 조사에서 제외했다. 참조되지 않는 잔재로 보이지만 정리 여부는 확인이 필요하다.
12. **InfoPanel 의 스크립트 참조가 깨져 있다** — `LDY_TestScene` / `LSO_Test` / `KTH_UiTestScene` / `DLJ_WillUIScene` / `CardDrowUI.prefab` 의 InfoPanel 에 붙은 MonoBehaviour 가 guid `d8bfb72d7e167984f976251b9cd42f84` 를 가리키는데, `Assets` 와 `PackageCache` 어디에도 해당 `.cs` 가 없다 (Missing Script). 픽셀화와 직접 관계는 없지만, InfoPanel 계층을 건드리는 작업 전에 정리하는 편이 안전하다.
13. **`LDY_MapScene` 과 `LDY/Map/Prefabs` 밑 노드 프리팹** — 실제 맵은 `KTH_StageScene` 의 MapCanvas 다. 이쪽을 고쳐도 게임에 반영되지 않는다. 같은 이름의 프리팹이 `_Scripts/LDY/Map/Prefabs` 와 `_Scripts/LDY/01.Scripts/LDY/Map/Prefabs` 두 군데에 중복 존재하니 주의.

---

## 10. 착수 순서 제안

1. **LSO 에게 `PC_Renderer` 의 Pixelize/Posterize 피처 정리 요청** — 이게 안 되면 뭘 해도 결과가 안 보인다.
2. CanvasScaler 정책 합의 (ScaleWithScreen 1920x1080 / match 0.5, 가상 픽셀 세로 해상도 확정 — 360 또는 270 권장).
3. LDY 단독으로 `LDY_UIPixelate.shader` + `LDY_UIPixelizer.cs` 작성 → `LDY_TestScene` 에서 프로토타입 검증 (Sliced Image, RectMask2D, 런타임 Starfield 3케이스 필수 확인).
4. 텍스처 임포트 설정 변경안을 스크린샷과 함께 팀에 제시 → 합의 후 일괄 적용.
5. 검증 통과 후 KTH/LSO 에게 캔버스 루트 컴포넌트 부착 요청.
