using System;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RenderGraphModule;
using UnityEngine.Rendering.Universal;
using UnityEngine.Rendering.RenderGraphModule.Util;

namespace _Scripts.DLJ.SceneFlow
{
    /// <summary>보스 등장 중 게임 카메라 화면에 굴절 파동과 감쇠하는 떨림을 합성한다.</summary>
    public sealed class DLJ_BossRoarRipple : IDisposable
    {
        private static readonly int RippleParams = Shader.PropertyToID("_RippleParams");
        private static readonly int ShakeParams = Shader.PropertyToID("_ShakeParams");
        private static readonly int FocusParams = Shader.PropertyToID("_FocusParams");
        private static readonly int DimmingParams = Shader.PropertyToID("_DimmingParams");
        private Material material;
        private RipplePass pass;
        private Camera target;
        private float startedAt;
        private float duration;
        private float strength;
        private float shakeDuration;
        private float shakeStrength;
        private float shakeFrequency;
        private bool subscribed;
        private Transform focus;
        private Renderer focusRenderer;
        private Vector2 focusViewport;
        private float dimmingDuration;
        private float dimmingStrength;
        private float focusRadius;
        private bool preparing;

        /// <summary>포효 전에는 보스 주변만 희미하게 남기고 파동·떨림 없이 유지한다.</summary>
        public void Prepare(Transform focus, float dimStrength, float brightRadius)
        {
            Play(0.1f, 0f, focus: focus, dimSeconds: 0.1f,
                dimStrength: dimStrength, brightRadius: brightRadius);
            preparing = subscribed;
        }

        public void Play(float seconds, float amplitude, float shakeSeconds = 0f,
            float shakeAmplitude = 0f, float shakeHz = 24f, Transform focus = null,
            float dimSeconds = 0f, float dimStrength = 0f, float brightRadius = 0.22f)
        {
            Stop();
            if (amplitude <= 0f && shakeAmplitude <= 0f && dimStrength <= 0f) return;
            target = Camera.main;
            if (target == null || !(GraphicsSettings.currentRenderPipeline is UniversalRenderPipelineAsset))
            {
                Debug.LogWarning("[보스 화면 울림] MainCamera 또는 URP가 없어 파동을 표시할 수 없습니다.");
                return;
            }
            if (material == null)
            {
                Shader shader = Resources.Load<Shader>("DLJ/DLJ_BossRoarRipple");
                if (shader == null || !shader.isSupported)
                {
                    Debug.LogWarning("[보스 화면 울림] 파동 셰이더가 없거나 지원되지 않습니다.");
                    return;
                }
                material = CoreUtils.CreateEngineMaterial(shader);
                pass = new RipplePass(material);
            }
            duration = Mathf.Max(0.1f, seconds);
            strength = Mathf.Clamp(amplitude, 0f, 0.03f);
            shakeDuration = Mathf.Max(0.1f, shakeSeconds);
            shakeStrength = Mathf.Clamp(shakeAmplitude, 0f, 0.02f);
            shakeFrequency = Mathf.Clamp(shakeHz, 1f, 40f);
            dimmingDuration = Mathf.Max(0.1f, dimSeconds);
            dimmingStrength = Mathf.Clamp(dimStrength, 0f, 0.95f);
            focusRadius = Mathf.Clamp(brightRadius, 0.05f, 0.5f);
            this.focus = focus;
            focusViewport = new Vector2(0.5f, 0.5f);
            if (focus != null)
            {
                // 파티클·궤적·UI 대신 가장 큰 모델 메시를 몸통 기준으로 삼는다.
                float largestSize = -1f;
                foreach (Renderer candidate in focus.GetComponentsInChildren<Renderer>())
                {
                    if (!candidate.enabled || !(candidate is MeshRenderer || candidate is SkinnedMeshRenderer)) continue;
                    float size = candidate.bounds.size.sqrMagnitude;
                    if (size <= largestSize) continue;
                    largestSize = size;
                    focusRenderer = candidate;
                }
            }
            startedAt = Time.unscaledTime;
            RenderPipelineManager.beginCameraRendering += OnCameraRendering;
            subscribed = true;
        }

        private void OnCameraRendering(ScriptableRenderContext context, Camera camera)
        {
            float elapsed = preparing ? 0f : Time.unscaledTime - startedAt;
            float progress = elapsed / duration;
            bool rippleFinished = strength <= 0f || progress >= 1f;
            bool shakeFinished = shakeStrength <= 0f || elapsed >= shakeDuration;
            bool dimmingFinished = dimmingStrength <= 0f || elapsed >= dimmingDuration;
            if ((rippleFinished && shakeFinished && dimmingFinished) || target == null)
            {
                Stop();
                return;
            }
            if (camera != target || camera.cameraType != CameraType.Game) return;
            if (focus != null)
            {
                Vector3 worldCenter = focusRenderer != null ? focusRenderer.bounds.center : focus.position;
                Vector3 viewport = camera.WorldToViewportPoint(worldCenter);
                // 카메라 뒤로 넘어가거나 기물이 사라지면 마지막 유효한 중심을 유지한다.
                if (viewport.z > 0f) focusViewport = new Vector2(viewport.x, viewport.y);
            }
            material.SetVector(FocusParams, new Vector4(focusViewport.x, focusViewport.y, 0f, 0f));
            material.SetVector(DimmingParams, new Vector4(Mathf.Clamp01(elapsed / dimmingDuration),
                dimmingStrength, focusRadius, dimmingStrength > 0f ? 1f : 0f));
            float aspect = camera.pixelWidth / (float)Mathf.Max(1, camera.pixelHeight);
            material.SetVector(RippleParams, new Vector4(Mathf.Clamp01(progress), strength,
                aspect, 0f));
            float shakeProgress = Mathf.Clamp01(elapsed / shakeDuration);
            float envelope = Mathf.SmoothStep(0f, 1f, shakeProgress / 0.08f)
                * (1f - shakeProgress) * (1f - shakeProgress);
            float phase = elapsed * shakeFrequency * Mathf.PI * 2f;
            float horizontal = (Mathf.Sin(phase) + Mathf.Sin(phase * 1.73f + 0.8f) * 0.35f) / 1.35f;
            float vertical = (Mathf.Sin(phase * 1.27f + 1.6f) + Mathf.Sin(phase * 2.11f) * 0.35f) / 1.35f;
            float amplitude = shakeStrength * envelope;
            // 화면 가장자리에 빈틈이 생기지 않을 만큼만 확대한다. 카메라 Transform은 변경하지 않는다.
            float crop = amplitude * Mathf.Max(1f, 1f / Mathf.Max(aspect, 0.01f));
            material.SetVector(ShakeParams, new Vector4(horizontal * amplitude / Mathf.Max(aspect, 0.01f),
                vertical * amplitude, crop, 0f));
            camera.GetUniversalAdditionalCameraData().scriptableRenderer.EnqueuePass(pass);
        }

        public void Stop()
        {
            if (subscribed) RenderPipelineManager.beginCameraRendering -= OnCameraRendering;
            subscribed = false;
            preparing = false;
            target = null;
            focus = null;
            focusRenderer = null;
            if (material != null)
            {
                material.SetVector(RippleParams, Vector4.zero);
                material.SetVector(ShakeParams, Vector4.zero);
                material.SetVector(DimmingParams, Vector4.zero);
            }
        }

        public void Dispose()
        {
            Stop();
            pass?.Dispose();
            pass = null;
            CoreUtils.Destroy(material);
            material = null;
        }

        private sealed class RipplePass : ScriptableRenderPass
        {
            private readonly Material material;
            private RTHandle copy;

            public RipplePass(Material material)
            {
                this.material = material;
                renderPassEvent = RenderPassEvent.AfterRenderingPostProcessing;
                requiresIntermediateTexture = true;
            }

            public override void RecordRenderGraph(RenderGraph graph, ContextContainer frameData)
            {
                var resources = frameData.Get<UniversalResourceData>();
                if (resources.isActiveTargetBackBuffer)
                {
                    Debug.LogWarning("[보스 화면 울림] 읽을 수 있는 카메라 컬러 버퍼가 없습니다.");
                    return;
                }
                var source = resources.activeColorTexture;
                var descriptor = graph.GetTextureDesc(source);
                descriptor.name = "Boss Roar Color Copy";
                descriptor.clearBuffer = false;
                var snapshot = graph.CreateTexture(descriptor);
                graph.AddBlitPass(source, snapshot, Vector2.one, Vector2.zero, passName: "Boss Roar Copy");
                graph.AddBlitPass(new RenderGraphUtils.BlitMaterialParameters(snapshot, source, material, 0),
                    passName: "Boss Roar Ripple");
            }

            // URP 호환 모드에서도 같은 효과를 사용한다.
#pragma warning disable CS0618, CS0672
            public override void OnCameraSetup(CommandBuffer cmd, ref RenderingData renderingData)
            {
                ResetTarget();
                var descriptor = renderingData.cameraData.cameraTargetDescriptor;
                descriptor.msaaSamples = 1;
                descriptor.depthBufferBits = 0;
                RenderingUtils.ReAllocateHandleIfNeeded(ref copy, descriptor, name: "Boss Roar Color Copy");
            }

            public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
            {
                var cmd = CommandBufferPool.Get("Boss Roar Ripple");
                var source = renderingData.cameraData.renderer.cameraColorTargetHandle;
                Blitter.BlitCameraTexture(cmd, source, copy);
                Blitter.BlitCameraTexture(cmd, copy, source, material, 0);
                context.ExecuteCommandBuffer(cmd);
                CommandBufferPool.Release(cmd);
            }
#pragma warning restore CS0618, CS0672

            public void Dispose() => copy?.Release();
        }
    }
}
