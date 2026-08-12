using System; using UnityEngine; using UnityEngine.Rendering;
namespace GameSystems.Feedbacks
{
    [Serializable] public sealed class FeedbackCue
    {
        public string label="Feedback"; public bool enabled=true; public FeedbackKind kind; public FeedbackTimeMode timeMode=FeedbackTimeMode.Unscaled;
        [Min(0f)] public float initialDelay; [Min(.001f)] public float duration=.15f; [Range(0f,100f)] public float chance=100f;
        [Min(0f)] public float cooldown; [Min(0)] public int repeats; [Min(0f)] public float repeatDelay; public bool constantIntensity;
        public Vector2 intensityRange=new(0f,float.MaxValue); public bool forward=true; public bool reverse=true; public bool restoreAfterPlay=true;
        public AnimationCurve curve=new(new Keyframe(0f,0f),new Keyframe(.2f,1f),new Keyframe(1f,0f)); public string bindingId;
        public Transform target; public ParticleSystem particles; public AudioSource audioSource; public AudioClip audioClip; public AudioClip[] audioClips;
        public Light light; public Renderer renderer; public Camera camera; public CanvasGroup canvasGroup; public Volume volume; public GameObject prefab;
        public string propertyName="_BaseColor"; public Vector3 vector=new(.08f,.08f,0f); public Vector3 vectorB=Vector3.one; public Color color=Color.white;
        public float amount=1f; public float frequency=35f; [Range(0f,1f)] public float damping=.72f; [Min(.01f)] public float springStrength=42f;
        public Vector2 pitchRange=new(.94f,1.06f); public Vector2 volumeRange=new(.85f,1f); public string animatorParameter;
        public ForceMode forceMode=ForceMode.Impulse; public UnityEngine.Events.UnityEvent unityEvent;
    }
}
