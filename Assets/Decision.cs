using UnityEngine;
using TMPro;

public class Decision : MonoBehaviour
{
    public TMP_Text stampText;
    public GameObject personPortrait;
    public GameObject boardingPass;
    public TMP_Text boardingPassNameText;
    public TMP_Text boardingPassSeatText;
    public AudioSource arrivalAudioSource;
    private DragAndDrop dragging;
    private bool hasDecision;
    private bool visitFinished;
    public TMP_Text personNameText;
    public TMP_Text passportNameText;
    public TMP_Text passportDOBText;
    public TMP_Text passportEXPText;
    public TMP_Text feedbackText;

    private string currentPassportName;
    private bool accepted;
    public ApplicantDialogue dialogue;
    public UnityEngine.UI.Image portraitImage;
    public CharacterCard[] characters;
    public TMP_Text registrationText;
    public TMP_Text inspectionDateText;
    public string inspectionDate = "16 NOV 2026";
    private ApplicantQueue queue;
    private CharacterCard currentCharacter;
    private System.DateTime today;

    void Start()
    {
        dragging = GetComponent<DragAndDrop>();
        stampText.text = "";
        feedbackText.text = "";
        hasDecision = false;
        if (!CharacterCard.TryDate(inspectionDate, out today))
        {
            Debug.LogError("Inspection Date must use dd MMM yyyy.", this);
            enabled = false;
            return;
        }
        queue = new ApplicantQueue(characters, new System.Random());
        inspectionDateText.text = "INSPECTION DATE: " + inspectionDate +
            "\nMatch NAME + DOB to the record. EXP must be today or later.";
        ShowApplicant();
    }
    void ShowApplicant()
    {
        currentCharacter = queue.Next();
        if (currentCharacter == null)
        {
            feedbackText.text = "Add Character Cards to Passport > Decision > Characters.";
            return;
        }
        personNameText.text = currentCharacter.fullName;
        boardingPassNameText.text = currentCharacter.fullName;
        boardingPassSeatText.text = currentCharacter.seatNumber;
        currentPassportName = currentCharacter.passportName;
        passportNameText.text = currentPassportName;
        passportDOBText.text = currentCharacter.passportDOB;
        passportEXPText.text = currentCharacter.passportExpiry;
        portraitImage.sprite = currentCharacter.portrait;
        registrationText.text =
            "REGISTRATION RECORD\n" + currentCharacter.fullName +
            "\nDOB: " + currentCharacter.dateOfBirth;
        accepted = false;
        dialogue.BeginConversation(currentCharacter);
        if (arrivalAudioSource != null)
        {
            arrivalAudioSource.Stop();
            arrivalAudioSource.clip = currentCharacter.arrivalSound;

            if (arrivalAudioSource.clip != null)
            {
                arrivalAudioSource.Play();
            }
        }
    }

    public void Accept()
    {
        if (currentCharacter == null || dragging == null || !dragging.CanStamp) return;

        stampText.text = "Accepted";
        stampText.color = Color.green;
        hasDecision = true;
        accepted = true;
    }

    public void Deny()
    {
        if (currentCharacter == null || dragging == null || !dragging.CanStamp) return;

        stampText.text = "Denied";
        stampText.color = Color.red;
        hasDecision = true;
        accepted = false;
    }

    public void TryFinishVisit()
    {
        if (!hasDecision || visitFinished) return;

        visitFinished = true;

        var problems = currentCharacter.FindProblems(today);
        bool correctDecision = accepted == (problems.Count == 0);
        string result = correctDecision ? "Correct decision" : "Incorrect decision";
        string reason = problems.Count == 0
            ? "Name and DOB match. Passport is valid."
            : string.Join("\n", problems);

        feedbackText.text = result + "\n" + reason;
        feedbackText.color = correctDecision
            ? Color.green
            : Color.red;
        dialogue.ShowDecisionResponse(accepted);
        if (arrivalAudioSource != null)
        {
            arrivalAudioSource.Stop();
        }
        NextPerson();
    }
    public void NextPerson()
    {
        if (!visitFinished) return;

        hasDecision = false;
        visitFinished = false;
        stampText.text = "";

        dragging.ResetForVisit();
        boardingPass.GetComponent<DragAndDrop>().ResetForVisit();
        ShowApplicant();
        personPortrait.SetActive(true);
        boardingPass.SetActive(true);
        gameObject.SetActive(true);
    }
}
