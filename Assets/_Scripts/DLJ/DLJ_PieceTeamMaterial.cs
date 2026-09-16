using System.Collections.Generic;
using _Scripts.LDY;
using TMPro;
using UnityEngine;

/// <summary>기물의 소속에 맞춰 모델 머티리얼 슬롯을 교체하는 표시 컴포넌트.</summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(LDY_Animal))]
public sealed class DLJ_PieceTeamMaterial : MonoBehaviour
{
    [Tooltip("비우면 Resources/DLJ/DLJ_PieceTeamMaterials의 공통 설정을 사용")]
    [SerializeField] private DLJ_PieceTeamMaterialSettingsSO settings;
    [Tooltip("비우면 modelTransform 아래의 메시를 자동 선택. 눈/장식 등을 제외하려면 몸체 렌더러만 지정")]
    [SerializeField] private Renderer[] targetRenderers;
    [Tooltip("교체할 머티리얼 슬롯. 0은 첫 번째 슬롯이며 나머지 슬롯은 유지")]
    [SerializeField, Min(0)] private int materialSlot;

    private LDY_Animal _animal;
    private readonly List<Binding> _bindings = new();
    private LDY_Team _appliedTeam;
    private Material _appliedMaterial;
    private bool _hasApplied;

    /// <summary>기물 Awake에서 호출. 프리팹에 설정된 컴포넌트가 있으면 그대로 사용.</summary>
    public static void Install(LDY_Animal animal)
    {
        if (animal != null && !animal.TryGetComponent<DLJ_PieceTeamMaterial>(out _))
            animal.gameObject.AddComponent<DLJ_PieceTeamMaterial>();
    }

    private void OnEnable()
    {
        _animal = GetComponent<LDY_Animal>();
        if (settings == null)
            settings = Resources.Load<DLJ_PieceTeamMaterialSettingsSO>(
                DLJ_PieceTeamMaterialSettingsSO.ResourcePath);

        if (settings == null)
            Debug.LogWarning("기물 팀 머티리얼 설정이 없어 원래 머티리얼을 유지해.", this);

        CacheRenderers();
        // Setup은 team을 먼저 정하고 이 이벤트를 발행하므로 생성 직후에도 즉시 반영.
        _animal.AbilitiesChanged += Refresh;
        Refresh();
    }

    private void LateUpdate()
    {
        // team은 public 필드라 직접 대입하는 테스트/팀 변경도 감지해야 함.
        Material selected = settings != null ? settings.GetMaterial(_animal.team) : null;
        if (!_hasApplied || _appliedTeam != _animal.team || _appliedMaterial != selected)
            Refresh();
    }

    private void OnDisable()
    {
        if (_animal != null) _animal.AbilitiesChanged -= Refresh;
        Restore();
        _bindings.Clear();
        _hasApplied = false;
    }

    private void CacheRenderers()
    {
        Transform root = _animal.modelTransform != null ? _animal.modelTransform : transform;
        Renderer[] renderers = targetRenderers != null && targetRenderers.Length > 0
            ? targetRenderers
            : root.GetComponentsInChildren<Renderer>(true);

        var seen = new HashSet<Renderer>();
        foreach (Renderer target in renderers)
        {
            if (target == null || !seen.Add(target)) continue;
            if (!(target is MeshRenderer) && !(target is SkinnedMeshRenderer)) continue;
            if (target.GetComponentInParent<LDY_Animal>(true) != _animal) continue;
            if (target.GetComponent<TMP_Text>() != null ||
                target.GetComponentInParent<ParticleSystem>(true) != null) continue;

            Material[] materials = target.sharedMaterials;
            if (materialSlot < 0 || materialSlot >= materials.Length) continue;
            _bindings.Add(new Binding(target, materialSlot, materials[materialSlot]));
        }
    }

    [ContextMenu("Refresh Team Material (Play Mode)")]
    public void Refresh()
    {
        if (!Application.isPlaying || !isActiveAndEnabled || _animal == null) return;
        Material selected = settings != null ? settings.GetMaterial(_animal.team) : null;
        // 같은 팀에서는 사망/페이드 연출이 교체한 머티리얼을 다시 덮지 않음.
        if (_hasApplied && _appliedTeam == _animal.team && _appliedMaterial == selected) return;

        foreach (Binding binding in _bindings)
        {
            if (binding.Renderer == null) continue;
            Material[] materials = binding.Renderer.sharedMaterials;
            if (binding.Slot >= materials.Length) continue;
            Material replacement = selected != null ? selected : binding.Original;
            materials[binding.Slot] = replacement;
            binding.Renderer.sharedMaterials = materials;
            binding.Applied = replacement;
        }

        _appliedTeam = _animal.team;
        _appliedMaterial = selected;
        _hasApplied = true;
    }

    private void Restore()
    {
        foreach (Binding binding in _bindings)
        {
            if (binding.Renderer == null) continue;
            Material[] materials = binding.Renderer.sharedMaterials;
            // 다른 연출이 교체한 슬롯은 해당 연출이 복구하도록 맡김.
            if (binding.Slot >= materials.Length || materials[binding.Slot] != binding.Applied) continue;
            materials[binding.Slot] = binding.Original;
            binding.Renderer.sharedMaterials = materials;
        }
    }

    private sealed class Binding
    {
        public readonly Renderer Renderer;
        public readonly int Slot;
        public readonly Material Original;
        public Material Applied;

        public Binding(Renderer renderer, int slot, Material original)
        {
            Renderer = renderer;
            Slot = slot;
            Original = original;
        }
    }
}
