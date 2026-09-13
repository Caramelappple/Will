using TMPro;
using UnityEngine;

/// <summary>장부 곡면을 따르는 글자와 SDF 잉크 번짐. 공유 폰트 머티리얼은 수정하지 않음.</summary>
[ExecuteAlways, DisallowMultipleComponent, RequireComponent(typeof(TextMeshPro))]
public sealed class DLJ_LedgerInkText : MonoBehaviour
{
    [SerializeField] private Transform pageSpace;
    [SerializeField] private bool animateInk;
    [SerializeField, Min(0.05f)] private float spreadDuration = 0.65f;
    private static readonly int ProgressId = Shader.PropertyToID("_InkProgress");
    private TextMeshPro text;
    private Renderer textRenderer;
    private MaterialPropertyBlock properties;
    private float elapsed;
    public string Value => Text.text;
    public float InkProgress => animateInk ? Mathf.Clamp01(elapsed / Mathf.Max(.05f, spreadDuration)) : 1f;
    private TextMeshPro Text => text != null ? text : text = GetComponent<TextMeshPro>();

    private void OnEnable()
    {
        text = Text;
        textRenderer = GetComponent<Renderer>();
        properties ??= new MaterialPropertyBlock();
        text.OnPreRenderText -= BendToPage;
        text.OnPreRenderText += BendToPage;
        elapsed = ShouldAnimate(Text.text) ? 0 : spreadDuration;
        ApplyProgress();
        text.SetVerticesDirty();
    }

    private void OnDisable()
    {
        if (text != null) text.OnPreRenderText -= BendToPage;
    }

    public void SetValue(string value, bool replay = false)
    {
        if (!replay && Text.text == value) return;
        Text.text = value;
        elapsed = ShouldAnimate(value) ? 0 : spreadDuration;
        ApplyProgress();
    }

    private bool ShouldAnimate(string value)
    {
        if (!Application.isPlaying || !animateInk || string.IsNullOrEmpty(value)) return false;
        foreach (char character in value)
            if (character < '0' || character > '9') return false;
        return true;
    }

    private void Update()
    {
        if (!Application.isPlaying || !animateInk || elapsed >= spreadDuration) return;
        elapsed = Mathf.Min(spreadDuration, elapsed + Time.unscaledDeltaTime);
        ApplyProgress();
    }

    private void ApplyProgress()
    {
        if (textRenderer == null) return;
        properties ??= new MaterialPropertyBlock();
        textRenderer.GetPropertyBlock(properties);
        properties.SetFloat(ProgressId, InkProgress);
        textRenderer.SetPropertyBlock(properties);
    }

    private void BendToPage(TMP_TextInfo info)
    {
        if (pageSpace == null) return;
        Matrix4x4 toPage = pageSpace.worldToLocalMatrix * transform.localToWorldMatrix;
        Matrix4x4 toText = toPage.inverse;
        for (int c = 0; c < info.characterCount; c++)
        {
            var character = info.characterInfo[c];
            if (!character.isVisible) continue;
            var vertices = info.meshInfo[character.materialReferenceIndex].vertices;
            for (int v = character.vertexIndex; v < character.vertexIndex + 4; v++)
            {
                Vector3 point = toPage.MultiplyPoint3x4(vertices[v]);
                // Same analytic surface as DLJ_LedgerLeft/RightPage, with a small depth offset.
                point.y = .153f + .095f * Mathf.Sin(Mathf.Abs(point.x) * .5f * Mathf.PI)
                    + .008f * Mathf.Sin((point.z / 2.53f + .5f) * Mathf.PI) + .006f;
                vertices[v] = toText.MultiplyPoint3x4(point);
            }
        }
    }
}
