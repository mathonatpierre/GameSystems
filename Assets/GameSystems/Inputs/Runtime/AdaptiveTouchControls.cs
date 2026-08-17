using System.Linq;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Layouts;
using UnityEngine.InputSystem.OnScreen;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace GameSystems.Inputs
{
    [DefaultExecutionOrder(-900)]
    public sealed class AdaptiveTouchControls : MonoBehaviour
    {
        const int SortingOrder = 2000;
        Canvas canvas;
        Sprite circleSprite;
        bool keyboardInUse;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Create()
        {
            if (FindAnyObjectByType<AdaptiveTouchControls>() != null) return;
            var host = new GameObject("Adaptive Touch Controls");
            DontDestroyOnLoad(host);
            host.AddComponent<AdaptiveTouchControls>();
        }

        void Awake()
        {
            EnsureEventSystem();
            BuildCanvas();
            InputSystem.onDeviceChange += OnDeviceChange;
            SceneManager.sceneLoaded += OnSceneLoaded;
            RefreshVisibility();
        }

        void OnDestroy()
        {
            InputSystem.onDeviceChange -= OnDeviceChange;
            SceneManager.sceneLoaded -= OnSceneLoaded;
            if (circleSprite != null) Destroy(circleSprite.texture);
        }
        void OnDeviceChange(InputDevice _, InputDeviceChange __) => RefreshVisibility();
        void OnSceneLoaded(Scene _, LoadSceneMode __)
        {
            EnsureEventSystem();
            RefreshVisibility();
        }

        void Update()
        {
            if (Touchscreen.current?.primaryTouch.press.wasPressedThisFrame == true)
                keyboardInUse = false;
            else if (Keyboard.current?.anyKey.wasPressedThisFrame == true)
                keyboardInUse = true;
            RefreshVisibility();
        }

        void RefreshVisibility()
        {
            bool hasPlayableCharacter = FindAnyObjectByType<GameSystems.Characters.PlayerAbilityInputSource>() != null;
            bool hasTouch = Application.isMobilePlatform || Touchscreen.current != null;
            bool hasPhysicalGamepad = Gamepad.all.Any(gamepad => gamepad.native);
            canvas.gameObject.SetActive(hasPlayableCharacter && hasTouch && !keyboardInUse && !hasPhysicalGamepad);
        }

        void BuildCanvas()
        {
            var root = new GameObject("Touch UI", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler),
                typeof(GraphicRaycaster));
            root.SetActive(false);
            root.transform.SetParent(transform, false);
            canvas = root.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = SortingOrder;
            CanvasScaler scaler = root.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1280f, 720f);
            scaler.matchWidthOrHeight = .5f;

            circleSprite = CreateCircleSprite();
            CreateStick(root.transform);
            CreateButton(root.transform, "Jump", "<Gamepad>/buttonSouth", "A", new Vector2(156f, 156f),
                new Vector2(-62f, 88f));
        }

        void CreateStick(Transform parent)
        {
            var zone = new GameObject("Dynamic Move Zone", typeof(RectTransform), typeof(Image),
                typeof(DynamicTouchStick));
            zone.transform.SetParent(parent, false);
            RectTransform zoneRect = zone.GetComponent<RectTransform>();
            zoneRect.anchorMin = Vector2.zero;
            zoneRect.anchorMax = new Vector2(.55f, 1f);
            zoneRect.offsetMin = zoneRect.offsetMax = Vector2.zero;
            Image zoneImage = zone.GetComponent<Image>();
            zoneImage.color = Color.clear;
            zoneImage.raycastTarget = true;

            var pad = new GameObject("Move", typeof(RectTransform), typeof(Image));
            pad.transform.SetParent(zone.transform, false);
            RectTransform padRect = pad.GetComponent<RectTransform>();
            padRect.anchorMin = padRect.anchorMax = padRect.pivot = new Vector2(.5f, .5f);
            padRect.sizeDelta = new Vector2(190f, 190f);
            padRect.anchoredPosition = Vector2.zero;
            Image padImage = pad.GetComponent<Image>();
            padImage.sprite = circleSprite;
            padImage.color = new Color(.03f, .045f, .06f, .58f);
            padImage.raycastTarget = false;

            var handle = new GameObject("Handle", typeof(RectTransform), typeof(Image));
            handle.transform.SetParent(pad.transform, false);
            RectTransform handleRect = handle.GetComponent<RectTransform>();
            handleRect.anchorMin = handleRect.anchorMax = handleRect.pivot = new Vector2(.5f, .5f);
            handleRect.sizeDelta = new Vector2(88f, 88f);
            handleRect.anchoredPosition = Vector2.zero;
            Image handleImage = handle.GetComponent<Image>();
            handleImage.sprite = circleSprite;
            handleImage.color = new Color(.92f, .94f, .97f, .78f);
            zone.GetComponent<DynamicTouchStick>().Configure(padRect, handleRect, 76f);
            pad.SetActive(false);
        }

        void CreateButton(Transform parent, string name, string path, string label, Vector2 size,
            Vector2 position)
        {
            var owner = new GameObject(name, typeof(RectTransform), typeof(Image),
                typeof(OnScreenButton), typeof(TouchHapticFeedback));
            owner.transform.SetParent(parent, false);
            RectTransform rect = owner.GetComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = rect.pivot = Vector2.right;
            rect.sizeDelta = size;
            rect.anchoredPosition = position;
            Image image = owner.GetComponent<Image>();
            image.sprite = circleSprite;
            image.color = new Color(.96f, .7f, .2f, .86f);
            owner.GetComponent<OnScreenButton>().controlPath = path;

            var textOwner = new GameObject("Label", typeof(RectTransform), typeof(Text));
            textOwner.transform.SetParent(owner.transform, false);
            RectTransform textRect = textOwner.GetComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = textRect.offsetMax = Vector2.zero;
            Text text = textOwner.GetComponent<Text>();
            text.text = label;
            text.alignment = TextAnchor.MiddleCenter;
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.fontSize = 36;
            text.fontStyle = FontStyle.Bold;
            text.color = Color.white;
            text.raycastTarget = false;
        }

        static Sprite CreateCircleSprite()
        {
            const int size = 64;
            var texture = new Texture2D(size, size, TextureFormat.RGBA32, false)
            {
                name = "Touch Control Circle",
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp
            };
            var pixels = new Color32[size * size];
            Vector2 center = new Vector2(size * .5f, size * .5f);
            float radius = size * .48f;
            for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                float distance = Vector2.Distance(new Vector2(x + .5f, y + .5f), center);
                byte alpha = (byte)Mathf.RoundToInt(Mathf.Clamp01(radius - distance + 1f) * 255f);
                pixels[y * size + x] = new Color32(255, 255, 255, alpha);
            }
            texture.SetPixels32(pixels);
            texture.Apply(false, true);
            return Sprite.Create(texture, new Rect(0f, 0f, size, size), new Vector2(.5f, .5f), size);
        }

        void EnsureEventSystem()
        {
            EventSystem events = FindAnyObjectByType<EventSystem>();
            if (events == null)
                events = new GameObject("EventSystem", typeof(EventSystem)).GetComponent<EventSystem>();
            if (events.transform.parent != transform) events.transform.SetParent(transform, false);
            if (events.GetComponent<InputSystemUIInputModule>() == null)
                events.gameObject.AddComponent<InputSystemUIInputModule>();
            StandaloneInputModule legacy = events.GetComponent<StandaloneInputModule>();
            if (legacy != null) Destroy(legacy);
        }
    }

    public sealed class DynamicTouchStick : OnScreenControl, IPointerDownHandler,
        IDragHandler, IPointerUpHandler
    {
        [InputControl(layout = "Vector2")]
        [SerializeField] string inputControlPath = "<Gamepad>/leftStick";
        [SerializeField, Min(20f)] float movementRange = 76f;
        [SerializeField, Range(0f, .5f)] float deadZone = .1f;
        [SerializeField, Range(.5f, 2f)] float responseExponent = .82f;
        RectTransform zone;
        RectTransform pad;
        RectTransform handle;
        int pointerId = int.MinValue;
        Vector2 origin;

        protected override string controlPathInternal
        {
            get => inputControlPath;
            set => inputControlPath = value;
        }

        public void Configure(RectTransform padTransform, RectTransform handleTransform,
            float range)
        {
            zone = (RectTransform)transform;
            pad = padTransform;
            handle = handleTransform;
            movementRange = range;
            controlPath = "<Gamepad>/leftStick";
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            if (pointerId != int.MinValue) return;
            pointerId = eventData.pointerId;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(zone, eventData.position,
                eventData.pressEventCamera, out origin);
            pad.anchoredPosition = origin;
            handle.anchoredPosition = Vector2.zero;
            pad.gameObject.SetActive(true);
            SendValueToControl(Vector2.zero);
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (eventData.pointerId != pointerId) return;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(zone, eventData.position,
                eventData.pressEventCamera, out Vector2 point);
            Vector2 raw = Vector2.ClampMagnitude((point - origin) / movementRange, 1f);
            float magnitude = raw.magnitude;
            Vector2 value = magnitude <= deadZone ? Vector2.zero : raw.normalized *
                Mathf.Pow(Mathf.InverseLerp(deadZone, 1f, magnitude), responseExponent);
            handle.anchoredPosition = value * movementRange;
            SendValueToControl(value);
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            if (eventData.pointerId != pointerId) return;
            pointerId = int.MinValue;
            SendValueToControl(Vector2.zero);
            handle.anchoredPosition = Vector2.zero;
            pad.gameObject.SetActive(false);
        }
    }

    public sealed class TouchHapticFeedback : MonoBehaviour, IPointerDownHandler
    {
        static float lastPulseTime;
        public void OnPointerDown(PointerEventData eventData)
        {
            if (!Application.isMobilePlatform || Time.unscaledTime - lastPulseTime < .08f) return;
            lastPulseTime = Time.unscaledTime;
#if UNITY_IOS || UNITY_ANDROID
            Handheld.Vibrate();
#endif
        }
    }
}
