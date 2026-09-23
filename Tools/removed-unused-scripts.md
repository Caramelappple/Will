# 미사용 스크립트 정리 — 2026-09-23

프로젝트 전용 스크립트 467개를 조사해 21개와 대응 `.meta`를 삭제했습니다.

판정 기준: 전체 Assets의 씬·프리팹·SO 등 직렬화 참조, 전체 C# 코드의 타입 참조, 문자열 타입 참조, 자동 초기화와 에디터 진입점을 확인했습니다. 삭제 대상끼리의 참조도 함께 확인했습니다. 기존 수정 사항이 있는 파일은 삭제 대상에 포함하지 않았습니다.

에디터 메뉴·검증 도구·런타임 자동 등록·SerializeReference 선택형 AI와 외부 라이브러리는 유지했습니다.

## 삭제 목록

- `Assets/_Scripts/LSO/Will/LSO_TestCurseWill.cs`
- `Assets/_Scripts/LSO/UI/Transition/LSO_SceneLoadButton.cs`
- `Assets/_Scripts/LSO/UI/Popup/LSO_IDamagePopupSpawner.cs`
- `Assets/_Scripts/LSO/UI/Popup/LSO_HealthPopupBinder.cs`
- `Assets/_Scripts/LSO/UI/Popup/LSO_HealthBar.cs`
- `Assets/_Scripts/LSO/UI/Popup/LSO_DamagePopupSpawner.cs`
- `Assets/_Scripts/LSO/Boss/CrowKing/LSO_CrowKingDebug.cs`
- `Assets/_Scripts/LSO/UI/Effect/LSO_ClickShakeEffect.cs`
- `Assets/_Scripts/LSO/UI/Effect/LSO_ClickScaleEffect.cs`
- `Assets/_Scripts/LSO/UI/Effect/LSO_ClickRotateEffect.cs`
- `Assets/_Scripts/KTH/CardManager/CardDeck/HandCard/KTH_HandCardAnchorSync.cs`
- `Assets/_Scripts/LDY/Save/LDY_SetNewRunFlag.cs`
- `Assets/_Scripts/LDY/Save/LDY_DataResetHandler.cs`
- `Assets/_Scripts/LDY/Save/LDY_ContinueButtonVisibility.cs`
- `Assets/_Scripts/DLJ/Will/DLJ_IWillActivation.cs`
- `Assets/_Scripts/DLJ/UI/WorldUI/DLJ_WorldUIBillboard.cs`
- `Assets/_Scripts/DLJ/UI/WorldUI/DLJ_WorldUIAnchor.cs`
- `Assets/_Scripts/DLJ/UI/InfoPanel/DLJ_InfoPanelDoubleClickTest.cs`
- `Assets/_Scripts/DLJ/UI/InfoPanel/DLJ_CardInfoRelay.cs`
- `Assets/_Scripts/DLJ/SceneFlow/DLJ_SceneState.cs`
- `Assets/_Scripts/DLJ/SceneFlow/DLJ_GameSceneStates.cs`
