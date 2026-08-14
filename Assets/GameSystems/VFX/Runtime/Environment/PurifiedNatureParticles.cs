using UnityEngine;
using UnityEngine.Rendering;

namespace GameSystems.VFX
{
    public sealed class PurifiedNatureParticles : MonoBehaviour
    {
        [SerializeField] Transform target;
        ParticleSystem fireflies, petals;
        Material fireflyMaterial, petalMaterial;
        Mesh fireflyMesh, petalMesh;
        float purityOverride = -1f;

        public void Configure(Transform follow) => target = follow;
        public void Configure(Transform follow, float purity)
        { target = follow; purityOverride = Mathf.Clamp01(purity); }

        void Awake()
        {
            fireflyMesh = BuildFireflyMesh();
            petalMesh = BuildPetalMesh();
            Shader shader = Shader.Find("Universal Render Pipeline/Unlit");
            fireflyMaterial = new Material(shader) { name = "MAT_Fireflies_Runtime" };
            petalMaterial = new Material(shader) { name = "MAT_WindPetals_Runtime" };
            fireflyMaterial.SetColor("_BaseColor", new Color(1f, .82f, .22f, 1f));
            petalMaterial.SetColor("_BaseColor", new Color(1f, .38f, .7f, 1f));
            fireflies = CreateSystem("Purified Fireflies", fireflyMesh, fireflyMaterial, 110, 5.5f, .032f, .12f, new Vector3(8f, 2.8f, 5.5f));
            petals = CreateSystem("Windblown Petals", petalMesh, petalMaterial, 90, 6.5f, .055f, .18f, new Vector3(9f, 1.8f, 5.5f));
        }

        void Update()
        {
            // This component shares its root with the purification field and vegetation meshes.
            // Move only the particle emitters; moving this transform would drag every plant chunk.
            if (target != null)
            {
                Vector3 emitterPosition = target.position + new Vector3(1.5f, 1.2f, 3.5f);
                fireflies.transform.position = emitterPosition;
                petals.transform.position = emitterPosition + Vector3.down * .55f;
            }
            float purity = purityOverride >= 0f ? purityOverride :
                Mathf.Clamp01(Shader.GetGlobalFloat("_PurificationCameraStrength"));
            var fireflyEmission = fireflies.emission; fireflyEmission.rateOverTimeMultiplier = 24f * purity;
            var petalEmission = petals.emission; petalEmission.rateOverTimeMultiplier = 4.5f * Mathf.SmoothStep(0f, 1f, purity);
            ApplyWind(fireflies, .18f, .08f);
            ApplyWind(petals, .72f, .48f);
        }

        static void ApplyWind(ParticleSystem system, float horizontalStrength, float liftStrength)
        {
            float time = Time.time;
            float gust = Mathf.Pow(Mathf.Sin(time * .88f) * .5f + .5f, 3f);
            float direction = Mathf.Sin(time * .43f) * .8f + Mathf.Sin(time * .77f) * .35f;
            var force = system.forceOverLifetime; force.enabled = true; force.space = ParticleSystemSimulationSpace.World;
            force.x = new ParticleSystem.MinMaxCurve((Mathf.Cos(direction) * (.28f + gust * 1.9f)) * horizontalStrength);
            force.z = new ParticleSystem.MinMaxCurve((Mathf.Sin(direction) * (.28f + gust * 1.9f)) * horizontalStrength);
            force.y = new ParticleSystem.MinMaxCurve((-.08f + gust * liftStrength), (.03f + gust * liftStrength * 1.45f));
        }

        ParticleSystem CreateSystem(string objectName, Mesh mesh, Material material, int maximum, float lifetime, float size, float speed, Vector3 volume)
        {
            var go = new GameObject(objectName, typeof(ParticleSystem)); go.transform.SetParent(transform, false);
            ParticleSystem system = go.GetComponent<ParticleSystem>();
            var main = system.main; main.loop = true; main.playOnAwake = true; main.maxParticles = maximum;
            main.startLifetime = new ParticleSystem.MinMaxCurve(lifetime * .65f, lifetime);
            main.startSize = new ParticleSystem.MinMaxCurve(size * .72f, size * 1.3f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(speed * .35f, speed);
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            var emission = system.emission; emission.rateOverTime = 0f;
            var shape = system.shape; shape.shapeType = ParticleSystemShapeType.Box; shape.scale = volume;
            var noise = system.noise; noise.enabled = true; noise.quality = ParticleSystemNoiseQuality.Low;
            noise.strength = objectName.Contains("Butter") ? .62f : .24f; noise.frequency = objectName.Contains("Butter") ? .42f : .18f;
            noise.scrollSpeed = .16f; noise.damping = true;
            var rotation = system.rotationOverLifetime; rotation.enabled = true; rotation.z = new ParticleSystem.MinMaxCurve(-2.2f, 2.2f);
            var renderer = go.GetComponent<ParticleSystemRenderer>(); renderer.renderMode = ParticleSystemRenderMode.Mesh;
            renderer.mesh = mesh; renderer.sharedMaterial = material; renderer.shadowCastingMode = ShadowCastingMode.Off; renderer.receiveShadows = false;
            system.Play(); return system;
        }

        static Mesh BuildFireflyMesh()
        {
            var mesh = new Mesh { name = "Firefly Pixel" };
            mesh.vertices = new[] { new Vector3(-.5f,-.5f,0), new Vector3(.5f,-.5f,0), new Vector3(.5f,.5f,0), new Vector3(-.5f,.5f,0) };
            mesh.triangles = new[] { 0,1,2, 0,2,3 }; mesh.RecalculateNormals(); return mesh;
        }

        static Mesh BuildPetalMesh()
        {
            var mesh = new Mesh { name = "Wind Petal Diamond" };
            mesh.vertices = new[] { new Vector3(0,0,-.65f), new Vector3(.42f,0,0), new Vector3(0,0,.65f), new Vector3(-.42f,0,0) };
            mesh.triangles = new[] { 0,1,2, 0,2,3 }; mesh.RecalculateNormals(); return mesh;
        }

        void OnDestroy()
        {
            if (fireflyMaterial != null) Destroy(fireflyMaterial);
            if (petalMaterial != null) Destroy(petalMaterial);
            if (fireflyMesh != null) Destroy(fireflyMesh);
            if (petalMesh != null) Destroy(petalMesh);
        }
    }
}
