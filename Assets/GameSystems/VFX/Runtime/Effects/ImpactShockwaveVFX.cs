using UnityEngine;

namespace GameSystems.VFX
{
    [DisallowMultipleComponent]
    public sealed class ImpactShockwaveVFX : MonoBehaviour
    {
        [SerializeField, Min(.01f)] float duration = .42f;
        [SerializeField] Renderer shockwaveRenderer;
        MaterialPropertyBlock properties;
        float startedAt;

        public void Configure(Renderer target, float lifetime)
        {
            shockwaveRenderer = target;
            duration = Mathf.Max(.01f, lifetime);
        }

        void OnEnable()
        {
            properties ??= new MaterialPropertyBlock();
            startedAt = Time.unscaledTime;
            SetProgress(0f);
            foreach (ParticleSystem particles in GetComponentsInChildren<ParticleSystem>(true))
            {
                particles.Clear(true);
                particles.Play(true);
            }
        }

        void LateUpdate()
        {
            float progress = Mathf.Clamp01((Time.unscaledTime - startedAt) / duration);
            SetProgress(progress);
        }

        void SetProgress(float value)
        {
            if (shockwaveRenderer == null) return;
            properties ??= new MaterialPropertyBlock();
            shockwaveRenderer.GetPropertyBlock(properties);
            properties.SetFloat("_Progress", value);
            shockwaveRenderer.SetPropertyBlock(properties);
        }
    }
}
