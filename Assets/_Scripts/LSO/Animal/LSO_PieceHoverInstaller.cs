using _Scripts.LDY;
using _Scripts.LSO.UI.Effect;
using _Scripts.LSO.UI.Input;
using UnityEngine;

namespace _Scripts.LSO.Animal
{
    /// <summary>
    /// 갓 태어난 기물에 호버 연출을 붙인다.
    ///
    /// 프리팹마다 손으로 붙이지 않는 이유는, 동물이 늘 때마다 반복해야 하고
    /// 한 번 빠뜨리면 그 기물만 조용히 반응이 없기 때문이다.
    /// 기물이 태어나는 입구는 LSO_AnimalFactory 하나뿐이라 여기 한 곳에서 챙길 수 있다.
    ///
    /// 프리팹이 이미 호버를 들고 있으면 아무것도 하지 않는다.
    /// 그쪽이 직접 맞춰둔 값이 있다는 뜻이고, 덮어쓰면 인스펙터에 보이는 값과
    /// 실제로 도는 값이 달라진다. 기본값과 다르게 하고 싶은 기물은 그렇게 하면 된다.
    /// </summary>
    public static class LSO_PieceHoverInstaller
    {
        private static LSO_PieceHoverSettingsSO _settings;

        // 씬 배선 경고는 기물마다 같은 말이 되므로 한 번만 낸다.
        private static bool _warnedAboutEventSystem;

        /// <summary>
        /// 기본값 한 벌. 처음 물어볼 때 Resources에서 읽고 그 뒤로는 들고 있는다.
        ///
        /// 에셋이 없으면 코드 기본값으로 돈다. 통째로 죽이지 않는 이유는,
        /// 그러면 "에셋을 안 만들었다"가 아니라 "원래 없는 기능이다"로 보이기 때문이다.
        /// 대신 한 번은 짚고 넘어간다.
        /// </summary>
        public static LSO_PieceHoverSettingsSO Settings
        {
            get
            {
                if (_settings != null) return _settings;

                _settings = Resources.Load<LSO_PieceHoverSettingsSO>(
                    LSO_PieceHoverSettingsSO.ResourcePath);

                if (_settings == null)
                {
                    Debug.LogWarning(
                        "[LSO_PieceHoverInstaller] " +
                        $"Assets/Resources/{LSO_PieceHoverSettingsSO.ResourcePath}.asset 이 없어 " +
                        "코드 기본값으로 호버를 붙입니다.");

                    _settings = LSO_PieceHoverSettingsSO.CreateDefault();
                }

                return _settings;
            }

            // 테스트에서 값을 갈아끼울 수 있게 열어둔다. 평소에는 쓰지 않는다.
            set => _settings = value;
        }

        /// <summary>
        /// static은 플레이를 멈춰도 남는다. 도메인 리로드를 꺼둔 프로젝트에서는
        /// 지난 판에서 읽은 설정과 이미 낸 경고를 그대로 들고 다시 시작한다.
        /// 에셋을 고쳐도 반영이 안 되고, 배선이 빠진 씬에서 경고가 안 나온다.
        /// </summary>
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            _settings = null;
            _warnedAboutEventSystem = false;
        }

        public static void Install(LDY_Animal animal)
        {
            if (animal == null) return;

            LSO_PieceHoverSettingsSO settings = Settings;

            if (!settings.EnableHover) return;

            GameObject target = animal.gameObject;

            // 프리팹이 직접 들고 있으면 그쪽이 주인이다.
            if (target.GetComponent<LSO_ButtonHoverHandler>() != null) return;

            WarnIfUnreachable(animal);

            // 연출을 먼저 붙인다. RequireComponent가 핸들러를 알아서 함께 붙인다.
            LSO_HoverMoveEffect move = target.AddComponent<LSO_HoverMoveEffect>();
            move.Configure(ModelOf(animal), settings.Move);

            if (settings.EnableCursorChange)
            {
                LSO_HoverCursorEffect cursor = target.AddComponent<LSO_HoverCursorEffect>();

                // 기물에는 클릭 핸들러가 없다. 선택은 LDY_SelectionController가 따로 맡는다.
                cursor.Configure(target.GetComponent<LSO_ButtonClickHandler>() != null);
            }

            LSO_TeamHoverGate gate = target.AddComponent<LSO_TeamHoverGate>();
            gate.Configure(animal, settings.AllowedTeam);

            // 핸들러는 연출보다 먼저 붙어서 Awake를 돌렸다. 그때는 목록이 비어 있었다.
            LSO_ButtonHoverHandler handler = target.GetComponent<LSO_ButtonHoverHandler>();

            if (handler != null) handler.Rescan();
        }

        /// <summary>
        /// 옮길 대상. 모델을 따로 두는 이유는 콜라이더를 제자리에 남기기 위해서다.
        /// 루트째 올리면 콜라이더가 커서 아래에서 빠져나가 떨림이 생긴다.
        /// </summary>
        private static Transform ModelOf(LDY_Animal animal)
        {
            Transform model = animal.modelTransform;

            // LDY_Animal이 비어 있으면 자기 자신을 넣어두므로 null로는 오지 않는다.
            // 그래서 "루트와 같은지"로 본다 — 프리팹에서 모델을 안 걸어둔 경우다.
            if (model != null && model != animal.transform) return model;

            Debug.LogWarning(
                $"[LSO_PieceHoverInstaller] {animal.name}의 modelTransform이 루트와 같아 루트를 옮깁니다. " +
                "콜라이더가 함께 움직여 커서가 들락거릴 수 있습니다. " +
                "프리팹에서 모델 자식을 걸어주세요.", animal);

            return animal.transform;
        }

        /// <summary>
        /// 호버는 EventSystem이 콜라이더를 때려야 온다. 둘 중 하나만 없어도 조용히 안 된다.
        /// </summary>
        private static void WarnIfUnreachable(LDY_Animal animal)
        {
            if (animal.GetComponentInChildren<Collider>() == null)
            {
                Debug.LogWarning(
                    $"[LSO_PieceHoverInstaller] {animal.name}에 Collider가 없어 호버가 오지 않습니다. " +
                    "프리팹에 Collider를 넣어주세요.", animal);
            }

            // 씬 사정이라 기물마다 같은 말이 나온다. 한 번만 짚는다.
            if (_warnedAboutEventSystem) return;

            if (UnityEngine.EventSystems.EventSystem.current == null)
            {
                _warnedAboutEventSystem = true;

                Debug.LogWarning(
                    "[LSO_PieceHoverInstaller] 씬에 EventSystem이 없어 호버가 오지 않습니다.");

                return;
            }

            if (Camera.main != null &&
                Camera.main.GetComponent<UnityEngine.EventSystems.PhysicsRaycaster>() == null)
            {
                _warnedAboutEventSystem = true;

                Debug.LogWarning(
                    "[LSO_PieceHoverInstaller] 메인 카메라에 Physics Raycaster가 없어 " +
                    "3D 콜라이더로 호버가 오지 않습니다.", Camera.main);
            }
        }
    }
}
