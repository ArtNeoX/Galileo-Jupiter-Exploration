using System;
using System.Collections.Generic;
using Galileo.Content;
using Galileo.Screens;
using Galileo.UI;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;
using Random = UnityEngine.Random;

namespace Galileo
{
    /// <summary>
    /// Entry point. Builds the canvas and background, then runs the screens in order:
    /// Menu → "October 1st 1977" → Scene 1 (characters) → 2 (objectives) → 2.1 (dialog) → 3 (mission concept)
    /// → "Orbiter probe" → 4 (orbiter design) → garage assembly → "Atmospheric probe" → atmospheric probe dialog
    /// → gravity assist → launch (3 attempts) → end of episode 1
    /// → episode 2: antenna failure → separation → descent mini-game → successful mission / game over → menu.
    /// </summary>
    [DisallowMultipleComponent]
    public class GalileoApp : MonoBehaviour
    {
        [Tooltip("Path of the Russian content JSON inside a Resources folder, without the extension. " +
                 "The other languages sit next to it as <path>_en and <path>_kk.")]
        [SerializeField] string contentPath = "Galileo/content";

        [Tooltip("Render in Resources/Galileo/Backgrounds shown until a mission concept is selected.")]
        [SerializeField] string defaultBackground = "render_conceptB";

        public const string MenuBackground = "bg_menu";
        public const string GarageBackground = "garage";

        public GalileoContent Content { get; private set; }

        RectTransform _screens, _stageLayer, _overlay;
        ScreenBase _current;
        Action _rebuild;

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
            LoadContent();
            EnsureEventSystem();
            GameAudio.Init(this);
            BuildCanvas();
        }

        void Start() => ShowMenu();

        // ---------- Languages: ENG (default) → KZ → RU ----------

        static readonly string[] LanguageLabels = { "ENG", "KZ", "RU" };
        static readonly string[] LanguageSuffixes = { "_en", "_kk", "" };
        int _language;

        public bool IsEnglish => _language == 0;
        Text _languageLabel;
        Button _languageButton;

        void LoadContent()
        {
            var path = contentPath + LanguageSuffixes[_language];
            Content = Resources.Load<TextAsset>(path) != null ? ContentLoader.Load(path) : ContentLoader.Load(contentPath);
        }

        public void NextLanguage()
        {
            _language = (_language + 1) % LanguageLabels.Length;
            LoadContent();
            _languageLabel.text = LanguageLabels[_language];
            _budget.SetLabel(Content.ui.budgetLabel);
            if (_current != null && _current.AllowLanguageToggle) _rebuild?.Invoke();
        }

        // ---------- Menu and game state ----------

        public void ShowMenu()
        {
            SetBackground(MenuBackground);
            SetBackgroundDim(false);
            Show<MenuScreen>();
        }

        public void StartGame()
        {
            ResetState();
            ShowIntro(new IntroRequest
            {
                text = c => c.menu.introDate,
                hold = 2f,
                onBlack = () =>
                {
                    SetBackground(defaultBackground, 0f);
                    SetBackgroundDim(true, 0f);
                },
                onDone = ShowCharacters
            });
        }

        public void ReturnToMenu()
        {
            ResetState();
            GameAudio.StopLoop();
            ShowMenu();
        }

        public void Restart() => ReturnToMenu();

        void ResetState()
        {
            _conceptIndex = -1;
            CompletedSystems.Clear();
            AnalysedSystems.Clear();
            MatchingPlacement.Clear();
            GaragePlaced.Clear();
            _optionOrder.Clear();
            ApproachShown = false;
            if (Stage != null) Destroy(Stage.gameObject);
            Stage = null;
            Budget = StartBudget;
            _budget.SetAmount(Budget);
        }

        public void ShowCharacters() => Show<CharacterIntroScreen>();
        public void ShowObjectives() => Show<ObjectivesScreen>();
        public void ShowConcepts() => Show<ConceptsScreen>();
        public void ShowEnd() => Show<EndScreen>();

        // ---------- Title cards ----------

        public IntroRequest PendingIntro { get; private set; }

        public void ShowIntro(IntroRequest request)
        {
            PendingIntro = request;
            Show<IntroScreen>();
        }

        // ---------- Economy (decorative) ----------

        public long Budget { get; private set; }

        /// <summary>The design systems and the essential garage parts add up to the starting budget, so it ends at exactly $0.</summary>
        public long StartBudget
        {
            get
            {
                long sum = 0;
                foreach (var s in Content.orbiterDesign.systems) sum += s.cost;
                foreach (var e in Content.garage.elements)
                    if (!e.optional) sum += e.cost;
                return sum;
            }
        }

        public void Spend(long amount)
        {
            if (amount == 0) return;
            Budget -= amount;
            _budget.Spend(amount, Budget);
            GameAudio.Play("purchase");
        }

        // ---------- Dialogs (Scene 2.1 and the wrong-concept explanation) ----------

        /// <summary>The dialog the next <see cref="DialogScreen"/> plays.</summary>
        public DialogRequest PendingDialog { get; private set; }

        /// <summary>Lines and name are read from the current content, so switching language re-reads them.</summary>
        public void ShowDialog(string speakerId, Func<GalileoContent, string[]> lines, Action onFinished,
            bool portraitLeft = false, Func<GalileoContent, string> speakerName = null)
        {
            PendingDialog = new DialogRequest
            {
                speakerId = speakerId, lines = lines, onFinished = onFinished, portraitLeft = portraitLeft,
                speakerName = speakerName
            };
            Show<DialogScreen>();
        }

        public void ShowDialog(string speakerId, string[] lines, Action onFinished, bool portraitLeft = false,
            string speakerName = null) =>
            ShowDialog(speakerId, _ => lines, onFinished, portraitLeft,
                speakerName == null ? null : (Func<GalileoContent, string>)(_ => speakerName));

        /// <summary>The given character id, or a random team member when it's empty.</summary>
        public string SpeakerOrRandom(string speakerId)
        {
            if (!string.IsNullOrEmpty(speakerId) || Content.characters.Length == 0) return speakerId;
            return Content.characters[Random.Range(0, Content.characters.Length)].id;
        }

        public void ShowMissionDialog() =>
            ShowDialog(Content.missionDialog.speakerId, c => c.missionDialog.lines, ShowConcepts);

        int _conceptIndex = -1;

        public ConceptInfo SelectedConcept
        {
            get
            {
                var items = Content.concepts.items;
                return _conceptIndex >= 0 && _conceptIndex < items.Length ? items[_conceptIndex] : null;
            }
            set => _conceptIndex = value == null ? -1 : Array.IndexOf(Content.concepts.items, value);
        }

        /// <summary>Scene 3 → Next. Only the correct concept moves on; otherwise a random team member explains why not.</summary>
        public void ConfirmConcept(ConceptInfo concept)
        {
            SelectedConcept = concept;
            if (concept.correct)
            {
                ShowIntro(new IntroRequest { text = c => c.ui.orbiterProbeIntro, hold = 1.4f, onDone = ShowOrbiterDesign });
                return;
            }
            var people = Content.characters;
            var speaker = people.Length > 0 ? people[Random.Range(0, people.Length)].id : null;
            int index = _conceptIndex;
            ShowDialog(speaker, c =>
            {
                var lines = index >= 0 && index < c.concepts.items.Length ? c.concepts.items[index].wrongChoiceLines : null;
                return lines != null && lines.Length > 0 ? lines : new[] { c.ui.explanationPlaceholder };
            }, ShowConcepts);
        }

        // ---------- Scene 4: Orbiter probe design ----------

        public readonly HashSet<string> CompletedSystems = new HashSet<string>();

        string _systemId;
        int _optionIndex;

        public DesignSystemInfo CurrentSystem => Content.FindSystem(_systemId);

        public DesignOptionInfo CurrentOption
        {
            get
            {
                var system = CurrentSystem;
                return system != null && _optionIndex >= 0 && _optionIndex < system.options.Length ? system.options[_optionIndex] : null;
            }
        }

        /// <summary>Per system: the display order of its options, as indices (kept for the whole game).</summary>
        readonly Dictionary<string, int[]> _optionOrder = new Dictionary<string, int[]>();

        public void ShowOrbiterDesign() => Show<SystemsListScreen>();

        public void ShowSystem(DesignSystemInfo system)
        {
            _systemId = system.id;
            Show<SystemScreen>();
        }

        public void ShowOption(DesignOptionInfo option)
        {
            _optionIndex = Array.IndexOf(CurrentSystem.options, option);
            Show<OptionScreen>();
        }

        public void RunTest() => Show<TestScreen>();

        // ---------- Scene 4 analyses (the ANALYSIS button on a system screen) ----------

        /// <summary>Systems whose analysis the player has completed.</summary>
        public readonly HashSet<string> AnalysedSystems = new HashSet<string>();

        /// <summary>Pros/cons matching: where each card currently is, per system (kept while the player reads the options).</summary>
        public readonly Dictionary<string, int[]> MatchingPlacement = new Dictionary<string, int[]>();

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
                    var id = system.id;
                    ShowDialog(analysis.speakerId, c => c.FindSystem(id)?.analysis.lines, CompleteAnalysis, false,
                        c => c.FindSystem(id)?.analysis.speakerName);
                    break;
            }
        }

        /// <summary>Marks the current system's analysis done and returns to its screen.</summary>
        public void CompleteAnalysis()
        {
            AnalysedSystems.Add(_systemId);
            ShowSystem(CurrentSystem);
        }

        public void MarkAnalysed() => AnalysedSystems.Add(_systemId);

        /// <summary>Dev shortcut L: skips the current mini-game.</summary>
        void Update()
        {
            var k = UnityEngine.InputSystem.Keyboard.current;
            if (k == null || !k.lKey.wasPressedThisFrame) return;
            if (_current is SystemsListScreen || _current is SystemScreen ||
                _current is OptionScreen || _current is TestScreen ||
                _current is MassAnalysisScreen || _current is MatchingAnalysisScreen)
                SkipOrbiterDesign();
            else if (_current is GarageScreen garage)
                garage.DevPlaceAll();
            else if (_current is DescentScreen descent)
                descent.DevCollectAll();
            else if (_current is GravityAssistScreen gravity)
                gravity.SetWindow(gravity.CorrectStartDay, gravity.CorrectEndDay);
            // LaunchScreen handles L itself (stops the pointer in the green zone).
        }

        public void SkipOrbiterDesign()
        {
            foreach (var system in Content.orbiterDesign.systems)
                if (CompletedSystems.Add(system.id)) Spend(system.cost);
            ShowGarage();
        }

        public void CompleteCurrentSystem()
        {
            var system = CurrentSystem;
            if (CompletedSystems.Add(system.id)) Spend(system.cost);
            ShowOrbiterDesign();
        }

        // ---------- Garage: orbiter assembly ----------

        /// <summary>Ids of the garage parts already attached to the orbiter.</summary>
        public readonly HashSet<string> GaragePlaced = new HashSet<string>();

        /// <summary>True when an optional part (the low-gain antenna) was attached in the garage.</summary>
        public bool HasLowGainAntenna
        {
            get
            {
                foreach (var e in Content.garage.elements)
                    if (e.optional && GaragePlaced.Contains(e.id)) return true;
                return false;
            }
        }

        public void ShowGarage()
        {
            SetBackground(GarageBackground);
            SetBackgroundDim(false);
            Show<GarageScreen>();
        }

        /// <summary>Garage → "Atmospheric probe" → the atmospheric probe dialog.</summary>
        public void FinishGarage()
        {
            ShowIntro(new IntroRequest
            {
                text = c => c.ui.atmosProbeIntro,
                hold = 1.4f,
                onBlack = () =>
                {
                    var dialog = Content.atmosProbeDialog;
                    SetBackground(string.IsNullOrEmpty(dialog.background) ? "render_conceptC_Atmos_probe" : dialog.background, 0f);
                    SetBackgroundDim(dialog.dim, 0f);
                },
                onDone = ShowAtmosProbeDialog
            });
        }

        // ---------- Scene 5: Atmospheric probe ----------

        /// <summary>
        /// A random team member introduces the atmospheric probe while the background fades to the
        /// atmospheric probe render.
        /// </summary>
        public void ShowAtmosProbeDialog()
        {
            var dialog = Content.atmosProbeDialog;
            SetBackground(string.IsNullOrEmpty(dialog.background) ? "render_conceptC_Atmos_probe" : dialog.background);
            SetBackgroundDim(dialog.dim);
            ShowDialog(SpeakerOrRandom(dialog.speakerId), c => c.atmosProbeDialog.lines, ShowGravityAssist, dialog.portraitLeft);
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

        // ---------- Episode 2 ----------

        /// <summary>The approach title has played once (a language switch shows the buttons straight away).</summary>
        public bool ApproachShown { get; set; }

        /// <summary>Called on a black screen at the end of episode 1.</summary>
        public void StartEpisode2()
        {
            if (Stage != null) Destroy(Stage.gameObject);
            Stage = null;
            SetBackground("jupiter_distanced", 0f);
            SetBackgroundDim(false, 0f);
            GameAudio.PlayLoop("ambience_ep2");
            Show<ApproachScreen>();
        }

        public void ShowSeparation()
        {
            SetBackground("jupiter_close");
            SetBackgroundDim(false);
            Show<SeparationScreen>();
        }

        public void ShowDescent() => Show<DescentScreen>();

        public void GameOver()
        {
            ShowIntro(new IntroRequest
            {
                text = c => c.episode2.gameOver,
                hold = 2.4f,
                textColor = Theme.Failure,
                onBlack = () =>
                {
                    GameAudio.StopLoop();
                    SetBackground(MenuBackground, 0f);
                    SetBackgroundDim(false, 0f);
                },
                onDone = ReturnToMenu
            });
        }

        public void MissionSuccess()
        {
            GameAudio.Play("success");
            ShowIntro(new IntroRequest
            {
                text = c => c.episode2.successText,
                hold = 0.6f,
                waitForInput = true,
                textColor = Theme.Success,
                onBlack = () =>
                {
                    SetBackground(MenuBackground, 0f);
                    SetBackgroundDim(false, 0f);
                },
                onDone = ReturnToMenu
            });
        }

        /// <summary>
        /// The two options in a randomly shuffled order (so the right answer isn't always "A"),
        /// kept the same for the rest of the session so retries don't reshuffle.
        /// </summary>
        public DesignOptionInfo[] OptionsInOrder(DesignSystemInfo system)
        {
            system = Content.FindSystem(system.id) ?? system;
            if (!_optionOrder.TryGetValue(system.id, out var order) || order.Length != system.options.Length)
            {
                order = new int[system.options.Length];
                for (int i = 0; i < order.Length; i++) order[i] = i;
                for (int i = order.Length - 1; i > 0; i--)
                {
                    int j = Random.Range(0, i + 1);
                    (order[i], order[j]) = (order[j], order[i]);
                }
                _optionOrder[system.id] = order;
            }
            var result = new DesignOptionInfo[order.Length];
            for (int i = 0; i < order.Length; i++) result[i] = system.options[order[i]];
            return result;
        }

        // ---------- Screens ----------

        void Show<T>() where T : ScreenBase => Show<T>(false);

        void Show<T>(bool instant) where T : ScreenBase
        {
            _rebuild = () => Show<T>(true);
            if (_current != null) Destroy(_current.gameObject);
            var rt = UIFactory.Rect(typeof(T).Name, _screens).Stretch();
            var fade = rt.gameObject.AddComponent<CanvasGroup>();
            fade.alpha = instant ? 1f : 0f;
            var screen = rt.gameObject.AddComponent<T>();
            _current = screen;
            screen.Init(this);
            if (_current != screen) return; // Build already moved on to another screen
            UpdateOverlay();
            if (!_current.FadesIn) fade.alpha = 1f;
            else if (!instant) _current.StartCoroutine(FadeIn(fade));
        }

        void UpdateOverlay()
        {
            _languageButton.gameObject.SetActive(_current.AllowLanguageToggle);
            _budget.SetVisible(_current.ShowBudget);
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

        BudgetHud _budget;

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
            SetBackground(MenuBackground, 0f);

            _stageLayer = UIFactory.Rect("Stage", root).Stretch();
            _screens = UIFactory.Rect("Screens", root).Stretch();

            // Always on top: language switch and budget in the top-right corner.
            _overlay = UIFactory.Rect("Overlay", root).Stretch();
            _languageButton = UIFactory.Button("Language", _overlay, LanguageLabels[_language], NextLanguage);
            _languageButton.GetComponent<RectTransform>().Pin(new Vector2(1, 1), new Vector2(92, 42), new Vector2(-24, -12));
            _languageLabel = _languageButton.transform.Find("Label").GetComponent<Text>();
            _languageLabel.fontSize = 22;
            _budget = BudgetHud.Create(_overlay);
            _budget.SetLabel(Content.ui.budgetLabel);
            Budget = StartBudget;
            _budget.SetAmount(Budget);
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
