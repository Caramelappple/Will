using System;
using UnityEngine;

namespace _Scripts.LSO.UI
{
    /// <summary>
    /// 커서 모양을 정하는 유일한 곳.
    ///
    /// 물건마다 자기가 Cursor.SetCursor를 부르면, 겹친 물건 위에서 순서가 꼬인다.
    /// A에서 벗어나는 신호가 B에 들어간 뒤에 오면 B 위인데도 기본 커서로 돌아간다.
    ///
    /// 그래서 "누가 요청했나"를 세어둔다.
    /// 하나라도 요청이 남아 있으면 그 모양을 유지하고, 전부 물러야 기본으로 돌아간다.
    ///
    /// 씬 배선: 씬 아무 곳에나 하나 두면 된다. 요청은 코드로 들어온다.
    /// </summary>
    [DisallowMultipleComponent]
    public class LSO_CursorManager : MonoBehaviour
    {
        [Serializable]
        private struct CursorShape
        {
            [Tooltip("이 모양이 나타내는 상태.")]
            public LSO_CursorState state;

            [Tooltip("커서 그림. 임포트 설정에서 Texture Type을 Cursor로 둘 것.\n" +
                     "비워두면 OS 기본 커서가 된다.")]
            public Texture2D texture;

            [Tooltip("실제로 눌리는 점. 텍스처 왼쪽 위가 (0,0)이다.\n" +
                     "Center Hotspot을 켜두면 이 값은 무시된다.")]
            public Vector2 hotspot;

            [Tooltip("켜면 hotspot 대신 그림 한가운데를 쓴다. 십자 커서에 편하다.")]
            public bool centerHotspot;
        }

        [Header("모양")]
        [Tooltip("상태마다 하나씩. 목록에 없는 상태는 OS 기본 커서가 된다.\n" +
                 "Hidden은 그림이 필요 없다 — 넣어도 무시된다.")]
        [SerializeField] private CursorShape[] shapes =
        {
            new CursorShape { state = LSO_CursorState.Default },
            new CursorShape { state = LSO_CursorState.Blocked },
            new CursorShape { state = LSO_CursorState.Interactable }
        };

        [Header("그리는 방식")]
        [Tooltip("Auto  : OS가 그린다. 빠르지만 크기 제한이 있다(보통 32×32)\n" +
                 "Force Software : 유니티가 그린다. 큰 커서를 쓸 수 있지만 한 프레임 늦다")]
        [SerializeField] private CursorMode mode = CursorMode.Auto;

        /// <summary>지금 씬의 커서 매니저.</summary>
        public static LSO_CursorManager Instance { get; private set; }

        /// <summary>지금 보이는 모양.</summary>
        public LSO_CursorState Current { get; private set; } = LSO_CursorState.Default;

        // 상태마다 몇 개가 요청 중인지. 겹친 물건 위에서 순서가 꼬이지 않게 세어둔다.
        //
        // 길이를 손으로 적지 않는다. 숫자를 적어두면 상태가 늘었을 때 Add가
        // 새 값을 범위 밖으로 보고 조용히 버린다 — 요청해도 아무 일이 없다.
        private readonly int[] _requests =
            new int[Enum.GetValues(typeof(LSO_CursorState)).Length];

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Debug.LogWarning($"{name}: 커서 매니저가 둘 이상입니다. 마지막 것이 쓰입니다.", this);
            }

            Instance = this;
        }

        private void OnEnable()
        {
            Apply(LSO_CursorState.Default);
        }

        private void OnDisable()
        {
            // 꺼질 때 OS 기본으로 돌려둔다. 남겨두면 씬을 나가도 게임 커서가 남는다.
            Cursor.SetCursor(null, Vector2.zero, CursorMode.Auto);

            // 감춘 채로 꺼지면 아무도 되돌려주지 않는다. 커서 없는 게임이 된다.
            Cursor.visible = true;
        }

        private void OnDestroy()
        {
            // 자기가 Instance일 때만 지운다. 중복이 사라질 때 지우면 살아 있는 쪽까지 날아간다.
            if (Instance == this) Instance = null;
        }

        /// <summary>
        /// 이 모양을 써달라고 요청한다. 물건에 커서가 올라갈 때 부른다.
        ///
        /// 반드시 Release와 짝을 맞출 것. 한쪽만 부르면 그 모양이 영영 남는다.
        /// </summary>
        public static void Request(LSO_CursorState state)
        {
            if (Instance == null) return;

            Instance.Add(state, 1);
        }

        /// <summary>요청을 무른다. 물건에서 커서가 벗어날 때 부른다.</summary>
        public static void Release(LSO_CursorState state)
        {
            if (Instance == null) return;

            Instance.Add(state, -1);
        }

        /// <summary>
        /// 요청을 전부 지우고 기본으로 되돌린다.
        ///
        /// 씬을 넘기거나 창이 통째로 닫힐 때 부른다.
        /// 커서가 올라간 채로 오브젝트가 사라지면 Release가 오지 않아 요청이 남는다.
        /// </summary>
        public static void ResetAll()
        {
            if (Instance == null) return;

            Array.Clear(Instance._requests, 0, Instance._requests.Length);

            Instance.Refresh();
        }

        private void Add(LSO_CursorState state, int delta)
        {
            int index = (int)state;

            if (index < 0 || index >= _requests.Length) return;

            // 0 아래로 내려가면 이후 Request가 먹지 않는다.
            // Release가 한 번 더 온 것이므로 여기서 막고 알린다.
            if (_requests[index] + delta < 0)
            {
                Debug.LogWarning($"{name}: {state} 요청이 없는데 Release가 왔습니다.", this);
                _requests[index] = 0;
            }
            else
            {
                _requests[index] += delta;
            }

            Refresh();
        }

        /// <summary>
        /// 요청 중인 것 가운데 가장 앞선 것을 고른다.
        ///
        /// 열거 순서가 곧 우선순위다. Interactable이 뒤에 있으므로
        /// "누를 수 있다"가 "못 누른다"를 이긴다 — 겹쳤을 때 눌러도 되는 쪽을 알려주는 편이 낫다.
        /// </summary>
        private void Refresh()
        {
            LSO_CursorState next = LSO_CursorState.Default;

            for (int i = _requests.Length - 1; i >= 0; i--)
            {
                if (_requests[i] <= 0) continue;

                next = (LSO_CursorState)i;
                break;
            }

            if (next == Current) return;

            Apply(next);
        }

        private void Apply(LSO_CursorState state)
        {
            Current = state;

            // 감추기는 그림 문제가 아니라 보이냐 마느냐다. 목록을 볼 것도 없다.
            Cursor.visible = state != LSO_CursorState.Hidden;

            if (state == LSO_CursorState.Hidden) return;

            foreach (CursorShape shape in shapes)
            {
                if (shape.state != state) continue;

                if (shape.texture == null) break;

                Vector2 hotspot = shape.centerHotspot
                    ? new Vector2(shape.texture.width * 0.5f, shape.texture.height * 0.5f)
                    : shape.hotspot;

                Cursor.SetCursor(shape.texture, hotspot, mode);
                return;
            }

            // 그림이 없으면 OS 기본으로 둔다. 아무것도 안 하면 이전 모양이 남는다.
            Cursor.SetCursor(null, Vector2.zero, CursorMode.Auto);
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            for (int i = 0; i < shapes.Length; i++)
            {
                for (int j = i + 1; j < shapes.Length; j++)
                {
                    if (shapes[i].state != shapes[j].state) continue;

                    Debug.LogWarning($"{name}: {shapes[i].state} 가 목록에 두 번 들어 있습니다.", this);
                    return;
                }
            }
        }
#endif
    }
}
