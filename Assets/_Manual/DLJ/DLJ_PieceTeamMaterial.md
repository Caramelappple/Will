# 기물의 적·아군 머티리얼

## 기본 동작

- `LDY_Animal.Awake`에서 `DLJ_PieceTeamMaterial`을 자동 연결해. 씬에 놓은 기물과 런타임 소환 기물 모두 대상이야.
- `LDY_Animal.team`이 `Player`면 파랑, `Enemy`면 빨강 머티리얼을 사용해. 색은 임시 기본값이야.
- `Setup`으로 정해진 팀은 즉시 반영하고, `team` 필드에 직접 대입한 변경은 `LateUpdate`에서 반영해.
- `modelTransform` 아래의 비활성 메시도 포함하며, 모델 참조가 없으면 기물 루트에서 찾아.
- 각 메시의 첫 번째 머티리얼 슬롯만 교체해. 파티클, 트레일, 라인, TMP 글자와 다른 기물의 자식 메시는 제외해.
- 에셋의 색이나 셰이더를 코드로 수정하지 않고 `sharedMaterials`의 참조만 교체해.

## 색 바꾸기

1. Project에서 `Assets/Resources/DLJ/DLJ_PieceTeamMaterials.asset`을 선택해.
2. `Player Material`과 `Enemy Material`에 원하는 머티리얼을 연결해.
3. 기본 머티리얼은 `Assets/_Material/DLJ/DLJ_PiecePlayer.mat`와 `DLJ_PieceEnemy.mat`이야. 각 머티리얼의 Base Map 색으로도 조절할 수 있어.

팀 머티리얼을 비우면 그 팀에는 원래 머티리얼을 사용해. 머티리얼 전체를 교체하므로 선택한 슬롯의 텍스처와 셰이더도 새 머티리얼 기준이야.

## 기물별 설정

프리팹의 `LDY_Animal`이 붙은 오브젝트에 `DLJ_PieceTeamMaterial`을 미리 추가해. 자동 연결은 기존 설정을 유지해.

| 필드 | 설정 |
| --- | --- |
| Settings | 기물 전용 설정 에셋. 비우면 공통 설정을 사용해 |
| Target Renderers | 몸체 등 교체할 렌더러를 직접 연결해. 비우면 모델 아래에서 자동 검색해 |
| Material Slot | 교체할 슬롯 번호. 기본 0이며 나머지 슬롯은 유지해 |

눈·장식·보스의 특수 셰이더를 유지하려면 `Target Renderers`에 몸체만 지정해. 해당 기물을 제외하려면 미리 붙인 컴포넌트를 비활성화해. 설정 에셋은 Create → DLJ → Piece Team Materials에서 만들 수 있어.

렌더러 목록과 슬롯은 플레이 시작 전에 지정해. 같은 팀에서는 머티리얼을 반복해서 덮지 않으므로 사망·페이드 연출의 교체가 유지돼. 컴포넌트를 끄면 자신이 적용한 머티리얼이 남아 있는 슬롯만 복구해.

## Unity 확인 절차

1. 같은 기물 두 개의 Team을 각각 Player / Enemy로 지정하고 플레이해. 파랑 / 빨강으로 구분되는지 확인해.
2. 플레이 중 Team을 바꿔 색이 따라오는지 확인해.
3. 카드 소환과 스테이지 적 배치에도 해당 팀 색이 적용되는지 확인해.
4. 몸체 외 장식과 사망·꼬리 페이드 등 개별 연출을 확인해. 기존 MaterialPropertyBlock이 색을 지정하는 모델은 그 색이 우선할 수 있어.

검증: Unity 6000.3.6f1의 컴파일러와 프로젝트 참조로 전체 런타임 어셈블리 컴파일 성공. 플레이 모드의 실제 화면 확인은 아직 하지 않았어.
