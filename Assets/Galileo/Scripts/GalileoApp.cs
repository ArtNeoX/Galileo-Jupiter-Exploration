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

        [Tooltip("Render in Resources/Galileo/Backgrounds shown until a mission concept is selected.")]
        [SerializeField] string defaultBackground = "render_conceptB";

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
        public void ShowConcepts() => Show<ConceptsScreen>();
        public void ShowEnd() => Show<EndScreen>();

        public void Restart()
        {
            SelectedConcept = null;
            CompletedSystems.Clear();
            _optionOrder.Clear();
            SetBackground(defaultBackground);
            ShowCharacters();
        }

        // ---------- Dialogs (Scene 2.1 and the wrong-concept explanation) ----------

        /// <summary>The dialog the next <see cref="DialogScreen"/> plays.</summary>
        public DialogRequest PendingDialog { get; private set; }

        public void ShowDialog(string speakerId, string[] lines, System.Action onFinished)
        {
            PendingDialog = new DialogRequest { speakerId = speakerId, lines = lines, onFinished = onFinished };
            Show<DialogScreen>();
        }

        public void ShowMissionDialog() =>
            ShowDialog(Content.missionDialog.speakerId, Content.missionDialog.lines, ShowConcepts);

        /// <summary>Scene 3 → Next. Only the correct concept moves on; otherwise a random team member explains why not.</summary>
        public void ConfirmConcept(ConceptInfo concept)
        {
            SelectedConcept = concept;
            if (concept.correct)
            {
                ShowOrbiterDesign();
                return;
            }
            var people = Content.characters;
            var speaker = people.Length > 0 ? people[Random.Range(0, people.Length)].id : null;
            var lines = concept.wrongChoiceLines != null && concept.wrongChoiceLines.Length > 0
                ? concept.wrongChoiceLines
                : new[] { Content.ui.explanationPlaceholder };
            ShowDialog(speaker, lines, ShowConcepts);
        }

        // ---------- Scene 4: Orbiter probe design ----------

        public readonly System.Collections.Generic.HashSet<string> CompletedSystems =
            new System.Collections.Generic.HashSet<string>();
        public DesignSystemInfo CurrentSystem { get; private set; }
        public DesignOptionInfo CurrentOption { get; private set; }

        readonly System.Collections.Generic.Dictionary<string, DesignOptionInfo[]> _optionOrder =
            new System.Collections.Generic.Dictionary<string, DesignOptionInfo[]>();

        public void ShowOrbiterDesign() => Show<SystemsListScreen>();

        public void ShowSystem(DesignSystemInfo system)
        {
            CurrentSystem = system;
            Show<SystemScreen>();
        }

        public void ShowOption(DesignOptionInfo option)
        {
            CurrentOption = option;
            Show<OptionScreen>();
        }

        public void RunTest() => Show<TestScreen>();

        public void CompleteCurrentSystem()
        {
            CompletedSystems.Add(CurrentSystem.id);
            ShowOrbiterDesign();
        }

        /// <summary>
        /// The two options in a randomly shuffled order (so the right answer isn't always "A"),
        /// kept the same for the rest of the session so retries don't reshuffle.
        /// </summary>
        public DesignOptionInfo[] OptionsInOrder(DesignSystemInfo system)
        {
            if (!_optionOrder.TryGetValue(system.id, out var order))
            {
                order = (DesignOptionInfo[])system.options.Clone();
                for (int i = order.Length - 1; i > 0; i--)
                {
                    int j = Random.Range(0, i + 1);
                    (order[i], order[j]) = (order[j], order[i]);
                }
                _optionOrder[system.id] = order;
            }
            return order;
        }

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

            // Background: a dark gradient, with the office render cross-faded on top of it.
            var bgRoot = UIFactory.Rect("Background", root).Stretch();
            var gradient = bgRoot.gameObject.AddComponent<Image>();
            gradient.sprite = ProceduralArt.VerticalGradient(Theme.BackgroundTop, Theme.BackgroundBottom);
            gradient.raycastTarget = false;
            bgRoot.gameObject.AddComponent<RectMask2D>();
            _backgroundLayer = UIFactory.Rect("Renders", bgRoot).Stretch();
            // Dim the render so panels and text stay readable.
            _backgroundDim = UIFactory.Line(bgRoot, Theme.BackgroundDim);
            _backgroundDim.rectTransform.Stretch();
            _backgroundDim.enabled = false;
            SetBackground(defaultBackground, 0f);

            _screens = UIFactory.Rect("Screens", root).Stretch();
        }

        // ---------- Background ----------

        RectTransform _backgroundLayer;
        Image _backgroundDim;
        string _backgroundName;
        Coroutine _backgroundFade;

        /// <summary>
        /// Cross-fades the background to the render Resources/Galileo/Backgrounds/&lt;name&gt;.
        /// Does nothing if that render is already showing or doesn't exist.
        /// </summary>
        public void SetBackground(string name, float fadeSeconds = 0.8f)
        {
            if (string.IsNullOrEmpty(name) || name == _backgroundName) return;
            var sprite = ContentLoader.LoadSprite("Galileo/Backgrounds/" + name);
            if (sprite == null)
            {
                Debug.LogWarning($"[Galileo] Background render not found: Resources/Galileo/Backgrounds/{name}");
                return;
            }
            _backgroundName = name;
            _backgroundDim.enabled = true;

            var img = UIFactory.Picture(name, _backgroundLayer, sprite, Color.white);
            img.preserveAspect = false;
            var fitter = img.gameObject.AddComponent<AspectRatioFitter>();
            fitter.aspectMode = AspectRatioFitter.AspectMode.EnvelopeParent;
            fitter.aspectRatio = sprite.rect.width / sprite.rect.height;

            if (_backgroundFade != null) StopCoroutine(_backgroundFade);
            _backgroundFade = StartCoroutine(FadeBackground(img, fadeSeconds));
        }

        System.Collections.IEnumerator FadeBackground(Image incoming, float duration)
        {
            var c = incoming.color;
            for (float t = 0; t < duration; t += Time.unscaledDeltaTime)
            {
                c.a = Mathf.SmoothStep(0f, 1f, t / duration);
                incoming.color = c;
                yield return null;
            }
            c.a = 1f;
            incoming.color = c;
            // Remove the renders now hidden underneath.
            foreach (Transform child in _backgroundLayer)
                if (child != incoming.transform) Destroy(child.gameObject);
            _backgroundFade = null;
        }

        static void EnsureEventSystem()
        {
            if (FindAnyObjectByType<EventSystem>() != null) return;
            var go = new GameObject("EventSystem", typeof(EventSystem));
            go.AddComponent<InputSystemUIInputModule>().AssignDefaultActions();
        }
    }
}
