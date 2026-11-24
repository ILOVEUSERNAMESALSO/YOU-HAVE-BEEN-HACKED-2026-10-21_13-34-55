using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class VirtualComputer : MonoBehaviour
{
    public ComputerDatabase database;
    public Decision checkpoint;
    private GameObject overlay;
    private TMP_InputField searchInput;
    private TMP_Text status, articleTitle, articleBody, pageText;
    private RectTransform bodyRect;
    private ScrollRect articleScroll;
    private readonly List<Button> resultButtons = new List<Button>();
    private readonly List<TMP_Text> resultLabels = new List<TMP_Text>();
    private readonly Dictionary<string, Button> categoryButtons = new Dictionary<string, Button>();
    private List<ComputerDatabase.Entry> results;
    private Button previous, next;
    private string category = "All";
    private int page;
    private TMP_FontAsset font;
    private readonly Color ink =
        new Color(.92f, .92f, .90f);

    private readonly Color panelColor =
        new Color(.095f, .10f, .105f);

    private readonly Color buttonColor =
        new Color(.14f, .15f, .16f);

    private readonly Color accent =
        new Color(.20f, .32f, .30f);
    private const int PageSize = 6;

    void Start()
    {
        font = checkpoint.passportNameText.font;
        var launcher = MakeButton(transform, "SearchDatabase", "SEARCH DATABASE", new Vector2(1010, 450), new Vector2(370, 80), Open);
        // A separate sorting canvas keeps the computer above draggable papers.
        var shade = Box(transform, "VirtualComputer", Vector2.zero, Vector2.zero, new Color(0, 0, 0, .7f));
        Stretch(shade.rectTransform);
        overlay = shade.gameObject;
        var layer = overlay.AddComponent<Canvas>(); layer.overrideSorting = true; layer.sortingOrder = 100;
        overlay.AddComponent<GraphicRaycaster>();
        var window = Box(overlay.transform, "TerminalWindow", Vector2.zero, new Vector2(2040, 1190), panelColor);
        Label(window.transform, "TerminalTitle", "CHECKPOINT / STAFF TERMINAL", new Vector2(-430, 525), new Vector2(1020, 65), 38);
        MakeButton(window.transform, "Close", "CLOSE", new Vector2(870, 525), new Vector2(210, 65), Close);
        Label(window.transform, "Date", "Inspection date: " + checkpoint.inspectionDate, new Vector2(375, 525), new Vector2(660, 55), 26);
        BuildInput(window.transform);
        MakeButton(window.transform, "Search", "SEARCH", new Vector2(650, 415), new Vector2(200, 72), () => Search());
        MakeButton(window.transform, "Clear", "CLEAR", new Vector2(870, 415), new Vector2(200, 72), () => { searchInput.text = ""; Search(); });
        Label(
            window.transform,
            "KeywordHint",
            "Try a keyword:",
            new Vector2(-815, 310),
            new Vector2(290, 64),
            26);

        string[] keywords = { "name", "DOB", "expiry", "seat" };

        for (int i = 0; i < keywords.Length; i++)
        {
            string keyword = keywords[i];

            MakeButton(
                window.transform,
                "Keyword_" + keyword,
                keyword,
                new Vector2(-500 + i * 340, 310),
                new Vector2(300, 64),
                () =>
                {
                    category = "All";
                    searchInput.text = keyword;
                    Search();
                });
        }
        status = Label(window.transform, "Status", "", new Vector2(-300, 230), new Vector2(1320, 45), 25);
        for (int i = 0; i < PageSize; i++)
        {
            int slot = i;
            var button = MakeButton(window.transform, "Result" + i, "", new Vector2(-660, 130 - i * 108), new Vector2(600, 94), () => Select(page * PageSize + slot));
            resultButtons.Add(button);
            var label = button.GetComponentInChildren<TMP_Text>(); label.fontSize = 27;
            label.alignment = TextAlignmentOptions.MidlineLeft;
            resultLabels.Add(label);
        }
        previous = MakeButton(window.transform, "Previous", "PREV", new Vector2(-865, -540), new Vector2(180, 60), () => { page--; RenderResults(); });
        next = MakeButton(window.transform, "Next", "NEXT", new Vector2(-455, -540), new Vector2(180, 60), () => { page++; RenderResults(); });
        pageText = Label(window.transform, "Page", "", new Vector2(-660, -540), new Vector2(200, 50), 25);
        pageText.alignment = TextAlignmentOptions.Center;
        var article = Box(window.transform, "Article", new Vector2(330, -150), new Vector2(1240, 800), new Color(.065f, .07f, .075f));
        articleTitle = Label(article.transform, "ArticleTitle", "", new Vector2(0, 325), new Vector2(1150, 90), 37);
        var viewport = Box(article.transform, "ScrollViewport", new Vector2(0, -50), new Vector2(1150, 620), new Color(0,0,0,0));
        viewport.gameObject.AddComponent<RectMask2D>();
        articleScroll = viewport.gameObject.AddComponent<ScrollRect>();
        articleScroll.horizontal = false; articleScroll.movementType = ScrollRect.MovementType.Clamped; articleScroll.scrollSensitivity = 45;
        articleScroll.viewport = viewport.rectTransform;
        articleBody = Label(viewport.transform, "ArticleBody", "", Vector2.zero, new Vector2(1100, 620), 30);
        articleBody.richText = false;
        bodyRect = articleBody.rectTransform; bodyRect.anchorMin = bodyRect.anchorMax = new Vector2(.5f, 1); bodyRect.pivot = new Vector2(.5f, 1);
        articleBody.alignment = TextAlignmentOptions.TopLeft;
        articleScroll.content = bodyRect;
        overlay.SetActive(false);
    }

    void Update()
    {
        if (overlay != null && overlay.activeSelf && Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame) Close();
    }

    public void Open()
    {
        overlay.SetActive(true);
        overlay.transform.SetAsLastSibling();
        Search();
        searchInput.Select(); searchInput.ActivateInputField();
    }
    public void Close()
    {
        overlay.SetActive(false);
        EventSystem.current.SetSelectedGameObject(null);
    }
    public void Search()
    {
        page = 0;

        results = database.Search(
            searchInput.text,
            category,
            checkpoint.inspectionDate);

        status.text = results.Count == 1
            ? "1 ARTICLE FOUND"
            : results.Count + " ARTICLES FOUND";

        RenderResults();
    }
    private void RenderResults()
    {
        for (int i = 0; i < PageSize; i++)
        {
            int index = page * PageSize + i;
            resultButtons[i].gameObject.SetActive(index < results.Count);
            if (index < results.Count) resultLabels[i].text = results[index].title + "\n<size=20>" + results[index].category + "</size>";
        }
        previous.interactable = page > 0;
        next.interactable = (page + 1) * PageSize < results.Count;
        pageText.text = (results.Count == 0 ? 0 : page + 1) + " / " + Mathf.CeilToInt(results.Count / (float)PageSize);
        if (results.Count > 0) Select(page * PageSize);
        else
        {
            articleTitle.text = "No matching records";
        SetBody(
            "No rules match those keywords.\n\n" +
            "Try a shorter term or choose a suggestion above.\n\n" +
            "Examples: name, DOB, expiry, seat.");
        }
    }
    private void Select(int index)
    {
        if (results == null || index < 0 || index >= results.Count)
            return;
        var entry = results[index]; articleTitle.text = entry.title;
        SetBody(entry.content.Replace("{DATE}", checkpoint.inspectionDate));
        for (int i = 0; i < resultButtons.Count; i++) resultButtons[i].image.color = page * PageSize + i == index ? accent : buttonColor;
    }
    private void SetBody(string text)
    {
        articleBody.text = text;
        bodyRect.sizeDelta = new Vector2(1100, Mathf.Max(620, articleBody.GetPreferredValues(text, 1100, 0).y + 25));
        bodyRect.anchoredPosition = Vector2.zero;
        articleScroll.verticalNormalizedPosition = 1;
    }
    private void BuildInput(Transform parent)
    {
        var box = Box(
            parent, "SearchInput",
            new Vector2(-220, 415),
            new Vector2(1480, 72),
            new Color(.065f, .07f, .075f));

        var border = box.gameObject.AddComponent<Outline>();
        border.effectColor = new Color(.32f, .34f, .35f);
        border.effectDistance = new Vector2(1, -1);

        var text = Label(
            box.transform, "Text", "",
            Vector2.zero, new Vector2(1420, 65), 30);
        text.color = ink;

        var placeholder = Label(
            box.transform, "Placeholder",
            "Search a rule: name, expiry, date of birth...",
            Vector2.zero, new Vector2(1420, 65), 30);
        placeholder.color = new Color(.58f, .60f, .61f);

        searchInput = box.gameObject.AddComponent<TMP_InputField>();
        searchInput.textViewport = box.rectTransform;
        searchInput.textComponent = (TextMeshProUGUI)text;
        searchInput.placeholder = placeholder;
        searchInput.targetGraphic = box;

        searchInput.customCaretColor = true;
        searchInput.caretColor = ink;
        searchInput.selectionColor = new Color(.25f, .45f, .40f, .6f);

        searchInput.characterLimit = 120;
        searchInput.lineType = TMP_InputField.LineType.SingleLine;
        searchInput.onSubmit.AddListener(_ => Search());
    }
    private Image Box(Transform parent, string name, Vector2 position, Vector2 size, Color color)
    {
        var obj = new GameObject(name, typeof(RectTransform), typeof(Image)); obj.transform.SetParent(parent, false);
        var rect = (RectTransform)obj.transform; rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(.5f,.5f);
        rect.anchoredPosition = position; rect.sizeDelta = size;
        var image = obj.GetComponent<Image>(); image.color = color; return image;
    }
    private TMP_Text Label(Transform parent, string name, string value, Vector2 position, Vector2 size, float fontSize)
    {
        var obj = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI)); obj.transform.SetParent(parent, false);
        var rect = (RectTransform)obj.transform; rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(.5f,.5f);
        rect.anchoredPosition = position; rect.sizeDelta = size;
        var text = obj.GetComponent<TextMeshProUGUI>(); text.font = font; text.fontSize = fontSize; text.color = ink;
        text.text = value; text.alignment = TextAlignmentOptions.MidlineLeft; text.raycastTarget = false;
        return text;
    }
    private Button MakeButton(Transform parent, string name, string caption, Vector2 position, Vector2 size, UnityEngine.Events.UnityAction action)
    {
        var image = Box(parent, name, position, size, buttonColor);
        var button = image.gameObject.AddComponent<Button>(); button.targetGraphic = image; button.onClick.AddListener(action);
        Label(image.transform, "Label", caption, Vector2.zero, size - new Vector2(32, 8), 28).alignment = TextAlignmentOptions.Center;
        return button;
    }
    private void Stretch(RectTransform rect)
    {
        rect.anchorMin = Vector2.zero; rect.anchorMax = Vector2.one; rect.offsetMin = rect.offsetMax = Vector2.zero;
    }
}
