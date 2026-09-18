using DevLib.SoundSystem.Runtime;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace DevLib.SoundSystem.Editor
{
    [CustomEditor(typeof(SoundClipSO))]
    public class SoundClipSOEditor : UnityEditor.Editor
    {
        [SerializeField] private VisualTreeAsset editorView;

        /// <summary>
        /// editorView 가 비었을 때 찾아볼 자리.
        ///
        /// 이 필드는 스크립트의 .meta(MonoImporter.defaultReferences)에 저장된다.
        /// 라이브러리를 옮겨오거나 .meta 가 새로 만들어지면 그 값이 날아가는데,
        /// 그러면 커스텀 인스펙터가 조용히 기본 인스펙터로 떨어진다.
        /// 경로로 한 번 더 찾아서 그 경우를 메운다.
        /// </summary>
        private const string ViewPath =
            "Assets/DevLib/SoundSystem/Editor/SoundClipEditor View.uxml";

        private const int WaveformHeight = 80;
        private const float MinDuration = 0.1f;
        private const float HandleGrabW = 15f; 
        
        private Texture _waveformTexture;
        private AudioClip _cachedClip;
        private bool _draggingStart;
        private bool _draggingEnd;
        private bool _isPlaying;
        private float _playEndClipTime;

        private Label _startLabel;
        private Label _endLabel;
        private Button _playBtn;
        private VisualElement _controlContainer;
        private IMGUIContainer _waveformContainer;

        /// <summary>
        /// 이거 2개는 에디터 전용이다. 씬에 저장되지 않고 보이지 않는다.
        /// </summary>
        private static GameObject _previewGO;
        private static AudioSource _previewSource;

        private void OnEnable()
        {
            EditorApplication.update += OnEditorUpdate;
        }

        private void OnDisable()
        {
            EditorApplication.update -= OnEditorUpdate;
            StopPreview();
            _isPlaying = false;
            if(_waveformTexture != null)
                DestroyImmediate(_waveformTexture);
        }

        public override VisualElement CreateInspectorGUI()
        {
            SoundClipSO so = target as SoundClipSO;

            if (editorView == null)
                editorView = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(ViewPath);

            if (editorView == null)
            {
                Debug.LogError(
                    "[SoundClipSOEditor] editorView 를 찾지 못해 기본 인스펙터로 돌립니다.\n" +
                    $"스크립트를 눌러 Editor View 칸에 UXML 을 꽂거나, {ViewPath} 에 두세요.");

                return base.CreateInspectorGUI(); //기본 인스펙터로 폴백해라.
            }

            VisualElement root = new VisualElement();
            editorView.CloneTree(root); //중요!!!!!
            root.Bind(serializedObject);

            _startLabel = root.Q<Label>("start-label");
            _endLabel = root.Q<Label>("end-label");
            _playBtn = root.Q<Button>("play-btn");
            _controlContainer = root.Q<VisualElement>("control-container");
            
            //웨이브 폼 그려주는 부분 여기서
            _waveformContainer = new IMGUIContainer(() => OnWaveformGUI(so));
            _waveformContainer.style.height = WaveformHeight;
            root.Q<VisualElement>("waveform-slot").Add(_waveformContainer);
            
            
            //클립을 사용자가 넣었을 때 감지하는 로직도 만들어야겠지
            root.Q<PropertyField>("clip-field")
                .RegisterValueChangeCallback(evt => OnClipFieldChange(so, evt));

            _playBtn.clicked += () => OnPlayBtnClicked(so);
            
            //초기화
            _cachedClip = so.clip;
            bool hasClip = so.clip != null;
            _controlContainer.style.display = hasClip ? DisplayStyle.Flex : DisplayStyle.None;

            if (hasClip)
                UpdateLabels(so);
            
            return root;
        }

        private void OnClipFieldChange(SoundClipSO so, SerializedPropertyChangeEvent evt)
        {
            AudioClip newClip = evt.changedProperty.objectReferenceValue as AudioClip;

            if (newClip == _cachedClip) return;
            
            _cachedClip = newClip;

            if (_waveformTexture != null)
            {
                DestroyImmediate(_waveformTexture);
                _waveformTexture = null;
            }

            if (newClip != null)
            {
                //클립이 교체된거니까 start와 endtime을 초기화해줘야 한다.
                serializedObject.Update();
                serializedObject.FindProperty("startTime").floatValue = 0;
                serializedObject.FindProperty("endTime").floatValue 
                    = Mathf.Clamp(newClip.length - 1f, MinDuration, newClip.length);
                serializedObject.ApplyModifiedPropertiesWithoutUndo();
            }
            
            _controlContainer.style.display = newClip != null 
                ? DisplayStyle.Flex : DisplayStyle.None;
            
            UpdateLabels(so);
            _waveformContainer?.MarkDirtyRepaint(); //웨이브폼이 그려져있는걸 더럽다고 마킹해서 다시 그리게
        }
        
        private void UpdateLabels(SoundClipSO so)
        {
            if (so == null) return;
            if (_startLabel != null) _startLabel.text = $"Start: {so.startTime:F3}s";
            if (_endLabel != null) _endLabel.text = $"End : {so.endTime:F3}s";
        }

        private void OnWaveformGUI(SoundClipSO so)
        {
            if (so == null || so.clip == null) return;

            Rect wRect = GUILayoutUtility.GetRect(GUIContent.none, GUIStyle.none,
                GUILayout.Height(WaveformHeight), GUILayout.ExpandWidth(true));

            if (Event.current.type == EventType.Repaint)
            {
                int w = Mathf.Max(1, (int)wRect.width);

                if (_waveformTexture == null || _waveformTexture.width != w)
                    _waveformTexture = BuildWaveform(so.clip, w, WaveformHeight);
            }
            
            if(_waveformTexture != null)
                DrawWaveformAndHandles(wRect, so);
            
            //드래그가 이루어지고 난뒤에 다시 라벨 갱신
            if(Event.current.type == EventType.Repaint)
                UpdateLabels(so);
        }

        private Texture BuildWaveform(AudioClip clip, int width, int height)
        {
            float[] samples = new float[clip.samples * clip.channels];
            clip.GetData(samples, 0);

            int total = clip.samples;
            int chans = clip.channels;
            
            Color bgColor = new Color(0.13f, 0.13f, 0.13f, 1f);
            Color waveColor = new Color(0.38f, 0.68f, 1f, 1f);
            Color[] pixels = new Color[width * height];
            
            for(int i = 0; i < pixels.Length; i++)
                pixels[i] = bgColor; //배경색으로 칠해버린다.
            
            //텍스처의 각 픽셀을 열단위로 순회한다. ( 한 열이 오디오 샘플 구간 하나에 대응한다.)
            for (int x = 0; x < width; x++)
            {
                int s0 = (int)((float)x / width * total) * chans;
                int s1 = (int)((float)(x + 1) / width * total) * chans;
                
                s1 = Mathf.Min(s1, samples.Length); //배열 범위를 초과하지 않도록 한다.
                //해당 구간 폭이 0이면 최소 샘플 한개는 읽도록 한다.
                if (s0 >= s1) s1 = s0 + 1;

                //해당 구간에서 최소값과 최대값을 탐색하여 기록한다.
                float lo = 0f, hi = 0f;
                for (int s = s0; s < s1; s++)
                {
                    if(samples[s] < lo) lo = samples[s];
                    if(samples[s] > hi) hi = samples[s];
                }
                
                //샘플링된 값을 -1~ +1을 픽셀좌표 (0~height)로 변환해서 그려준다.
                int yLo = Mathf.Clamp((int)((lo * 0.5f + 0.5f) * height), 0, height - 1);
                int yHi = Mathf.Clamp((int)((hi * 0.5f + 0.5f) * height), 0, height - 1);
                
                for(int y = yLo; y <= yHi; y++)
                    pixels[y * width + x] = waveColor;
            }
            
            Texture2D tex = new Texture2D(width, height, TextureFormat.RGBA32, false);
            tex.SetPixels(pixels);
            tex.Apply();
            return tex; 
        }

        private void DrawWaveformAndHandles(Rect wRect, SoundClipSO so)
        {
            float duration = so.clip.length;
            float startX = wRect.x + (so.startTime / duration) * wRect.width;
            float endX = wRect.x + (so.endTime / duration) * wRect.width;
            
            GUI.DrawTexture(wRect, _waveformTexture, ScaleMode.StretchToFill);

            Color outColor = new Color(0, 0, 0, 0.5f);
            EditorGUI.DrawRect(new Rect(wRect.x, wRect.y, startX - wRect.x, wRect.height), outColor);
            EditorGUI.DrawRect(new Rect(endX, wRect.y, wRect.xMax - endX, wRect.height), outColor);
            
            //재생해주는 재생바 헤드
            if (_isPlaying && _previewSource != null && _previewSource.clip == so.clip)
            {
                float headX = wRect.x + (_previewSource.time/ duration) * wRect.width;
                EditorGUI.DrawRect(new Rect(headX - 1, wRect.y, 2, wRect.height), Color.white);
            }
            
            EditorGUI.DrawRect(new Rect(startX - 1, wRect.y, 2, wRect.height), 
                new Color(0.25f, 0.9f, 0.25f));
            EditorGUI.DrawRect(new Rect(endX - 1, wRect.y, 2, wRect.height), 
                new Color(0.95f, 0.35f, 0.2f));
            EditorGUI.DrawRect(new Rect(startX - 5, wRect.y, 10, 12), 
                new Color(0.25f, 0.9f, 0.25f));
            EditorGUI.DrawRect(new Rect(endX - 5, wRect.y, 10, 12), 
                new Color(0.95f, 0.35f, 0.2f));
            
            Rect startGrab = new Rect(startX - HandleGrabW * 0.5f, wRect.y, HandleGrabW, wRect.height);
            Rect endGrab = new Rect(endX - HandleGrabW * 0.5f, wRect.y, HandleGrabW, wRect.height);
            
            EditorGUIUtility.AddCursorRect(startGrab, MouseCursor.ResizeHorizontal);
            EditorGUIUtility.AddCursorRect(endGrab, MouseCursor.ResizeHorizontal);
            
            HandleDrag(wRect, so, startGrab, endGrab, duration);
        }

        private void HandleDrag(Rect rect, SoundClipSO so, Rect startGrab, Rect endGrab, float duration)
        {
            Event e = Event.current; //현재 이벤트 받아서
            switch (e.type)
            {
                //좌버튼 다운
                case EventType.MouseDown when e.button == 0:
                {
                    if (startGrab.Contains(e.mousePosition))
                    {
                        _draggingStart = true;
                        e.Use(); //이벤트를 소모시킨다.
                    }
                    else if (endGrab.Contains(e.mousePosition))
                    {
                        _draggingEnd = true;
                        e.Use();
                    }
                    break;
                }

                case EventType.MouseUp when e.button == 0:
                {
                    _draggingStart = _draggingEnd = false;
                    break;
                }

                case EventType.MouseDrag when _draggingStart || _draggingEnd :
                {
                    float t = Mathf.Clamp01((e.mousePosition.x - rect.x) / rect.width) * duration;
                    serializedObject.Update();

                    if (_draggingStart)
                    {
                        t = Mathf.Clamp(t, 0f, so.endTime - MinDuration);
                        serializedObject.FindProperty("startTime").floatValue = t;
                    }
                    else
                    {
                        t = Mathf.Clamp(t, so.startTime + MinDuration, duration);
                        serializedObject.FindProperty("endTime").floatValue = t;
                    }
                    serializedObject.ApplyModifiedProperties();
                    UpdateLabels(so);
                    e.Use();
                    break;
                }
            }
        }

        private void OnPlayBtnClicked(SoundClipSO so)
        {
            if (_isPlaying)
            {
                StopPreview();
                _isPlaying = false;
                _playBtn.text = "▷Play";
            }
            else
            {
                float pitch = so.pitch;
                if (so.randomizePitch)
                {
                    pitch = Mathf.Clamp(pitch + Random.Range(-so.randomPitchModifier, so.randomPitchModifier)
                        , 0.1f, 3f);
                }
                
                _playEndClipTime = so.endTime;
                PlayPreview(so.clip, so.startTime, pitch);
                _isPlaying = true;
                _playBtn.text = "□ Stop";
            }
        }

        private void PlayPreview(AudioClip clip, float startTime, float pitch)
        {
            AudioSource src = EnsurePreviewSource();
            src.clip = clip;
            src.pitch = pitch;
            src.timeSamples = Mathf.RoundToInt(startTime * clip.frequency);
            src.Play();
        }

        
        private void OnEditorUpdate()
        {
            if (!_isPlaying) return;
            
            bool srcStopped = _previewSource == null || !_previewSource.isPlaying;
            bool reachedEnd = _previewSource != null && _previewSource.time >= _playEndClipTime;

            if (srcStopped || reachedEnd)
            {
                StopPreview();
                _isPlaying = false;
                if (_playBtn != null)
                    _playBtn.text = "▷Play";
            }
            
            _waveformContainer?.MarkDirtyLayout();
        }

        private void StopPreview()
        {
            _previewSource?.Stop();
        }
        
        private static AudioSource EnsurePreviewSource()
        {
            if (_previewSource != null) return _previewSource;
            
            _previewGO = EditorUtility.CreateGameObjectWithHideFlags(
                "~SoundPreview",HideFlags.HideAndDontSave,typeof(AudioSource));
            _previewSource = _previewGO.GetComponent<AudioSource>();
            
            return _previewSource;
        }
    }
}