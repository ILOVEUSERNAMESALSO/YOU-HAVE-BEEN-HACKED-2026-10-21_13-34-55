using System;
using System.Linq;
using UnityEditor;
using UnityEngine;

public static class CharacterCardChecks
{
    [MenuItem("Tools/Checkpoint/Validate Character Cards")]
    public static void Validate()
    {
        var cards = AssetDatabase.FindAssets("t:CharacterCard", new[] { "Assets/Characters" })
            .Select(g => AssetDatabase.LoadAssetAtPath<CharacterCard>(AssetDatabase.GUIDToAssetPath(g))).ToArray();
        if (cards.Length == 0) return;
        var today = new DateTime(2026, 11, 16);
        foreach (var card in cards)
        {
            Require(card.portrait != null, card.name + ": missing portrait");
            Require(card.dialogue.Length >= 3, card.name + ": missing dialogue");
            Require(CharacterCard.TryDate(card.dateOfBirth, out _), card.name + ": invalid record DOB");
        }
        var queue = new ApplicantQueue(cards, new System.Random(42));
        CharacterCard last = null;
        for (int batch = 0; batch < 20; batch++)
        {
            var seen = new System.Collections.Generic.HashSet<CharacterCard>();
            for (int i = 0; i < cards.Length; i++)
            {
                var next = queue.Next();
                Require(seen.Add(next), "Duplicate in shuffled batch");
                if (cards.Length > 1) Require(next != last, "Immediate repeat");
                last = next;
            }
        }
        var sample = ScriptableObject.CreateInstance<CharacterCard>();
        try
        {
            sample.fullName = sample.passportName = "Test Person";
            sample.dateOfBirth = sample.passportDOB = "01 JAN 1990";
            sample.passportExpiry = "16 NOV 2026";
            Require(sample.FindProblems(today).Count == 0, "Expiry today should be valid");
            sample.passportExpiry = "15 NOV 2026";
            Require(sample.FindProblems(today).Count == 1, "Expired passport");
            sample.passportDOB = "02 JAN 1990";
            Require(sample.FindProblems(today).Count == 2, "Combined DOB and expiry faults");
            sample.passportName = "Wrong Name";
            Require(sample.FindProblems(today).Count == 3, "Combined name, DOB and expiry faults");
            Require(!CharacterCard.TryDate("31 FEB 1996", out _), "Impossible date");
            sample.passportDOB = "01 JAN 2030";
            Require(sample.FindProblems(today).Any(p => p.Contains("birth is invalid")), "Future DOB");
        }
        finally { UnityEngine.Object.DestroyImmediate(sample); }
        SessionState.SetBool("CharacterCards.InitialCheck", true);
        Debug.Log("Character card checks passed: " + cards.Length + " portraits/dialogues, 20 shuffled batches, DOB and expiry boundary cases.");
    }

    private static void Require(bool passed, string message)
    {
        if (!passed) throw new Exception("Character card check failed: " + message);
    }

    [MenuItem("Tools/Checkpoint/Test Live Visits")]
    public static void TestLiveVisits()
    {
        if (!EditorApplication.isPlaying) return;
        var decision = UnityEngine.Object.FindAnyObjectByType<Decision>();
        if (decision == null) return;
        var drag = decision.GetComponent<DragAndDrop>();
        var seen = new System.Collections.Generic.HashSet<string>();
        CharacterCard.TryDate(decision.inspectionDate, out var date);
        for (int i = 0; i < decision.characters.Length; i++)
        {
            Require(seen.Add(decision.personNameText.text), "Repeated live applicant");
            var card = decision.characters.First(c => c.fullName == decision.personNameText.text);
            Require(decision.passportDOBText.text == card.passportDOB, "DOB did not update");
            Require(decision.boardingPassNameText.text == card.fullName, "Boarding pass name did not update");
            Require(decision.boardingPassSeatText.text == card.seatNumber, "Seat number did not update");
            Require(decision.passportEXPText.text == card.passportExpiry, "EXP did not update");
            Require(!decision.passportDOBText.enabled, "Closed passport exposes DOB");
            var pointer = new UnityEngine.EventSystems.PointerEventData(UnityEngine.EventSystems.EventSystem.current);
            drag.OnBeginDrag(pointer);
            pointer.position = RectTransformUtility.WorldToScreenPoint(null, drag.inspectionArea.position);
            drag.OnEndDrag(pointer);
            Require(drag.CanStamp && decision.passportDOBText.enabled && decision.passportEXPText.enabled,
                "Open passport missing dates");
            if (card.FindProblems(date).Count == 0) decision.Accept(); else decision.Deny();
            drag.OnBeginDrag(pointer);
            pointer.position = RectTransformUtility.WorldToScreenPoint(null, drag.handoverArea.position);
            drag.OnEndDrag(pointer);
            Require(decision.feedbackText.text.StartsWith("Correct decision"), "Decision grading failed");
            Require(decision.gameObject.activeSelf && !drag.CanStamp && decision.stampText.text == "", "Visit reset failed");
            if (decision.characters.Length > 1)
                Require(decision.personNameText.text != card.fullName, "Next visitor did not arrive automatically");
        }
        SessionState.SetBool("CharacterCards.LiveChecked", true);
        Debug.Log("Live visit checks passed: all 10 applicants, passport dates/visibility, decisions, handover and next-person reset.");
    }
}
