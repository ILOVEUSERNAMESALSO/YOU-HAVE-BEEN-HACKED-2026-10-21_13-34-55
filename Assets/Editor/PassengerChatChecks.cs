using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

public static class PassengerChatChecks
{
    [MenuItem("Tools/Checkpoint/Test Passenger Chat Lifecycle")]
    public static void RunLive()
    {
        if (!EditorApplication.isPlaying) throw new Exception("Run in Play mode in GameIntegration.");
        PassengerChatSetup.Validate();
        var actual = UnityEngine.Object.FindAnyObjectByType<AIChatbot>();
        var decision = UnityEngine.Object.FindAnyObjectByType<Decision>();
        var driver = new GameObject("PassengerChatTestRunner").AddComponent<PassengerChatCheckRunner>();
        driver.Begin(actual, decision);
    }
}

// Uses no network and never reads a real key.
public class PassengerChatStub : AIChatbot
{
    public string Reply = "I am here for the stage production.";
    public string Failure;
    public bool MissingKey;
    public float Delay = .08f;
    public Action<string, string> PendingCompletion;
    protected override string GetApiKey() { return MissingKey ? "" : "local-test-placeholder"; }
    protected override IEnumerator RequestReply(List<Message> messages, Action<string, string> completed)
    {
        PendingCompletion = completed;
        yield return new WaitForSecondsRealtime(Delay);
        completed(Reply, Failure);
    }
}

public class PassengerChatCheckRunner : MonoBehaviour
{
    private AIChatbot actual;
    private Decision decision;
    private PassengerChatStub stub;
    private GameObject testPanel;
    private GameObject testManager;

    public void Begin(AIChatbot chat, Decision checkpoint)
    {
        actual = chat; decision = checkpoint;
        StartCoroutine(Run());
    }

    private IEnumerator Run()
    {
        var tests = Check();
        bool failed = false;
        while (true)
        {
            bool more;
            try { more = tests.MoveNext(); }
            catch (Exception e)
            {
                File.WriteAllText("Library/PassengerChatResult.txt", "FAIL: live-checks\n" + e);
                Debug.LogException(e); failed = true; break;
            }
            if (!more) break;
            yield return tests.Current;
        }
        if (testPanel != null) Destroy(testPanel);
        if (testManager != null) Destroy(testManager);
        if (!failed)
        {
            const string result = "PASS: live-checks. Ten complete visits; distinct character facts; fresh histories; missing credentials; failed and empty replies; successful typing; cancellation during request and typing; disabled input after departure. No network requests made.";
            File.WriteAllText("Library/PassengerChatResult.txt", result);
            Debug.Log(result);
        }
        Destroy(gameObject);
    }

    private IEnumerator Check()
    {
        var seen = new HashSet<CharacterCard>();
        var drag = decision.GetComponent<DragAndDrop>();
        CharacterCard.TryDate(decision.inspectionDate, out var today);
        for (int i = 0; i < decision.characters.Length; i++)
        {
            var passenger = actual.CurrentCharacter;
            Require(seen.Add(passenger), "Repeated passenger in batch.");
            Require(passenger.fullName == decision.personNameText.text, "Chat and portrait visitor differ.");
            Require(actual.VisitActive && actual.HistoryCount == 2, "Visit did not start with fresh history.");
            Require(!decision.passportDOBText.enabled, "Passport starts open.");
            var pointer = new UnityEngine.EventSystems.PointerEventData(UnityEngine.EventSystems.EventSystem.current);
            drag.OnBeginDrag(pointer);
            pointer.position = RectTransformUtility.WorldToScreenPoint(null, drag.inspectionArea.position);
            drag.OnEndDrag(pointer);
            Require(drag.CanStamp && decision.passportDOBText.enabled, "Passport did not open.");
            bool valid = passenger.FindProblems(today).Count == 0;
            if (valid) decision.Accept(); else decision.Deny();
            drag.OnBeginDrag(pointer);
            pointer.position = RectTransformUtility.WorldToScreenPoint(null, drag.handoverArea.position);
            drag.OnEndDrag(pointer);
            Require(decision.feedbackText.text.StartsWith("Correct decision"), "Existing document grading regressed.");
            Require(actual.HistoryCount == 2 && actual.VisitActive, "Next visitor did not reset.");
            Require(actual.CurrentCharacter != passenger, "Previous passenger remained after automatic handover.");
            Require(actual.CurrentCharacter.fullName == decision.personNameText.text, "Automatic arrival and interview differ.");
            Require(actual.sendButton.interactable && actual.inputField.interactable, "New visitor cannot be questioned.");
            Require(!drag.CanStamp && !decision.passportDOBText.enabled, "Next passport did not reset closed.");
        }

        var sourcePanel = actual.inputField.transform.parent.gameObject;
        testPanel = Instantiate(sourcePanel, sourcePanel.transform.parent);
        testPanel.name = "HiddenChatTestPanel";
        var group = testPanel.AddComponent<CanvasGroup>(); group.alpha = 0; group.blocksRaycasts = false;
        testManager = new GameObject("ChatTestTransport"); testManager.SetActive(false);
        stub = testManager.AddComponent<PassengerChatStub>();
        stub.inputField = testPanel.GetComponentInChildren<TMP_InputField>();
        stub.sendButton = testPanel.GetComponentsInChildren<Button>().First(b => b.name == "sendbutton");
        stub.chatScroll = testPanel.GetComponentInChildren<ScrollRect>();
        stub.chatText = stub.chatScroll.content.Find("chattext").GetComponent<TMP_Text>();
        stub.passengerTitle = testPanel.transform.Find("PassengerTitle").GetComponent<TMP_Text>();
        stub.statusText = testPanel.transform.Find("ChatStatus").GetComponent<TMP_Text>();
        // Runtime listeners on cloned UI must not address the live chat.
        stub.sendButton.onClick.RemoveAllListeners(); stub.inputField.onSubmit.RemoveAllListeners();
        testManager.SetActive(true);
        var first = decision.characters[0]; var second = decision.characters[1];
        stub.BeginConversation(first);
        stub.MissingKey = true; stub.inputField.text = "My question"; stub.SendQuestion();
        Require(!stub.IsSending && stub.HistoryCount == 2 && stub.inputField.text == "My question", "Missing key lost the question.");
        stub.MissingKey = false; stub.Failure = "Simulated timeout";
        stub.SendQuestion(); yield return new WaitForSecondsRealtime(.2f);
        Require(!stub.IsSending && stub.HistoryCount == 2 && stub.inputField.text == "My question", "Failure did not roll back history.");
        Require(stub.sendButton.interactable, "Failed request left Send disabled.");
        stub.Failure = null; stub.Reply = "";
        stub.SendQuestion(); yield return new WaitForSecondsRealtime(.2f);
        Require(!stub.IsSending && stub.HistoryCount == 2, "Empty reply was stored in history.");
        stub.Reply = "Literal <b>text</b> stays plain."; stub.wordsPerSecond = 100;
        stub.SendQuestion(); yield return new WaitForSecondsRealtime(.3f);
        Require(!stub.IsSending && stub.HistoryCount == 4 && !stub.chatText.richText && stub.chatText.text.Contains(stub.Reply), "Successful reply or literal text failed.");

        stub.BeginConversation(first); stub.Delay = .5f; stub.inputField.text = "Old question"; stub.SendQuestion();
        yield return null;
        var oldCompletion = stub.PendingCompletion;
        stub.BeginConversation(second);
        oldCompletion?.Invoke("OLD PASSENGER REPLY", null);
        yield return new WaitForSecondsRealtime(.6f);
        Require(stub.CurrentCharacter == second && stub.HistoryCount == 2 && !stub.IsSending && !stub.chatText.text.Contains("OLD PASSENGER REPLY"), "Old request contaminated new visitor.");

        stub.Delay = 0; stub.wordsPerSecond = 1; stub.Reply = "A deliberately long response that is still typing.";
        stub.inputField.text = "Slow reply"; stub.SendQuestion();
        yield return new WaitForSecondsRealtime(.1f);
        Require(stub.IsSending, "Typing simulation never started.");
        stub.EndConversation(false);
        string farewell = stub.chatText.text;
        yield return new WaitForSecondsRealtime(.2f);
        Require(!stub.IsSending && !stub.VisitActive && !stub.sendButton.interactable && stub.chatText.text == farewell, "Typing continued after departure.");
        stub.BeginConversation(first);
        Require(stub.HistoryCount == 2 && !stub.chatText.text.Contains("Slow reply"), "Typing state carried into the next visitor.");
    }

    static void Require(bool condition, string message) { if (!condition) throw new Exception("Passenger chat: " + message); }
}
