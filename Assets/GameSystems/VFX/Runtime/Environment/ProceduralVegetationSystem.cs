using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace GameSystems.VFX
{
    public sealed class ProceduralVegetationSystem : MonoBehaviour
    {
        [Header("Purification")]
        [SerializeField] Transform target;
        [SerializeField, Range(.25f, 2f)] float cellSize = .55f;
        [SerializeField, Range(8, 80)] int foregroundSamplesPerCell = 68;
        [SerializeField, Range(0, 48)] int backgroundSamplesPerCell = 42;
        [SerializeField, Range(0, 40)] int ivySamplesPerCell = 28;
        [SerializeField, Range(2f, 12f)] float minimumIvyLength = 5f;
        [SerializeField, Range(6f, 30f)] float maximumIvyLength = 22f;
        [SerializeField, Range(0f, .5f)] float flowerRatio = .18f;
        [SerializeField, Range(0f, .5f)] float fernRatio = .16f;
        [SerializeField, Range(1000, 300000)] int maximumPlants = 220000;
        [SerializeField, Range(4f, 40f)] float backgroundReach = 24f;
        [SerializeField, Range(2f, 16f)] float meshChunkLength = 8f;
        [SerializeField, Range(.2f, 3f)] float movingPlatformGrowthSpeed = .9f;

        [Header("Materials")]
        [SerializeField] Material grassMaterial;
        [SerializeField] Material flowerMauveMaterial;
        [SerializeField] Material flowerYellowMaterial;
        [SerializeField] Material flowerBlueMaterial;
        [SerializeField] Material fernMaterial;

        readonly HashSet<int> visitedCells = new();
        readonly HashSet<Transform> seededMovingPlatforms = new();
        readonly Dictionary<long, PlantBatch> batches = new();
        readonly Dictionary<Transform, PlatformGrowthState> platformGrowth = new();
        Mesh[] sourceMeshes;
        Material[] materials;
        Material ivyMaterial;
        BackgroundVoxelWorld voxelWorld;
        int totalCount;

        sealed class PlantBatch
        {
            public readonly List<Matrix4x4> instances = new();
            public Mesh source;
            public Mesh combined;
            public GameObject gameObject;
            public bool dirty;
            public Transform anchor;
        }

        sealed class PlatformGrowthState
        {
            public readonly List<Vector4> points = new();
        }

        public void Configure(Transform follow, Material grass, Material mauve, Material yellow, Material blue, Material fern)
        {
            target = follow; grassMaterial = grass; flowerMauveMaterial = mauve;
            flowerYellowMaterial = yellow; flowerBlueMaterial = blue; fernMaterial = fern;
        }

        void Awake()
        {
            EnsureInitialized();
        }

        void EnsureInitialized()
        {
            if (sourceMeshes != null || grassMaterial == null || fernMaterial == null) return;
            sourceMeshes = new[] { BuildGrassMesh(), BuildFlowerMesh(5, false), BuildFlowerMesh(6, true), BuildBellFlowerMesh(), BuildFernMesh(),
                BuildIvyMesh(minimumIvyLength), BuildIvyMesh(Mathf.Lerp(minimumIvyLength, maximumIvyLength, .34f)),
                BuildIvyMesh(Mathf.Lerp(minimumIvyLength, maximumIvyLength, .68f)), BuildIvyMesh(maximumIvyLength) };
            ivyMaterial = new Material(fernMaterial) { name = "MAT_ProceduralIvy_Runtime" };
            ivyMaterial.SetFloat("_HangingGrowth", 1f);
            ivyMaterial.SetFloat("_WindStrength", .018f);
            materials = new[] { grassMaterial, flowerMauveMaterial, flowerYellowMaterial, flowerBlueMaterial, fernMaterial,
                ivyMaterial, ivyMaterial, ivyMaterial, ivyMaterial };
            voxelWorld = FindAnyObjectByType<BackgroundVoxelWorld>();
        }

        void Update()
        {
            if (target == null) return;
            EnsureInitialized();
            if (sourceMeshes == null) return;
            int centerCell = Mathf.FloorToInt(target.position.x / cellSize);
            for (int offset = -2; offset <= 2; offset++) GrowCell(centerCell + offset);
            RebuildDirtyBatches();
            UpdateMovingPlatformGrowth();
        }

        public void GrowRegion(Vector3 center, float radius, float density, int seed)
        {
            EnsureInitialized();
            if (sourceMeshes == null || radius <= 0f || density <= 0f) return;
            if (voxelWorld == null) voxelWorld = FindAnyObjectByType<BackgroundVoxelWorld>();
            if (voxelWorld == null) return;
            var random = new System.Random(seed);
            int samples = Mathf.CeilToInt(radius * radius * Mathf.Lerp(18f, 72f, Mathf.Clamp01(density)));
            for (int i = 0; i < samples; i++)
            {
                float angle = (float)random.NextDouble() * Mathf.PI * 2f;
                float distance = Mathf.Sqrt((float)random.NextDouble()) * radius;
                float x = center.x + Mathf.Cos(angle) * distance;
                float z = center.z + Mathf.Sin(angle) * distance;
                if (voxelWorld.TryGetVisibleSurface(x, z, (float)random.NextDouble(),
                        out Vector3 point, out Vector3 normal) && normal.y > .55f)
                    AddPlant(random, point, normal, false);
                if (random.NextDouble() < density * .28f &&
                    voxelWorld.TryGetVisibleVerticalSurface(x, z, (float)random.NextDouble() * .2f,
                        out point, out normal))
                    AddPlant(random, point, normal, true);
            }
            RebuildDirtyBatches();
        }

        public void ResetGeneratedVegetation()
        {
            foreach (PlantBatch batch in batches.Values)
            {
                if (batch.gameObject != null)
                {
                    if (Application.isPlaying) Destroy(batch.gameObject);
                    else DestroyImmediate(batch.gameObject);
                }
                if (batch.combined != null)
                {
                    if (Application.isPlaying) Destroy(batch.combined);
                    else DestroyImmediate(batch.combined);
                }
            }
            batches.Clear();
            for (int i = transform.childCount - 1; i >= 0; i--)
            {
                GameObject child = transform.GetChild(i).gameObject;
                if (!child.name.StartsWith("VegetationChunk_")) continue;
                if (Application.isPlaying) Destroy(child); else DestroyImmediate(child);
            }
            visitedCells.Clear();
            seededMovingPlatforms.Clear();
            platformGrowth.Clear();
            totalCount = 0;
            voxelWorld = FindAnyObjectByType<BackgroundVoxelWorld>();
        }

        void OnDestroy()
        {
            if (ivyMaterial != null) Destroy(ivyMaterial);
        }

        void GrowCell(int cell)
        {
            if (!visitedCells.Add(cell) || totalCount >= maximumPlants) return;
            var random = new System.Random(cell * 73856093 ^ 4177);
            float baseX = (cell + .5f) * cellSize;
            for (int i = 0; i < foregroundSamplesPerCell; i++)
            {
                float x = baseX + Signed(random) * cellSize * .55f;
                float z = Signed(random) * 1.35f;
                if (TryPlatformSurface(x, z, out float y, out Transform anchor))
                {
                    if (anchor != null) EnsureMovingPlatformSeeded(anchor);
                    else AddPlant(random, new Vector3(x, y, z), Vector3.up, false, null, cell);
                }
            }
            if (voxelWorld == null) voxelWorld = FindAnyObjectByType<BackgroundVoxelWorld>();
            if (voxelWorld == null) return;
            for (int i = 0; i < backgroundSamplesPerCell; i++)
            {
                float x = baseX + Signed(random) * cellSize * .8f;
                float z = Mathf.Lerp(3.1f, backgroundReach, Mathf.Pow((float)random.NextDouble(), .72f));
                if (voxelWorld.TryGetVisibleSurface(x, z, (float)random.NextDouble(), out Vector3 point, out Vector3 normal))
                    AddPlant(random, point, normal, true);
            }
            for (int i = 0; i < ivySamplesPerCell; i++)
            {
                float x = baseX + Signed(random) * cellSize * 1.15f;
                float z = Mathf.Lerp(3.1f, backgroundReach, Mathf.Pow((float)random.NextDouble(), .72f));
                // The candidate list is top-to-bottom: stay near its beginning so vines originate at upper edges.
                if (voxelWorld.TryGetVisibleVerticalSurface(x, z, (float)random.NextDouble() * .12f, out Vector3 point, out Vector3 normal))
                    AddPlant(random, point, normal, true);
            }
        }

        bool TryPlatformSurface(float x, float z, out float y, out Transform anchor)
        {
            Vector3 origin = new(x, target.position.y + 9f, z);
            RaycastHit[] hits = Physics.RaycastAll(origin, Vector3.down, 20f, ~0, QueryTriggerInteraction.Ignore);
            float best = float.NegativeInfinity;
            Transform bestAnchor = null;
            foreach (RaycastHit hit in hits)
            {
                if (hit.collider == null || hit.transform.IsChildOf(target) || hit.collider.GetComponentInParent<GameSystems.Abilities.CharacterAbilityController>() != null) continue;
                if (hit.collider.GetComponentInParent<VegetationExcludedSurface>() != null) continue;
                if (hit.normal.y < .82f || hit.point.y <= best) continue;
                best = hit.point.y;
                VegetationMovingSurface moving = hit.collider.GetComponentInParent<VegetationMovingSurface>();
                bestAnchor = moving != null ? moving.transform : null;
            }
            y = best;
            anchor = bestAnchor;
            return best > float.NegativeInfinity;
        }

        void EnsureMovingPlatformSeeded(Transform anchor)
        {
            if (anchor == null || !seededMovingPlatforms.Add(anchor)) return;
            platformGrowth[anchor] = new PlatformGrowthState();
            BoxCollider surface = anchor.GetComponentInChildren<BoxCollider>();
            if (surface == null) return;
            Bounds bounds = surface.bounds;
            var random = new System.Random(anchor.name.GetHashCode() ^ 0x4f31a7);
            const float spacing = .16f;
            int growthGroup = anchor.name.GetHashCode();
            for (float x = bounds.min.x + spacing * .45f; x <= bounds.max.x - spacing * .3f; x += spacing)
            for (float z = bounds.min.z + spacing * .45f; z <= bounds.max.z - spacing * .3f; z += spacing)
            {
                float jitterX = Signed(random) * spacing * .34f;
                float jitterZ = Signed(random) * spacing * .34f;
                Vector3 point = new(x + jitterX, bounds.max.y + .012f, z + jitterZ);
                AddPlant(random, point, Vector3.up, false, anchor, growthGroup);
            }
        }

        void AddPlant(System.Random random, Vector3 position, Vector3 surfaceNormal, bool background, Transform anchor = null, int growthGroup = 0)
        {
            if (totalCount >= maximumPlants || !Finite(position.x) || !Finite(position.y) || !Finite(position.z) || Mathf.Abs(position.y) > 1000f) return;
            float roll = (float)random.NextDouble();
            int type;
            bool verticalFace = surfaceNormal.y < .55f;
            if (verticalFace)
            {
                // Ivy colonies start at upper edges, then their connected stems cover the cliff face.
                if (roll > .48f) return;
                float lengthChoice = Mathf.Pow((float)random.NextDouble(), .7f);
                type = lengthChoice < .25f ? 5 : lengthChoice < .5f ? 6 : lengthChoice < .78f ? 7 : 8;
            }
            else if (roll < flowerRatio)
            {
                float flowerKind = (float)random.NextDouble();
                type = flowerKind < .38f ? 1 : flowerKind < .72f ? 2 : 3;
            }
            else type = roll < flowerRatio + fernRatio ? 4 : 0;
            float yaw = (float)random.NextDouble() * 360f;
            float scale = Mathf.Lerp(.56f, 1.08f, (float)random.NextDouble()) * (background ? .82f : 1f);
            if (type == 0) scale *= .82f;
            float verticalScale = scale;
            if (verticalFace)
            {
                float availableDrop = background && voxelWorld != null
                    ? voxelWorld.MeasureVerticalSurfaceDrop(position, surfaceNormal,
                        maximumIvyLength * scale)
                    : MeasureVerticalSurfaceDrop(position, surfaceNormal, maximumIvyLength * scale);
                float sourceLength = type == 5 ? minimumIvyLength : type == 6 ? Mathf.Lerp(minimumIvyLength, maximumIvyLength, .34f) :
                                     type == 7 ? Mathf.Lerp(minimumIvyLength, maximumIvyLength, .68f) : maximumIvyLength;
                if (availableDrop < .35f)
                {
                    if (background) return;
                    Vector3 dropOrigin = position + surfaceNormal * .08f + Vector3.up * .04f;
                    if (Physics.Raycast(dropOrigin, Vector3.down, out RaycastHit floorHit,
                        sourceLength * scale, ~0, QueryTriggerInteraction.Ignore) && floorHit.normal.y > .45f)
                        availableDrop = floorHit.distance;
                    else
                        availableDrop = Mathf.Min(4.5f, sourceLength * scale * .42f);
                }
                availableDrop = Mathf.Max(.25f, availableDrop - .22f);
                verticalScale = Mathf.Min(scale, availableDrop / Mathf.Max(.01f, sourceLength));
            }
            Quaternion orientation = verticalFace
                ? Quaternion.LookRotation(surfaceNormal, Vector3.up) * Quaternion.AngleAxis(Mathf.Lerp(-8f, 8f, (float)random.NextDouble()), Vector3.forward)
                : Quaternion.AngleAxis(yaw, Vector3.up);
            float surfaceOffset = verticalFace ? .045f : .012f;
            Matrix4x4 matrix = Matrix4x4.TRS(position + surfaceNormal * surfaceOffset, orientation,
                verticalFace ? new Vector3(scale, verticalScale, scale) : Vector3.one * scale);
            if (anchor != null) matrix = anchor.worldToLocalMatrix * matrix;
            int chunk = Mathf.FloorToInt(position.x / meshChunkLength);
            PlantBatch plantBatch = GetBatch(chunk, type, anchor, growthGroup);
            plantBatch.instances.Add(matrix); plantBatch.dirty = true; totalCount++;
        }

        static float MeasureVerticalSurfaceDrop(Vector3 origin, Vector3 outwardNormal, float requestedLength)
        {
            const float step = .15f;
            float lastSupported = 0f;
            int consecutiveMisses = 0;
            for (float distance = step; distance <= requestedLength; distance += step)
            {
                Vector3 probe = origin + outwardNormal * .09f - Vector3.up * distance;
                bool supported = Physics.Raycast(probe, -outwardNormal, out RaycastHit hit, .24f, ~0, QueryTriggerInteraction.Ignore) &&
                                 Mathf.Abs(Vector3.Dot(hit.normal, outwardNormal)) > .45f;
                if (supported) { lastSupported = distance; consecutiveMisses = 0; }
                else if (++consecutiveMisses >= 2) break;
            }
            return lastSupported;
        }

        PlantBatch GetBatch(int chunk, int type, Transform anchor, int growthGroup)
        {
            long anchorKey = anchor != null ? (uint)anchor.name.GetHashCode() : 0u;
            uint localKey = anchor != null
                ? unchecked((uint)(growthGroup * 73856093) ^ (uint)(type * 19349663))
                : unchecked((uint)(chunk * 73856093) ^ (uint)(type * 19349663));
            long key = (anchorKey << 32) ^ localKey;
            if (batches.TryGetValue(key, out PlantBatch existing)) return existing;
            var go = new GameObject($"VegetationChunk_{chunk}_{type}", typeof(MeshFilter), typeof(MeshRenderer));
            go.transform.SetParent(anchor != null ? anchor : transform, false);
            var mesh = new Mesh { name = go.name, indexFormat = IndexFormat.UInt32 }; mesh.MarkDynamic();
            go.GetComponent<MeshFilter>().sharedMesh = mesh;
            go.GetComponent<MeshRenderer>().sharedMaterial = materials[type];
            if (anchor != null)
            {
                var properties = new MaterialPropertyBlock();
                properties.SetFloat("_MovingPlatformSmooth", 1f);
                properties.SetFloat("_PlatformGrowthOverride", 1f);
                properties.SetFloat("_PlatformGrowthAmount", 0f);
                properties.SetFloat("_PlatformGrowthPointCount", 0f);
                go.GetComponent<MeshRenderer>().SetPropertyBlock(properties);
            }
            var created = new PlantBatch { source = sourceMeshes[type], combined = mesh, gameObject = go, dirty = true, anchor = anchor };
            batches.Add(key, created);
            return created;
        }

        void UpdateMovingPlatformGrowth()
        {
            if (target != null)
            {
                Vector3 origin = target.position + Vector3.up * .18f;
                foreach (RaycastHit hit in Physics.RaycastAll(origin, Vector3.down, .72f, ~0, QueryTriggerInteraction.Ignore))
                {
                    if (hit.collider == null || hit.transform.IsChildOf(target)) continue;
                    VegetationMovingSurface moving = hit.collider.GetComponentInParent<VegetationMovingSurface>();
                    if (moving != null && platformGrowth.TryGetValue(moving.transform, out PlatformGrowthState state))
                    {
                        Vector3 local = moving.transform.InverseTransformPoint(hit.point);
                        bool separated = state.points.Count == 0;
                        if (!separated)
                        {
                            Vector4 previous = state.points[state.points.Count - 1];
                            separated = Vector2.Distance(new Vector2(previous.x, previous.y), new Vector2(local.x, local.z)) >= .22f;
                        }
                        if (separated)
                        {
                            if (state.points.Count >= 32) state.points.RemoveAt(0);
                            state.points.Add(new Vector4(local.x, local.z, 0f, 0f));
                        }
                        break;
                    }
                }
            }

            foreach (PlatformGrowthState state in platformGrowth.Values)
                for (int i = 0; i < state.points.Count; i++)
                {
                    Vector4 point = state.points[i];
                    point.z = Mathf.MoveTowards(point.z, 1f, movingPlatformGrowthSpeed * Time.deltaTime);
                    state.points[i] = point;
                }

            foreach (PlantBatch batch in batches.Values)
            {
                if (batch.anchor == null || batch.gameObject == null || !platformGrowth.TryGetValue(batch.anchor, out PlatformGrowthState state)) continue;
                var properties = new MaterialPropertyBlock();
                properties.SetFloat("_MovingPlatformSmooth", 1f);
                properties.SetFloat("_PlatformGrowthOverride", 1f);
                properties.SetFloat("_PlatformGrowthAmount", 0f);
                properties.SetFloat("_PlatformGrowthPointCount", state.points.Count);
                if (state.points.Count > 0) properties.SetVectorArray("_PlatformGrowthPoints", state.points);
                batch.gameObject.GetComponent<MeshRenderer>().SetPropertyBlock(properties);
            }
        }

        void RebuildDirtyBatches()
        {
            foreach (PlantBatch plantBatch in batches.Values)
                if (plantBatch.dirty) Rebuild(plantBatch);
        }

        static void Rebuild(PlantBatch plantBatch)
        {
            Vector3[] sourceVertices = plantBatch.source.vertices;
            Vector3[] sourceNormals = plantBatch.source.normals;
            Color[] sourceColors = plantBatch.source.colors;
            int[] sourceTriangles = plantBatch.source.triangles;
            int vertexCount = sourceVertices.Length * plantBatch.instances.Count;
            var vertices = new List<Vector3>(vertexCount);
            var normals = new List<Vector3>(vertexCount);
            var colors = new List<Color>(vertexCount);
            var roots = new List<Vector3>(vertexCount);
            var triangles = new List<int>(sourceTriangles.Length * plantBatch.instances.Count);
            foreach (Matrix4x4 matrix in plantBatch.instances)
            {
                int offset = vertices.Count;
                Vector3 root = matrix.MultiplyPoint3x4(Vector3.zero);
                for (int i = 0; i < sourceVertices.Length; i++)
                {
                    vertices.Add(matrix.MultiplyPoint3x4(sourceVertices[i]));
                    normals.Add(matrix.MultiplyVector(sourceNormals[i]).normalized);
                    colors.Add(sourceColors[i]); roots.Add(root);
                }
                for (int i = 0; i < sourceTriangles.Length; i++) triangles.Add(offset + sourceTriangles[i]);
            }
            plantBatch.combined.Clear();
            plantBatch.combined.SetVertices(vertices); plantBatch.combined.SetNormals(normals); plantBatch.combined.SetColors(colors);
            plantBatch.combined.SetUVs(1, roots); plantBatch.combined.SetTriangles(triangles, 0); plantBatch.combined.RecalculateBounds();
            Bounds bounds = plantBatch.combined.bounds; bounds.Expand(.3f); plantBatch.combined.bounds = bounds;
            plantBatch.dirty = false;
        }

        static Mesh BuildGrassMesh()
        {
            var v = new List<Vector3>(); var t = new List<int>(); var c = new List<Color>();
            for (int blade = 0; blade < 4; blade++)
            {
                float angle = blade * 1.91f + .37f; Vector3 side = new(Mathf.Cos(angle), 0, Mathf.Sin(angle)); int s = v.Count;
                Vector3 baseOffset = new(Mathf.Sin(blade * 2.43f) * .075f, 0f, Mathf.Cos(blade * 1.77f) * .065f);
                float height = .14f + (blade % 3) * .045f;
                v.Add(baseOffset - side * .032f); v.Add(baseOffset + side * .032f); v.Add(baseOffset + side * (.018f + blade * .004f) + Vector3.up * height);
                c.Add(Color.black); c.Add(Color.black); c.Add(Color.white); t.Add(s); t.Add(s+1); t.Add(s+2);
            }
            return FinishMesh("Grass Tuft", v, t, c);
        }

        static Mesh BuildFlowerMesh(int petals, bool star)
        {
            var v = new List<Vector3> { new(-.018f,0,0), new(.018f,0,0), new(0,.42f,0) };
            var t = new List<int> { 0,1,2 }; var c = new List<Color> { Color.black, Color.black, new(.72f,0,0) };
            for (int petal = 0; petal < petals; petal++)
            {
                float a = petal * Mathf.PI * 2f / petals; Vector3 d = new(Mathf.Cos(a), 0, Mathf.Sin(a)); int s = v.Count;
                float length = star ? .13f : .095f;
                v.Add(Vector3.up * .4f); v.Add(Vector3.up * (star ? .435f : .46f) + d * length); v.Add(Vector3.up * .49f + d * .028f);
                c.Add(Color.white); c.Add(Color.white); c.Add(Color.white); t.Add(s); t.Add(s+1); t.Add(s+2);
            }
            return FinishMesh(star ? "Star Flower" : "Round Flower", v, t, c);
        }

        static Mesh BuildBellFlowerMesh()
        {
            var v = new List<Vector3>(); var t = new List<int>(); var c = new List<Color>();
            for (int side = -1; side <= 1; side += 2)
            {
                int s = v.Count; v.Add(new(-.014f,0,0)); v.Add(new(.014f,0,0)); v.Add(new(side*.08f,.36f,0));
                c.Add(Color.black); c.Add(Color.black); c.Add(new(.68f,0,0)); t.Add(s); t.Add(s+1); t.Add(s+2);
                s = v.Count; v.Add(new(side*.08f,.34f,-.055f)); v.Add(new(side*.08f,.34f,.055f)); v.Add(new(side*.12f,.25f,0));
                c.Add(Color.white); c.Add(Color.white); c.Add(Color.white); t.Add(s); t.Add(s+1); t.Add(s+2);
            }
            return FinishMesh("Bell Flowers", v, t, c);
        }

        static Mesh BuildFernMesh()
        {
            var v = new List<Vector3>(); var t = new List<int>(); var c = new List<Color>();
            for (int frond = 0; frond < 3; frond++)
            {
                float angle = frond == 0 ? -.68f : frond == 1 ? .14f : .86f;
                int leafCount = frond == 0 ? 5 : frond == 1 ? 7 : 4;
                float frondScale = frond == 0 ? .9f : frond == 1 ? 1.08f : .76f;
                for (int side = -1; side <= 1; side += 2)
                for (int leaf = 0; leaf < leafCount; leaf++)
                {
                    float y = (.048f + leaf * .055f) * frondScale; float length = (.205f - leaf * .021f) * frondScale; int s = v.Count;
                    float asymmetry = Mathf.Sin(leaf * 2.17f + frond * 1.31f) * .025f;
                    Vector3 center = new(Mathf.Sin(angle) * y + asymmetry, y, Mathf.Cos(angle) * y * .22f);
                    v.Add(center); v.Add(center + new Vector3(side * length, .024f + (leaf % 2) * .018f, .018f + asymmetry)); v.Add(center + Vector3.up * (.045f + frond * .006f));
                    float g = y / .42f; c.Add(new(g,0,0)); c.Add(new(g+.15f,0,0)); c.Add(new(g+.08f,0,0)); t.Add(s); t.Add(s+1); t.Add(s+2);
                }
            }
            return FinishMesh("Wide Fern", v, t, c);
        }

        static Mesh BuildIvyMesh(float requestedLength)
        {
            var v = new List<Vector3>(); var t = new List<int>(); var c = new List<Color>();
            void Segment(Vector2 a, Vector2 b, float width, float growth)
            {
                Vector2 direction = (b - a).normalized;
                Vector2 side = new(-direction.y, direction.x); int s = v.Count;
                v.Add(new(a.x + side.x * width, a.y + side.y * width, .006f));
                v.Add(new(a.x - side.x * width, a.y - side.y * width, .006f));
                v.Add(new(b.x - side.x * width, b.y - side.y * width, .008f));
                v.Add(new(b.x + side.x * width, b.y + side.y * width, .008f));
                for (int i = 0; i < 4; i++) c.Add(new(growth, 0, 0));
                t.Add(s); t.Add(s + 1); t.Add(s + 2); t.Add(s); t.Add(s + 2); t.Add(s + 3);
            }
            void Leaf(Vector2 p, float size, float growth, bool flip)
            {
                int s = v.Count; float direction = flip ? -1f : 1f;
                v.Add(new(p.x, p.y, .012f));
                v.Add(new(p.x + direction * size, p.y + size * .38f, .014f));
                v.Add(new(p.x + direction * size * .72f, p.y - size, .014f));
                // Green channel marks leaves: the shader grows stems first, foliage second.
                c.Add(new(growth,1,0)); c.Add(new(growth,1,0)); c.Add(new(growth * .86f,1,0));
                t.Add(s); t.Add(s + 1); t.Add(s + 2);
            }
            void Flower(Vector2 p, float size, float growth)
            {
                for (int petal = 0; petal < 4; petal++)
                {
                    float angle = petal * Mathf.PI * .5f + .785f;
                    Vector2 direction = new(Mathf.Cos(angle), Mathf.Sin(angle));
                    int s = v.Count;
                    v.Add(new(p.x, p.y, .021f));
                    v.Add(new(p.x + direction.x * size, p.y + direction.y * size, .023f));
                    v.Add(new(p.x + Mathf.Cos(angle + .55f) * size * .56f, p.y + Mathf.Sin(angle + .55f) * size * .56f, .024f));
                    c.Add(new Color(growth, 1, 0, .25f)); c.Add(new Color(growth, 1, 0, .25f)); c.Add(new Color(growth, 1, 0, .25f));
                    t.Add(s); t.Add(s + 1); t.Add(s + 2);
                }
            }

            // Three long parent stems. Each one descends from the top edge and forks repeatedly.
            for (int strand = 0; strand < 3; strand++)
            {
                float rootX = (strand - 1f) * .34f;
                Vector2 previous = new(rootX, .035f);
                int sections = 12 + strand * 2;
                for (int section = 1; section <= sections; section++)
                {
                    float depth = section * requestedLength / sections;
                    Vector2 next = new(rootX + Mathf.Sin(section * 1.17f + strand * 2.1f) * (.08f + section * .009f), -depth);
                    float growth = 1f - section / (float)(sections + 1);
                    Segment(previous, next, Mathf.Lerp(.014f, .007f, section / (float)sections), growth);
                    if (section > 1)
                    {
                        Leaf(next, .105f + (section % 3) * .018f, growth, (section + strand) % 2 == 0);
                        if (section % 3 == 0)
                            Leaf(next + new Vector2((section % 2 == 0 ? 1f : -1f) * .035f, -.025f), .09f, growth, (section + strand) % 2 != 0);
                        if ((section + strand * 3) % 11 == 0) Flower(next, .052f, growth);
                    }

                    if (section == 3 || section == 6 || (section == 9 && strand != 1))
                    {
                        float branchDirection = (strand + section) % 2 == 0 ? 1f : -1f;
                        Vector2 branchPrevious = next;
                        int branchSections = section == 6 ? 5 : 4;
                        for (int branch = 1; branch <= branchSections; branch++)
                        {
                            Vector2 branchEnd = next + new Vector2(branchDirection * branch * (.14f + strand * .018f), -branch * .075f + Mathf.Sin(branch * 1.8f) * .035f);
                            float branchGrowth = growth * (1f - branch / (float)(branchSections + 2));
                            Segment(branchPrevious, branchEnd, .0075f, branchGrowth);
                            if (branch > 0) Leaf(branchEnd, .092f + branch * .009f, branchGrowth, branchDirection < 0f);
                            branchPrevious = branchEnd;
                        }
                    }
                    previous = next;
                }
            }
            return FinishMesh($"Branching Hanging Ivy {requestedLength:0.#}m", v, t, c);
        }

        static Mesh FinishMesh(string name, List<Vector3> vertices, List<int> triangles, List<Color> colors)
        {
            var mesh = new Mesh { name = name }; mesh.SetVertices(vertices); mesh.SetTriangles(triangles, 0); mesh.SetColors(colors);
            mesh.RecalculateNormals(); mesh.RecalculateBounds(); return mesh;
        }

        static float Signed(System.Random random) => (float)random.NextDouble() * 2f - 1f;
        static bool Finite(float value) => !float.IsNaN(value) && !float.IsInfinity(value);
    }
}
