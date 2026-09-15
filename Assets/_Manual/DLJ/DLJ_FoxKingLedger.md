# DLJ 여우왕 장부

- 씬: `Assets/_Scenes/DLJ/DLJ_GameTestScene.unity`
- 프리팹: `Assets/_Prefabs/DLJ/DLJ_FoxKingLedger.prefab`
- 씬 루트 이름: `DLJ_FoxKingLedger`
- 위치: `(5.6245, -0.595, 9.94)`, 회전: `(0, 12, 0)`, 크기: `(1, 1, 1)`
- 기존 흰색 `Quad`, `Quad (1)` 두 개를 장부 프리팹 하나로 교체.
- 두 장의 곡면 메시, 종이 단면, 가죽 표지, 책등 바느질과 황동 모서리로 구성.
- 종이·가죽·단면·황동은 각각 별도 URP/Lit 머티리얼 사용.
- 프리팹과 글자는 편집 모드에서도 보임. Collider와 UI Canvas 없음.
- 종이는 단색 아이보리. 기존 얼룩 텍스처와 고정 문구는 머티리얼에서 해제했으며 PNG는 미사용 원본으로 보관.
- 참고 이미지 형식: 왼쪽 페이지는 비우고 오른쪽에 `수탈 숫자 / 탐욕 숫자 / 구분선 / 보상 세 줄` 순으로 배치. 달성한 보상 문구 위에 붉은 취소선을 왼쪽에서 오른쪽으로 긋기.
- `DLJ_FoxKingLedger`가 같은 씬의 활성·생존 여우왕을 최대 0.25초 간격으로 검색하고, 연결 후 `OnStolenResourcesChanged` / `OnGreedChanged` 이벤트 구독.
- 최초 연결 시 현재 보유량 동기화. 획득·투자 소비 즉시 반영. 사망·비활성·제거 시 `-` 표시, 새 여우왕 생성 시 재연결. 장부 비활성 시 구독 해제.
- `DLJ_LedgerInkText`의 `Animate Ink`는 수탈·탐욕 숫자 두 오브젝트에서만 활성화. 숫자가 등장하거나 바뀌면 0.65초 동안 번진 후 마르는 효과 적용. 같은 값은 다시 재생하지 않음. 제목·보상 문구·`-` 표시는 즉시 보이며 번짐 없음. `Spread Duration`으로 시간 조절.
- `DLJ_LedgerStrike`는 실제 보상 문구 길이를 측정해 종이 곡면 위에 취소선 생성. `Draw Duration` 기본 0.22초, `Stroke Width` 기본 0.018. 여러 단계 동시 달성 시 위에서부터 한 줄씩 긋고 `Stroke Interval` 기본 0.1초만큼 쉬기. 완료된 선은 추가 탐욕 획득 시 재생하지 않음. 사망/미연결/비활성 시 그리기 대기열과 선 초기화.
- `DLJ/Ledger Ink` 셰이더와 Renderer별 MaterialPropertyBlock 사용. 공유 폰트/머티리얼을 런타임에 변경하지 않음.
- 글자·구분선·취소선 메시를 종이 곡면에 맞춰 배치. 수치 기본 글자 크기 4.2, 제목 3.8, 보상 2.05; 영역에 맞춰 자동 축소.
- 프리팹의 `Milestone Rows` / `Milestone Strikes`는 기존 여우왕 보상 3단계에 대응. 보상 단계를 추가하려면 표시 행과 취소선도 같은 순서로 추가.
- `DLJ_LedgerFont.asset`은 기존 Song Myung 원본 폰트로 만든 장부 전용 정적 SDF. 공백·숫자·필요한 한글을 포함하며 기존 공용 폰트는 수정하지 않음.

## 검증

전체 게임 C# 컴파일 통과(기존 미사용 멤버 경고 2개).
Unity 6000.3.6f1 별도 배치 프로젝트의 실제 플레이 모드에서 84개 검사 통과.
실제 `DLJ_FoxKingBoss.Gain/TrySpend`, TMP, 전용 셰이더를 사용해 최초 연결, 획득·소비, 연속 변경, 보상 달성, 비활성/재활성, 사망, 새 보스 연결, 다른 씬 보스 제외, 글리프와 글자 영역을 검사.
별도 검증의 Health/Animal/Phase는 최소 대체 컴포넌트 사용.
잉크 진행도 0 / 0.35 / 1 및 취소선 부분/완성 렌더링 확인. 동시 단계 달성 시 순차 예약, 대기 중 숨김, 좌→우 길이 증가, 완료 후 재생 방지, 사망 시 선/대기 초기화 검사 포함. 미리보기는 별도 기본 렌더 파이프라인 조명이며 실제 게임 씬의 URP 조명·후처리 결과와는 차이가 있음.
씬 수정은 파일로 적용했고 실시간 에디터 제어는 사용하지 않음.

2026-09-13 TMP 호환 수정: 잉크 셰이더에 `_OutlineSoftness` 속성 선언 추가.
TMP `UpdateShaderRatios`가 이 속성을 조건 없이 읽으므로 머티리얼에 저장된 값만으로는 충분하지 않음.
수정 전 새 머티리얼에서 동일 오류 재현, 수정 후 새/저장된 머티리얼의 비율 계산·단일/배열 패딩 계산·최초/변경 글자 메시 생성에서 오류 로그 0개 확인.

## 미사용 원본 질감 생성 기록

내장 image_gen 도구 사용. 최종 프롬프트:

```text
Generate a production game texture, a flat orthographic top-down albedo scan of an antique open ledger's TWO parchment pages filling the ENTIRE rectangular image edge to edge, aspect ratio 1.6:1 (two portrait pages side by side). No background, no table, no perspective, no book cover, no external shadow. Thin dark vertical binding crease exactly at center x=50%. Left page is blank aged parchment with subtle ink ghosting, brown mottling and worn edges. Right page has clear dark handwritten Korean text centered in its top half: first line '수탈  3', second line '탐욕  7', thin hand-drawn horizontal divider, then three lines '5  공격 +1', '10  공격 +1', '15  최대 체력 +5'. A small dark red check mark at the right of the 5 row. Text must be exact and legible. Warm muted ivory tan parchment, restrained mottling, dark brown ink, thin antique sepia double-rule border near outer page edges. Dark fantasy medieval ledger, realistic paper fibers, subtle worn brown edges. Balanced flat diffuse illumination, NO specular highlights, NO green, NO directional lighting. This texture maps directly onto two 3D pages so perfectly rectangular, full-bleed pages and undistorted aligned text are essential.
```
