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
    /// Scene 1 (characters) → 2 (objectives) → 2.1 (dialog) → 3 (mission concept) → 4 (orbiter design)
    /// → atmospheric probe dialog → gravity assist → launch (3 attempts) → end of episode 1.
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

        RectTransform _screens, _stageLayer;
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
            AnalysedSystems.Clear();
            MatchingPlacement.Clear();
            _optionOrder.Clear();
            SetBackground(defaultBackground);
            SetBackgroundDim(true);
            if (Stage != null) Destroy(Stage.gameObject);
            Stage = null;
            ShowCharacters();
        }

        // ---------- Dialogs (Scene 2.1 and the wrong-concept explanation) ----------

        /// <summary>The dialog the next <see cref="DialogScreen"/> plays.</summary>
        public DialogRequest PendingDialog { get; private set; }

        public void ShowDialog(string speakerId, string[] lines, System.Action onFinished, bool portraitLeft = false,
            string speakerName = null)
        {
            PendingDialog = new DialogRequest
            {
                speakerId = speakerId, lines = lines, onFinished = onFinished, portraitLeft = portraitLeft,
                speakerName = speakerName
            };
            Show<DialogScreen>();
        }

        /// <summary>The given character id, or a random team member when it's empty.</summary>
        public string SpeakerOrRandom(string speakerId)
        {
            if (!string.IsNullOrEmpty(speakerId) || Content.characters.Length == 0) return speakerId;
            return Content.characters[Random.Range(0, Content.characters.Length)].id;
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

        // ---------- Scene 4 analyses (the ANALYSIS button on a system screen) ----------

        /// <summary>Systems whose analysis the player has completed.</summary>
        public readonly System.Collections.Generic.HashSet<string> AnalysedSystems =
            new System.Collections.Generic.HashSet<string>();

        /// <summary>Pros/cons matching: where each card currently is, per system (kept while the player reads the options).</summary>
        public readonly System.Collections.Generic.Dictionary<string, int[]> MatchingPlacement =
            new System.Collections.Generic.Dictionary<string, int[]>();

        public static bool HasAnalysis(DesignSystemInfo system) =>
            system.analysis != null && !string.IsNullOrEmpty(system.analysis.type);

        /// <summary>False while a required analysis is still to do (the options can't be tested yet).</summary>
        public bool CanDecide(DesignSystemInfo system) =>
            !HasAnalysis(system) || !system.analysis.required || AnalysedSystems.Contains(system.id);

        public void ShowAnalysis()
        {
            var system = CurrentSystem;
            var analysis = system.analysis;
            switch (analysis.type)
            {
                case "mass": Show<MassAnalysisScreen>(); break;
                case "matching": Show<MatchingAnalysisScreen>(); break;
                default: // engineer
                    ShowDialog(analysis.speakerId, analysis.lines, () => CompleteAnalysis(), false, analysis.speakerName);
                    break;
            }
        }

        /// <summary>Marks the current system's analysis done and returns to its screen.</summary>
        public void CompleteAnalysis()
        {
            AnalysedSystems.Add(CurrentSystem.id);
            ShowSystem(CurrentSystem);
        }

        public void MarkAnalysed() => AnalysedSystems.Add(CurrentSystem.id);

        /// <summary>Dev shortcut L: skips the orbiter design, sets the right launch window, or hits the green zone.</summary>
        void Update()
        {
            var k = UnityEngine.InputSystem.Keyboard.current;
            if (k == null || !k.lKey.wasPressedThisFrame) return;
            if (_current is SystemsListScreen || _current is SystemScreen ||
                _current is OptionScreen || _current is TestScreen ||
                _current is MassAnalysisScreen || _current is MatchingAnalysisScreen)
                SkipOrbiterDesign();
            else if (_current is GravityAssistScreen gravity)
                gravity.SetWindow(gravity.CorrectStartDay, gravity.CorrectEndDay);
            // LaunchScreen handles L itself (stops the pointer in the green zone).
        }

        public void SkipOrbiterDesign()
        {
            foreach (var system in Content.orbiterDesign.systems)
                CompletedSystems.Add(system.id);
            ShowAtmosProbeDialog();
        }

        public void CompleteCurrentSystem()
        {
            CompletedSystems.Add(CurrentSystem.id);
            ShowOrbiterDesign();
        }

        // ---------- Scene 5: Atmospheric probe ----------

        /// <summary>
        /// Orbiter design done → a random team member introduces the atmospheric probe while the
        /// background fades to the atmospheric probe render.
        /// </summary>
        public void ShowAtmosProbeDialog()
        {
            var dialog = Content.atmosProbeDialog;
            SetBackground(string.IsNullOrEmpty(dialog.background) ? "render_conceptC_Atmos_probe" : dialog.background);
            SetBackgroundDim(dialog.dim);
            ShowDialog(SpeakerOrRandom(dialog.speakerId), dialog.lines, ShowGravityAssist, dialog.portraitLeft);
        }

        // ---------- Gravity assist and launch ----------

        public void ShowGravityAssist() => Show<GravityAssistScreen>();

        /// <summary>Launch pad bitmaps; stays on screen behind the launch dialogs.</summary>
        public LaunchStage Stage { get; private set; }
        public int LaunchIndex { get; private set; }

        public LaunchAttempt CurrentLaunch =>
            Content.launch.attempts[Mathf.Clamp(LaunchIndex, 0, Content.launch.attempts.Length - 1)];

        public void StartLaunch()
        {
            LaunchIndex = 0;
            if (Stage == null) Stage = LaunchStage.Create(_stageLayer, Content.launch);
            Stage.ResetState();
            Show<LaunchScreen>();
        }

        public void NextLaunchAttempt()
        {
            LaunchIndex = Mathf.Min(LaunchIndex + 1, Content.launch.attempts.Length - 1);
            Stage.ResetState();
            Show<LaunchScreen>();
        }

        /// <summary>Same date again (the last attempt missed the green zone).</summary>
        public void RetryLaunch() => Show<LaunchScreen>();

        public void ShowEpisodeEnd() => Show<EpisodeEndScreen>();

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

            _stageLayer = UIFactory.Rect("Stage", root).Stretch();
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

        Coroutine _dimFade;

        /// <summary>Fades the darkening layer over the render in (on) or out (off).</summary>
        public void SetBackgroundDim(bool on, float fadeSeconds = 0.8f)
        {
            if (_dimFade != null) StopCoroutine(_dimFade);
            _dimFade = StartCoroutine(FadeDim(on ? Theme.BackgroundDim.a : 0f, fadeSeconds));
        }

        System.Collections.IEnumerator FadeDim(float target, float duration)
        {
            var c = _backgroundDim.color;
            float from = c.a;
            for (float t = 0; t < duration; t += Time.unscaledDeltaTime)
            {
                c.a = Mathf.Lerp(from, target, Mathf.SmoothStep(0f, 1f, t / duration));
                _backgroundDim.color = c;
                yield return null;
            }
            c.a = target;
            _backgroundDim.color = c;
            _dimFade = null;
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
