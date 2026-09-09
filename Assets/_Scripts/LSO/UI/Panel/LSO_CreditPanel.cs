using DG.Tweening;
using TMPro;
using UnityEngine;
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

      [Tooltip("끄면 아래에서 위로 올라간다(영화 엔딩 방식).")]
      [SerializeField] private bool scrollDown = true;

      [Tooltip("시작 전 잠깐 멈춰 있는 시간.")]
      [SerializeField, Min(0f)] private float startDelay = 0.5f;

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

         // 방금 만든 텍스트들은 이번 프레임 끝에야 배치된다.
         // 그 전에 높이를 읽으면 0이라 아무것도 안 움직인다.
         LayoutRebuilder.ForceRebuildLayoutImmediate(_layoutRect);

         // 목록 전체가 화면 밖에서 들어와 화면 밖으로 빠져나가는 거리.
         float distance = _layoutRect.rect.height + ViewportHeight;
         if (distance <= 0f) return;

         // uGUI는 +y가 위쪽이라 아래로 내리려면 빼야 한다.
         float targetY = scrollDown ? _startY - distance : _startY + distance;

         _layoutRect.anchoredPosition = new Vector2(_layoutRect.anchoredPosition.x, _startY);

         _tween = _layoutRect
            .DOAnchorPosY(targetY, distance / scrollSpeed)
            .SetEase(Ease.Linear) // 크레딧은 일정한 속도라야 읽힌다
            .SetDelay(startDelay)
            .SetUpdate(ignoreTimeScale)
            .SetLink(gameObject).OnComplete(Stop);

         if (loop)
            _tween.SetLoops(-1, LoopType.Restart);
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

         if (menuActions != null)
            menuActions.CloseCredits();
      }

      private void KillTween()
      {
         if (_tween == null) return;

         _tween.Kill();
         _tween = null;
      }

      /// <summary>잘라낼 영역의 높이. 목록이 화면 밖에서 들어와 화면 밖으로 나가게 하는 데 쓴다.</summary>
      private float ViewportHeight
      {
         get
         {
            if (viewport != null) return viewport.rect.height;

            return _layoutRect.parent is RectTransform parentRect ? parentRect.rect.height : 0f;
         }
      }
   }
}
