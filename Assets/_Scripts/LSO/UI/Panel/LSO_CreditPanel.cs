using System.Collections;
using _Scripts.LSO.UI.Transition;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace _Scripts.LSO.UI.Panel
{
   /// <summary>
   /// 크레딧 목록을 만들고, 만들어진 목록을 흘려보낸다.
   ///
   /// 씬 구성:
   ///   CreditPanel
   ///   └── Viewport   RectTransform + Mask(또는 RectMask2D)  ← 이 영역 밖은 잘린다
   ///       └── Layout VerticalLayoutGroup + ContentSizeFitter(Vertical: Preferred Size)
   ///
   /// Layout을 씬에서 놓아둔 위치가 연출의 시작 지점이 된다.
   /// </summary>
   public class LSO_CreditPanel : MonoBehaviour
   {
      [Header("메뉴 매니저")]
      [SerializeField] private LSO_MenuActions menuActions;
      
      [Header("크레딧 텍스트 프리팹")]
      [SerializeField] private GameObject textPrefab;

      [Header("텍스트가 들어갈 레이아웃")]
      [SerializeField] private GameObject layout;

      [Header("텍스트 목록")]
      [SerializeField] private string[] textList;

      [Header("스크롤 연출")]
      [Tooltip("잘라낼 영역. 비우면 Layout의 부모를 쓴다.")]
      [SerializeField] private RectTransform viewport;

      [Tooltip("초당 몇 픽셀 움직일지.\n" +
               "시간이 아니라 속도로 두면 크레딧이 길어져도 흐르는 빠르기가 그대로다.")]
      [SerializeField, Min(1f)] private float scrollSpeed = 60f;

      [Tooltip("마우스 좌클릭을 누르고 있는 동안 적용할 크레딧 재생 속도 배율.")]
      [SerializeField, Min(1f)] private float fastForwardMultiplier = 4f;

      [Tooltip("한 줄이 차지하는 높이. 줄 간격까지 포함한 값이다.\n" +
               "목록이 한 줄 늘어날 때마다 이만큼 더 흐른다.\n" +
               "\n" +
               "0으로 두면 만들어진 첫 줄을 재서 쓴다.\n" +
               "줄바꿈되는 긴 줄이 섞여 있으면 자동값이 모자라므로 직접 넣을 것.")]
      [SerializeField, Min(0f)] private float lineHeight;

      [Tooltip("끄면 아래에서 위로 올라간다(영화 엔딩 방식).")]
      [SerializeField] private bool scrollDown = true;

      [Tooltip("시작 전 잠깐 멈춰 있는 시간.")]
      [SerializeField, Min(0f)] private float startDelay = 0.5f;

      [Tooltip("마지막 줄이 영역을 벗어난 뒤 창을 닫기까지의 시간.\n" +
               "곧바로 닫아 메뉴로 튕기지 않게 한다.\n" +
               "\n" +
               "이 동안에도 텍스트는 같은 속도로 계속 흐른다.\n" +
               "세워두면 아직 화면에 남은 줄이 그 자리에 굳어 보인다.")]
      [SerializeField, Min(0f)] private float endDelay = 1f;

      [SerializeField] private bool loop = true;

      [Tooltip("창이 켜질 때 자동으로 재생한다.")]
      [SerializeField] private bool playOnEnable = true;

      [Tooltip("timescale 영향 여부.")]
      [SerializeField] private bool ignoreTimeScale = true;

      private RectTransform _layoutRect;
      private float _startY;
      private Tween _tween;

      public bool IsPlaying => _tween != null && _tween.IsActive() && _tween.IsPlaying();

      private void Awake()
      {
         if (layout == null)
         {
            Debug.LogError("Layout이 비어 있습니다 : LSO_CreditPanel", this);
            return;
         }

         if (menuActions == null)
         {
            Debug.LogError("매니저가 없습니다! :  LSO_CreditPanel", this);
            return;
         }

         _layoutRect = (RectTransform)layout.transform;
         
         _startY = _layoutRect.anchoredPosition.y;

         LoadText();
      }

      private void OnEnable()
      {
         if (playOnEnable)
            Play();
      }

      private void Update()
      {
         if (_tween == null || !_tween.IsActive()) return;

         bool fastForward = Application.isFocused &&
                            Mouse.current != null && Mouse.current.leftButton.isPressed;
         _tween.timeScale = fastForward ? fastForwardMultiplier : 1f;
      }

      private void OnDisable()
      {
         // 닫지 않는다. 이미 꺼지는 중인데 또 닫으라고 하면 되돌아 들어간다.
         KillTween();
      }

      private void LoadText()
      {
         foreach (string text in textList)
         {
            GameObject textObj = Instantiate(textPrefab, layout.transform); 
            textObj.GetComponentInChildren<TextMeshProUGUI>().text = text;
         }

         Debug.Log("크레딧 로딩 완료!");
      }

      /// <summary>처음부터 다시 흘려보낸다.</summary>
      private void Play()
      {
         if (_layoutRect == null) return;

         // 여기서 Stop을 부르면 안 된다. Stop은 창을 닫는 것까지 하므로
         // 다시 틀 때마다 시작하자마자 메인으로 돌아가버린다.
         KillTween();

         RectTransform view = Viewport;
         RectTransform space = _layoutRect.parent as RectTransform;

         if (view == null || space == null)
         {
            Debug.LogWarning(
               $"{name}: 잘라낼 영역을 찾지 못해 크레딧을 흘려보낼 수 없습니다. " +
               "Viewport 를 넣거나 Layout 을 RectTransform 아래에 두세요 : LSO_CreditPanel", this);
            return;
         }

         // 재기 전에 시작 자리로 되돌린다.
         // 지난 재생이 끝난 자리에서 재면 이미 다 지나간 뒤라 거리가 0으로 나온다.
         _layoutRect.anchoredPosition = new Vector2(_layoutRect.anchoredPosition.x, _startY);

         // 방금 만든 텍스트들은 이번 프레임 끝에야 배치된다.
         // 그 전에 높이를 읽으면 0이라 아무것도 안 움직인다.
         LayoutRebuilder.ForceRebuildLayoutImmediate(_layoutRect);

         // 한 줄 높이를 모르면 목록이 아무리 길어도 거리가 안 늘어난다.
         // 그러면 출발 자리에서 영역까지 오는 만큼만 흐르고 끝나버린다.
         if (ContentHeight <= 0f)
         {
            Debug.LogWarning(
               $"{name}: 한 줄 높이를 알 수 없어 목록 길이가 연출에 반영되지 않습니다. " +
               "Line Height 에 값을 넣으세요 : LSO_CreditPanel", this);
            return;
         }

         float travel = TravelDistance(view, space);

         if (travel <= 0f)
         {
            // 놓아둔 자리가 이미 뷰포트를 지나쳐 있다는 뜻이다.
            // 조용히 넘어가면 "크레딧을 켰는데 아무 일도 없다"가 된다.
            Debug.LogWarning(
               $"{name}: Layout 이 이미 잘라낼 영역을 지나쳐 있어 흘려보낼 것이 없습니다. " +
               "씬에서 Layout 의 위치를 확인하세요 : LSO_CreditPanel", this);
            return;
         }

         // 끝 기다림을 멈춤이 아니라 "계속 흐르는 시간"으로 둔다.
         // 세워두면 아직 화면에 남은 줄이 그 자리에 굳어 보이고,
         // 거리를 덜 잡았을 때 그 어긋남이 그대로 드러난다.
         //
         // 속도가 같으므로 화면에서는 끊기는 데 없이 그대로 이어서 흐른다.
         float distance = travel + endDelay * scrollSpeed;

         // uGUI는 +y가 위쪽이라 아래로 내리려면 빼야 한다.
         float targetY = scrollDown ? _startY - distance : _startY + distance;

         Tween scroll = _layoutRect
            .DOAnchorPosY(targetY, distance / scrollSpeed)
            .SetEase(Ease.Linear); // 크레딧은 일정한 속도라야 읽힌다

         // 시작 기다림은 트윈의 delay 가 아니라 시퀀스의 구간으로 둔다.
         // delay 로 두면 DOTween 이 반복할 때 첫 회차만 기다리고 그 뒤로는
         // 건너뛰어서, 반복할 때마다 앞이 달라진다.
         //
         // 끝 기다림은 여기 없다. 세워두는 시간이 아니라 계속 흐르는 시간이라
         // 위에서 거리에 이미 녹여뒀다.
         _tween = DOTween.Sequence()
            .AppendInterval(startDelay)
            .Append(scroll)
            .SetUpdate(ignoreTimeScale)
            .SetLink(gameObject)
            .OnComplete(Stop);

         if (loop)
            _tween.SetLoops(-1, LoopType.Restart);
      }

      /// <summary>
      /// 지금 놓인 자리에서 마지막 줄이 잘라낼 영역을 완전히 빠져나갈 때까지의 거리.
      ///
      /// ── 왜 이렇게 재는가 ─────────────────────────────────────
      /// 두 가지가 각각 한 번씩 연출을 중간에 끊었다.
      ///
      /// 1. 예전에는 "목록 높이 + 영역 높이"를 그대로 썼다. 그 값은 내용이
      ///    <b>영역 바깥에 딱 붙어 있다가</b> 들어와서 반대쪽으로 나가는 거리다.
      ///    그런데 출발점은 씬에서 Layout 을 놓아둔 자리라, 영역까지 오는 데 쓴
      ///    만큼이 뒤에서 모자랐다.
      ///
      /// 2. 목록 높이를 rect 에서 읽는 것도 믿을 수 없었다. 그 값은
      ///    ContentSizeFitter 가 채워주는데, 그게 없거나 Preferred Size 가
      ///    아니면 <b>내용과 상관없는 높이가 그대로 나온다.</b>
      ///
      /// 그래서 지금은 둘 다 가정하지 않는다.
      /// 출발 자리는 실제 위치에서 재고, 내용 높이는 <b>줄 수에서 바로 계산한다.</b>
      /// 목록이 한 줄 늘면 거리도 그만큼 는다 — 레이아웃이 어떻게 설정돼 있든 같다.
      /// ─────────────────────────────────────────────────────────
      /// </summary>
      /// <param name="view">잘라낼 영역.</param>
      /// <param name="space">거리를 잴 기준. anchoredPosition 이 움직이는 공간이라 Layout 의 부모다.</param>
      private float TravelDistance(RectTransform view, RectTransform space)
      {
         GetLocalYRange(view, space, out float viewBottom, out float viewTop);

         // 목록이 차지하는 자리를 피벗 위치에서 직접 펼친다.
         // rect 를 읽지 않으므로 ContentSizeFitter 설정에 휘둘리지 않는다.
         float height = ContentHeight;
         float pivotY = space.InverseTransformPoint(_layoutRect.position).y;

         float contentBottom = pivotY - _layoutRect.pivot.y * height;
         float contentTop = contentBottom + height;

         // 내려가는 연출이면 내용의 위쪽 끝이 영역의 아래쪽 끝을 지나야 끝난다.
         // 올라가는 연출이면 반대로 내용의 아래쪽 끝이 영역의 위쪽 끝을 지나야 한다.
         return scrollDown
            ? contentTop - viewBottom
            : viewTop - contentBottom;
      }

      /// <summary>
      /// 목록 전체가 차지하는 높이. 줄 수에 비례한다.
      ///
      /// 화면에 실제로 그려진 높이를 재지 않는 것이 요점이다.
      /// 그쪽은 ContentSizeFitter 가 제대로 걸려 있어야만 맞는데,
      /// 안 걸려 있으면 조용히 작은 값이 나와서 연출이 중간에 끝난다.
      /// </summary>
      private float ContentHeight => textList.Length * LineHeight;

      /// <summary>
      /// 한 줄이 차지하는 높이. 인스펙터 값이 0이면 만들어진 첫 줄을 재서 쓴다.
      ///
      /// 자동으로 잰 값은 <b>줄바꿈되는 긴 줄을 모른다.</b> 그런 줄이 섞여 있으면
      /// 그만큼 모자라므로 인스펙터에 직접 넣어야 한다.
      /// </summary>
      private float LineHeight
      {
         get
         {
            if (lineHeight > 0f) return lineHeight;

            if (_layoutRect.childCount == 0) return 0f;

            float measured = _layoutRect.GetChild(0) is RectTransform first
               ? first.rect.height
               : 0f;

            // 줄 사이 간격도 한 줄이 차지하는 자리에 포함된다.
            if (_layoutRect.TryGetComponent(out VerticalLayoutGroup group))
               measured += group.spacing;

            return measured;
         }
      }

      /// <summary>
      /// 어떤 RectTransform 이 기준 공간에서 세로로 어디부터 어디까지 차지하는지.
      ///
      /// 네 모서리를 모두 옮겨서 재는 이유는 피벗·앵커가 무엇이든 맞히기 위해서다.
      /// rect.height 와 anchoredPosition 으로 직접 계산하면 피벗이 가운데가 아닐 때 어긋난다.
      /// </summary>
      private static void GetLocalYRange(
         RectTransform target, RectTransform space, out float min, out float max)
      {
         var corners = new Vector3[4];
         target.GetWorldCorners(corners);

         min = float.MaxValue;
         max = float.MinValue;

         for (int i = 0; i < corners.Length; i++)
         {
            float y = space.InverseTransformPoint(corners[i]).y;

            if (y < min) min = y;
            if (y > max) max = y;
         }
      }

      /// <summary>
      /// 다 흘렀으니 창을 닫는다. 끝까지 재생됐을 때만 부른다.
      ///
      /// 정리(KillTween)와 닫기를 나눠둔 이유는, 예전에 이 둘이 한 함수였을 때
      /// "다시 틀기"와 "꺼지는 중 정리"까지 전부 창을 닫아버렸기 때문이다.
      /// 트윈을 멈추는 것과 화면을 넘기는 것은 서로 다른 일이다.
      /// </summary>
      private void Stop()
      {
         if (_tween == null) return;

         KillTween();

         if (menuActions == null) return;

         // 암전 판이 씬에 없으면 그냥 넘어간다. 크레딧이 안 끝나는 것보다는
         // 페이드 없이 넘어가는 쪽이 낫다.
         LSO_IScreenFader fader = LSO_ScreenFader.Current;

         if (fader == null)
         {
            menuActions.CloseCredits();
            return;
         }

         // ── 왜 이 코루틴을 내가 안 돌리는가 ──────────────────────
         // CloseCredits 는 이 패널을 끈다. 코루틴을 여기서 돌리면 그 줄에서
         // 오브젝트가 꺼지면서 코루틴도 같이 죽는다. Cover 는 이미 끝났고
         // Reveal 은 아직인 시점이라 <b>화면이 검은 채로 굳는다.</b>
         //
         // 그래서 꺼지지 않는 쪽에서 돌린다. 암전 판은 DontDestroyOnLoad 라
         // 이 연출이 끝날 때까지 반드시 살아 있다.
         // ─────────────────────────────────────────────────────────
         if (fader is MonoBehaviour host)
         {
            host.StartCoroutine(Co_ToMain(fader));
            return;
         }

         // 암전 판이 MonoBehaviour 가 아니면 코루틴을 맡길 곳이 없다.
         // 조용히 검은 화면에 갇히느니 페이드를 포기한다.
         Debug.LogWarning(
            $"{name}: 암전 판이 MonoBehaviour 가 아니라 페이드 없이 넘어갑니다 : LSO_CreditPanel", this);

         menuActions.CloseCredits();
      }

      /// <summary>
      /// 화면을 덮고, 그 뒤에서 창을 바꾸고, 다시 걷는다.
      ///
      /// 창을 바꾸는 순간이 검은 화면에 가려져 있어야 갈아끼우는 게 안 보인다.
      /// 덮기 전에 바꾸면 크레딧이 사라지는 장면이 그대로 노출된다.
      /// </summary>
      private IEnumerator Co_ToMain(LSO_IScreenFader fader)
      {
         yield return fader.Cover();

         // 덮는 동안 씬이 바뀌어 메뉴가 사라졌을 수 있다.
         // 창을 못 바꾸더라도 화면은 반드시 걷어야 한다 — 안 그러면 검은 채로 남는다.
         if (menuActions != null)
            menuActions.CloseCredits();

         yield return fader.Reveal();
      }

      private void KillTween()
      {
         if (_tween == null) return;

         _tween.Kill();
         _tween = null;
      }

      /// <summary>잘라낼 영역. 비워두면 Layout의 부모를 쓴다.</summary>
      private RectTransform Viewport =>
         viewport != null ? viewport : _layoutRect.parent as RectTransform;
   }
}
