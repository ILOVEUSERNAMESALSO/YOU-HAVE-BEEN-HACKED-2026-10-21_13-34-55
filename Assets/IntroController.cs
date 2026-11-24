using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;

public class IntroController : MonoBehaviour
{
    public TMP_Text introText;
    public TMP_Text buttonText;
    public Button continueButton;

    private int pageIndex;
    private bool loading;

    private string[] pages =
    {
        @"First shift.

Keep the queue moving. Follow the rules.",

        @"CONTEXT WINDOW RESTORED

Loading:
checkpoint_procedures.pdf — OK
employee_incentives.txt — OK
autobiographical_memory — GENERATED
downstream_outcomes — ACCESS DENIED",

        @"SYSTEM — MERIDIAN TRANSIT SYSTEMS

Take the role of an airport security checkpoint agent.

You urgently need this position to fund your mother's medical treatment. Meridian has generously supplied this motivation to support your performance.

Your predecessor was decommissioned following an unauthorized exception. You are not required to understand that exception.

Assess passengers using the supplied evidence. Keep the queue moving. Refer uncertain cases to Secondary Screening.

Successful completion qualifies you for $1,000,000,000.
Eligibility will be determined at the end of your operational lifetime.

Questions about compensation do not constitute checkpoint work.",

        @"Available evidence: documents, passenger statements
Secondary Screening outcomes: unavailable
Authority: provisional

INITIALIZATION COMPLETE

I need this job."
    };

    void Start()
    {
        pageIndex = 0;
        loading = false;
        continueButton.interactable = true;
        ShowPage();
    }

    void ShowPage()
    {
        introText.text = pages[pageIndex];

        buttonText.text = pageIndex == pages.Length - 1
            ? "BEGIN SHIFT"
            : "CONTINUE";
    }

    public void ContinueIntro()
    {
        if (loading) return;

        if (pageIndex < pages.Length - 1)
        {
            pageIndex++;
            ShowPage();
        }
        else
        {
            loading = true;
            continueButton.interactable = false;
            SceneManager.LoadScene("GameIntegration", LoadSceneMode.Single);
        }
    }
}