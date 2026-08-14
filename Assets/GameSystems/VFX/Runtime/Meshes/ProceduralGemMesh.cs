using System.Collections.Generic;
using UnityEngine;

namespace GameSystems.VFX
{
    [ExecuteAlways, DisallowMultipleComponent]
    [RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
    public sealed class ProceduralGemMesh : MonoBehaviour
    {
        [SerializeField, Range(0, 2)] int variant;
        Mesh generatedMesh;

        public void Configure(int value)
        {
            variant = Mathf.Abs(value) % 3;
            Rebuild();
        }

        void OnEnable() => Rebuild();
        void OnValidate() => Rebuild();

        void Rebuild()
        {
            int sides = variant switch { 0 => 6, 1 => 5, _ => 8 };
            float width = variant switch { 0 => .34f, 1 => .38f, _ => .31f };
            float height = variant switch { 0 => .72f, 1 => .64f, _ => .78f };
            float twist = variant == 1 ? 18f : variant == 2 ? 11.25f : 0f;
            var vertices = new List<Vector3>(sides * 18);
            var uv = new List<Vector2>(sides * 18);
            var triangles = new List<int>(sides * 18);
            for (int i = 0; i < sides; i++)
            {
                float a = i * Mathf.PI * 2f / sides;
                float b = (i + 1) * Mathf.PI * 2f / sides;
                float twistRadians = twist * Mathf.Deg2Rad;
                Vector3 upperA = Ring(a, width, height * .22f);
                Vector3 upperB = Ring(b, width, height * .22f);
                Vector3 lowerA = Ring(a + twistRadians, width * .68f, -height * .2f);
                Vector3 lowerB = Ring(b + twistRadians, width * .68f, -height * .2f);
                AddTriangle(vertices, uv, triangles, Vector3.up * height, upperA, upperB);
                AddTriangle(vertices, uv, triangles, upperA, lowerA, upperB);
                AddTriangle(vertices, uv, triangles, upperB, lowerA, lowerB);
                AddTriangle(vertices, uv, triangles, lowerA, -Vector3.up * height, lowerB);
            }
            if (generatedMesh == null)
                generatedMesh = new Mesh { name = "Procedural Gem" };
            else generatedMesh.Clear();
            generatedMesh.SetVertices(vertices);
            generatedMesh.SetUVs(0, uv);
            generatedMesh.SetTriangles(triangles, 0);
            generatedMesh.RecalculateNormals();
            generatedMesh.RecalculateBounds();
            GetComponent<MeshFilter>().sharedMesh = generatedMesh;
        }

        static Vector3 Ring(float angle, float radius, float y) =>
            new(Mathf.Cos(angle) * radius, y, Mathf.Sin(angle) * radius);

        static void AddTriangle(List<Vector3> vertices, List<Vector2> uv, List<int> triangles,
            Vector3 a, Vector3 b, Vector3 c)
        {
            int start = vertices.Count;
            vertices.Add(a); vertices.Add(b); vertices.Add(c);
            uv.Add(new Vector2(.5f, 1f)); uv.Add(Vector2.zero); uv.Add(Vector2.right);
            triangles.Add(start); triangles.Add(start + 1); triangles.Add(start + 2);
        }

        void OnDestroy()
        {
            if (generatedMesh == null) return;
            if (Application.isPlaying) Destroy(generatedMesh);
            else DestroyImmediate(generatedMesh);
        }
    }
}
