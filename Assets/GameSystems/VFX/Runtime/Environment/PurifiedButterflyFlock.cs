using UnityEngine;
using UnityEngine.Rendering;

namespace GameSystems.VFX
{
    public sealed class PurifiedButterflyFlock : MonoBehaviour
    {
        [SerializeField] Transform target;
        [SerializeField, Range(3, 12)] int butterflyCount = 7;
        [SerializeField] float perchDelay = 2.3f;
        Material material;
        Mesh wingMesh;
        GameSystems.Abilities.CharacterAbilityController controller;
        float idleTimer;

        sealed class Butterfly
        {
            public Transform root, leftWing, rightWing;
            public Vector3 velocity;
            public float seed, scale;
        }
        Butterfly[] flock;

        public void Configure(Transform follow) => target = follow;

        void Awake()
        {
            controller = target != null ? target.GetComponent<GameSystems.Abilities.CharacterAbilityController>() : null;
            wingMesh = BuildWingMesh();
            material = new Material(Shader.Find("Universal Render Pipeline/Unlit")) { name = "MAT_ButterflyWings_Runtime" };
            material.SetFloat("_Cull", 0f);
            flock = new Butterfly[butterflyCount];
            for (int i = 0; i < flock.Length; i++) flock[i] = CreateButterfly(i);
        }

        Butterfly CreateButterfly(int index)
        {
            var root = new GameObject($"Butterfly_{index:00}").transform; root.SetParent(transform, false);
            Transform left = Wing(root, "Left Wing", true, index); Transform right = Wing(root, "Right Wing", false, index);
            float seed = index * 1.731f + .37f;
            root.position = target != null ? target.position + new Vector3(Mathf.Sin(seed) * 2f, 1f + Mathf.Repeat(seed, 1f), Mathf.Cos(seed) * 1.4f) : Vector3.zero;
            return new Butterfly { root = root, leftWing = left, rightWing = right, seed = seed, scale = Mathf.Lerp(.72f, 1.15f, Mathf.Repeat(seed * .73f, 1f)) };
        }

        Transform Wing(Transform parent, string name, bool left, int index)
        {
            var go = new GameObject(name, typeof(MeshFilter), typeof(MeshRenderer)); go.transform.SetParent(parent, false);
            go.transform.localScale = new Vector3(left ? -1f : 1f, 1f, 1f);
            go.GetComponent<MeshFilter>().sharedMesh = wingMesh;
            MeshRenderer renderer = go.GetComponent<MeshRenderer>(); renderer.sharedMaterial = material;
            renderer.shadowCastingMode = ShadowCastingMode.Off; renderer.receiveShadows = false;
            var block = new MaterialPropertyBlock();
            Color color = index % 3 == 0 ? new Color(.95f,.66f,.18f) : index % 3 == 1 ? new Color(.82f,.27f,.82f) : new Color(.28f,.66f,.94f);
            block.SetColor("_BaseColor", color); renderer.SetPropertyBlock(block); return go.transform;
        }

        void Update()
        {
            if (target == null || flock == null) return;
            if (controller == null) controller = target.GetComponent<GameSystems.Abilities.CharacterAbilityController>();
            bool still = controller?.Motor != null && controller.Motor.Result.Ground.IsGrounded &&
                         controller.Motor.Result.Velocity.sqrMagnitude < .025f;
            idleTimer = still ? idleTimer + Time.deltaTime : 0f;
            float purity = Mathf.Clamp01(Shader.GetGlobalFloat("_PurificationCameraStrength"));
            float now = Time.time;
            Vector3 targetVelocity = controller?.Motor != null ? controller.Motor.Result.Velocity : Vector3.zero;
            Vector3 anticipatedTarget = target.position + Vector3.ClampMagnitude(targetVelocity * .28f, 2.2f);
            for (int i = 0; i < flock.Length; i++)
            {
                Butterfly butterfly = flock[i]; bool wantsPerch = idleTimer > perchDelay && i < 2;
                Vector3 desired;
                if (wantsPerch)
                    desired = target.position + new Vector3(i == 0 ? -.18f : .2f, i == 0 ? .93f : 1.08f, -.03f + i * .08f);
                else
                {
                    float t = now * (.54f + i * .037f) + butterfly.seed;
                    desired = anticipatedTarget + new Vector3(
                        Mathf.Sin(t * 1.13f) * (1.4f + i * .18f) + Mathf.Sin(t * 2.71f) * .35f,
                        1.0f + Mathf.Sin(t * 1.77f) * .55f + Mathf.Cos(t * .63f) * .24f,
                        Mathf.Cos(t * .91f) * (1.05f + (i % 3) * .38f));
                }
                float smooth = wantsPerch ? .2f : .3f;
                butterfly.root.position = Vector3.SmoothDamp(butterfly.root.position, desired,
                    ref butterfly.velocity, smooth, wantsPerch ? 4.5f : 4.2f);
                if (butterfly.velocity.sqrMagnitude > .002f)
                    butterfly.root.rotation = Quaternion.Slerp(butterfly.root.rotation, Quaternion.LookRotation(butterfly.velocity.normalized, Vector3.up), Time.deltaTime * 5f);
                float perchBlend = wantsPerch ? Mathf.Clamp01(1f - Vector3.Distance(butterfly.root.position, desired) / .3f) : 0f;
                float flapSpeed = Mathf.Lerp(14f + (i % 3) * 1.8f, 2.2f, perchBlend);
                float flapAngle = Mathf.Lerp(58f, 16f, perchBlend) * Mathf.Sin(now * flapSpeed + butterfly.seed * 5f);
                butterfly.leftWing.localRotation = Quaternion.Euler(0f, 0f, flapAngle + 18f);
                butterfly.rightWing.localRotation = Quaternion.Euler(0f, 0f, -flapAngle - 18f);
                butterfly.root.localScale = Vector3.one * butterfly.scale * Mathf.SmoothStep(0f, 1f, purity);
            }
        }

        static Mesh BuildWingMesh()
        {
            var mesh = new Mesh { name = "Butterfly Wing Quad" };
            mesh.vertices = new[] { new Vector3(0,0,-.055f), new Vector3(.18f,0,-.035f), new Vector3(.155f,0,.13f), new Vector3(0,0,.075f) };
            mesh.triangles = new[] { 0,1,2, 0,2,3 }; mesh.RecalculateNormals(); mesh.RecalculateBounds(); return mesh;
        }

        void OnDestroy()
        {
            if (material != null) Destroy(material);
            if (wingMesh != null) Destroy(wingMesh);
        }
    }
}
