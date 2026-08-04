# KTH SoundManager 코드 리뷰

**대상**: `Assets/_Scripts/KTH/SoundManager/` 8개 파일 (약 240줄)
**작성**: LSO 브랜치 작업 중 구조 점검
**요약**: 프로젝트 내에서 설계가 가장 깔끔한 모듈입니다. 인터페이스 분리와 책임 분할이 제대로 돼 있습니다. 다만 씬 전환 시 터지는 싱글톤 버그와, 사운드가 늘어나면 시스템 전체를 죽이는 예외 하나가 있어 먼저 처리가 필요합니다.

---

## 현재 구조

```
KTH_SoundSettingManager  (볼륨 UI)
        │
KTH_SoundManager  (싱글톤 · 조회 후 위임)
        ├─ KTH_SoundLibrarySO : KTH_ISoundRepository    id → KTH_SoundData 조회
        ├─ KTH_BgmPlayer      : KTH_IAudioPlayer        AudioSource.Play
        └─ KTH_SfxPlayer      : KTH_IAudioPlayer        AudioSource.PlayOneShot
```

| 파일 | 줄 수 | 역할 |
|---|---:|---|
| `KTH_SoundManager` | 46 | 파사드. 조회 후 플레이어에 위임 |
| `KTH_SoundSettingManager` | 47 | 볼륨 슬라이더 UI |
| `KTH_BgmPlayer` | 34 | 루프 재생 + 볼륨 |
| `KTH_SfxPlayer` | 30 | 원샷 재생 + 볼륨 |
| `KTH_SoundLibrarySO` | 21 | id → 데이터 조회 |
| `KTH_SoundData` | 10 | 클립·볼륨·루프 데이터 |
| `KTH_ISoundRepository` | 6 | 조회 계약 |
| `KTH_IAudioPlayer` | 8 | 재생 계약 |

---

## 잘 된 부분

**1. 인터페이스 분리 (ISP · DIP)**

`KTH_IAudioPlayer`(재생)와 `KTH_ISoundRepository`(조회)를 나눠서, 매니저는 구체 타입이 아니라 계약에만 의존할 수 있는 구조가 갖춰졌습니다. 나중에 Addressables나 원격 로딩으로 바꿔도 `KTH_SoundManager`는 그대로입니다.

**2. 같은 계약, 다른 구현**

BGM은 `audioSource.Play()`로 루프 재생, SFX는 `PlayOneShot()`으로 중첩 재생. 인터페이스 하나로 묶으면서 재생 방식 차이를 각 구현체가 흡수합니다. 다형성이 실제로 값을 하는 사례입니다.

**3. 책임 분할**

클래스당 30~45줄이고 역할이 하나씩입니다. 매니저는 조회·위임만, 볼륨 계산은 각 플레이어가, 데이터는 SO가 담당합니다.

**4. 조회 성능**

`List` 순회가 아니라 `Dictionary` 캐싱으로 O(1) 조회입니다.

---

## 개선 제안

### 우선순위 상 — 싱글톤이 중복을 처리하지 않음

**현상**

```csharp
private void Awake()
{
    Instance = this;   // 조건 없이 덮어씀
}
```

`KTH_DontDestroy`로 매니저가 씬을 넘어 살아남는데, 다음 씬에도 매니저가 배치돼 있으면 `Instance`가 새 오브젝트로 교체됩니다. 결과적으로 **이전 인스턴스는 살아 있으면서 참조만 끊기고**, 볼륨 설정은 옛 인스턴스에 남은 채 재생은 새 인스턴스로 나갑니다. AudioSource가 둘 다 살아 있어 BGM이 겹쳐 들릴 수도 있습니다.

**제안**

```csharp
private void Awake()
{
    if (Instance != null && Instance != this)
    {
        Destroy(gameObject);
        return;
    }
    Instance = this;
}

private void OnDestroy()
{
    if (Instance == this) Instance = null;
}
```

LSO의 `MonoSingleton<T>`를 상속하면 이 처리와 종료 시점 안전장치가 이미 들어 있습니다. 재사용을 권합니다.

### 우선순위 상 — `ToDictionary`가 SO 로딩을 통째로 실패시킴

**현상**

```csharp
private void OnEnable()
{
    _lookup = sounds.ToDictionary(s => s.id, s => s);
}
```

- `sounds`에 빈 항목이 있으면 → `NullReferenceException`
- **id가 중복되면 → `ArgumentException`**

사운드가 늘어나면 ID 중복은 반드시 발생하는데, 그 순간 라이브러리 로딩이 실패해 **사운드 시스템 전체가 죽습니다.** 게다가 원인이 SO 로딩 시점이라 추적이 어렵습니다.

**제안**

```csharp
private void BuildLookup()
{
    _lookup = new Dictionary<string, KTH_SoundData>();

    foreach (var sound in sounds)
    {
        if (sound == null || string.IsNullOrEmpty(sound.id)) continue;

        if (!_lookup.TryAdd(sound.id, sound))
            Debug.LogWarning($"중복 사운드 ID: {sound.id}", this);
    }
}

public KTH_SoundData GetSound(string id)
{
    if (_lookup == null) BuildLookup();   // OnEnable 호출 순서에 의존하지 않음
    _lookup.TryGetValue(id, out var data);
    return data;
}
```

`_lookup`이 null인 상태로 `GetSound`가 불릴 여지도 함께 막힙니다.

### 우선순위 중 — 사운드 ID가 매직 스트링

**현상**

```csharp
KTH_SoundManager.Instance.PlayBgm("1");
KTH_SoundManager.Instance.PlaySfx("2");
```

오타가 나도 컴파일은 통과하고, 런타임에 `GetSound`가 null을 반환해 **아무 일도 일어나지 않은 채 조용히 넘어갑니다.** 어떤 ID가 존재하는지 코드만 봐서는 알 수 없고, `"1"`, `"2"`라는 이름은 의미도 전달하지 못합니다.

**제안 (둘 중 택 1)**

```csharp
// A. 상수 클래스 — 도입이 가장 쉬움
public static class KTH_SoundId
{
    public const string TitleBgm = "title_bgm";
    public const string CardDraw = "card_draw";
}

// B. enum + SO에서 enum 필드로 관리 — 타입 안전성이 가장 높음
```

어느 쪽이든 **조회 실패 시 경고 로그는 반드시 추가**하는 편이 좋습니다.

```csharp
var data = library.GetSound(id);
if (data == null)
{
    Debug.LogWarning($"사운드 ID '{id}'를 찾을 수 없습니다.");
    return;
}
```

지금 사운드가 2개일 때 바꾸는 것이 가장 저렴합니다.

### 우선순위 중 — `KTH_SoundData.loop`가 무시됨

**현상**

```csharp
public void Play(KTH_SoundData data)
{
    audioSource.loop = true;   // data.loop을 쓰지 않음
}
```

SO에 `loop` 필드를 만들어 뒀는데 사용되지 않습니다. 승리 팡파레처럼 한 번만 재생하는 소리를 BGM 채널로 틀 수 없습니다.

**제안**: `audioSource.loop = data.loop;`

### 우선순위 중 — 볼륨 반영의 비대칭

`KTH_SfxPlayer.SetVolume()`은 필드만 갱신하고 `audioSource`를 건드리지 않습니다. `PlayOneShot` 특성상 이미 재생 중인 소리에는 적용할 수 없어 구조상 자연스럽지만, 결과적으로 **BGM 슬라이더는 즉시 반영되고 SFX 슬라이더는 다음 소리부터 반영**됩니다. 의도한 동작인지 확인이 필요하며, 의도한 것이라면 주석으로 남겨두는 편이 좋습니다.

### 우선순위 하 — 볼륨 설정이 저장되지 않음

```csharp
private void Start()
{
    masterSlider.value = 1f;   // 매번 1로 초기화
    bgmSlider.value = 1f;
    sfxSlider.value = 1f;
}
```

게임을 재시작하면 설정이 사라집니다. `PlayerPrefs` 또는 `GameSaveData`에 연결할 자리입니다.

### 우선순위 하 — 입력 시스템 혼재

```csharp
using UnityEngine.InputSystem;   // 선언만 하고
...
if (Input.GetKeyDown(KeyCode.Space))   // 레거시 Input 사용
```

`KTH_SoundTest`와 `KTH_SoundSettingManager` 모두 레거시 `Input`을 쓰는데, LDY 쪽은 `Mouse.current` 기반 New Input System을 씁니다. 현재 프로젝트 설정이 Both라 둘 다 동작하지만, 팀 차원의 통일이 필요합니다.

### 우선순위 하 — 네임스페이스 부재

8개 파일 전부 전역 네임스페이스입니다. LSO와 LDY는 정리가 끝나 KTH와 DLJ만 남았습니다. `namespace _Scripts.KTH.Sound` 추가를 권합니다. 외부 참조가 `KTH_SoundManager.Instance` 호출 정도라 파급이 작습니다.

---

## 체크리스트

- [ ] `KTH_SoundManager` 싱글톤 중복 처리 (또는 `MonoSingleton<T>` 상속)
- [ ] `KTH_SoundLibrarySO`의 `ToDictionary`를 예외 안전한 루프로 교체
- [ ] `GetSound` 진입 시 `_lookup` null 가드
- [ ] 사운드 ID를 상수/enum으로 전환
- [ ] 조회 실패 시 경고 로그 추가
- [ ] `KTH_BgmPlayer`에서 `data.loop` 반영
- [ ] SFX 볼륨 반영 시점이 의도한 동작인지 확인 후 주석
- [ ] 볼륨 설정 저장·복원
- [ ] 입력 시스템 통일 (팀 논의)
- [ ] `namespace _Scripts.KTH.Sound` 적용
