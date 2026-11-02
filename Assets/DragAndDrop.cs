using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class DragAndDrop : MonoBehaviour, IDragHandler, IBeginDragHandler, IEndDragHandler
{
    private RectTransform rectTransform;
    private CanvasGroup canvasGroup;
    private Vector2 dragStartPosition;
    public RectTransform handoverArea;
    public RectTransform inspectionArea;
    private Canvas canvas;
    private Outline handoverOutline;
    private Outline inspectionOutline;

    void Start()
    {
        canvas = GetComponentInParent<Canvas>().rootCanvas;
        rectTransform = GetComponent<RectTransform>();
        canvasGroup = GetComponent<CanvasGroup>();

        handoverOutline = handoverArea.GetComponent<Outline>();
        inspectionOutline = inspectionArea.GetComponent<Outline>();

        handoverOutline.enabled = false;
        inspectionOutline.enabled = false;

        canvasGroup.alpha = 1f;
        rectTransform.localScale = new Vector3(0.6f, 0.6f, 1f);
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        dragStartPosition = rectTransform.anchoredPosition;
        rectTransform.SetAsLastSibling();

        canvasGroup.alpha = 0.67f;

        UpdateDropHighlights(eventData);
    }

    public void OnDrag(PointerEventData eventData)
    {
        rectTransform.anchoredPosition +=
            eventData.delta / canvas.scaleFactor;

        UpdateDropHighlights(eventData);
    }
    void UpdateDropHighlights(PointerEventData eventData)
    {
        handoverOutline.enabled =
            RectTransformUtility.RectangleContainsScreenPoint(
                handoverArea,
                eventData.position,
                eventData.pressEventCamera);

        inspectionOutline.enabled =
            RectTransformUtility.RectangleContainsScreenPoint(
                inspectionArea,
                eventData.position,
                eventData.pressEventCamera);
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        canvasGroup.alpha = 1f;
        handoverOutline.enabled = false;
        inspectionOutline.enabled = false;
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
            StartCoroutine(ReturnToStart());
        }
        IEnumerator ReturnToStart()
        {
            Vector2 releasedPosition = rectTransform.anchoredPosition;
            Vector2 destination = dragStartPosition;

            float duration = 0.2f;
            float elapsed = 0f;

            // Prevent another pickup during the return animation.
            canvasGroup.blocksRaycasts = false;

            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;

                float progress = Mathf.Clamp01(elapsed / duration);
                float easedProgress = Mathf.SmoothStep(0f, 1f, progress);

                rectTransform.anchoredPosition = Vector2.Lerp(
                    releasedPosition,
                    destination,
                    easedProgress);

                yield return null;
            }

            rectTransform.anchoredPosition = destination;
            canvasGroup.blocksRaycasts = true;
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