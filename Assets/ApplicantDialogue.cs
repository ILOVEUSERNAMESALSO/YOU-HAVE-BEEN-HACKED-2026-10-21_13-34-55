using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class ApplicantDialogue : MonoBehaviour
{
    public TMP_Text dialogueText;
    public Button continueButton;
    [Tooltip("Optional: question this passenger using the imported chat panel.")]
    public AIChatbot passengerChat;

    private string[] lines;
    private int lineIndex;

    private CharacterCard character;

    public void BeginConversation(CharacterCard applicant)
    {
        character = applicant;
        if (passengerChat != null)
        {
            passengerChat.BeginConversation(applicant);
            return;
        }
        lines = applicant.dialogue != null && applicant.dialogue.Length > 0
            ? applicant.dialogue
            : new[] { "Hello. My name is " + applicant.fullName + "." };
        lineIndex = 0;
        ShowCurrentLine();
    }

    public void ContinueDialogue()
    {
        if (passengerChat != null) return;
        if (lines == null || lineIndex >= lines.Length - 1)
            return;

        lineIndex++;
        ShowCurrentLine();
    }

    private void ShowCurrentLine()
    {
        dialogueText.text = lines[lineIndex];
        continueButton.interactable =
            lineIndex < lines.Length - 1;
    }

    public void ShowDecisionResponse(bool wasAccepted)
    {
        if (passengerChat != null)
        {
            passengerChat.EndConversation(wasAccepted);
            return;
        }
        dialogueText.text = wasAccepted
            ? character.acceptedResponse
            : character.deniedResponse;

        continueButton.interactable = false;
    }
}
