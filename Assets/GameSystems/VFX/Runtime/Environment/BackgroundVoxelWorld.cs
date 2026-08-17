using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace GameSystems.VFX
{
    public readonly struct VoxelCavityVolume
    {
        public readonly Bounds Inner;
        public readonly Bounds Outer;

        public VoxelCavityVolume(Bounds inner, Bounds outer)
        {
            Inner = inner;
            Outer = outer;
        }
    }

    [ExecuteAlways]
    public sealed class BackgroundVoxelWorld : MonoBehaviour
    {
        [Header("World Extent")]
        [SerializeField, Range(24, 96)] int width = 56;
        [SerializeField, Range(16, 128)] int depth = 36;
        [SerializeField, Range(8, 30)] int maximumHeight = 18;
        [SerializeField, Range(4, 16)] int chunkSize = 8;
        [SerializeField, Range(0.6f, 2f)] float voxelSize = 1.15f;
        [SerializeField] int seed = 4127;

        [Header("Ground Shape")]
        [SerializeField, Range(0.01f, 0.25f)] float groundNoiseScale = 0.075f;
        [SerializeField, Range(0, 5)] int groundRoughness = 3;
        [Header("Rising Structures")]
        [SerializeField, Range(0f, 1f)] float structureDensity = 0.48f;
        [SerializeField, Range(2f, 20f)] float nearStructureHeight = 5f;
        [SerializeField, Range(4f, 28f)] float farStructureHeight = 15f;
        [SerializeField, Range(0f, 1f)] float distanceGrowth = 0.78f;
        [SerializeField, Range(0f, 0.8f)] float brokenTopAmount = 0.42f;
        [SerializeField, Range(0f, 0.6f)] float holesAmount = 0.18f;
        [SerializeField, Range(3, 32)] int structureStartDepth = 5;
        [Header("Gameplay Clearance")]
        [SerializeField, Min(0f)] float geometryHorizontalPadding = 3.5f;
        [SerializeField, Min(0f)] float geometryVerticalClearance = 2f;
        [SerializeField, Min(0f)] float visibilityDepthBehindGeometry = 14f;
        [SerializeField] Material material;
        readonly List<Bounds> levelGeometry = new();
        readonly List<VoxelCavityVolume> cavityVolumes = new();
        float terrainBaseWorldY;
        float gameplayBackZ;
        bool surroundGameplay;
        float surroundingRadius;
        float surroundingStructureDistance;
        float surroundingNearHeight;
        int[,] groundHeights;
        bool[,,] occupancy;

        static readonly Vector3Int[] Neighbours =
        {
            Vector3Int.left, Vector3Int.right, Vector3Int.down,
            Vector3Int.up, new(0, 0, -1), new(0, 0, 1)
        };

        static readonly Vector3[,] FaceVertices =
        {
            { new(-.5f,-.5f,-.5f), new(-.5f,-.5f,.5f), new(-.5f,.5f,.5f), new(-.5f,.5f,-.5f) },
            { new(.5f,-.5f,.5f), new(.5f,-.5f,-.5f), new(.5f,.5f,-.5f), new(.5f,.5f,.5f) },
            { new(-.5f,-.5f,.5f), new(-.5f,-.5f,-.5f), new(.5f,-.5f,-.5f), new(.5f,-.5f,.5f) },
            { new(-.5f,.5f,-.5f), new(-.5f,.5f,.5f), new(.5f,.5f,.5f), new(.5f,.5f,-.5f) },
            { new(.5f,-.5f,-.5f), new(-.5f,-.5f,-.5f), new(-.5f,.5f,-.5f), new(.5f,.5f,-.5f) },
            { new(-.5f,-.5f,.5f), new(.5f,-.5f,.5f), new(.5f,.5f,.5f), new(-.5f,.5f,.5f) }
        };

        static readonly Vector3[] FaceNormals =
        {
            Vector3.left, Vector3.right, Vector3.down, Vector3.up, Vector3.back, Vector3.forward
        };

        public void Configure(Material concrete, int newSeed = 4127)
        {
            material = concrete;
            seed = newSeed;
            Generate();
        }

        public void FitToPlayableLevel(float startX, float endX, float highestPlatform,
            IReadOnlyList<Bounds> geometry = null, float worldSurroundRadius = 0f,
            float nearSceneryDistance = 0f, float nearSceneryHeightOverride = 0f,
            IReadOnlyList<VoxelCavityVolume> cavities = null)
        {
            levelGeometry.Clear();
            cavityVolumes.Clear();
            if (cavities != null)
                for (int i = 0; i < cavities.Count; i++) cavityVolumes.Add(cavities[i]);
            float lowestPlatform = highestPlatform;
            float furthestBack = transform.position.z;
            if (geometry != null)
                for (int i = 0; i < geometry.Count; i++)
                {
                    Bounds bounds = geometry[i];
                    if (bounds.size.sqrMagnitude <= .0001f) continue;
                    levelGeometry.Add(bounds);
                    lowestPlatform = Mathf.Min(lowestPlatform, bounds.min.y);
                    highestPlatform = Mathf.Max(highestPlatform, bounds.max.y);
                    furthestBack = Mathf.Max(furthestBack, bounds.max.z);
                }
            float span = Mathf.Max(48f, endX - startX + 56f);
            voxelSize = Mathf.Max(1.15f, span / 512f);
            width = Mathf.Clamp(Mathf.CeilToInt(span / voxelSize), 24, 512);
            chunkSize = width > 240 ? 16 : 8;
            surroundGameplay = worldSurroundRadius > 0f;
            surroundingRadius = Mathf.Max(0f, worldSurroundRadius);
            surroundingStructureDistance = Mathf.Max(0f, nearSceneryDistance);
            surroundingNearHeight = Mathf.Max(0f, nearSceneryHeightOverride);
            depth = surroundGameplay
                ? Mathf.Clamp(Mathf.CeilToInt(surroundingRadius * 2f / voxelSize), 40, 128)
                : Mathf.Clamp(Mathf.RoundToInt(46f + span * .1f), 40, 80);
            terrainBaseWorldY = lowestPlatform - 8f;
            maximumHeight = Mathf.Clamp(Mathf.CeilToInt((highestPlatform - terrainBaseWorldY + 18f) / voxelSize), 12, 72);
            gameplayBackZ = furthestBack + 6f;
            Vector3 position = transform.position;
            position.x = (startX + endX) * .5f;
            position.y = terrainBaseWorldY;
            position.z = surroundGameplay
                ? -depth * voxelSize * .5f
                : -depth * voxelSize * .32f;
            transform.position = position;
            BuildGroundHeightMap();
            Generate();
        }

        void BuildGroundHeightMap()
        {
            groundHeights = new int[width, depth];
            for (int z = 0; z < depth; z++)
            for (int x = 0; x < width; x++)
            {
                Vector3 worldCenter = transform.TransformPoint(new Vector3(
                    (x - width * .5f + .5f) * voxelSize, 0f, (z + .5f) * voxelSize));
                float broad = Mathf.PerlinNoise((x + seed) * groundNoiseScale,
                    (z + seed * .31f) * groundNoiseScale);
                float groundTop = terrainBaseWorldY + 2.5f + broad * groundRoughness * voxelSize;
                float supportedTop = float.PositiveInfinity;
                for (int i = 0; i < levelGeometry.Count; i++)
                {
                    Bounds bounds = levelGeometry[i];
                    float dx = Mathf.Max(bounds.min.x - worldCenter.x, 0f, worldCenter.x - bounds.max.x);
                    float dz = Mathf.Max(bounds.min.z - worldCenter.z, 0f, worldCenter.z - bounds.max.z);
                    float distance = Mathf.Sqrt(dx * dx + dz * dz);
                    if (distance > 7f) continue;
                    float support = bounds.min.y - Mathf.Max(geometryVerticalClearance, voxelSize);
                    float blend = 1f - Mathf.SmoothStep(0f, 1f, distance / 7f);
                    supportedTop = Mathf.Min(supportedTop, Mathf.Lerp(groundTop, support, blend));
                }
                if (!float.IsPositiveInfinity(supportedTop)) groundTop = Mathf.Min(groundTop, supportedTop);
                groundHeights[x, z] = Mathf.Clamp(
                    Mathf.FloorToInt((groundTop - terrainBaseWorldY) / voxelSize), 1, maximumHeight);
            }
        }

        public bool TryGetSurface(float worldX, float worldZ, out float worldY)
        {
            Vector3 local = transform.InverseTransformPoint(new Vector3(worldX, transform.position.y, worldZ));
            int x = Mathf.FloorToInt(local.x / voxelSize + width * .5f);
            int z = Mathf.FloorToInt(local.z / voxelSize);
            for (int y = maximumHeight - 1; y >= 0; y--)
            {
                if (!Occupied(x, y, z)) continue;
                worldY = transform.TransformPoint(new Vector3(local.x, (y + 1) * voxelSize, local.z)).y;
                return true;
            }
            worldY = 0f;
            return false;
        }

        public bool TryGetVisibleSurface(float worldX, float worldZ, float selector, out Vector3 position, out Vector3 normal)
            => TryGetVisibleSurfaceInternal(worldX, worldZ, selector, false, out position, out normal);

        public bool TryGetVisibleVerticalSurface(float worldX, float worldZ, float selector, out Vector3 position, out Vector3 normal)
            => TryGetVisibleSurfaceInternal(worldX, worldZ, selector, true, out position, out normal);

        public float MeasureVerticalSurfaceDrop(Vector3 origin, Vector3 outwardNormal, float maximumDrop)
        {
            float step = Mathf.Max(.12f, voxelSize * .35f);
            float supported = 0f;
            int misses = 0;
            for (float distance = step; distance <= maximumDrop; distance += step)
            {
                Vector3 probe = origin - Vector3.up * distance - outwardNormal * voxelSize * .2f;
                Vector3 local = transform.InverseTransformPoint(probe);
                int x = Mathf.FloorToInt(local.x / voxelSize + width * .5f);
                int y = Mathf.FloorToInt(local.y / voxelSize);
                int z = Mathf.FloorToInt(local.z / voxelSize);
                if (Occupied(x, y, z)) { supported = distance; misses = 0; }
                else if (++misses >= 2) break;
            }
            return supported;
        }

        bool TryGetVisibleSurfaceInternal(float worldX, float worldZ, float selector, bool verticalOnly, out Vector3 position, out Vector3 normal)
        {
            Vector3 local = transform.InverseTransformPoint(new Vector3(worldX, transform.position.y, worldZ));
            int x = Mathf.FloorToInt(local.x / voxelSize + width * .5f);
            int z = Mathf.FloorToInt(local.z / voxelSize);
            var candidates = new List<(Vector3 point, Vector3 direction)>(24);
            Vector3[] directions = verticalOnly
                ? new[] { Vector3.left, Vector3.right, Vector3.back, Vector3.forward }
                : new[] { Vector3.up, Vector3.left, Vector3.right, Vector3.back, Vector3.forward };
            Vector3Int[] neighbours = verticalOnly
                ? new[] { Vector3Int.left, Vector3Int.right, new Vector3Int(0,0,-1), new Vector3Int(0,0,1) }
                : new[] { Vector3Int.up, Vector3Int.left, Vector3Int.right, new Vector3Int(0,0,-1), new Vector3Int(0,0,1) };
            for (int y = maximumHeight - 1; y >= 0; y--)
            {
                if (!Occupied(x, y, z)) continue;
                Vector3 center = new((x - width * .5f + .5f) * voxelSize, (y + .5f) * voxelSize, (z + .5f) * voxelSize);
                for (int face = 0; face < directions.Length; face++)
                {
                    Vector3Int neighbour = new Vector3Int(x, y, z) + neighbours[face];
                    if (!Occupied(neighbour.x, neighbour.y, neighbour.z))
                        candidates.Add((center + directions[face] * voxelSize * .505f, directions[face]));
                }
            }
            if (candidates.Count == 0) { position = Vector3.zero; normal = Vector3.up; return false; }
            int selected = Mathf.Clamp(Mathf.FloorToInt(Mathf.Repeat(selector, 1f) * candidates.Count), 0, candidates.Count - 1);
            position = transform.TransformPoint(candidates[selected].point);
            normal = transform.TransformDirection(candidates[selected].direction).normalized;
            return true;
        }

        void OnEnable()
        {
            if (material != null && (transform.childCount == 0 || transform.GetChild(0).GetComponent<MeshFilter>()?.sharedMesh == null))
                Generate();
        }

        [ContextMenu("Generate Voxel Background")]
        public void Generate()
        {
            if (material == null) return;
            while (transform.childCount > 0)
            {
                GameObject child = transform.GetChild(0).gameObject;
                child.transform.SetParent(null, true);
                if (Application.isPlaying) Destroy(child); else DestroyImmediate(child);
            }

            BuildOccupancyMap();

            int chunksX = Mathf.CeilToInt(width / (float)chunkSize);
            int chunksZ = Mathf.CeilToInt(depth / (float)chunkSize);
            for (int cz = 0; cz < chunksZ; cz++)
            for (int cx = 0; cx < chunksX; cx++)
                BuildChunk(cx, cz);
        }

        void BuildOccupancyMap()
        {
            occupancy = new bool[width, maximumHeight, depth];
            for (int z = 0; z < depth; z++)
            for (int x = 0; x < width; x++)
            for (int y = 0; y < maximumHeight; y++)
                occupancy[x, y, z] = CalculateOccupied(x, y, z);
        }

        void BuildChunk(int chunkX, int chunkZ)
        {
            var vertices = new List<Vector3>(4096);
            var normals = new List<Vector3>(4096);
            var uvs = new List<Vector2>(4096);
            var triangles = new List<int>(6144);
            int minX = chunkX * chunkSize;
            int minZ = chunkZ * chunkSize;
            int maxX = Mathf.Min(minX + chunkSize, width);
            int maxZ = Mathf.Min(minZ + chunkSize, depth);

            for (int z = minZ; z < maxZ; z++)
            for (int x = minX; x < maxX; x++)
            for (int y = 0; y < maximumHeight; y++)
            {
                if (!Occupied(x, y, z)) continue;
                for (int face = 0; face < 6; face++)
                {
                    Vector3Int neighbour = new Vector3Int(x, y, z) + Neighbours[face];
                    if (Occupied(neighbour.x, neighbour.y, neighbour.z)) continue;
                    int start = vertices.Count;
                    Vector3 center = new((x - width * .5f + .5f) * voxelSize, (y + .5f) * voxelSize, (z + .5f) * voxelSize);
                    for (int i = 0; i < 4; i++)
                    {
                        vertices.Add(center + FaceVertices[face, i] * voxelSize);
                        normals.Add(FaceNormals[face]);
                        uvs.Add(i switch { 0 => Vector2.zero, 1 => Vector2.right, 2 => Vector2.one, _ => Vector2.up });
                    }
                    triangles.Add(start); triangles.Add(start + 1); triangles.Add(start + 2);
                    triangles.Add(start); triangles.Add(start + 2); triangles.Add(start + 3);
                }
            }

            if (vertices.Count == 0) return;
            var chunk = new GameObject($"VoxelChunk_{chunkX}_{chunkZ}", typeof(MeshFilter), typeof(MeshRenderer));
            chunk.transform.SetParent(transform, false);
            var mesh = new Mesh { name = $"VoxelBackground_{chunkX}_{chunkZ}", indexFormat = vertices.Count > 65000 ? IndexFormat.UInt32 : IndexFormat.UInt16 };
            mesh.SetVertices(vertices); mesh.SetNormals(normals); mesh.SetUVs(0, uvs); mesh.SetTriangles(triangles, 0); mesh.RecalculateBounds();
            chunk.GetComponent<MeshFilter>().sharedMesh = mesh;
            var renderer = chunk.GetComponent<MeshRenderer>();
            renderer.sharedMaterial = material;
            renderer.renderingLayerMask = 1u;
            renderer.shadowCastingMode = ShadowCastingMode.On;
            renderer.receiveShadows = true;
        }

        bool Occupied(int x, int y, int z)
        {
            if (x < 0 || x >= width || z < 0 || z >= depth || y < 0 || y >= maximumHeight) return false;
            if (occupancy != null && occupancy.GetLength(0) == width &&
                occupancy.GetLength(1) == maximumHeight && occupancy.GetLength(2) == depth)
                return occupancy[x, y, z];
            return CalculateOccupied(x, y, z);
        }

        bool CalculateOccupied(int x, int y, int z)
        {
            Vector3 localCenter = new((x - width * .5f + .5f) * voxelSize,
                (y + .5f) * voxelSize, (z + .5f) * voxelSize);
            Vector3 worldCenter = transform.TransformPoint(localCenter);
            float voxelTop = worldCenter.y + voxelSize * .5f;
            if (IsCavityShell(worldCenter)) return true;
            if (voxelTop > MaximumVisibleHeight(worldCenter.x, worldCenter.z)) return false;
            int ground = groundHeights != null && groundHeights.GetLength(0) == width &&
                         groundHeights.GetLength(1) == depth
                ? groundHeights[x, z]
                : 1 + Mathf.FloorToInt(Mathf.PerlinNoise((x + seed) * groundNoiseScale,
                    (z + seed * .31f) * groundNoiseScale) * groundRoughness);
            if (y < ground) return true;

            // Keep the first depth layers as ground relief only. Tall isolated towers
            // close to the gameplay camera read as accidental green pylons.
            float sceneryDistance;
            if (surroundGameplay)
            {
                sceneryDistance = DistanceFromGameplay(worldCenter.x, worldCenter.z);
                float structureDistance = surroundingStructureDistance > 0f
                    ? surroundingStructureDistance : structureStartDepth * voxelSize;
                if (sceneryDistance < structureDistance) return false;
            }
            else
            {
                if (worldCenter.z < gameplayBackZ || z < structureStartDepth) return false;
                sceneryDistance = z * voxelSize;
            }

            // Sparse architectural masses become taller and more monumental toward the horizon.
            int cellX = x / 4;
            int cellZ = z / 4;
            float chance = Hash(cellX, cellZ, seed);
            float horizon = surroundGameplay
                ? Mathf.InverseLerp(surroundingStructureDistance > 0f
                        ? surroundingStructureDistance : structureStartDepth * voxelSize,
                    Mathf.Max((surroundingStructureDistance > 0f
                        ? surroundingStructureDistance : structureStartDepth * voxelSize) + 1f,
                        surroundingRadius),
                    sceneryDistance)
                : Mathf.InverseLerp(2f, depth, z);
            float growth = Mathf.Lerp(horizon * .35f, horizon, distanceGrowth);
            bool structure = chance > Mathf.Lerp(1f - structureDensity * .55f, 1f - structureDensity, growth);
            if (!structure) return false;
            int insetX = x & 3;
            int insetZ = z & 3;
            int footprint = chance > .88f ? 3 : 2;
            if (insetX >= footprint || insetZ >= footprint) return false;
            float localNearHeight = surroundingNearHeight > 0f
                ? surroundingNearHeight : nearStructureHeight;
            int towerHeight = ground + 2 + Mathf.FloorToInt(Hash(cellX + 91, cellZ - 37, seed) * Mathf.Lerp(localNearHeight, farStructureHeight, growth));
            towerHeight = Mathf.Min(towerHeight, maximumHeight);
            bool brokenTop = y > towerHeight - 4 && Hash(x + y * 7, z - y * 3, seed + 19) < brokenTopAmount;
            bool window = y > ground + 2 && (y % 4 == 2) && insetX == 0 && insetZ == 0 && Hash(x, y + z, seed + 73) < holesAmount * 1.8f;
            return y < towerHeight && !brokenTop && !window;
        }

        bool IsCavityShell(Vector3 worldCenter)
        {
            for (int i = 0; i < cavityVolumes.Count; i++)
            {
                VoxelCavityVolume volume = cavityVolumes[i];
                if (!volume.Outer.Contains(worldCenter)) continue;
                // Ignore the longitudinal inner bounds to keep both ends open.
                bool insidePassage = worldCenter.y > volume.Inner.min.y &&
                                     worldCenter.y < volume.Inner.max.y &&
                                     worldCenter.z > volume.Inner.min.z &&
                                     worldCenter.z < volume.Inner.max.z;
                if (!insidePassage) return true;
            }
            return false;
        }

        float MaximumVisibleHeight(float worldX, float worldZ)
        {
            float ceiling = float.PositiveInfinity;
            for (int i = 0; i < levelGeometry.Count; i++)
            {
                Bounds bounds = levelGeometry[i];
                if (worldX < bounds.min.x - geometryHorizontalPadding ||
                    worldX > bounds.max.x + geometryHorizontalPadding) continue;

                // Protect both the physical space below the platform and the view
                // corridor behind it. This also receives swept bounds for movers.
                float backPadding = surroundGameplay
                    ? geometryHorizontalPadding : visibilityDepthBehindGeometry;
                if (worldZ < bounds.min.z - backPadding ||
                    worldZ > bounds.max.z + backPadding) continue;
                ceiling = Mathf.Min(ceiling,
                    bounds.min.y - Mathf.Max(geometryVerticalClearance, voxelSize * .75f));
            }
            return ceiling;
        }

        float DistanceFromGameplay(float worldX, float worldZ)
        {
            float nearest = float.PositiveInfinity;
            for (int i = 0; i < levelGeometry.Count; i++)
            {
                Bounds bounds = levelGeometry[i];
                float dx = Mathf.Max(bounds.min.x - worldX, 0f, worldX - bounds.max.x);
                float dz = Mathf.Max(bounds.min.z - worldZ, 0f, worldZ - bounds.max.z);
                nearest = Mathf.Min(nearest, Mathf.Sqrt(dx * dx + dz * dz));
            }
            return float.IsPositiveInfinity(nearest) ? surroundingRadius : nearest;
        }

        static float Hash(int x, int z, int value)
        {
            unchecked
            {
                int h = x * 73856093 ^ z * 19349663 ^ value * 83492791;
                h ^= h << 13; h ^= h >> 17; h ^= h << 5;
                return (h & 0x7fffffff) / (float)int.MaxValue;
            }
        }
    }
}
