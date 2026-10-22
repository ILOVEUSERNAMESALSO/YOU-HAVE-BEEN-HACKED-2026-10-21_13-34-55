using System;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UIElements;

namespace Checkpoint
{
    [RequireComponent(typeof(UIDocument))]
    public sealed class GameManager : MonoBehaviour
    {
        public ShiftManager Shift { get; private set; }
        public UIController Interface { get; private set; }
        AudioSource audioSource;
        AudioClip clickClip, stampClip;
        public bool SoundEnabled { get; set; } = true;
        public bool IsProbe { get; private set; }

        void Start()
        {
            Application.targetFrameRate = 60;
            IsProbe = Array.Exists(Environment.GetCommandLineArgs(), a => a == "-checkpointProbe");
            try
            {
                Shift = new ShiftManager(CaseRepository.Load());
                Shift.Changed += () => { if (!IsProbe) SaveManager.Write(Shift.State); };
                audioSource = gameObject.AddComponent<AudioSource>();
                audioSource.playOnAwake = false;
                clickClip = MakeTone("terminal key", 420, .045f, .08f);
                stampClip = MakeTone("decision stamp", 130, .19f, .15f);
                var document = GetComponent<UIDocument>();
                if (document.panelSettings == null) document.panelSettings = Resources.Load<PanelSettings>("Checkpoint/CheckpointPanel");
                if (document.panelSettings == null) throw new InvalidOperationException("Missing checkpoint PanelSettings. Run Checkpoint > Create or repair scene assets.");
                Interface = new UIController(document.rootVisualElement, this);
                Interface.ShowTitle();
                if (IsProbe) gameObject.AddComponent<RuntimeProbe>();
            }
            catch (Exception ex)
            {
                Debug.LogException(ex);
                var root = GetComponent<UIDocument>().rootVisualElement;
                root.Add(new Label("Checkpoint initialization failed. Check the Console.\n" + ex.Message));
            }
        }
        void Update()
        {
            var keyboard = Keyboard.current;
            if (keyboard == null || Interface == null) return;
            if (keyboard.f1Key.wasPressedThisFrame) Interface.OpenRules();
            if (keyboard.escapeKey.wasPressedThisFrame) Interface.CancelModal();
        }
        public void Cue(bool stamp = false)
        {
            if (SoundEnabled && audioSource != null) audioSource.PlayOneShot(stamp ? stampClip : clickClip);
        }
        static AudioClip MakeTone(string name, float frequency, float duration, float amplitude)
        {
            const int rate = 22050;
            int count = Mathf.CeilToInt(rate * duration);
            var samples = new float[count];
            for (int i = 0; i < count; i++)
            {
                float t = (float)i / rate;
                float envelope = Mathf.Min(1, t * 200) * Mathf.Pow(1f - (float)i / count, 3);
                samples[i] = Mathf.Sin(t * frequency * Mathf.PI * 2) * envelope * amplitude;
            }
            var clip = AudioClip.Create(name, count, 1, rate, false);
            clip.SetData(samples, 0); return clip;
        }
        void OnDestroy()
        {
            if (clickClip != null) Destroy(clickClip);
            if (stampClip != null) Destroy(stampClip);
        }
    }
}
