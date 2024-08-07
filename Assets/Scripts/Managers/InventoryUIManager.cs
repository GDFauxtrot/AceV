using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace AceV
{
    public class InventoryUIManager : MonoBehaviour
    {
        public GameObject inventoryItemOptionPrefab;

        public Transform inventoryItemOptionsParent;
        public TextMeshProUGUI inventoryItemName;

        public GameObject inventoryDetailParent;
        public TextMeshProUGUI inventoryDetailName;
        public TextMeshProUGUI inventoryDetailDescription;
        public Image inventoryDetailIcon;

        public int highlightedItemIndex;
        public int selectedItemIndex;
        public bool isInDetailMode;

        /// <summary>
        /// Update the inventory screen.
        /// Typically ran just as we're displaying the inventory menu.
        /// </summary>
        public void Opened()
        {
            inventoryItemName.text = "";
            
            // Clear existing items (if there are any)
            // TODO these could be pooled and reused! Maybe a max predefined?
            foreach (Transform child in inventoryItemOptionsParent)
            {
                Destroy(child.gameObject);
            }
        
            // Create an inventory item for each AceItem the player is holding
            int index = 0;
            foreach (AceItem inventoryItem in StoryManager.Instance.playerInventory.Values)
            {
                GameObject newInventoryItem = Instantiate(inventoryItemOptionPrefab, inventoryItemOptionsParent);
                newInventoryItem.GetComponent<InventoryItemUI>().Initialize(inventoryItem, this, index++);
            }

            selectedItemIndex = -1;
            highlightedItemIndex = 0;


            AceVButtonHandler presentButtonHandler = UIManager.Instance.presentButton.GetComponent<AceVButtonHandler>();
            presentButtonHandler.isDisabledAppearanceForcedEnabled = StoryManager.Instance.playerInventory.Count > 0;
            presentButtonHandler.SetButtonInteractable(StoryManager.Instance.playerInventory.Count > 0);
            // UIManager.SetInteractable(UIManager.Instance.presentButton, StoryManager.Instance.playerInventory.Count > 0);
        }


        public void OnItemHoverEnter(InventoryItemUI itemUI)
        {
            inventoryItemName.text = itemUI.itemData.name;
            // TODO highlight box outline, SFX play
        }


        public void OnItemHoverExit(InventoryItemUI itemUI)
        {
            // Not sure if we need to do anything here?
        }


        public void OnItemSelected(InventoryItemUI itemUI)
        {
            // Since we're choosing an item in the inventory screen, let's open up
            // the detailed inventory view and populate its info with this item info
            SelectItemByIndex(itemUI.index);
        }


        public void CloseDetailMode()
        {
            isInDetailMode = false;
            highlightedItemIndex = selectedItemIndex; // Just in case?
            selectedItemIndex = -1;
            inventoryDetailParent.SetActive(false);
        }


        public void SelectItemByIndex(int index)
        {
            InventoryItemUI itemUI = inventoryItemOptionsParent.GetChild(index).GetComponent<InventoryItemUI>();
            selectedItemIndex = itemUI.index;
            isInDetailMode = true;
            inventoryDetailParent.SetActive(true);
            inventoryDetailName.text = itemUI.itemData.name;
            inventoryDetailDescription.text = itemUI.itemData.description;
            inventoryDetailIcon.sprite = itemUI.itemData.icon;
            inventoryDetailIcon.color = inventoryDetailIcon.sprite == null ? Color.clear : Color.white;
        }


        public AceItem GetAceItemForIndex(int index)
        {
            return inventoryItemOptionsParent.GetChild(index).GetComponent<InventoryItemUI>().itemData;
        }
    }
}
