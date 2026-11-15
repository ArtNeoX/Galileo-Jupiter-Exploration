using System;
using UnityEngine;

namespace Galileo.Content
{
    /// <summary>
    /// All game text. Loaded from Resources/Galileo/content.json so it can be edited without touching code.
    /// </summary>
    [Serializable]
    public class GalileoContent
    {
        public UiText ui = new UiText();
        public CharacterProfile[] characters = Array.Empty<CharacterProfile>();
        public ObjectivesData objectives = new ObjectivesData();
        public DialogData missionDialog = new DialogData();
        public ConceptsData concepts = new ConceptsData();
        public OrbiterDesignData orbiterDesign = new OrbiterDesignData();
        public DialogData atmosProbeDialog = new DialogData();
        public GravityAssistData gravityAssist = new GravityAssistData();
        public LaunchData launch = new LaunchData();
        public string episodeEndText = "КОНЕЦ 1 ЭПИЗОДА";
        public MenuData menu = new MenuData();
        public GarageData garage = new GarageData();
        public Episode2Data episode2 = new Episode2Data();

        public CharacterProfile FindCharacter(string id)
        {
            foreach (var c in characters)
                if (c != null && c.id == id) return c;
            return null;
        }

        public DesignSystemInfo FindSystem(string id)
        {
            foreach (var s in orbiterDesign.systems)
                if (s != null && s.id == id) return s;
            return null;
        }
    }

    /// <summary>Main menu and the opening date card.</summary>
    [Serializable]
    public class MenuData
    {
        public string title = "Galileo";
        public string subtitle = "Jupiter Exploration";
        public string play = "PLAY";
        public string quit = "QUIT";
        public string introDate = "OCTOBER 1ST 1977";
    }

    /// <summary>
    /// Orbiter assembly in the garage. Every bitmap lives in Resources/Galileo/Garage/ and is painted on the same
    /// canvas (same size), so laying them all over each other shows the finished probe.
    /// </summary>
    [Serializable]
    public class GarageData
    {
        public string title = "ORBITER ASSEMBLY";
        public string hint = "Drag each part onto its place on the silhouette.";
        public string infoTitle = "PART INFO";
        public string infoPlaceholder = "Hover over a part to see what it does.";
        public string checklistTitle = "CHECKLIST";
        public string completeMessage = "All essential systems are attached.";
        public string outlineFile = "outline";
        public string structureFile = "main_structure";
        /// <summary>Placeholder rectangle of the main structure until its bitmap exists (centre x, y; size w, h; 0..1, y up).</summary>
        public float structureX = 0.5f, structureY = 0.52f, structureW = 0.16f, structureH = 0.36f;
        /// <summary>Character id for the "do we need it?" question; empty picks a random team member.</summary>
        public string optionalSpeakerId = "";
        public string[] optionalLines = { "Are you sure we need it? It costs additional money." };
        /// <summary>What the same team member says after the optional part is attached anyway.</summary>
        public string[] overBudgetLines = { "You have spent our entire budget!" };
        public string attachAnyway = "ATTACH ANYWAY";
        public string leaveIt = "LEAVE IT";
        public GarageElement[] elements = Array.Empty<GarageElement>();
    }

    [Serializable]
    public class GarageElement
    {
        public string id;
        /// <summary>Bitmap name in Resources/Galileo/Garage/ (defaults to the id).</summary>
        public string file;
        public string name;
        public string info;
        /// <summary>Not on the checklist; attaching it asks first and costs <see cref="cost"/>.</summary>
        public bool optional;
        public long cost;
        /// <summary>Placeholder rectangle until the bitmap exists (centre x, y; size w, h; 0..1 of the canvas, y up).</summary>
        public float x = 0.5f, y = 0.5f, w = 0.1f, h = 0.1f;
    }

    /// <summary>Episode 2: arrival at Jupiter, the probe release and the descent mini-game.</summary>
    [Serializable]
    public class Episode2Data
    {
        public string approachText = "150 DAYS UNTIL THE JUPITER APPROACH\nMAIN ANTENNA HAS FAILED";
        public string switchAntenna = "SWITCH TO LOW-GAIN ANTENNA";
        public string abortMission = "ABORT MISSION";
        public string noAntennaNote = "There is no low-gain antenna on board.";
        public string gameOver = "GAME OVER";
        public string separationHint = "Galileo has reached Jupiter. Release the atmospheric probe.";
        public string separate = "SEPARATE";
        public string collectHint = "Catch the gas samples before they disappear!";
        public string collectedFormat = "SAMPLES {0} / {1}";
        public string sendData = "SEND DATA";
        public string successText = "SUCCESSFUL MISSION";
        public string[] gases = { "H₂", "He", "CH₄", "NH₃", "H₂O", "H₂S", "C₂H₆", "C₂H₂" };
        public int sampleCount = 20;
        public float timeLimit = 60f;
        public float sampleSeconds = 1f;
    }

    [Serializable]
    public class UiText
    {
        public string next = "Далее";
        public string clickToContinue = "Нажмите в любом месте, чтобы продолжить";
        public string viewedFormat = "Изучено: {0} / {1}";
        public string selectedFormat = "Выбрано: {0}";
        public string nothingSelected = "Выбрано: —";
        public string prosLabel = "Преимущества";
        public string consLabel = "Ограничения";
        public string educationLabel = "Образование";
        public string specializationLabel = "Специализация";
        public string teamRoleLabel = "Роль в команде";
        public string teamTitle = "Знакомство с командой";
        public string teamSubtitle = "";
        public string endTitle = "Продолжение следует";
        public string endBody = "Следующие сцены будут добавлены позже.";
        public string chosenFormat = "Ваш выбор: {0}";
        public string restart = "Начать заново";
        public string explanationPlaceholder = "[Пояснение будет добавлено позже]";

        // Scene 4
        public string back = "Назад";
        public string test = "Испытать";
        public string tryAgain = "Попробовать снова";
        public string testInProgress = "Идёт испытание";
        public string standBy = "Пожалуйста, ожидайте";
        public string successfulTest = "Испытание успешно";
        public string unsuccessfulTest = "Испытание не пройдено";
        public string problemLabel = "Инженерная проблема";
        public string behaviorLabel = "Поведение в космической среде";
        public string whyChosenLabel = "Почему выбран для Galileo";
        public string whyNotChosenLabel = "Почему не выбран для Galileo";
        public string recommendationLabel = "Рекомендация инженерной группы";
        public string finalDecisionLabel = "Итоговое решение";
        public string designedFormat = "Спроектировано систем: {0} из {1}";

        // Scene 4 analyses
        public string analysis = "Анализ";
        public string info = "Инфо";
        public string returnLabel = "Вернуться";
        public string check = "Проверить";
        public string analysisRequired = "Сначала выполните анализ";
        public string analysisDone = "Анализ выполнен";

        // Intros and economy
        public string orbiterProbeIntro = "ORBITER PROBE";
        public string atmosProbeIntro = "ATMOSPHERIC PROBE";
        public string budgetLabel = "BUDGET";
    }

    [Serializable]
    public class CharacterProfile
    {
        /// <summary>Also the portrait file name in Resources/Galileo/Characters/.</summary>
        public string id;
        public string name;
        public string role;
        public string bio;
        public string education;
        public string specialization;
        public string teamRole;
    }

    [Serializable]
    public class ObjectivesData
    {
        public string title = "Цели миссии";
        public string subtitle = "";
        public string hint = "";
        public ObjectiveInfo[] items = Array.Empty<ObjectiveInfo>();
    }

    [Serializable]
    public class ObjectiveInfo
    {
        public string number;
        public string title;
        public string info;
        /// <summary>Procedural icon id, or a sprite name in Resources/Galileo/Icons/.</summary>
        public string icon;
    }

    [Serializable]
    public class DialogData
    {
        /// <summary>Character id. Leave empty to pick a random team member.</summary>
        public string speakerId;
        public string[] lines = Array.Empty<string>();
        /// <summary>Optional render in Resources/Galileo/Backgrounds the background fades to when this dialog opens.</summary>
        public string background;
        /// <summary>Darken the background render behind this dialog (off lets the render's details show).</summary>
        public bool dim = true;
        /// <summary>Put the character on the left instead of the centre, leaving the render visible.</summary>
        public bool portraitLeft;
    }

    [Serializable]
    public class ConceptsData
    {
        public string title = "Концепция миссии";
        public string subtitle = "";
        public string hint = "";
        public ConceptInfo[] items = Array.Empty<ConceptInfo>();
    }

    [Serializable]
    public class ConceptInfo
    {
        public string letter;
        public string title;
        public string subtitle;
        public string info;
        public string pros;
        public string cons;
        public string icon;
        /// <summary>The historically chosen variant. Not shown to the player; kept for later scenes.</summary>
        public bool correct;
        /// <summary>Render in Resources/Galileo/Backgrounds the background fades to when this concept is selected.</summary>
        public string background;
        /// <summary>What a random team member says when this (wrong) concept is chosen.</summary>
        public string[] wrongChoiceLines = Array.Empty<string>();
    }

    /// <summary>Runtime-only: what the dialog screen should play and where to go afterwards.</summary>
    public class DialogRequest
    {
        public string speakerId;
        /// <summary>Reads the lines from the current content, so a language switch re-reads them.</summary>
        public Func<GalileoContent, string[]> lines;
        public bool portraitLeft;
        /// <summary>Overrides the character's name (for speakers who aren't team members).</summary>
        public Func<GalileoContent, string> speakerName;
        public Action onFinished;
        /// <summary>Line being shown; kept so a language switch resumes on the same line.</summary>
        public int index;
    }

    /// <summary>Runtime-only: a full-screen title card (black, text in the centre).</summary>
    public class IntroRequest
    {
        public Func<GalileoContent, string> text;
        public float hold = 1.6f;
        /// <summary>Runs once the screen is fully black (swap the background here).</summary>
        public Action onBlack;
        public Action onDone;
        /// <summary>Wait for a click / key before continuing.</summary>
        public bool waitForInput;
        /// <summary>The screen is already black (coming from another black screen).</summary>
        public bool startBlack;
        public Color textColor = new Color(0.95f, 0.89f, 0.78f);
    }

    /// <summary>Gravity assist scene: pick the launch window on a heliocentric map.</summary>
    [Serializable]
    public class GravityAssistData
    {
        public string title = "Гравитационный манёвр";
        public string subtitle = "";
        /// <summary>Slider range and the correct launch window, as yyyy-MM-dd.</summary>
        public string rangeStart = "1989-10-01";
        public string rangeEnd = "1989-12-31";
        public string windowStart = "1989-10-12";
        public string windowEnd = "1989-11-21";
        /// <summary>A handle turns fully green when it is this many days from its correct date or closer.</summary>
        public int colorRangeDays = 30;
        public string[] monthsShort = { "ЯНВ", "ФЕВ", "МАР", "АПР", "МАЙ", "ИЮН", "ИЮЛ", "АВГ", "СЕН", "ОКТ", "НОЯ", "ДЕК" };
        public string sun = "Солнце";
        public string venus = "Венера";
        public string earth = "Земля";
        public string jupiter = "Юпитер";
        /// <summary>{0} = date the planets are shown at (the start of the chosen window).</summary>
        public string positionsFormat = "Положение планет на {0}";
        public string startLabel = "Начало окна";
        public string endLabel = "Конец окна";
        public string hint = "";
        public string correctMessage = "";
    }

    /// <summary>Launch scene: the three launch attempts of STS-34 with the timing mini-game.</summary>
    [Serializable]
    public class LaunchData
    {
        /// <summary>{0} minutes, {1} opens, {2} closes (UTC).</summary>
        public string windowFormat = "Окно запуска: {0} мин ({1}–{2} UTC)";
        public string utcLabel = "UTC";
        public string hint = "Нажмите ПРОБЕЛ или кликните, когда указатель в зелёной зоне";
        public string perfect = "Идеально!";
        public string good = "Почти";
        public string miss = "Мимо";
        public string retry = "Запуск не удался — попробуйте ещё раз";
        public string windowClosed = "Окно запуска закрылось — попробуйте ещё раз";
        /// <summary>Launch-window minutes that pass per real second (the UTC clock runs this much faster).</summary>
        public float windowMinutesPerSecond = 1f;
        /// <summary>Full left-right-left sweeps of the pointer per second.</summary>
        public float pointerSpeed = 0.45f;
        /// <summary>Widths of the zones as a fraction of the bar.</summary>
        public float greenZone = 0.07f;
        public float yellowZone = 0.3f;
        /// <summary>Where the engine-failure cross goes, in the shuttle bitmap's normalised coordinates.</summary>
        public float crossX = 0.5f;
        public float crossY = 0.38f;
        public string successSpeakerId = "";
        public string[] successLines = Array.Empty<string>();
        public LaunchAttempt[] attempts = Array.Empty<LaunchAttempt>();
    }

    [Serializable]
    public class LaunchAttempt
    {
        public string date;
        public int windowMinutes;
        public string utcStart;
        public string utcEnd;
        /// <summary>engineFailure | badWeather | success</summary>
        public string outcome;
        /// <summary>Character id; empty picks a random team member.</summary>
        public string speakerId;
        public string[] lines = Array.Empty<string>();
    }

    [Serializable]
    public class OrbiterDesignData
    {
        public string title = "Проектирование космического зонда";
        public string subtitle = "";
        public string hint = "";
        public DesignSystemInfo[] systems = Array.Empty<DesignSystemInfo>();
    }

    [Serializable]
    public class DesignSystemInfo
    {
        public string id;
        public string title;
        public string subtitle;
        public string problem;
        public string task;
        public string finalDecision;
        /// <summary>Taken from the budget when this system passes its test (decorative economy).</summary>
        public long cost;
        /// <summary>Characters on the left and right of the system screen; each presents one option.</summary>
        public string[] speakers = Array.Empty<string>();
        public DesignOptionInfo[] options = Array.Empty<DesignOptionInfo>();
        /// <summary>The ANALYSIS button on the system screen. No type = no button.</summary>
        public AnalysisData analysis = new AnalysisData();
    }

    /// <summary>
    /// What the ANALYSIS button on a system screen opens:
    ///   mass     — calculate the mass of an equal volume of each material (uses each option's density)
    ///   matching — sort both options' pros and cons (bullet lines of their pros / cons) into four columns
    ///   engineer — a short dialog with an engineer bitmap (no player task)
    /// </summary>
    [Serializable]
    public class AnalysisData
    {
        public string type;
        /// <summary>The options' Test buttons stay locked until this analysis is completed.</summary>
        public bool required;
        public string title = "";
        public string hint = "";
        public string successMessage = "";
        public string errorMessage = "";

        // mass
        /// <summary>Sample volume in m³.</summary>
        public float volume = 0.1f;
        public string volumeFormat = "V = {0} м³";
        public string massLabel = "m =";
        public string massUnit = "кг";
        /// <summary>An answer counts when it is within this fraction of the exact mass.</summary>
        public float tolerance = 0.005f;
        public string infoTitle = "";

        // engineer
        /// <summary>Bitmap in Resources/Galileo/Characters/ (needn't be a team member).</summary>
        public string speakerId = "engineer_angry";
        public string speakerName = "";
        public string[] lines = Array.Empty<string>();
    }

    [Serializable]
    public class DesignOptionInfo
    {
        public string name;
        public string icon;
        public string behavior;
        public string pros;
        public string cons;
        public string why;
        public bool correct;
        public string resultTitle;
        public string result;
        public string recommendation;
        /// <summary>Material density in kg/m³ (mass analysis).</summary>
        public float density;
        /// <summary>Sample bitmap in Resources/Galileo/Analysis/ for the mass analysis (square).</summary>
        public string sample;
        /// <summary>If set, the option screen shows this long text instead of behaviour / pros / cons.</summary>
        public string detailText;
    }

    public static class ContentLoader
    {
        public static GalileoContent Load(string resourcePath)
        {
            var asset = Resources.Load<TextAsset>(resourcePath);
            if (asset == null)
            {
                Debug.LogError($"[Galileo] Content file not found at Resources/{resourcePath}.json");
                return new GalileoContent();
            }
            return JsonUtility.FromJson<GalileoContent>(asset.text) ?? new GalileoContent();
        }

        /// <summary>
        /// Loads an image from Resources whether it was imported as a Sprite or as a plain texture,
        /// so dropped-in PNGs work without changing import settings.
        /// </summary>
        public static Sprite LoadSprite(string resourcePath)
        {
            var sprite = Resources.Load<Sprite>(resourcePath);
            if (sprite != null) return sprite;
            var tex = Resources.Load<Texture2D>(resourcePath);
            if (tex == null) return null;
            return Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f), 100f);
        }
    }
}
