using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// Attach to any slot (inventory or equipment) that should allow drag & drop.
/// Requires an Image component on the same GameObject for the item icon.
/// Works together with DroppableSlot for the full drag & drop system.
/// </summary>
public class DraggableItem : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    [HideInInspector] public bool hasItem = false;

    // Source info (set by the owning UI before each refresh)
    [HideInInspector] public bool isEquipmentSlot = false;
    [HideInInspector] public EquipmentSlot equipSlot;
    [HideInInspector] public int inventoryIndex = -1;

    // Drag ghost
    static GameObject ghostObj;
    static Image ghostImage;
    static Canvas rootCanvas;
    static DraggableItem currentlyDragging;

    CanvasGroup canvasGroup;
    Image icon;

    void Awake()
    {
        canvasGroup = GetComponent<CanvasGroup>();
        if (canvasGroup == null)
            canvasGroup = gameObject.AddComponent<CanvasGroup>();

        icon = GetComponent<Image>();
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (!hasItem || icon == null || icon.sprite == null) return;

        currentlyDragging = this;

        // Find root canvas for ghost positioning
        if (rootCanvas == null)
            rootCanvas = GetComponentInParent<Canvas>().rootCanvas;

        // Create ghost icon
        ghostObj = new GameObject("DragGhost");
        ghostObj.transform.SetParent(rootCanvas.transform, false);
        ghostObj.transform.SetAsLastSibling();

        var rt = ghostObj.AddComponent<RectTransform>();
        rt.sizeDelta = ((RectTransform)transform).sizeDelta;

        ghostImage = ghostObj.AddComponent<Image>();
        ghostImage.sprite = icon.sprite;
        ghostImage.raycastTarget = false;
        ghostImage.color = new Color(1f, 1f, 1f, 0.75f);

        // Make source slot semi-transparent
        canvasGroup.alpha = 0.4f;
        canvasGroup.blocksRaycasts = false;
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (ghostObj == null) return;

        // Follow pointer
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            rootCanvas.transform as RectTransform,
            eventData.position,
            eventData.pressEventCamera,
            out Vector2 localPoint);

        ghostObj.transform.localPosition = localPoint;
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        // Restore source slot
        canvasGroup.alpha = 1f;
        canvasGroup.blocksRaycasts = true;

        // Destroy ghost
        if (ghostObj != null)
            Destroy(ghostObj);

        currentlyDragging = null;
    }

    public static DraggableItem GetCurrentlyDragging()
    {
        return currentlyDragging;
    }
}
