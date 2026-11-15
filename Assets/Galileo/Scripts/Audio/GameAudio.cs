using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Galileo
{
    /// <summary>
    /// Sound effects and the looping background track. Clips come from Resources/Galileo/Sounds/&lt;name&gt;;
    /// a missing clip is simply silent.
    ///   click    — any button
    ///   purchase — money leaves the budget
    ///   popup    — a part snaps into place, a character pops up
    ///   coins    — a gas sample is collected
    ///   success  — a successful test, the finished assembly, the end of the mission
    ///   ambience_ep2 — background loop of episode 2
    /// </summary>
    public static class GameAudio
    {
        const string Folder = "Galileo/Sounds/";

        static AudioSource _sfx, _loop;
        static MonoBehaviour _host;
        static Coroutine _loopFade;
        static readonly Dictionary<string, AudioClip> Clips = new Dictionary<string, AudioClip>();

        public static void Init(MonoBehaviour host)
        {
            _host = host;
            _sfx = host.gameObject.AddComponent<AudioSource>();
            _sfx.playOnAwake = false;
            _loop = host.gameObject.AddComponent<AudioSource>();
            _loop.playOnAwake = false;
            _loop.loop = true;
            _loop.volume = 0f;
        }

        static AudioClip Clip(string name)
        {
            if (string.IsNullOrEmpty(name)) return null;
            if (!Clips.TryGetValue(name, out var clip))
            {
                clip = Resources.Load<AudioClip>(Folder + name);
                Clips[name] = clip;
            }
            return clip;
        }

        public static void Play(string name, float volume = 1f)
        {
            var clip = Clip(name);
            if (_sfx != null && clip != null) _sfx.PlayOneShot(clip, volume);
        }

        public static void Click() => Play("click", 0.7f);

        /// <summary>Starts (or keeps) the given loop, fading it in.</summary>
        public static void PlayLoop(string name, float volume = 0.55f)
        {
            if (_loop == null) return;
            var clip = Clip(name);
            if (clip == null) return;
            if (_loop.clip != clip || !_loop.isPlaying)
            {
                _loop.clip = clip;
                _loop.volume = 0f;
                _loop.Play();
            }
            FadeLoop(volume, 2f);
        }

        public static void StopLoop(float fadeSeconds = 1.2f) => FadeLoop(0f, fadeSeconds);

        static void FadeLoop(float target, float seconds)
        {
            if (_host == null) return;
            if (_loopFade != null) _host.StopCoroutine(_loopFade);
            _loopFade = _host.StartCoroutine(Fade(target, seconds));
        }

        static IEnumerator Fade(float target, float seconds)
        {
            float from = _loop.volume;
            for (float t = 0; t < seconds; t += Time.unscaledDeltaTime)
            {
                _loop.volume = Mathf.Lerp(from, target, t / seconds);
                yield return null;
            }
            _loop.volume = target;
            if (target <= 0f) _loop.Stop();
            _loopFade = null;
        }
    }
}
