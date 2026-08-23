using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace _Scripts.LDY.UI
{
    /// <summary>
    /// 캔버스 루트에 붙여서 하위 Image / RawImage에만 픽셀화 머티리얼을 꽂는다.
    ///
    /// 핵심 규칙: TMP_Text는 절대 건드리지 않는다.
    /// 대상을 "Image 또는 RawImage"로 화이트리스트해서, TMP(TextMeshProUGUI / TMP_SubMeshUI)는
    /// 자기 SDF 머티리얼을 그대로 유지 → 글자가 항상 선명하게 남는다.
    /// (TMP 어셈블리를 참조하지 않으므로 asmdef 의존도 늘지 않는다)
    ///
    /// RenderMode / 계층 구조 / 좌표계는 일절 건드리지 않아서 클릭 판정에 영향이 없다.
    /// </summary>
    [DisallowMultipleComponent]
    [AddComponentMenu("LDY/UI/LDY_UIPixelizer")]
    public sealed class LDY_UIPixelizer : MonoBehaviour
    {
        [Header("머티리얼 소스 (둘 중 하나만 있으면 됨)")]
        [Tooltip("비워두면 아래 셰이더로 직접 굽는다. 지정하면 그 머티리얼을 복제해서 쓴다.")]
        [SerializeField] private Material materialTemplate;
        [Tooltip("LDY_UIPixelate.shader")]
        [SerializeField] private Shader pixelShader;

        [Header("픽셀 격자")]
        [Tooltip("블록 한 변의 길이(스크린 픽셀). 1이면 원본과 동일.")]
        [SerializeField, Range(1f, 64f)] private float blockSize = 4f;

        [Header("옵션")]
        [Tooltip("켜면 이미 커스텀 머티리얼을 쓰는 그래픽까지 덮어쓴다. 기본은 존중해서 건너뜀.")]
        [SerializeField] private bool overrideCustomMaterials;

        private const string ShaderName = "LDY/UI/Pixelate";
        private static readonly int BlockId = Shader.PropertyToID("_Block");

        private LDY_UIPixelMaterial pixelMaterial;
        private readonly Dictionary<Graphic, Material> originalMaterials = new Dictionary<Graphic, Material>();
        private readonly HashSet<LDY_UIPixelizerRelay> relays = new HashSet<LDY_UIPixelizerRelay>();
        private bool dirty;

        /// <summary>런타임에 그래픽이 새로 생겼을 수 있다는 표시. 실제 적용은 LateUpdate에서 한 번만.</summary>
        internal void MarkDirty() => dirty = true;

        private void OnEnable()
        {
            pixelMaterial = new LDY_UIPixelMaterial(materialTemplate, ResolveShader());

            if (!pixelMaterial.IsValid)
            {
                Debug.LogError($"[{nameof(LDY_UIPixelizer)}] 머티리얼도 셰이더도 못 찾았다. " +
                               $"인스펙터에 LDY_UIPixelate.shader를 지정할 것.", this);
                enabled = false;
                return;
            }

            PushBlockSize();
            Apply();
        }

        private void OnDisable()
        {
            Restore();

            pixelMaterial?.Dispose();
            pixelMaterial = null;

            // 꺼진 뒤 남은 머티리얼 사본이 픽셀화된 채로 그려지지 않도록 격자를 1(=원본)로 되돌린다.
            Shader.SetGlobalFloat(BlockId, 1f);
        }

        private void LateUpdate()
        {
            if (!dirty) return;
            dirty = false;
            Apply();
        }

        // 캔버스 루트 직계 자식은 이걸로, 그 아래는 Relay가 잡는다.
        private void OnTransformChildrenChanged() => MarkDirty();

        private void OnValidate()
        {
            if (Application.isPlaying && isActiveAndEnabled) PushBlockSize();
        }

        // 머티리얼이 아니라 전역에 꽂는다 — Mask(스텐실) 밑에서 UGUI가 만든 머티리얼 사본까지 같이 따라오게.
        private void PushBlockSize()
        {
            Shader.SetGlobalFloat(BlockId, Mathf.Max(1f, blockSize));
        }

        private Shader ResolveShader()
        {
            return pixelShader != null ? pixelShader : Shader.Find(ShaderName);
        }

        private void Apply()
        {
            Walk(transform);
        }

        private void Walk(Transform current)
        {
            // 루트는 이 컴포넌트 자신의 OnTransformChildrenChanged가 이미 감시한다.
            if (current != transform)
                relays.Add(LDY_UIPixelizerRelay.Attach(current, this));

            if (current.TryGetComponent(out Graphic graphic) && IsPixelizable(graphic))
                Inject(graphic);

            for (int i = 0; i < current.childCount; i++)
                Walk(current.GetChild(i));
        }

        // Image / RawImage만 대상 → TMP는 구조적으로 제외된다.
        private static bool IsPixelizable(Graphic graphic)
        {
            return graphic is Image || graphic is RawImage;
        }

        private void Inject(Graphic graphic)
        {
            Material target = pixelMaterial.Material;
            Material current = graphic.material;

            if (current == target) return;

            // 이미 자기만의 머티리얼을 쓰는 그래픽(이펙트 등)은 기본적으로 존중한다.
            if (!overrideCustomMaterials && current != null && current != graphic.defaultMaterial)
                return;

            if (!originalMaterials.ContainsKey(graphic))
                originalMaterials.Add(graphic, current);

            graphic.material = target;
        }

        private void Restore()
        {
            foreach (KeyValuePair<Graphic, Material> entry in originalMaterials)
            {
                if (entry.Key == null) continue;
                entry.Key.material = entry.Value;
            }

            originalMaterials.Clear();

            foreach (LDY_UIPixelizerRelay relay in relays)
            {
                if (relay != null) relay.Detach(this);
            }

            relays.Clear();
        }
    }
}
