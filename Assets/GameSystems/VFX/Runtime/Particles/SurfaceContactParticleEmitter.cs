using UnityEngine;

namespace GameSystems.VFX
{
    [DisallowMultipleComponent]
    public sealed class SurfaceContactParticleEmitter : MonoBehaviour
    {
        [SerializeField] Material material;
        ParticleSystem particles;

        public void Configure(Material value) { material = value; EnsureSystem(); }

        public void SetContact(Vector3 point, Vector3 normal, Vector3 tangent, bool active)
        {
            EnsureSystem();
            if (particles == null) return;
            transform.SetPositionAndRotation(point + normal * .035f,
                Quaternion.LookRotation(tangent.sqrMagnitude > .1f ? tangent : Vector3.forward,
                    normal.sqrMagnitude > .1f ? normal : Vector3.up));
            var emission = particles.emission;
            emission.enabled = active;
            if (active && !particles.isPlaying) particles.Play();
            else if (!active && particles.isPlaying)
                particles.Stop(true, ParticleSystemStopBehavior.StopEmitting);
        }

        void Awake() => EnsureSystem();

        void EnsureSystem()
        {
            if (particles != null) return;
            particles = GetComponent<ParticleSystem>();
            if (particles == null) particles = gameObject.AddComponent<ParticleSystem>();
            var main = particles.main;
            main.loop = true;
            main.startLifetime = new ParticleSystem.MinMaxCurve(.28f, .62f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(.18f, .65f);
            main.startSize = new ParticleSystem.MinMaxCurve(.028f, .095f);
            main.startColor = new ParticleSystem.MinMaxGradient(
                new Color(.12f, 1.55f, 1.3f), new Color(1.6f, .16f, 1.25f));
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.maxParticles = 280;
            var emission = particles.emission;
            emission.rateOverTime = 92f;
            emission.enabled = false;
            var shape = particles.shape;
            shape.shapeType = ParticleSystemShapeType.Box;
            shape.scale = new Vector3(.38f, .035f, .16f);
            var velocity = particles.velocityOverLifetime;
            velocity.enabled = true;
            velocity.space = ParticleSystemSimulationSpace.Local;
            velocity.x = new ParticleSystem.MinMaxCurve(0f, 0f);
            velocity.y = new ParticleSystem.MinMaxCurve(.15f, .55f);
            velocity.z = new ParticleSystem.MinMaxCurve(-.7f, -.18f);
            var color = particles.colorOverLifetime;
            color.enabled = true;
            color.color = new ParticleSystem.MinMaxGradient(new Gradient
            {
                colorKeys = new[]
                {
                    new GradientColorKey(Color.cyan, 0f),
                    new GradientColorKey(Color.yellow, .35f),
                    new GradientColorKey(Color.magenta, .72f),
                    new GradientColorKey(new Color(.45f, .2f, 1f), 1f)
                },
                alphaKeys = new[] { new GradientAlphaKey(.9f, 0f), new GradientAlphaKey(0f, 1f) }
            });
            ParticleSystemRenderer renderer = particles.GetComponent<ParticleSystemRenderer>();
            renderer.renderMode = ParticleSystemRenderMode.Billboard;
            renderer.sharedMaterial = material;
        }
    }
}
