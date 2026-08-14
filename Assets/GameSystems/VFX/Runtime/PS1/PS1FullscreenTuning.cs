using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace GameSystems.VFX
{
    [ExecuteAlways]
    public sealed class PS1FullscreenTuning : MonoBehaviour
    {
        [SerializeField] Material fullscreenMaterial;
        [Header("Résolution 3D")]
        [Tooltip("Largeur maximale du rendu 3D. L'UI reste à la résolution de l'écran.")]
        [SerializeField, Min(320)] int renderWidth = 640;
        [Tooltip("Hauteur maximale du rendu 3D. Le ratio de l'écran est conservé.")]
        [SerializeField, Min(180)] int renderHeight = 360;
        [Header("Pixelisation")]
        [SerializeField, Range(1f, 6f)] float pixelSize = 1.75f;
        [SerializeField, Range(8f, 128f)] float colorSteps = 72f;
        [Header("Trame")]
        [SerializeField, Range(0f, 1f)] float ditherStrength = .1f;
        [SerializeField, Range(1f, 8f)] float ditherPatternScale = 2f;
        [SerializeField, Range(0f, .3f)] float scanlineStrength = .025f;
        [SerializeField, Range(1f, 8f)] float scanlineSpacing = 3f;
        [Header("Glow")]
        [Tooltip("Brightness level above which pixels emit a halo.")]
        [SerializeField, Range(.5f, 2f)] float glowThreshold = .92f;
        [Tooltip("Strength of the fullscreen halo around emissive objects.")]
        [SerializeField, Range(0f, 2f)] float glowIntensity = .42f;
        [Tooltip("Radius of the halo in screen pixels.")]
        [SerializeField, Range(.5f, 8f)] float glowRadius = 2.2f;

        UniversalRenderPipelineAsset runtimePipeline;
        float originalRenderScale;
        int appliedScreenWidth;
        int appliedScreenHeight;

        public void Configure(Material material) { fullscreenMaterial = material; ApplyMaterial(); }
        void OnEnable()
        {
            ApplyMaterial();
            if (Application.isPlaying) ApplyRenderScale();
        }
        void Update()
        {
            if (!Application.isPlaying || Application.isBatchMode) return;
            if (Screen.width != appliedScreenWidth || Screen.height != appliedScreenHeight)
                ApplyRenderScale();
        }
        void OnDisable()
        {
            if (runtimePipeline != null) runtimePipeline.renderScale = originalRenderScale;
            runtimePipeline = null;
        }
        void OnValidate()
        {
            ApplyMaterial();
        }

        void ApplyMaterial()
        {
            if (fullscreenMaterial == null) return;
            fullscreenMaterial.SetFloat("_Pixelation", pixelSize);
            fullscreenMaterial.SetFloat("_ColorSteps", colorSteps);
            fullscreenMaterial.SetFloat("_DitherStrength", ditherStrength);
            fullscreenMaterial.SetFloat("_DitherScale", ditherPatternScale);
            fullscreenMaterial.SetFloat("_ScanlineStrength", scanlineStrength);
            fullscreenMaterial.SetFloat("_ScanlineSpacing", scanlineSpacing);
            fullscreenMaterial.SetFloat("_GlowThreshold", glowThreshold);
            fullscreenMaterial.SetFloat("_GlowIntensity", glowIntensity);
            fullscreenMaterial.SetFloat("_GlowRadius", glowRadius);
        }

        void ApplyRenderScale()
        {
            UniversalRenderPipelineAsset pipeline = UniversalRenderPipeline.asset;
            if (pipeline == null || Screen.width <= 0 || Screen.height <= 0) return;
            if (runtimePipeline == null)
            {
                runtimePipeline = pipeline;
                originalRenderScale = pipeline.renderScale;
            }

            float widthScale = (float)renderWidth / Screen.width;
            float heightScale = (float)renderHeight / Screen.height;
            pipeline.renderScale = Mathf.Clamp(Mathf.Min(1f, widthScale, heightScale), .1f, 1f);
            appliedScreenWidth = Screen.width;
            appliedScreenHeight = Screen.height;
        }

    }
}
