using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// Handles pointer clicks on an inventory slot and notifies BuilderController.
/// </summary>
public class InventorySlotClickHandler : MonoBehaviour, IPointerClickHandler
{
    private BuilderController builderController;
    private BlockInventoryEntry entry;

    public void Bind(BuilderController controller, BlockInventoryEntry inventoryEntry)
    {
        builderController = controller;
        entry = inventoryEntry;
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (builderController == null || entry == null) return;
        if (builderController.currentMode == GameMode.Play) return;
        if (eventData != null && eventData.button != PointerEventData.InputButton.Left) return;

        builderController.SelectInventoryEntry(entry);
    }
}
