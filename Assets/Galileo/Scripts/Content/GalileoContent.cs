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

        public CharacterProfile FindCharacter(string id)
        {
            foreach (var c in characters)
                if (c != null && c.id == id) return c;
            return null;
        }
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
        public string[] lines;
        public bool portraitLeft;
        public Action onFinished;
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
        /// <summary>Characters on the left and right of the system screen; each presents one option.</summary>
        public string[] speakers = Array.Empty<string>();
        public DesignOptionInfo[] options = Array.Empty<DesignOptionInfo>();
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
