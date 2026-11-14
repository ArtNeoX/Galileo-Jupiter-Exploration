using Galileo.Content;
using Galileo.Screens;
using Galileo.UI;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

namespace Galileo
{
    /// <summary>
    /// Entry point. Builds the canvas and background, then runs the screens in order:
    /// Scene 1 (characters) → Scene 2 (objectives) → Scene 2.1 (dialog) → Scene 3 (mission concept).
    /// </summary>
    [DisallowMultipleComponent]
    public class GalileoApp : MonoBehaviour
    {
        [Tooltip("Path of the content JSON inside a Resources folder, without the extension.")]
        [SerializeField] string contentPath = "Galileo/content";

        [Tooltip("Path of the background image inside a Resources folder, without the extension.")]
        [SerializeField] string backgroundPath = "Galileo/Backgrounds/office";

        public GalileoContent Content { get; private set; }
        public ConceptInfo SelectedConcept { get; set; }

        RectTransform _screens;
        ScreenBase _current;

        /// <summary>
        /// Starts the game even when Play is pressed in a scene that has no GalileoApp (e.g. SampleScene).
        /// </summary>
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Bootstrap()
        {
            if (FindAnyObjectByType<GalileoApp>() == null)
                new GameObject("Galileo App").AddComponent<GalileoApp>();
        }

        void Awake()
        {
            Content = ContentLoader.Load(contentPath);
            EnsureEventSystem();
            BuildCanvas();
        }

        void Start() => ShowCharacters();

        public void ShowCharacters() => Show<CharacterIntroScreen>();
        public void ShowObjectives() => Show<ObjectivesScreen>();
        public void ShowMissionDialog() => Show<DialogScreen>();
        public void ShowConcepts() => Show<ConceptsScreen>();
        public void ShowEnd() => Show<EndScreen>();

        void Show<T>() where T : ScreenBase
        {
            if (_current != null) Destroy(_current.gameObject);
            var rt = UIFactory.Rect(typeof(T).Name, _screens).Stretch();
            var fade = rt.gameObject.AddComponent<CanvasGroup>();
            fade.alpha = 0f;
            _current = rt.gameObject.AddComponent<T>();
            _current.Init(this);
            if (_current != null && _current.gameObject == rt.gameObject)
                _current.StartCoroutine(FadeIn(fade));
        }

        static System.Collections.IEnumerator FadeIn(CanvasGroup group)
        {
            const float duration = 0.35f;
            for (float t = 0; t < duration; t += Time.unscaledDeltaTime)
            {
                group.alpha = t / duration;
                yield return null;
            }
            group.alpha = 1f;
        }

        void BuildCanvas()
        {
            var go = new GameObject("Galileo Canvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster))
            {
                layer = 5
            };
            go.transform.SetParent(transform, false);
            go.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = go.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;
            var root = (RectTransform)go.transform;

            // Background: the office image if present (cropped to fill), otherwise a dark gradient.
            var bgRoot = UIFactory.Rect("Background", root).Stretch();
            var gradient = bgRoot.gameObject.AddComponent<Image>();
            gradient.sprite = ProceduralArt.VerticalGradient(Theme.BackgroundTop, Theme.BackgroundBottom);
            gradient.raycastTarget = false;

            var office = ContentLoader.LoadSprite(backgroundPath);
            if (office != null)
            {
                bgRoot.gameObject.AddComponent<RectMask2D>();
                var img = UIFactory.Picture("Office", bgRoot, office, Color.white);
                img.preserveAspect = false;
                var fitter = img.gameObject.AddComponent<AspectRatioFitter>();
                fitter.aspectMode = AspectRatioFitter.AspectMode.EnvelopeParent;
                fitter.aspectRatio = office.rect.width / office.rect.height;
                // Dim the photo so panels and text stay readable.
                UIFactory.Line(bgRoot, Theme.BackgroundDim).rectTransform.Stretch();
            }

            _screens = UIFactory.Rect("Screens", root).Stretch();
        }

        static void EnsureEventSystem()
        {
            if (FindAnyObjectByType<EventSystem>() != null) return;
            var go = new GameObject("EventSystem", typeof(EventSystem));
            go.AddComponent<InputSystemUIInputModule>().AssignDefaultActions();
        }
    }
}
