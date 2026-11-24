using UnityEngine;
using UnityEngine.UI;
using TMPro;

[RequireComponent(typeof(Image))]
public class PassportVisual : MonoBehaviour
{
    public Sprite closedSprite;
    public Sprite openSprite;

    private Image passportImage;
    private RectTransform rectTransform;
    private TMP_Text[] labels;
    private float documentHeight;

    void Awake()
    {
        passportImage = GetComponent<Image>();
        rectTransform = GetComponent<RectTransform>();
        labels = GetComponentsInChildren<TMP_Text>(true);

        documentHeight = rectTransform.rect.height;
    }

    public void SetOpen(bool isOpen)
    {
        Sprite selectedSprite = isOpen ? openSprite : closedSprite;
        passportImage.sprite = selectedSprite;

        // Keep the artwork's proportions when changing sprites.
        if (selectedSprite != null)
        {
            float aspectRatio =
                selectedSprite.rect.width / selectedSprite.rect.height;

            rectTransform.SetSizeWithCurrentAnchors(
                RectTransform.Axis.Horizontal,
                documentHeight * aspectRatio);
        }

        foreach (TMP_Text label in labels)
        {
            label.enabled = isOpen;
        }
    }
}