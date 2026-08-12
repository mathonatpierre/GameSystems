using System.Collections.Generic; using UnityEngine;
namespace GameSystems.Feedbacks
{
    public sealed class FeedbackContext
    {
        readonly Dictionary<string,Object> bindings=new(); readonly Dictionary<string,object> data=new();
        public GameObject Source{get;private set;} public GameObject Target{get;private set;} public Vector3 Position{get;private set;}
        public Quaternion Rotation{get;private set;}=Quaternion.identity; public Vector3 Normal{get;private set;}=Vector3.up;
        public float Intensity{get;private set;}=1f; public string Channel{get;private set;}
        public static FeedbackContext From(GameObject source) { var context=new FeedbackContext{Source=source}; if(source!=null){context.Position=source.transform.position;context.Rotation=source.transform.rotation;} return context; }
        public FeedbackContext WithTarget(GameObject value){Target=value;return this;} public FeedbackContext WithPosition(Vector3 value){Position=value;return this;}
        public FeedbackContext WithRotation(Quaternion value){Rotation=value;return this;} public FeedbackContext WithNormal(Vector3 value){Normal=value;return this;}
        public FeedbackContext WithIntensity(float value){Intensity=Mathf.Max(0f,value);return this;} public FeedbackContext WithChannel(string value){Channel=value;return this;}
        public FeedbackContext Bind(string id,Object value){if(!string.IsNullOrWhiteSpace(id))bindings[id]=value;return this;}
        public FeedbackContext Set<T>(string key,T value){if(!string.IsNullOrWhiteSpace(key))data[key]=value;return this;}
        public bool TryBinding(string id,out Object value)=>bindings.TryGetValue(id,out value)&&value!=null;
        public bool TryGet<T>(string key,out T value){if(data.TryGetValue(key,out object raw)&&raw is T typed){value=typed;return true;}value=default;return false;}
    }
}
