using System;
using GameSystems.Sequencing;
using GameSystems.Sequencing.Values;
using UnityEngine;

namespace GameSystems.Feedbacks.Actions
{
    [Serializable]
    public sealed class ShakeTransformAction : GameAction
    {
        [SerializeReference] GameObjectValue target = new SelfGameObjectValue();
        [SerializeField] Vector3 amplitude = new(.08f, .08f, 0f);
        [SerializeField, Min(.001f)] float duration = .15f;
        [SerializeField, Min(1f)] float frequency = 35f;
        [SerializeField] AnimationCurve curve = new(new Keyframe(0,0), new Keyframe(.2f,1), new Keyframe(1,0));
        public ShakeTransformAction() { }
        public ShakeTransformAction(GameObjectValue target, Vector3 amplitude, float duration, float frequency, AnimationCurve curve)
        { this.target=target;this.amplitude=amplitude;this.duration=duration;this.frequency=frequency;this.curve=curve; }
        public override string Summary => $"Shake Transform for {duration:0.###}s";
        public override GameActionRuntime CreateRuntime() => new Runtime();
        sealed class Runtime : GameActionRuntime
        {
            Transform target; Vector3 start; float elapsed; ShakeTransformAction Data => (ShakeTransformAction)Definition;
            protected override void OnEnter(){base.OnEnter();target=Data.target?.Get(Context)?.transform;if(target==null){Fail("Missing Transform.");return;}start=target.localPosition;elapsed=0;}
            protected override bool Tick(float deltaTime){if(Failed)return true;elapsed+=Time.unscaledDeltaTime;float t=Mathf.Clamp01(elapsed/Mathf.Max(.001f,Data.duration));float value=(Data.curve?.Evaluate(t)??t)*FeedbackActionUtility.Intensity(Context);float phase=elapsed*Data.frequency;target.localPosition=start+Vector3.Scale(Data.amplitude,new Vector3(Mathf.Sin(phase),Mathf.Sin(phase*.73f+.8f),Mathf.Sin(phase*.91f+1.4f)))*value;return t>=1f;}
            protected override void OnExit(){if(target!=null)target.localPosition=start;base.OnExit();}
        }
    }

    [Serializable]
    public sealed class SquashStretchTransformAction : GameAction
    {
        [SerializeReference] GameObjectValue target = new SelfGameObjectValue();
        [SerializeField] float amount = .2f;
        [SerializeField, Min(.001f)] float duration = .2f;
        [SerializeField, Min(.01f)] float springStrength = 42f;
        [SerializeField, Range(0f,1f)] float damping = .72f;
        [SerializeField] AnimationCurve curve = new(new Keyframe(0,0),new Keyframe(.2f,1),new Keyframe(1,0));
        public SquashStretchTransformAction() { }
        public SquashStretchTransformAction(GameObjectValue target,float amount,float duration,float strength,float damping,AnimationCurve curve)
        {this.target=target;this.amount=amount;this.duration=duration;springStrength=strength;this.damping=damping;this.curve=curve;}
        public override string Summary => $"Squash/stretch Transform for {duration:0.###}s";
        public override GameActionRuntime CreateRuntime()=>new Runtime();
        sealed class Runtime:GameActionRuntime
        {
            Transform target;Vector3 start,current,velocity;float elapsed;SquashStretchTransformAction Data=>(SquashStretchTransformAction)Definition;
            protected override void OnEnter(){base.OnEnter();target=Data.target?.Get(Context)?.transform;if(target==null){Fail("Missing Transform.");return;}start=current=target.localScale;velocity=Vector3.zero;elapsed=0;}
            protected override bool Tick(float deltaTime){if(Failed)return true;elapsed+=Time.unscaledDeltaTime;float t=Mathf.Clamp01(elapsed/Mathf.Max(.001f,Data.duration));float value=Data.curve?.Evaluate(t)??t;float a=Data.amount*value*FeedbackActionUtility.Intensity(Context);Vector3 multiplier=new(1f+a,1f-a*.7f,1f+a);Vector3 goal=Vector3.Scale(start,multiplier);float dt=Mathf.Min(Time.unscaledDeltaTime,.033f);velocity+=(goal-current)*Mathf.Max(.01f,Data.springStrength)*dt;velocity*=Mathf.Pow(Mathf.Clamp01(Data.damping),dt*60f);current+=velocity*dt;target.localScale=current;return t>=1f;}
            protected override void OnExit(){if(target!=null)target.localScale=start;base.OnExit();}
        }
    }
}
