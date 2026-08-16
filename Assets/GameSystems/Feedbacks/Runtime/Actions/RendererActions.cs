using System;
using GameSystems.Sequencing;
using GameSystems.Sequencing.Values;
using UnityEngine;

namespace GameSystems.Feedbacks.Actions
{
    [Serializable]
    public sealed class BlinkRenderersAction : GameAction
    {
        [SerializeReference] GameObjectValue target = new SelfGameObjectValue();
        [SerializeField, Min(.001f)] float duration = .2f;
        [SerializeField, Min(1f)] float frequency = 16f;
        public BlinkRenderersAction() { }
        public BlinkRenderersAction(GameObjectValue target, float duration, float frequency)
        { this.target = target; this.duration = duration; this.frequency = frequency; }
        public override string Summary => $"Blink Renderers for {duration:0.###}s";
        public override GameActionRuntime CreateRuntime() => new Runtime();
        sealed class Runtime : GameActionRuntime
        {
            Renderer[] renderers; bool[] initial; float elapsed; BlinkRenderersAction Data => (BlinkRenderersAction)Definition;
            protected override void OnEnter()
            { base.OnEnter(); GameObject go = Data.target?.Get(Context); if (go == null) { Fail("Missing renderer hierarchy."); return; } renderers = go.GetComponentsInChildren<Renderer>(true); initial = new bool[renderers.Length]; for (int i=0;i<renderers.Length;i++) initial[i]=renderers[i]!=null&&renderers[i].enabled; elapsed=0; }
            protected override bool Tick(float deltaTime)
            { if (Failed) return true; elapsed += Time.unscaledDeltaTime; bool visible = Mathf.FloorToInt(elapsed * Data.frequency) % 2 == 0; for(int i=0;i<renderers.Length;i++) if(renderers[i]!=null) renderers[i].enabled=visible&&initial[i]; return elapsed>=Data.duration; }
            protected override void OnExit() { if(renderers!=null) for(int i=0;i<renderers.Length;i++) if(renderers[i]!=null) renderers[i].enabled=initial[i]; base.OnExit(); }
        }
    }

    [Serializable]
    public sealed class TweenMaterialFloatHierarchyAction : GameAction
    {
        [SerializeReference] GameObjectValue target = new SelfGameObjectValue();
        [SerializeField] string propertyName = "_BaseColor";
        [SerializeField] float value = 1f;
        [SerializeField, Min(.001f)] float duration = .15f;
        [SerializeField] AnimationCurve curve = AnimationCurve.EaseInOut(0, 0, 1, 1);
        public TweenMaterialFloatHierarchyAction() { }
        public TweenMaterialFloatHierarchyAction(GameObjectValue target, string propertyName, float value, float duration, AnimationCurve curve)
        { this.target=target; this.propertyName=propertyName; this.value=value; this.duration=duration; this.curve=curve; }
        public override string Summary => $"Tween Renderer [{propertyName}] to {value:0.##}";
        public override GameActionRuntime CreateRuntime() => new Runtime();
        sealed class Runtime : GameActionRuntime
        {
            Renderer[] renderers; MaterialPropertyBlock block; int property; float elapsed; TweenMaterialFloatHierarchyAction Data => (TweenMaterialFloatHierarchyAction)Definition;
            protected override void OnEnter() { base.OnEnter(); GameObject go=Data.target?.Get(Context); if(go==null){Fail("Missing renderer hierarchy.");return;} renderers=go.GetComponentsInChildren<Renderer>(true); block=new MaterialPropertyBlock(); property=Shader.PropertyToID(Data.propertyName); elapsed=0; }
            protected override bool Tick(float deltaTime) { if(Failed)return true; elapsed+=Time.unscaledDeltaTime; float t=Mathf.Clamp01(elapsed/Mathf.Max(.001f,Data.duration)); float current=Data.value*(Data.curve?.Evaluate(t)??t)*FeedbackActionUtility.Intensity(Context); foreach(Renderer renderer in renderers){if(renderer==null)continue;renderer.GetPropertyBlock(block);block.SetFloat(property,current);renderer.SetPropertyBlock(block);} return t>=1f; }
        }
    }
}
