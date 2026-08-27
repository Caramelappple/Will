#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEngine;

namespace _Scripts.LDY.Effect.Debugging
{
    /// <summary>
    /// ⚠ 임시 디버그 코드다. 게임 로직이 여기에 기대게 만들지 말 것.
    ///
    /// ── 제거 예정 ─────────────────────────────────────────────
    /// KTH의 보상 quad가 앵커에 붙는 순간 이 컴포넌트와 씬 배치를 제거할 것.
    ///
    /// 지울 때 함께 치울 것:
    ///   - 이 파일 (LDY_RewardAnchorDummy.cs, .meta 포함)
    ///   - LDY_RewardAnchor에 붙여둔 컴포넌트
    /// ─────────────────────────────────────────────────────────
    ///
    /// 보상 앵커 자리에 판때기 세 장을 세워 회전 각도·앵커 좌표를 눈으로 맞추게 해준다.
    /// KTH의 quad를 기다리지 않고 연출을 완결 검증하기 위한 것이다.
    ///
    /// 보상 앵커(LDY_RewardAnchor)에 붙인다. 만들어진 판은 이 오브젝트의 자식이 된다.
    ///
    /// 파일 전체가 UNITY_EDITOR로 묶여 있어 빌드에는 들어가지 않는다.
    /// 그래서 이 컴포넌트를 붙여둔 씬은 빌드에서 "missing script" 경고를 낸다.
    /// </summary>
    [DisallowMultipleComponent]
    public class LDY_RewardAnchorDummy : MonoBehaviour
    {
        private const string BuiltName = "LDY_DummyRewardQuad";

        [Header("배치")]
        [SerializeField, Min(1)] private int count = 3;

        [Tooltip("판 사이 간격(로컬 X). 앵커의 로컬 +X는 화면 오른쪽이다.")]
        [SerializeField] private float spacing = 1.95f;

        [Tooltip("판 하나의 크기(로컬). 카메라까지 8.86 거리에서 1.6 × 2.3이면 화면 높이의 약 23%다.")]
        [SerializeField] private Vector2 quadSize = new Vector2(1.6f, 2.3f);

        [Tooltip("Unity의 Quad는 로컬 −Z를 향한다. 앵커의 −Z가 카메라 쪽이라 회전이 0이면 정면으로 보인다.\n" +
                 "뒤집혀 보이면 (0, 180, 0)으로 바꿀 것.")]
        [SerializeField] private Vector3 quadLocalEuler = Vector3.zero;

        [Header("레이어")]
        [Tooltip("보상 클릭 판정용 레이어. 보드 레이어(LDY_Board)를 재사용하면 " +
                 "SelectionController의 레이캐스트에 걸려 WorldToGrid가 엉뚱한 칸을 집는다.")]
        [SerializeField] private string rewardLayerName = "LDY_Reward";

        [Header("모양")]
        [Tooltip("판마다 다른 색을 준다. 좌우 순서가 화면에서 어느 쪽인지 확인하는 용도다.")]
        [SerializeField]
        private Color[] tints =
        {
            new Color(0.85f, 0.30f, 0.30f),
            new Color(0.30f, 0.70f, 0.85f),
            new Color(0.85f, 0.72f, 0.30f)
        };

        private readonly List<Material> _materials = new();

        private void Awake()
        {
            Build();
        }

        private void OnDestroy()
        {
            // 런타임에 만든 머티리얼은 씬이 정리해주지 않는다.
            foreach (Material material in _materials)
            {
                if (material != null) Destroy(material);
            }
            _materials.Clear();
        }

        private void Build()
        {
            int layer = ResolveLayer();
            Shader shader = ResolveShader();

            if (shader == null)
            {
                Debug.LogError(
                    "[LDY_RewardAnchorDummy] 쓸 만한 셰이더를 찾지 못해 더미를 만들지 못했습니다.", this);
                return;
            }

            float first = -(count - 1) * 0.5f * spacing;

            for (int i = 0; i < count; i++)
            {
                // Quad 프리미티브에는 MeshCollider가 함께 붙는다. Physics.Raycast 확인에 그대로 쓸 수 있다.
                GameObject quad = GameObject.CreatePrimitive(PrimitiveType.Quad);
                quad.name = $"{BuiltName}_{i}";
                quad.layer = layer;

                Transform t = quad.transform;
                t.SetParent(transform, false);
                t.localPosition = new Vector3(first + i * spacing, 0f, 0f);
                t.localRotation = Quaternion.Euler(quadLocalEuler);
                t.localScale = new Vector3(quadSize.x, quadSize.y, 1f);

                // 조명 상태와 무관하게 보여야 위치·크기를 판단할 수 있다.
                // 뒤집힌 보드 아랫면은 거의 무광원이라 Lit을 쓰면 새까맣게 나온다.
                var material = new Material(shader) { color = PickTint(i) };
                if (material.HasProperty("_BaseColor"))
                    material.SetColor("_BaseColor", PickTint(i));

                quad.GetComponent<MeshRenderer>().sharedMaterial = material;
                _materials.Add(material);
            }

            Debug.Log(
                $"[LDY_RewardAnchorDummy] 더미 보상 판 {count}장 생성 " +
                $"(레이어 {LayerMask.LayerToName(layer)})", this);
        }

        private Color PickTint(int index)
        {
            if (tints == null || tints.Length == 0) return Color.white;
            return tints[index % tints.Length];
        }

        private int ResolveLayer()
        {
            int layer = LayerMask.NameToLayer(rewardLayerName);
            if (layer >= 0) return layer;

            Debug.LogWarning(
                $"[LDY_RewardAnchorDummy] '{rewardLayerName}' 레이어가 없습니다. " +
                "Project Settings > Tags and Layers에서 추가하세요. 일단 Default로 만듭니다.", this);

            return 0;
        }

        private static Shader ResolveShader()
        {
            return Shader.Find("Universal Render Pipeline/Unlit")
                   ?? Shader.Find("Unlit/Color")
                   ?? Shader.Find("Universal Render Pipeline/Lit");
        }
    }
}
#endif
