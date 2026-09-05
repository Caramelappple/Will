using System.Collections;
using System.Collections.Generic;
using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.InputSystem;

namespace _Scripts.LSO.Camera
{
    /// <summary>
    /// 어느 카메라를 볼지 정한다. 시네머신 Priority를 만지는 곳은 여기 하나뿐이다.
    ///
    /// 카메라마다 Priority를 직접 올리고 내리는 코드가 여기저기 흩어지면
    /// 두 곳이 동시에 올렸을 때 어느 쪽이 이기는지 알 수 없게 된다.
    /// 그래서 "고른 하나만 높이고 나머지는 전부 기본값"을 매번 다시 맞춘다.
    ///
    /// 이징과 머무는 시간은 카메라마다 다르다. 그것을 브레인의 Default Blend에
    /// 넣어준 뒤 Priority를 올린다. 순서가 중요하다 —
    /// 전환이 시작된 뒤에 바꾸면 이미 진행 중인 것에는 반영되지 않는다.
    ///
    /// 씬 배선: 씬 아무 곳에나 하나 두고 Shots에 카메라들을 등록할 것.
    /// </summary>
    public class LSO_CameraDirector : MonoBehaviour
    {
        [Header("연결")]
        [Tooltip("비워두면 씬에서 찾는다.")]
        [SerializeField] private CinemachineBrain brain;

        [Header("샷 목록")]
        [Tooltip("여기 등록한 카메라만 이 매니저가 관리한다.")]
        [SerializeField] private List<LSO_CameraShot> shots = new List<LSO_CameraShot>();

        [Header("우선순위")]
        [Tooltip("꺼진 카메라에 줄 값. 켰을 때의 값은 샷마다 따로 정한다.")]
        [SerializeField] private int idlePriority;

        [Header("기본 샷")]
        [Tooltip("시작할 때 고를 샷이자, 돌아갈 곳. 비워두면 목록의 첫 번째.")]
        [SerializeField] private string startId;

        [Tooltip("기본 샷으로 돌아가게 만들 조작. 비워두면 자동으로 돌아가지 않는다.\n" +
                 "여러 개를 넣으면 그중 하나만 들어와도 돌아간다.")]
        [SerializeField] private List<LSO_CameraReturnTrigger> returnTriggers = new List<LSO_CameraReturnTrigger>();

        private readonly Dictionary<string, LSO_CameraShot> _byKey = new Dictionary<string, LSO_CameraShot>();

        private LSO_CameraShot _current;
        private LSO_CameraShot _previous;
        private Coroutine _holdRoutine;

        // 샷이 바뀐 프레임. 그 프레임의 조작은 돌아가기로 치지 않는다.
        private int _shotFrame = -1;

        /// <summary>지금 보고 있는 샷의 이름. 없으면 빈 문자열.</summary>
        public string CurrentId => _current != null ? _current.Key : string.Empty;

        /// <summary>전환이 진행 중인지. 연출을 기다릴 때 본다.</summary>
        public bool IsBlending => brain != null && brain.IsBlending;

        private void Awake()
        {
            if (brain == null)
                brain = FindAnyObjectByType<CinemachineBrain>();

            if (brain == null)
                Debug.LogError($"{name}: 씬에 CinemachineBrain이 없습니다.", this);

            BuildLookup();
        }

        private void Start()
        {
            LSO_CameraShot first = Find(startId) ?? (shots.Count > 0 ? shots[0] : null);
            if (first == null) return;

            // 시작 샷은 전환 없이 바로 잡는다. 게임에 들어가자마자 카메라가 흘러가면
            // 무엇을 보라는 것인지 알 수 없다.
            Play(first, instant: true);
        }

        private void BuildLookup()
        {
            _byKey.Clear();

            foreach (LSO_CameraShot shot in shots)
            {
                if (shot == null || shot.camera == null) continue;

                string key = shot.Key;

                if (string.IsNullOrEmpty(key)) continue;

                // 같은 이름이 둘이면 나중 것이 앞의 것을 가린다.
                // 부르는 쪽은 이름만 아는데 엉뚱한 카메라가 잡히므로 짚어준다.
                if (_byKey.ContainsKey(key))
                {
                    Debug.LogError($"{name}: '{key}' 이름이 두 번 쓰였습니다. Id를 서로 다르게 두세요.", this);
                    continue;
                }

                _byKey.Add(key, shot);
            }
        }

        /// <summary>이름으로 카메라를 바꾼다. 버튼이나 UnityEvent에서 부를 수 있다.</summary>
        public void Play(string id)
        {
            LSO_CameraShot shot = Find(id);

            if (shot == null)
            {
                Debug.LogWarning($"{name}: '{id}' 샷을 찾지 못했습니다.", this);
                return;
            }

            Play(shot, instant: false);
        }

        private void Update()
        {
            if (returnTriggers.Count == 0) return;

            // 이미 기본 샷이면 돌아갈 곳이 없다.
            if (_current == null || _current.Key == DefaultId) return;

            // 이 샷이 조작으로 빠져나오는 것을 허락하지 않는다.
            // 컷신처럼 끝까지 보여줘야 하는 샷은 Hold Time이나 Play로만 벗어난다.
            if (!_current.canReturn) return;

            // 샷을 켠 그 프레임의 조작은 무시한다.
            // 안 그러면 "아무 데나 클릭"이 방금 연 샷을 그 자리에서 도로 닫는다.
            if (Time.frameCount == _shotFrame) return;

            if (!IsReturnPressed()) return;

            ReturnToDefault();
        }

        /// <summary>
        /// 등록된 조작 중 하나라도 이번 프레임에 완성됐는지.
        ///
        /// 뗀 순간(wasReleasedThisFrame)을 본다. 누른 순간을 보면 버튼을 누르고 있는 동안
        /// 곧바로 돌아가버려서, 클릭 한 번이 아니라 "누르자마자"로 느껴진다.
        ///
        /// 뗄 때를 봐도 샷을 연 그 클릭에 걸리지는 않는다.
        /// uGUI 클릭도 뗄 때 발생하므로 Play와 같은 프레임이 되고, 그 프레임은 위에서 걸러진다.
        /// </summary>
        private bool IsReturnPressed()
        {
            foreach (LSO_CameraReturnTrigger trigger in returnTriggers)
            {
                switch (trigger)
                {
                    case LSO_CameraReturnTrigger.LeftClickAnywhere:
                        if (Mouse.current != null && Mouse.current.leftButton.wasReleasedThisFrame)
                            return true;
                        break;

                    case LSO_CameraReturnTrigger.RightClickAnywhere:
                        if (Mouse.current != null && Mouse.current.rightButton.wasReleasedThisFrame)
                            return true;
                        break;
                }
            }

            return false;
        }

        /// <summary>기본 샷으로 돌아간다. UnityEvent에 걸 수도 있다.</summary>
        public void ReturnToDefault()
        {
            LSO_CameraShot shot = Find(DefaultId) ?? (shots.Count > 0 ? shots[0] : null);

            if (shot == null) return;

            Play(shot, instant: false);
        }

        /// <summary>기본 샷의 이름. 비워뒀으면 목록의 첫 번째.</summary>
        private string DefaultId =>
            !string.IsNullOrEmpty(startId) ? startId
            : shots.Count > 0 && shots[0] != null ? shots[0].Key
            : string.Empty;

        /// <summary>직전 샷으로 돌아간다.</summary>
        public void Back()
        {
            if (_previous == null) return;

            Play(_previous, instant: false);
        }

        private void Play(LSO_CameraShot shot, bool instant)
        {
            if (shot?.camera == null) return;
            if (_current == shot) return;

            StopHold();

            // 브레인에 이징을 먼저 넣는다. Priority를 올린 뒤에 바꾸면
            // 이미 시작된 전환은 예전 설정으로 진행된다.
            if (brain != null)
                brain.DefaultBlend = instant
                    ? new CinemachineBlendDefinition(CinemachineBlendDefinition.Styles.Cut, 0f)
                    : shot.ToBlend();

            _previous = _current;
            _current = shot;
            _shotFrame = Time.frameCount;

            ApplyPriorities();

            if (!instant && shot.holdTime > 0f)
                _holdRoutine = StartCoroutine(Co_Hold(shot));
        }

        /// <summary>
        /// 고른 것만 높이고 나머지는 전부 내린다.
        ///
        /// 이전 것만 내리고 새 것만 올리는 편이 빠르지만, 씬에서 손으로 올려둔 카메라나
        /// 다른 코드가 올린 것이 섞이면 둘이 동시에 높은 채로 남는다.
        /// 개수가 몇 개뿐이므로 매번 전부 맞추는 쪽이 안전하다.
        /// </summary>
        private void ApplyPriorities()
        {
            foreach (LSO_CameraShot shot in shots)
            {
                if (shot?.camera == null) continue;

                // 켤 때는 그 샷이 들고 있는 값을 쓴다.
                // 공통 값을 쓰면 여러 카메라가 동시에 켜졌을 때 전부 같은 숫자가 되어
                // 시네머신이 어느 것을 고를지 알 수 없다.
                shot.camera.Priority = shot == _current ? shot.priority : idlePriority;
            }
        }

        #region 겹쳐 쓰기 (보류)

        // 샷 하나만 켜고 끄는 방식. 나머지를 내리지 않으므로 여럿이 동시에 켜져 있을 수 있고,
        // 그때 화면을 잡는 것은 우선순위가 가장 높은 카메라다.
        //
        // 지금은 쓰지 않는다. 카메라 하나만 켜지는 Play로 충분한데
        // 비슷한 것이 셋이면 어느 것을 써야 하는지가 매번 고민거리가 된다.
        //
        // 컷신이 여러 겹으로 쌓이는 상황이 생기면 그때 되살릴 것.
        // 그때는 샷마다 Priority가 서로 달라야 어느 것이 이길지 정해진다.
        //
        // public void Raise(string id)
        // {
        //     LSO_CameraShot shot = Find(id);
        //
        //     if (shot?.camera == null)
        //     {
        //         Debug.LogWarning($"{name}: '{id}' 샷을 찾지 못했습니다.", this);
        //         return;
        //     }
        //
        //     if (brain != null)
        //         brain.DefaultBlend = shot.ToBlend();
        //
        //     shot.camera.Priority = shot.priority;
        // }
        //
        // public void Lower(string id)
        // {
        //     LSO_CameraShot shot = Find(id);
        //
        //     if (shot?.camera == null)
        //     {
        //         Debug.LogWarning($"{name}: '{id}' 샷을 찾지 못했습니다.", this);
        //         return;
        //     }
        //
        //     shot.camera.Priority = idlePriority;
        // }

        #endregion

        /// <summary>
        /// 머무는 시간이 끝나면 다음 샷으로 넘긴다.
        ///
        /// 전환이 끝난 뒤부터 시간을 센다. 들어가는 도중부터 세면
        /// 블렌드가 긴 샷은 도착하자마자 떠나버린다.
        /// </summary>
        private IEnumerator Co_Hold(LSO_CameraShot shot)
        {
            while (IsBlending)
                yield return null;

            yield return new WaitForSeconds(shot.holdTime);

            _holdRoutine = null;

            LSO_CameraShot next = !string.IsNullOrEmpty(shot.nextId) ? Find(shot.nextId) : _previous;

            if (next != null)
                Play(next, instant: false);
        }

        private void StopHold()
        {
            if (_holdRoutine == null) return;

            StopCoroutine(_holdRoutine);
            _holdRoutine = null;
        }

        /// <summary>
        /// 꺼지면 코루틴은 유니티가 알아서 죽이지만 _holdRoutine 은 그대로 남는다.
        /// 죽은 핸들이 남아 있으면 다음에 켜졌을 때 "아직 머무는 중"으로 읽혀서,
        /// 그 샷이 시간이 다 돼도 다음으로 넘어가지 않는다.
        ///
        /// 카메라 우선순위는 건드리지 않는다. 지금 잡고 있는 그림까지 되돌리면
        /// 잠깐 껐다 켜는 것만으로 화면이 튄다.
        /// </summary>
        private void OnDisable()
        {
            StopHold();
        }

        private LSO_CameraShot Find(string id)
        {
            if (string.IsNullOrEmpty(id)) return null;

            return _byKey.TryGetValue(id, out LSO_CameraShot shot) ? shot : null;
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            var seenPriority = new Dictionary<int, string>();

            for (int i = 0; i < shots.Count; i++)
            {
                LSO_CameraShot shot = shots[i];
                if (shot == null) continue;

                // 카메라를 안 꽂으면 그 샷은 조용히 무시된다.
                if (shot.camera == null)
                {
                    Debug.LogWarning($"{name}: Shots {i}번에 카메라가 없습니다.", this);
                    continue;
                }

                // 켠 값이 겹치면 둘이 동시에 켜졌을 때 어느 쪽이 잡힐지 시네머신이 정한다.
                // 그 결과가 실행할 때마다 달라질 수 있어서 원인을 찾기가 어렵다.
                if (seenPriority.TryGetValue(shot.priority, out string other))
                {
                    Debug.LogWarning(
                        $"{name}: '{shot.Key}'와 '{other}'의 Priority가 {shot.priority}로 같습니다. " +
                        "샷마다 다른 값을 주세요.", this);
                    continue;
                }

                seenPriority.Add(shot.priority, shot.Key);

                if (shot.priority <= idlePriority)
                {
                    Debug.LogWarning(
                        $"{name}: '{shot.Key}'의 Priority({shot.priority})가 " +
                        $"Idle Priority({idlePriority}) 이하라 켜도 화면을 잡지 못합니다.", this);
                }
            }
        }
#endif
    }
}
