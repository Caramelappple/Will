using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

[InitializeOnLoad]
public static class DLJ_PixelExplosionEffectBuilder
{
    private const string ShaderPath = "Assets/_Shaders/DLJ/Shader_PixelExplosion.shader";
    private const string MaterialFolder = "Assets/_Material/DLJ/WillMaterial";
    private const string PrefabPath = "Assets/_Prefabs/DLJ/WillEffect/PixelExplosionEffect.prefab";

    private static int importRetryCount;

    static DLJ_PixelExplosionEffectBuilder()
    {
        EditorApplication.delayCall += BuildOnFirstImport;
    }

    [MenuItem("Tools/DLJ/Effects/Rebuild Pixel Explosion Effect")]
    public static void Rebuild()
    {
        BuildEffect(true);
    }

    private static void BuildOnFirstImport()
    {
        if (AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath) != null)
            return;

        if (AssetDatabase.LoadAssetAtPath<Shader>(ShaderPath) == null)
        {
            if (importRetryCount++ < 20)
                EditorApplication.delayCall += BuildOnFirstImport;
            return;
        }

        BuildEffect(false);
    }

    private static void BuildEffect(bool logCompletion)
    {
        Shader shader = AssetDatabase.LoadAssetAtPath<Shader>(ShaderPath);
        if (shader == null)
        {
            Debug.LogError($"Pixel Explosion 셰이더를 찾을 수 없음: {ShaderPath}");
            return;
        }

        Material coreMaterial = CreateOrUpdateMaterial(
            $"{MaterialFolder}/PixelExplosion_Core.mat",
            shader,
            new Color(5.5f, 5.0f, 1.6f, 1f),
            new Color(4.2f, 1.8f, 0.08f, 1f),
            new Color(1.3f, 0.12f, 0.01f, 1f),
            0.08f,
            5.0f,
            0.16f,
            false,
            3.0f,
            4.0f);

        Material flameMaterial = CreateOrUpdateMaterial(
            $"{MaterialFolder}/PixelExplosion_Flame.mat",
            shader,
            new Color(4.2f, 3.5f, 0.6f, 1f),
            new Color(3.4f, 1.05f, 0.035f, 1f),
            new Color(0.75f, 0.045f, 0.008f, 1f),
            0.24f,
            6.0f,
            0.1f,
            false,
            3.0f,
            4.0f);

        Material outerFlameMaterial = CreateOrUpdateMaterial(
            $"{MaterialFolder}/PixelExplosion_OuterFlame.mat",
            shader,
            new Color(3.6f, 2.2f, 0.18f, 1f),
            new Color(2.5f, 0.55f, 0.018f, 1f),
            new Color(0.45f, 0.02f, 0.005f, 1f),
            0.32f,
            7.5f,
            0.09f,
            false,
            3.0f,
            4.0f);

        Material ringMaterial = CreateOrUpdateMaterial(
            $"{MaterialFolder}/PixelExplosion_Shockwave.mat",
            shader,
            new Color(4.8f, 3.1f, 0.35f, 1f),
            new Color(3.0f, 0.75f, 0.025f, 1f),
            new Color(0.65f, 0.035f, 0.005f, 1f),
            0.13f,
            9.0f,
            0.08f,
            true,
            3.0f,
            4.0f);

        GameObject root = new GameObject("PixelExplosionEffect");
        root.SetActive(false);

        CreateCore(root.transform, coreMaterial);
        CreateFireball(root.transform, flameMaterial);
        CreateOuterPuffs(root.transform, outerFlameMaterial);
        CreateShockwaves(root.transform, ringMaterial);
        CreateSparks(root.transform, coreMaterial);

        root.SetActive(true);
        PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
        Object.DestroyImmediate(root);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        if (logCompletion)
            Debug.Log($"픽셀 폭발 이펙트 재생성 완료: {PrefabPath}");
    }

    private static Material CreateOrUpdateMaterial(
        string path,
        Shader shader,
        Color core,
        Color middle,
        Color outer,
        float noiseStrength,
        float noiseScale,
        float edgeSoftness,
        bool ringMode,
        float ditherScale,
        float colorSteps)
    {
        Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (material == null)
        {
            material = new Material(shader);
            AssetDatabase.CreateAsset(material, path);
        }
        else
        {
            material.shader = shader;
        }

        material.SetColor("_CoreColor", core);
        material.SetColor("_MidColor", middle);
        material.SetColor("_OuterColor", outer);
        material.SetFloat("_NoiseStrength", noiseStrength);
        material.SetFloat("_NoiseScale", noiseScale);
        material.SetFloat("_NoiseSpeed", 0.28f);
        material.SetFloat("_EdgeSoftness", edgeSoftness);
        material.SetFloat("_RingMode", ringMode ? 1f : 0f);
        material.SetFloat("_RingRadius", 0.72f);
        material.SetFloat("_RingWidth", ringMode ? 0.15f : 0.12f);
        material.SetFloat("_DitherScale", ditherScale);
        material.SetFloat("_ColorSteps", colorSteps);
        material.SetFloat("_DitherStrength", 1f);
        material.SetFloat("_BlendSrc", (float)BlendMode.SrcAlpha);
        material.SetFloat("_BlendDst", (float)BlendMode.One);
        material.SetFloat("_ZWrite", 0f);
        material.SetFloat("_ZTest", (float)CompareFunction.LessEqual);
        material.enableInstancing = true;
        EditorUtility.SetDirty(material);
        return material;
    }

    private static void CreateCore(Transform parent, Material material)
    {
        ParticleSystem ps = CreateBaseSystem(
            parent,
            "CoreFlash",
            material,
            0.9f,
            new ParticleSystem.MinMaxCurve(0.72f, 0.95f),
            new ParticleSystem.MinMaxCurve(0f),
            new ParticleSystem.MinMaxCurve(1.8f, 2.25f),
            3);

        SetBurst(ps, 0f, 1, 1);
        SetSizeCurve(ps,
            new Keyframe(0f, 0.2f),
            new Keyframe(0.08f, 1f),
            new Keyframe(0.45f, 1.55f),
            new Keyframe(1f, 1.15f));
        SetFadeGradient(ps, 0.015f, 0.42f, 1f);
    }

    private static void CreateFireball(Transform parent, Material material)
    {
        ParticleSystem ps = CreateBaseSystem(
            parent,
            "FireballVolume",
            material,
            2.5f,
            new ParticleSystem.MinMaxCurve(1.85f, 2.45f),
            new ParticleSystem.MinMaxCurve(0.22f, 0.68f),
            new ParticleSystem.MinMaxCurve(1.05f, 1.75f),
            2);

        SetBurst(ps, 0.03f, 16, 22);
        ConfigureSphereShape(ps, 0.38f, 1f);
        SetSizeCurve(ps,
            new Keyframe(0f, 0.45f),
            new Keyframe(0.12f, 0.95f),
            new Keyframe(0.62f, 1.42f),
            new Keyframe(1f, 1.15f));
        SetFadeGradient(ps, 0.035f, 0.62f, 1f);

        var noise = ps.noise;
        noise.enabled = true;
        noise.quality = ParticleSystemNoiseQuality.Medium;
        noise.strength = new ParticleSystem.MinMaxCurve(0.16f, 0.32f);
        noise.frequency = 0.42f;
        noise.scrollSpeed = 0.13f;
        noise.damping = true;
    }

    private static void CreateOuterPuffs(Transform parent, Material material)
    {
        ParticleSystem ps = CreateBaseSystem(
            parent,
            "OuterFlamePuffs",
            material,
            2.1f,
            new ParticleSystem.MinMaxCurve(1.25f, 1.9f),
            new ParticleSystem.MinMaxCurve(0.85f, 1.8f),
            new ParticleSystem.MinMaxCurve(0.38f, 0.82f),
            1);

        SetBurst(ps, 0.12f, 18, 26);
        ConfigureSphereShape(ps, 0.28f, 0.65f);
        SetSizeCurve(ps,
            new Keyframe(0f, 0.35f),
            new Keyframe(0.16f, 1f),
            new Keyframe(0.7f, 1.28f),
            new Keyframe(1f, 0.72f));
        SetFadeGradient(ps, 0.08f, 0.5f, 0.82f);

        var velocity = ps.limitVelocityOverLifetime;
        velocity.enabled = true;
        velocity.limit = 1.15f;
        velocity.dampen = 0.28f;
    }

    private static void CreateShockwaves(Transform parent, Material material)
    {
        ParticleSystem ps = CreateBaseSystem(
            parent,
            "GroundShockwaves",
            material,
            1.7f,
            new ParticleSystem.MinMaxCurve(1.2f, 1.55f),
            new ParticleSystem.MinMaxCurve(0f),
            new ParticleSystem.MinMaxCurve(1f),
            0,
            ParticleSystemRenderMode.HorizontalBillboard);

        var emission = ps.emission;
        emission.SetBursts(new[]
        {
            new ParticleSystem.Burst(0.04f, (short)1, (short)1),
            new ParticleSystem.Burst(0.24f, (short)1, (short)1)
        });
        SetSizeCurve(ps,
            new Keyframe(0f, 0.18f),
            new Keyframe(0.22f, 3.3f),
            new Keyframe(0.66f, 6.1f),
            new Keyframe(1f, 7.6f));
        SetFadeGradient(ps, 0.03f, 0.32f, 0.68f);
    }

    private static void CreateSparks(Transform parent, Material material)
    {
        ParticleSystem ps = CreateBaseSystem(
            parent,
            "FlyingEmbers",
            material,
            1.8f,
            new ParticleSystem.MinMaxCurve(0.85f, 1.65f),
            new ParticleSystem.MinMaxCurve(2.2f, 4.8f),
            new ParticleSystem.MinMaxCurve(0.055f, 0.16f),
            4,
            ParticleSystemRenderMode.Stretch);

        var main = ps.main;
        main.gravityModifier = new ParticleSystem.MinMaxCurve(0.16f, 0.42f);
        SetBurst(ps, 0.08f, 28, 42);
        ConfigureSphereShape(ps, 0.16f, 1f);
        SetSizeCurve(ps,
            new Keyframe(0f, 1f),
            new Keyframe(0.55f, 0.62f),
            new Keyframe(1f, 0.05f));
        SetFadeGradient(ps, 0.01f, 0.58f, 1f);

        ParticleSystemRenderer renderer = ps.GetComponent<ParticleSystemRenderer>();
        renderer.velocityScale = 0.08f;
        renderer.lengthScale = 1.6f;
    }

    private static ParticleSystem CreateBaseSystem(
        Transform parent,
        string name,
        Material material,
        float duration,
        ParticleSystem.MinMaxCurve lifetime,
        ParticleSystem.MinMaxCurve speed,
        ParticleSystem.MinMaxCurve size,
        int sortingOrder,
        ParticleSystemRenderMode renderMode = ParticleSystemRenderMode.Billboard)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(parent, false);

        ParticleSystem ps = go.AddComponent<ParticleSystem>();
        var main = ps.main;
        main.duration = duration;
        main.loop = false;
        main.prewarm = false;
        main.playOnAwake = true;
        main.startDelay = 0f;
        main.startLifetime = lifetime;
        main.startSpeed = speed;
        main.startSize = size;
        main.startRotation = new ParticleSystem.MinMaxCurve(0f, Mathf.PI * 2f);
        main.startColor = Color.white;
        main.simulationSpace = ParticleSystemSimulationSpace.Local;
        main.scalingMode = ParticleSystemScalingMode.Hierarchy;
        main.maxParticles = 96;
        main.stopAction = ParticleSystemStopAction.None;

        var emission = ps.emission;
        emission.enabled = true;
        emission.rateOverTime = 0f;
        emission.rateOverDistance = 0f;

        var shape = ps.shape;
        shape.enabled = false;

        ParticleSystemRenderer renderer = go.GetComponent<ParticleSystemRenderer>();
        renderer.renderMode = renderMode;
        renderer.sharedMaterial = material;
        renderer.alignment = ParticleSystemRenderSpace.View;
        renderer.sortMode = ParticleSystemSortMode.Distance;
        renderer.sortingOrder = sortingOrder;
        renderer.allowRoll = true;
        return ps;
    }

    private static void SetBurst(ParticleSystem ps, float time, int minCount, int maxCount)
    {
        var emission = ps.emission;
        emission.SetBursts(new[]
        {
            new ParticleSystem.Burst(time, (short)minCount, (short)maxCount)
        });
    }

    private static void ConfigureSphereShape(ParticleSystem ps, float radius, float radiusThickness)
    {
        var shape = ps.shape;
        shape.enabled = true;
        shape.shapeType = ParticleSystemShapeType.Sphere;
        shape.radius = radius;
        shape.radiusThickness = radiusThickness;
        shape.randomDirectionAmount = 0.18f;
    }

    private static void SetSizeCurve(ParticleSystem ps, params Keyframe[] keys)
    {
        var size = ps.sizeOverLifetime;
        size.enabled = true;
        size.size = new ParticleSystem.MinMaxCurve(1f, new AnimationCurve(keys));
    }

    private static void SetFadeGradient(ParticleSystem ps, float fadeInEnd, float holdEnd, float peakAlpha)
    {
        Gradient gradient = new Gradient();
        gradient.SetKeys(
            new[]
            {
                new GradientColorKey(Color.white, 0f),
                new GradientColorKey(Color.white, 1f)
            },
            new[]
            {
                new GradientAlphaKey(0f, 0f),
                new GradientAlphaKey(peakAlpha, fadeInEnd),
                new GradientAlphaKey(peakAlpha * 0.9f, holdEnd),
                new GradientAlphaKey(0f, 1f)
            });

        var color = ps.colorOverLifetime;
        color.enabled = true;
        color.color = gradient;
    }
}
