using System;
using _Scripts.LSO.UI.Effect;
using _Scripts.LSO.Will;
using UnityEngine;
using UnityEngine.InputSystem;

namespace _Scripts.LDY
{
    // 씬 배선: BoardManager/MoveSystem/AttackSystem/TileHighlighter를 연결하고,
    // boardLayerMask에는 타일(바닥) 콜라이더가 속한 레이어를 지정할 것.
    // targetCamera를 비워두면 Camera.main을 사용한다.
    // 프로젝트의 Active Input Handling이 New Input System 전용이므로 UnityEngine.InputSystem을 사용한다.
    // 조작: 좌클릭 = 내 기물 선택/공격, 우클릭 = 갈 수 있는 칸이면 이동 · 그 밖이면 선택 해제.
    //       이동하거나 공격하면 선택이 풀린다. 이어서 또 시키려면 다시 고른다.
    //       카드로 소환한 기물은 놓자마자 골라진다(HandlePlaced).
    public class LDY_SelectionController : MonoBehaviour
    {
        [SerializeField] private LDY_BoardManager board;
        [SerializeField] private LDY_MoveSystem moveSystem;
        [SerializeField] private LDY_AttackSystem attackSystem;
        [SerializeField] private LDY_TileHighlighter highlighter;
        [SerializeField] private LayerMask boardLayerMask;
        [SerializeField] private Camera targetCamera;
        [SerializeField] private LDY_TurnManager turnManager;
        [SerializeField] private LDY_CardPlacer cardPlacer;

        /// <summary>지금 선택된 기물. 선택된 것이 없으면 null.</summary>
        public LDY_Animal Selected { get; private set; }

        /// <summary>
        /// 밖에 "골랐다"고 알려둔 상태인지.
        ///
        /// Selected 로 갈음할 수 없다. 기물이 파괴되면 유니티는 그 참조를 null 로 읽어,
        /// 골라뒀던 사실까지 함께 사라진다. 그러면 해제를 아무에게도 못 알리고
        /// 정보창은 죽은 기물을 계속 띄운 채로 남는다.
        /// </summary>
        private bool _hasSelection;

        /// <summary>
        /// 내 기물 선택이 바뀔 때마다 발생한다. 선택되면 그 기물이, 해제되면 null이 넘어온다.
        /// 아군 정보창이 구독하면 된다(적 정보창은 OnEnemyInspectedChanged를 쓴다).
        /// </summary>
        public event Action<LDY_Animal> OnSelectionChanged;

        /// <summary>정보만 들여다보고 있는 적 기물. 없으면 null. 조작 대상인 Selected와는 별개다.</summary>
        public LDY_Animal InspectedEnemy { get; private set; }

        /// <summary>
        /// 적 기물을 클릭해 정보를 볼 때 발생한다. 볼 대상이 없어지면 null이 넘어온다.
        /// 적 전용 정보창이 구독하면 된다(내 기물 정보창은 OnSelectionChanged를 쓴다).
        /// </summary>
        public event Action<LDY_Animal> OnEnemyInspectedChanged;

        /// <summary>
        /// 좌클릭한 칸의 기물을 그대로 전달한다. 같은 기물을 다시 눌러도 매번 발생한다.
        /// </summary>
        public event Action<LDY_Animal> OnAnimalClicked;

        private void Awake()
        {
            if (targetCamera == null)
                targetCamera = Camera.main;

            if (cardPlacer == null) cardPlacer = FindAnyObjectByType<LDY_CardPlacer>();

            if (cardPlacer == null)
                Debug.LogWarning(
                    $"{name}: LDY_CardPlacer 를 못 찾았습니다. 소환한 기물이 자동으로 " +
                    "골라지지 않고, 칸을 고르는 중에도 보드 클릭이 막히지 않습니다.", this);

            // 호버 연출은 여기서 붙이지 않는다.
            // 기물 프리팹의 LSO_ButtonHoverHandler + LSO_HoverMoveEffect가 맡는다.
        }

        private void OnEnable()
        {
            _Scripts.LSO.Will.LSO_WillSelection.BoardInteractionLockChanged += HandleBoardInteractionLockChanged;
            if (turnManager != null) turnManager.OnTurnChanged += HandleTurnChanged;
            if (cardPlacer != null) cardPlacer.Placed += HandlePlaced;
        }

        private void OnDisable()
        {
            _Scripts.LSO.Will.LSO_WillSelection.BoardInteractionLockChanged -= HandleBoardInteractionLockChanged;
            if (turnManager != null) turnManager.OnTurnChanged -= HandleTurnChanged;
            if (cardPlacer != null) cardPlacer.Placed -= HandlePlaced;
            Deselect();
        }

        /// <summary>
        /// 방금 소환한 기물을 바로 고른다.
        ///
        /// ── 왜 자동으로 고르나 ───────────────────────────────────
        /// 놓자마자 움직이거나 때리고 싶은 것이 보통이다. 그런데 놓은 직후에는
        /// 아무것도 안 골라진 상태라, 방금 놓은 그 기물을 한 번 더 눌러야 했다.
        ///
        /// 놓는 것과 고르는 것은 같은 손짓의 앞뒤라 이어주는 편이 자연스럽다.
        /// ─────────────────────────────────────────────────────────
        ///
        /// 적 기물은 안 고른다 — 스테이지 배치도 이 길을 지날 수 있다.
        /// </summary>
        private void HandlePlaced(LDY_Animal animal)
        {
            if (!IsSelectable(animal)) return;

            Select(animal);

            // ── 놓은 클릭이 여기까지 흘러오는 것을 막는다 ─────────────
            // 칸을 눌러 기물을 놓는 것은 LDY_CardPlacer 의 Update 다. 그 클릭은
            // 같은 프레임 동안 wasPressedThisFrame 으로 남아 있어서, 이 컴포넌트의
            // Update 가 뒤에 돌면 **같은 클릭을 한 번 더 읽는다.**
            //
            // 그러면 방금 놓인 기물의 칸을 누른 것으로 읽혀 "이미 고른 것을 다시
            // 눌렀다" → 선택 해제가 된다. 자동 선택이 한 프레임 만에 풀렸다.
            //
            // 실행 순서는 보장되지 않으므로 순서를 맞추는 대신 그 프레임을 건너뛴다.
            // ─────────────────────────────────────────────────────────
            _ignoreClicksOnFrame = Time.frameCount;
        }

        /// <summary>이 프레임의 클릭은 이미 다른 곳이 썼다. 여기서는 안 읽는다.</summary>
        private int _ignoreClicksOnFrame = -1;

        private void HandleTurnChanged(LDY_Team team)
        {
            if (team != LDY_Team.Player) Deselect();
        }

        private void HandleBoardInteractionLockChanged(bool locked)
        {
            if (!locked) return;

            Deselect();
            InspectEnemy(null);
        }

        private void Update()
        {
            // 클릭을 보기 전에 한다. 입력이 없어도 기물은 죽고 연출은 끝난다.
            // 아래 조기 반환들에 걸려 영영 안 돌면, 죽은 기물이 골라진 채로 남거나
            // 칸 표시가 사라진 채로 남는다.
            DropSelectionIfGone();

            if (Mouse.current == null) return;
            if (Time.frameCount == _ignoreClicksOnFrame) return;

            // 계승 대기 중에는 좌클릭이 계승 대상 선택으로만 쓰이고, 그 외 조작은 전부 막힌다.
            // 계승은 주로 적 턴에(내 기물이 맞아 죽을 때) 발동하므로 턴 가드보다 앞에 둬야 클릭을 받을 수 있다.
            // 대기 중이 아닐 때는 곧바로 아래 기존 흐름으로 떨어지므로, 적 턴 클릭이 새어나가지 않는다.
            if (DLJ_SuccessionSystem.IsWaitingForSuccessionTarget)
            {
                if (Mouse.current.leftButton.wasPressedThisFrame)
                    HandleSuccessionClick();
                return;
            }

            if (turnManager != null && turnManager.CurrentTurn != LDY_Team.Player) return;

            // 유언 창이 답을 기다리는 동안엔 창 뒤의 보드를 만질 수 없게 한다.
            // 턴 가드보다 뒤에 둬도 되는 것은, 턴이 넘어가면 LDY_CardPlacer가 창을 닫기 때문이다.
            if (LSO_WillSelection.IsSelecting) return;

            if (cardPlacer != null && cardPlacer.IsPlacing) return; // 카드 배치 위치 선택 중엔 이동/공격 클릭을 막는다.
            if (_Scripts.LSO.Will.LSO_WillSelection.IsBoardInteractionLocked) return;

            bool leftClicked = Mouse.current.leftButton.wasPressedThisFrame;
            bool rightClicked = Mouse.current.rightButton.wasPressedThisFrame;
            if (!leftClicked && !rightClicked) return;

            // 우클릭은 판 밖에서도 받는다. 아래 레이캐스트에 막히면 판을 벗어나 누른 것이
            // 아무 일도 안 하게 되는데, 선택을 푸는 데는 그곳이 가장 자연스러운 자리다.
            if (rightClicked)
            {
                HandleRightClick();
                return;
            }

            if (!TryRaycastToGrid(out var gridPos)) return;

            LDY_Animal clickedAnimal = board.Get(gridPos);
            OnAnimalClicked?.Invoke(clickedAnimal);
            HandleSelectOrAttackClick(clickedAnimal);
        }

        // 유효한 대상인지는 DLJ_SuccessionSystem이 판단한다. 여기서는 어느 칸을 클릭했는지만 넘긴다.
        // 거부되면 대기 상태가 그대로 유지되므로 다시 클릭하면 된다.
        private void HandleSuccessionClick()
        {
            if (!TryRaycastToGrid(out var gridPos)) return;

            DLJ_SuccessionSystem.TrySelectSuccessionTarget(board.Get(gridPos));
        }

        /// <summary>
        /// 우클릭 하나가 두 가지를 한다.
        ///
        ///     갈 수 있는 칸을 눌렀다   → 그리로 간다
        ///     그 밖의 아무 데나 눌렀다 → 선택을 푼다
        ///
        /// 판 밖도 "그 밖"이다. 무르는 동작을 따로 외우지 않아도 되도록,
        /// 이동이 아닌 우클릭은 전부 무르기로 읽는다.
        ///
        /// 연출 중에는 둘 다 하지 않는다. 논리 좌표가 이미 목적지로 바뀌어 있어서
        /// 지금 자리를 기준으로 판단하면 엉뚱한 칸을 고르게 된다.
        /// </summary>
        private void HandleRightClick()
        {
            if (!_Scripts.LSO.Tutorial.LSO_TutorialLock.Allows(_Scripts.LSO.Tutorial.LSO_TutorialAction.Move)) return;
            if (Selected == null) return;
            if (IsActing(Selected)) return;

            if (TryRaycastToGrid(out Vector3Int gridPos) &&
                moveSystem.GetMovableTiles(Selected).Contains(gridPos))
            {
                LDY_Animal movingAnimal = Selected;

                // 한 번 움직이면 선택이 풀린다. 이어서 또 시키려면 다시 고른다.
                Deselect();

                moveSystem.MoveTo(movingAnimal, gridPos);
                return;
            }

            Deselect();
        }

        private void HandleSelectOrAttackClick(LDY_Animal occupant)
        {
            // 튜토리얼 단계가 바뀌기 직전에 선택된 기물이 남아 있는 경우에도
            // 제한 밖 기물을 공격 주체로 쓰지 못하게 실행 직전에 다시 확인한다.
            if (Selected != null && !IsSelectable(Selected)) Deselect();

            // 논리 좌표는 출발 즉시 바뀌므로 연출 중인 기물의 재조작은 막는다.
            if (IsActing(occupant) || IsActing(Selected)) return;

            if (occupant == null)
            {
                Deselect();
                InspectEnemy(null);
                return;
            }

            if (Selected == null)
            {
                if (IsSelectable(occupant))
                    Select(occupant);
                else
                    InspectEnemy(occupant); // 적이면 정보만 보여주고, 빈 칸이면 보던 정보를 닫는다.
                return;
            }

            if (occupant == Selected)
            {
                Deselect();
                return;
            }

            // 사거리 안의 적을 클릭한 것은 "공격"이 우선이다. 정보를 보려면 선택을 푼 뒤 클릭하면 된다.
            if (occupant != null && attackSystem.GetAttackTargets(Selected).Contains(occupant))
            {
                if (!_Scripts.LSO.Tutorial.LSO_TutorialLock.Allows(_Scripts.LSO.Tutorial.LSO_TutorialAction.Attack)) return;
                LDY_Animal attacker = Selected;

                // 공격은 뜬 상태에서 그대로 재생돼야 한다("공격할때는 떠있는 상태에서 포물선으로").
                // 선택은 지금 풀되 호버는 내리지 않는다. 내리는 것은 연출이 끝난 뒤 콜백에서 한다.
                Deselect(lowerHover: false);

                attackSystem.Attack(attacker, occupant, () => SetSelectedHover(attacker, false));
                return;
            }

            if (IsSelectable(occupant))
                Select(occupant);
            else
                InspectEnemy(occupant);
        }

        // 적 기물(또는 빈 칸=null)을 정보 조회 대상으로 삼는다. 실제로 바뀐 경우에만 알린다.
        private void InspectEnemy(LDY_Animal animal)
        {
            LDY_Animal next = (animal != null && animal.team == LDY_Team.Enemy) ? animal : null;
            if (InspectedEnemy == next) return;

            InspectedEnemy = next;
            OnEnemyInspectedChanged?.Invoke(next);
        }

        // 내 팀(Player) 기물만 선택 가능. 이게 없으면 좌클릭으로 상대 기물을 직접 조작하게 되는 버그가 생긴다.
        // 행동력이 남아있는 한 같은 기물도 여러 번 선택해서 행동할 수 있다.
        private bool IsSelectable(LDY_Animal animal)
        {
            return animal != null &&
                   animal.team == LDY_Team.Player &&
                   _Scripts.LSO.Tutorial.LSO_TutorialLock.AllowsSelecting(animal);
        }

        private void Select(LDY_Animal animal)
        {
            InspectEnemy(null); // 내 기물을 고르면 적 정보창은 닫는다(둘이 동시에 떠 있지 않게).

            SetSelectedHover(Selected, false);
            Selected = animal;
            SetSelectedHover(Selected, true);

            _hasSelection = true;

            ShowHighlightsFor(animal);

            OnSelectionChanged?.Invoke(animal);
        }

        /// <summary>
        /// 이 기물이 갈 수 있는 칸과 때릴 수 있는 칸을 그린다.
        ///
        /// 고를 때와 행동이 끝났을 때 둘 다 여기를 지난다. 두 곳에서 따로 그리면
        /// 한쪽만 고쳤을 때 "고르면 보이는데 움직이고 나면 안 보이는" 식으로 갈린다.
        /// </summary>
        private void ShowHighlightsFor(LDY_Animal animal)
        {
            if (highlighter == null || animal == null) return;

            highlighter.ClearHighlights(this);
            highlighter.ShowMoveHighlights(this, moveSystem.GetMovableTiles(animal));
            highlighter.ShowAttackHighlights(this, attackSystem.GetAttackableTiles(animal));
        }

        /// <summary>
        /// 선택을 푼다.
        ///
        /// lowerHover 를 false 로 주면 선택은 풀되 호버는 뜬 채로 남겨둔다.
        /// 공격처럼 뜬 상태에서 이어서 연출해야 할 때 쓴다 — 그 경우 부른 쪽이
        /// 연출이 끝난 뒤 SetSelectedHover(animal, false) 로 직접 내려놓아야 한다.
        /// </summary>
        private void Deselect(bool lowerHover = true)
        {
            // 파괴된 기물은 Selected 가 null 로 읽히므로 그것으로는 셀 수 없다.
            bool hadSelection = _hasSelection;

            _hasSelection = false;

            if (lowerHover) SetSelectedHover(Selected, false);

            Selected = null;

            if (highlighter != null) highlighter.ClearHighlights(this);

            // 선택이 없던 상태에서 또 불려도 UI가 헛돌지 않게 실제로 바뀐 경우에만 알린다.
            if (hadSelection) OnSelectionChanged?.Invoke(null);
        }

        /// <summary>튜토리얼처럼 외부 규칙이 현재 선택을 무효화할 때 선택을 해제한다.</summary>
        public void ClearSelection()
        {
            Deselect();
        }

        /// <summary>
        /// 고른 기물이 판에서 사라졌으면 선택을 푼다.
        ///
        /// ── 행동과 묶지 않는 이유 ─────────────────────────────────
        /// 처음에는 행동이 끝났는지 볼 때 같이 봤다. 그런데 기물이 죽는 시점은
        /// 그 기물의 행동과 아무 상관이 없다 — **유언은 남이 죽을 때 터지고**,
        /// 내 기물이 가만히 서 있는 동안에도 그 불똥에 맞는다.
        ///
        /// 그래서 행동 여부와 관계없이 매 프레임 본다.
        /// ─────────────────────────────────────────────────────────
        /// </summary>
        private void DropSelectionIfGone()
        {
            if (!_hasSelection) return;
            if (IsAlive(Selected)) return;

            Deselect();
        }

        /// <summary>
        /// 아직 판에 남아 있는지.
        ///
        /// 파괴만 보면 놓친다. 죽어도 디졸브가 끝날 때까지 오브젝트가 남아 있어서,
        /// 그 사이에는 살아 있는 것처럼 읽힌다. 체력 쪽도 같이 본다.
        /// </summary>
        private static bool IsAlive(LDY_Animal animal)
        {
            if (animal == null) return false;

            return animal.health == null || !animal.health.IsDestroyed;
        }

        private bool IsActing(LDY_Animal animal)
        {
            return (moveSystem != null && moveSystem.IsMoving(animal))
                   || (attackSystem != null && attackSystem.IsAttacking(animal));
        }

        private static void SetSelectedHover(LDY_Animal animal, bool selected)
        {
            if (animal == null) return;
            foreach (var effect in animal.GetComponentsInChildren<LSO_HoverMoveEffect>(true))
                effect.SetSelected(selected);
        }

        private bool TryRaycastToGrid(out Vector3Int gridPos)
        {
            gridPos = default;
            if (targetCamera == null) return false;

            var ray = targetCamera.ScreenPointToRay(Mouse.current.position.ReadValue());
            if (!Physics.Raycast(ray, out var hit, 100f, boardLayerMask)) return false;

            gridPos = board.WorldToGrid(hit.point);
            return board.IsInside(gridPos);
        }
    }
}
