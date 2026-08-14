using System.Collections.Generic;
using UnityEngine;

namespace GameSystems.VFX
{
    public static class SplineRibbonMeshGenerator
    {
        public static Mesh Build(IReadOnlyList<Vector3> points, float width, float thickness,
            float textureTiling = .28f, string meshName = "Spline Ribbon")
        {
            int count = points?.Count ?? 0;
            if (count < 2) return new Mesh { name = meshName };

            var vertices = new Vector3[count * 4];
            var uv = new Vector2[vertices.Length];
            var progressUv = new Vector2[vertices.Length];
            var triangles = new int[(count - 1) * 24 + 12];
            Vector3 previousSide = Vector3.forward;
            float distance = 0f;
            for (int i = 0; i < count; i++)
            {
                if (i > 0) distance += Vector3.Distance(points[i - 1], points[i]);
                Vector3 tangent = i == 0 ? points[1] - points[0] :
                    i == count - 1 ? points[count - 1] - points[count - 2] :
                    points[i + 1] - points[i - 1];
                tangent.Normalize();
                Vector3 side = Vector3.ProjectOnPlane(Vector3.forward, tangent);
                if (side.sqrMagnitude < .001f) side = previousSide;
                side.Normalize();
                if (i > 0 && Vector3.Dot(side, previousSide) < 0f) side = -side;
                previousSide = side;
                Vector3 normal = Vector3.Cross(side, tangent).normalized;
                int v = i * 4;
                vertices[v] = points[i] + side * width * .5f;
                vertices[v + 1] = points[i] - side * width * .5f;
                vertices[v + 2] = vertices[v] - normal * thickness;
                vertices[v + 3] = vertices[v + 1] - normal * thickness;
                float progress = i / (float)(count - 1);
                for (int j = 0; j < 4; j++)
                {
                    uv[v + j] = new Vector2(distance * textureTiling, j % 2 == 0 ? 1f : 0f);
                    progressUv[v + j] = new Vector2(progress, 0f);
                }
            }

            int index = 0;
            for (int i = 0; i < count - 1; i++)
            {
                int a = i * 4;
                int b = (i + 1) * 4;
                AddQuad(triangles, ref index, a, b, a + 1, b + 1);
                AddQuad(triangles, ref index, a + 3, b + 3, a + 2, b + 2);
                AddQuad(triangles, ref index, a + 2, b + 2, a, b);
                AddQuad(triangles, ref index, a + 1, b + 1, a + 3, b + 3);
            }
            AddQuad(triangles, ref index, 2, 0, 3, 1);
            int end = (count - 1) * 4;
            AddQuad(triangles, ref index, end, end + 2, end + 1, end + 3);

            var mesh = new Mesh { name = meshName };
            mesh.vertices = vertices;
            mesh.uv = uv;
            mesh.uv2 = progressUv;
            mesh.triangles = triangles;
            mesh.RecalculateNormals();
            mesh.RecalculateTangents();
            mesh.RecalculateBounds();
            return mesh;
        }

        static void AddQuad(int[] triangles, ref int index, int a, int b, int c, int d)
        {
            triangles[index++] = a; triangles[index++] = b; triangles[index++] = c;
            triangles[index++] = c; triangles[index++] = b; triangles[index++] = d;
        }
    }
}
