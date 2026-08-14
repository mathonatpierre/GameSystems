using UnityEngine;

namespace GameSystems.VFX
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
    public sealed class VertexMaterialPainter : MonoBehaviour
    {
        [SerializeField] Material baseMaterial;
        [SerializeField] Material paintedMaterial;
        [SerializeField, Min(.0005f)] float transitionWidth = .0035f;
        [SerializeField, Range(0f, .02f)] float noiseAmount = .0018f;
        [SerializeField, Range(0f, 3f)] float paintedEmission = 1.3f;
        Mesh paintedMesh;
        Vector3[] vertices;
        Vector2[] progress;
        Color[] colors;
        Material blendMaterial;
        float paintedUntil = -1f;

        void Awake() => Initialize();

        public void Configure(Material newBaseMaterial, Material newPaintedMaterial)
        {
            baseMaterial = newBaseMaterial;
            paintedMaterial = newPaintedMaterial;
            Initialize();
        }

        void Initialize()
        {
            if (paintedMesh != null || blendMaterial != null) return;
            MeshFilter filter = GetComponent<MeshFilter>();
            if (filter.sharedMesh == null || baseMaterial == null || paintedMaterial == null) return;
            paintedMesh = Instantiate(filter.sharedMesh);
            paintedMesh.name = filter.sharedMesh.name + " Painted";
            filter.sharedMesh = paintedMesh;
            vertices = paintedMesh.vertices;
            progress = paintedMesh.uv2;
            colors = new Color[paintedMesh.vertexCount];
            paintedMesh.colors = colors;

            blendMaterial = new Material(baseMaterial) { name = baseMaterial.name + " Vertex Blend" };
            // Painted surfaces own their visual transition. They must not also inherit
            // the world's organic purification replacement from the source material.
            if (blendMaterial.HasProperty("_PurificationResponse"))
                blendMaterial.SetFloat("_PurificationResponse", 0f);
            blendMaterial.SetFloat("_VertexSurfaceBlend", 1f);
            blendMaterial.SetTexture("_SecondaryMap", paintedMaterial.GetTexture("_BaseMap"));
            blendMaterial.SetColor("_SecondaryColor", paintedMaterial.HasProperty("_BaseColor")
                ? paintedMaterial.GetColor("_BaseColor") : Color.white);
            blendMaterial.SetFloat("_SecondaryEmission", paintedEmission);
            GetComponent<MeshRenderer>().sharedMaterial = blendMaterial;
        }

        public void PaintUntil(float normalizedProgress)
        {
            PaintRange(Mathf.Max(0f, paintedUntil), normalizedProgress);
            paintedUntil = Mathf.Max(paintedUntil, Mathf.Clamp01(normalizedProgress));
        }

        public void PaintRange(float fromProgress, float toProgress)
        {
            if (paintedMesh == null || progress == null || progress.Length != paintedMesh.vertexCount) return;
            float minimum = Mathf.Clamp01(Mathf.Min(fromProgress, toProgress));
            float maximum = Mathf.Clamp01(Mathf.Max(fromProgress, toProgress));
            for (int i = 0; i < colors.Length; i++)
            {
                float noise = StableNoise(vertices[i]) * noiseAmount;
                float enter = SmoothThreshold(minimum - transitionWidth + noise,
                    minimum + transitionWidth + noise, progress[i].x);
                float leave = 1f - SmoothThreshold(maximum - transitionWidth + noise,
                    maximum + transitionWidth + noise, progress[i].x);
                float mask = enter * leave;
                colors[i].r = Mathf.Max(colors[i].r, mask);
                colors[i].a = 1f;
            }
            paintedMesh.colors = colors;
        }

        public void PaintAt(float normalizedProgress, float radius = .0035f)
        {
            radius = Mathf.Max(.0005f, radius);
            PaintRange(normalizedProgress - radius, normalizedProgress + radius);
        }

        static float StableNoise(Vector3 point)
            => Mathf.PerlinNoise(point.x * 1.73f + 17.1f, point.z * 2.11f + point.y * .83f) * 2f - 1f;

        static float SmoothThreshold(float edge0, float edge1, float value)
        {
            float t = Mathf.InverseLerp(edge0, edge1, value);
            return t * t * (3f - 2f * t);
        }

        void OnDestroy()
        {
            if (paintedMesh != null)
            {
                if (Application.isPlaying) Destroy(paintedMesh); else DestroyImmediate(paintedMesh);
            }
            if (blendMaterial != null)
            {
                if (Application.isPlaying) Destroy(blendMaterial); else DestroyImmediate(blendMaterial);
            }
        }
    }
}
