using UnityEngine;

namespace GameSystems.VFX
{
    [ExecuteAlways]
    public sealed class ProceduralGodRayVolumes : MonoBehaviour
    {
        [SerializeField, Range(3, 32)] int maximumRays = 14;
        [SerializeField, Range(.02f, .2f)] float opacity = .072f;
        Material material;

        void OnEnable()
        {
            if (transform.childCount == 0) Build();
        }

        [ContextMenu("Regenerate Volumetric God Rays")]
        public void Build()
        {
            while (transform.childCount > 0)
            {
                GameObject child = transform.GetChild(0).gameObject;
                if (Application.isPlaying) Destroy(child); else DestroyImmediate(child);
            }
            Shader shader = Shader.Find("Lennie/PS1 God Ray Volume");
            if (shader == null) return;
            material = new Material(shader) { name = "MAT_GodRays3D_Runtime" };
            material.SetColor("_Color", new Color(1f, .76f, .34f, 1f)); material.SetFloat("_Opacity", opacity);

            Bounds bounds = CalculateVisibleBounds();
            int count = Mathf.Clamp(Mathf.CeilToInt(bounds.size.x / 13f), 4, maximumRays);
            var random = new System.Random(9417);
            for (int i = 0; i < count; i++)
            {
                float x = Mathf.Lerp(bounds.min.x, bounds.max.x, (i + .45f) / count) + Signed(random) * 3.2f;
                float z = Mathf.Lerp(2.5f, 15f, (float)random.NextDouble());
                float width = Mathf.Lerp(2.2f, 5.8f, (float)random.NextDouble());
                float length = Mathf.Lerp(17f, 29f, (float)random.NextDouble());
                var ray = new GameObject($"GodRayVolume_{i:00}", typeof(MeshFilter), typeof(MeshRenderer));
                ray.transform.SetParent(transform, false); ray.transform.position = new Vector3(x, bounds.max.y + 8f, z);
                ray.transform.rotation = Quaternion.Euler(0f, 0f, Mathf.Lerp(12f, 23f, (float)random.NextDouble()));
                ray.GetComponent<MeshFilter>().sharedMesh = BuildCrossedBeam(width, length);
                ray.GetComponent<MeshRenderer>().sharedMaterial = material;
            }
        }

        static Mesh BuildCrossedBeam(float width, float length)
        {
            var vertices = new Vector3[12]; var colors = new Color[12]; var uv = new Vector2[12]; var triangles = new int[18];
            for (int plane = 0; plane < 3; plane++)
            {
                float angle = plane * Mathf.PI / 3f; Vector3 side = new(Mathf.Cos(angle), 0f, Mathf.Sin(angle)); int v = plane * 4;
                vertices[v] = -side * width * .22f; vertices[v+1] = side * width * .22f;
                vertices[v+2] = side * width - Vector3.up * length; vertices[v+3] = -side * width - Vector3.up * length;
                uv[v] = new(0,0); uv[v+1] = new(1,0); uv[v+2] = new(1,1); uv[v+3] = new(0,1);
                colors[v] = colors[v+1] = new Color(1,1,1,.55f); colors[v+2] = colors[v+3] = Color.white;
                int t = plane * 6; triangles[t]=v; triangles[t+1]=v+1; triangles[t+2]=v+2; triangles[t+3]=v; triangles[t+4]=v+2; triangles[t+5]=v+3;
            }
            var mesh = new Mesh { name = "Crossed Volumetric Light Beam" }; mesh.vertices = vertices; mesh.colors = colors; mesh.uv = uv; mesh.triangles = triangles; mesh.RecalculateBounds(); return mesh;
        }

        static Bounds CalculateVisibleBounds()
        {
            Renderer[] renderers = FindObjectsByType<Renderer>();
            if (renderers.Length == 0)
                return new Bounds(Vector3.zero, new Vector3(80f, 20f, 8f));
            Bounds bounds = renderers[0].bounds;
            for (int i = 1; i < renderers.Length; i++) bounds.Encapsulate(renderers[i].bounds);
            return bounds;
        }

        static float Signed(System.Random random) => (float)random.NextDouble() * 2f - 1f;
        void OnDestroy() { if (material != null) { if (Application.isPlaying) Destroy(material); else DestroyImmediate(material); } }
    }
}
