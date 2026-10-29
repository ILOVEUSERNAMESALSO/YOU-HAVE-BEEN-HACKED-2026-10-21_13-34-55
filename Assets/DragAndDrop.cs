using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class DragAndDrop : MonoBehaviour, IDragHandler, IBeginDragHandler, IEndDragHandler
{
    private RectTransform rectTransform;
    private Image image;
    private Vector2 dragStartPosition;
    public RectTransform handoverArea;
    public RectTransform inspectionArea;
    private Color originalColor;

    void Start()
    {
        rectTransform = GetComponent<RectTransform>();
        image = GetComponent<Image>();
        originalColor = image.color;
        rectTransform.localScale = new Vector3(0.6f, 0.6f, 1f);
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        dragStartPosition = rectTransform.anchoredPosition;
        rectTransform.SetAsLastSibling();
        Color dragColor = originalColor;
        dragColor.a *= 0.67f;
        image.color = dragColor;
    }

    public void OnDrag(PointerEventData eventData)
    {
        rectTransform.anchoredPosition += eventData.delta;
        //transform.position = Input.mousePosition;
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        image.color = originalColor;

        if (handoverArea != null &&
            RectTransformUtility.RectangleContainsScreenPoint(
                handoverArea,
                eventData.position,
                eventData.pressEventCamera))
        {
            rectTransform.localScale = new Vector3(0.6f, 0.6f, 1f);
            KeepInsideArea(handoverArea);
        }
        else if (inspectionArea != null &&
            RectTransformUtility.RectangleContainsScreenPoint(
                inspectionArea,
                eventData.position,
                eventData.pressEventCamera))
        {
            rectTransform.localScale = Vector3.one;
            KeepInsideArea(inspectionArea);
        }
        else
        {
            rectTransform.anchoredPosition = dragStartPosition;
        }
    }
    void KeepInsideArea(RectTransform area)
    {
        Vector3[] areaCorners = new Vector3[4];
        Vector3[] paperCorners = new Vector3[4];

        area.GetWorldCorners(areaCorners);
        rectTransform.GetWorldCorners(paperCorners);

        Vector3 correction = Vector3.zero;

        // Check the left and right edges.
        if (paperCorners[0].x < areaCorners[0].x)
            correction.x = areaCorners[0].x - paperCorners[0].x;
        else if (paperCorners[2].x > areaCorners[2].x)
            correction.x = areaCorners[2].x - paperCorners[2].x;

        // Check the bottom and top edges.
        if (paperCorners[0].y < areaCorners[0].y)
            correction.y = areaCorners[0].y - paperCorners[0].y;
        else if (paperCorners[2].y > areaCorners[2].y)
            correction.y = areaCorners[2].y - paperCorners[2].y;

        rectTransform.position += correction;
    }
}