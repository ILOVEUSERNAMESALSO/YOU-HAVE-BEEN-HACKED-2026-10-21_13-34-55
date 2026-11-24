using System;
using System.IO;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public static class PassengerChatSetup
{
    public const string ScenePath = "Assets/Scenes/GameIntegration.unity";
    private const string CommandPath = "Library/PassengerChatCommand.txt";
    private const string ResultPath = "Library/PassengerChatResult.txt";

    [InitializeOnLoadMethod]
    static void Listen() { EditorApplication.update -= RunQueued; EditorApplication.update += RunQueued; }

    // Explicit local commands only; importing the script alone never changes a scene.
    static void RunQueued()
    {
        if (EditorApplication.isCompiling || EditorApplication.isUpdating || !File.Exists(CommandPath)) return;
        string command = File.ReadAllText(CommandPath).Trim();
        File.Delete(CommandPath);
        try
        {
            if (command == "build") CreateIntegration();
            else if (command == "validate") Validate();
            else if (command == "play") EditorApplication.isPlaying = true;
            else if (command == "stop") EditorApplication.isPlaying = false;
            else if (command == "live-checks") { PassengerChatChecks.RunLive(); return; }
            else throw new Exception("Unknown passenger integration command.");
            File.WriteAllText(ResultPath, "PASS: " + command);
        }
        catch (Exception e) { File.WriteAllText(ResultPath, "FAIL: " + command + "\n" + e); Debug.LogException(e); }
    }

    [MenuItem("Tools/Checkpoint/Create Passenger Chat Integration")]
    public static void CreateIntegration()
    {
        if (EditorApplication.isPlaying) throw new Exception("Exit Play mode before creating the scene.");
        if (File.Exists(ScenePath)) throw new Exception("GameIntegration already exists; it will not be overwritten.");
        for (int i = 0; i < SceneManager.sceneCount; i++)
            if (SceneManager.GetSceneAt(i).isDirty) throw new Exception("Save your open scenes first.");
        if (!AssetDatabase.CopyAsset("Assets/Scenes/Game.unity", ScenePath)) throw new Exception("Could not copy Game.");
        var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        var decision = UnityEngine.Object.FindAnyObjectByType<Decision>();
        var canvas = decision.GetComponentInParent<Canvas>().rootCanvas;
        var andrew = EditorSceneManager.OpenScene("Assets/Scenes/TheAndrewUnityFile.unity", OpenSceneMode.Additive);
        GameObject source = andrew.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<Transform>(true))
            .First(t => t.name == "chatpanel").gameObject;
        var panel = UnityEngine.Object.Instantiate(source);
        SceneManager.MoveGameObjectToScene(panel, scene);
        panel.transform.SetParent(canvas.transform, false);
        panel.name = "PassengerChatPanel";
        EditorSceneManager.CloseScene(andrew, true);
        SceneManager.SetActiveScene(scene);

        var dark = new Color(.055f, .10f, .13f, 1);
        var ink = new Color(.87f, .95f, .93f, 1);
        var font = decision.passportNameText.font;
        Rect(panel.transform, new Vector2(250, 400), new Vector2(1050, 400));
        panel.GetComponent<Image>().color = dark;
        var scroll = panel.GetComponentInChildren<ScrollRect>(true);
        Rect(scroll.transform, new Vector2(0, 25), new Vector2(1000, 240));
        scroll.horizontal = false;
        scroll.movementType = ScrollRect.MovementType.Clamped;
        scroll.scrollSensitivity = 40;
        scroll.horizontalScrollbarVisibility = ScrollRect.ScrollbarVisibility.Permanent;
        scroll.verticalScrollbarVisibility = ScrollRect.ScrollbarVisibility.Permanent;
        if (scroll.horizontalScrollbar != null) scroll.horizontalScrollbar.gameObject.SetActive(false);
        if (scroll.verticalScrollbar != null) scroll.verticalScrollbar.gameObject.SetActive(false);
        scroll.horizontalScrollbar = null;
        scroll.verticalScrollbar = null;
        scroll.GetComponent<Image>().color = new Color(.08f, .15f, .18f, 1);
        Stretch(scroll.viewport);
        var viewportImage = scroll.viewport.GetComponent<Image>();
        if (viewportImage != null) viewportImage.color = Color.white;
        var mask = scroll.viewport.GetComponent<Mask>();
        if (mask != null) mask.showMaskGraphic = false;
        var content = scroll.content;
        content.anchorMin = new Vector2(0, 1); content.anchorMax = Vector2.one;
        content.pivot = new Vector2(.5f, 1); content.anchoredPosition = Vector2.zero;
        content.sizeDelta = new Vector2(0, 0);
        var layout = content.GetComponent<VerticalLayoutGroup>();
        layout.padding = new RectOffset(18, 18, 12, 12);
        layout.childControlHeight = true; layout.childControlWidth = true;
        layout.childForceExpandHeight = false; layout.childForceExpandWidth = true;
        content.GetComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        var transcript = content.Find("chattext").GetComponent<TMP_Text>();
        var fitter = transcript.GetComponent<ContentSizeFitter>();
        if (fitter != null) UnityEngine.Object.DestroyImmediate(fitter);
        StyleText(transcript, font, ink, 30);
        transcript.alignment = TextAlignmentOptions.TopLeft;
        transcript.text = "The current passenger's conversation appears here.";
        transcript.richText = false;
        transcript.overflowMode = TextOverflowModes.Overflow;

        var input = panel.GetComponentInChildren<TMP_InputField>(true);
        Rect(input.transform, new Vector2(-90, -135), new Vector2(820, 52));
        input.GetComponent<Image>().color = new Color(.9f, .95f, .92f, 1);
        input.lineType = TMP_InputField.LineType.SingleLine;
        input.characterLimit = 500;
        input.onSubmit = new TMP_InputField.SubmitEvent();
        input.onEndEdit = new TMP_InputField.SubmitEvent();
        StyleText(input.textComponent, font, dark, 28);
        var placeholder = (TMP_Text)input.placeholder;
        StyleText(placeholder, font, new Color(.3f, .4f, .4f), 28);
        placeholder.text = "Ask the passenger a question...";
        var send = panel.GetComponentsInChildren<Button>(true).First(b => b.name == "sendbutton");
        Rect(send.transform, new Vector2(420, -135), new Vector2(160, 52));
        send.onClick = new Button.ButtonClickedEvent();
        send.image.color = new Color(.18f, .43f, .38f, 1);
        var label = send.GetComponentInChildren<TMP_Text>();
        StyleText(label, font, Color.white, 28); label.text = "SEND";
        var title = Label(panel.transform, "PassengerTitle", font, "INTERVIEW", new Vector2(0, 174), new Vector2(1000, 36), 27, ink);
        var status = Label(panel.transform, "ChatStatus", font, "Ask about their journey or documents.", new Vector2(0, -179), new Vector2(1000, 28), 21, ink);

        if (!AssetDatabase.IsValidFolder("Assets/Prefabs")) AssetDatabase.CreateFolder("Assets", "Prefabs");
        PrefabUtility.SaveAsPrefabAssetAndConnect(panel, "Assets/Prefabs/PassengerChatPanel.prefab", InteractionMode.AutomatedAction);
        var manager = new GameObject("AIManager");
        var chat = manager.AddComponent<AIChatbot>();
        chat.inputField = input; chat.chatText = transcript; chat.sendButton = send;
        chat.chatScroll = scroll; chat.passengerTitle = title; chat.statusText = status;
        decision.dialogue.passengerChat = chat;
        decision.dialogue.gameObject.SetActive(false);
        Rect(decision.feedbackText.transform, new Vector2(250, 100), new Vector2(1050, 140));
        decision.feedbackText.fontSize = 28;
        decision.feedbackText.text = "";
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);

        // Preserve the original Game entry, and make the authored intro the build entry point.
        var builds = EditorBuildSettings.scenes.Where(s => s.path != ScenePath && s.path != "Assets/Scenes/Intro.unity").ToList();
        builds.Insert(0, new EditorBuildSettingsScene("Assets/Scenes/Intro.unity", true));
        builds.Add(new EditorBuildSettingsScene(ScenePath, true));
        EditorBuildSettings.scenes = builds.ToArray();
        AssetDatabase.SaveAssets();
        Validate();
        Selection.activeGameObject = manager;
        Debug.Log("Passenger chat integration created. Open GameIntegration and press Play.");
    }

    [MenuItem("Tools/Checkpoint/Validate Passenger Chat Integration")]
    public static void Validate()
    {
        var chat = UnityEngine.Object.FindAnyObjectByType<AIChatbot>();
        var decision = UnityEngine.Object.FindObjectsByType<Decision>(FindObjectsInactive.Include, FindObjectsSortMode.None).First();
        Require(chat != null && chat.inputField != null && chat.chatText != null && chat.sendButton != null && chat.chatScroll != null, "Chat references missing.");
        Require(decision.dialogue.passengerChat == chat, "Passenger dialogue is not connected.");
        Require(chat.sendButton.onClick.GetPersistentEventCount() == 0, "Send button has duplicate Inspector listeners.");
        Require(chat.chatScroll.content != null && chat.chatScroll.viewport != null, "Scroll hierarchy missing.");
        Require(UnityEngine.Object.FindObjectsByType<UnityEngine.EventSystems.EventSystem>(FindObjectsSortMode.None).Length == 1, "Expected one EventSystem.");
        foreach (var card in decision.characters)
        {
            Require(card != null, "Missing character card.");
            string prompt = AIChatbot.BuildSystemPrompt(card);
            Require(prompt.Contains(card.fullName) && prompt.Contains(card.passportDOB), "Passenger facts missing from prompt.");
        }
        Debug.Log("Passenger integration references and all character prompts validated.");
    }

    static void Require(bool condition, string message) { if (!condition) throw new Exception(message); }
    static void Rect(Transform transform, Vector2 position, Vector2 size)
    {
        var rect = (RectTransform)transform;
        rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(.5f, .5f);
        rect.localScale = Vector3.one; rect.anchoredPosition = position; rect.sizeDelta = size;
    }
    static void Stretch(RectTransform rect)
    { rect.anchorMin = Vector2.zero; rect.anchorMax = Vector2.one; rect.offsetMin = rect.offsetMax = Vector2.zero; }
    static void StyleText(TMP_Text text, TMP_FontAsset font, Color color, float size)
    { text.font = font; text.color = color; text.fontSize = size; text.enableAutoSizing = false; text.raycastTarget = false; }
    static TMP_Text Label(Transform parent, string name, TMP_FontAsset font, string value, Vector2 position, Vector2 size, float fontSize, Color color)
    {
        var obj = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
        obj.transform.SetParent(parent, false); Rect(obj.transform, position, size);
        var label = obj.GetComponent<TMP_Text>(); StyleText(label, font, color, fontSize);
        label.text = value; label.alignment = TextAlignmentOptions.MidlineLeft; return label;
    }
}

public class PassengerChatSettings : EditorWindow
{
    private string key = "";
    [MenuItem("Tools/Checkpoint/Passenger Chat Settings")]
    public static void Open() { GetWindow<PassengerChatSettings>("Passenger Chat"); }
    void OnEnable() { key = EditorPrefs.GetString(AIChatbot.EditorKeyPreference, ""); }
    void OnGUI()
    {
        EditorGUILayout.LabelField("Groq access for local playtesting", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox("The key is stored in your local Unity Editor preferences, not in scenes or character assets. GROQ_API_KEY takes precedence. A distributed game should use a server-side endpoint.", MessageType.Info);
        key = EditorGUILayout.PasswordField("Groq API key", key);
        if (GUILayout.Button("Save locally")) { EditorPrefs.SetString(AIChatbot.EditorKeyPreference, key.Trim()); Close(); }
    }
}
