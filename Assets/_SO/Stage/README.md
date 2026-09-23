# 스테이지 SO

일반 전장과 개발용 전장 SO는 `Assets/_SO/Stage/`에서 관리합니다.

| 폴더 | 내용 |
| --- | --- |
| `Chapter1` | 1막 챕터 SO와 일반 전장 A/B/C, 보스 전장 |
| `Chapter2` | 2막 챕터 SO와 일반 전장 A/B/C, 보스 전장 |
| `Chapter3` | 3막 챕터 SO와 일반 전장 A/B/C, 보스 전장 |
| `Chapter4` | 4막 챕터 SO와 일반 전장 A/B/C, 보스 전장 |
| `Development/DLJ` | DLJ 테스트 씬용 2챕터·6전장·보상표 |
| `Development/LDY` | 기존 테스트·샘플·황소왕 전장 |
| `Development/LSO` | `LSO_TestState` 테스트 전장 |

`LSO_StageProgression.chapters`에는 각 막의 `LSO_Chapter1`~`LSO_Chapter4`를 순서대로 연결합니다. 전장 배치는 각 `Stage_*`에서, 전장 선택 후보와 진행 순서는 챕터 SO에서 수정합니다.

튜토리얼 전투는 `Assets/_SO/Tutorial/Battle/`에 있습니다. 튜토리얼 씬은 첫 막에 `TutorialBattleChapter`를 사용합니다.

폴더 이동 시 기존 `.meta`와 GUID를 보존했습니다. 개발용 에셋도 삭제하지 않았으므로 기존 테스트 씬에서 계속 사용할 수 있습니다.
