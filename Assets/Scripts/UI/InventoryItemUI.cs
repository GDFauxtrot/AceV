using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace AceV
{
    public class InventoryItemUI : MonoBehaviour
    {
        private InventoryUIManager inventoryManager;

        public AceItem itemData;

        public Image itemIcon;
        public Button itemButton;

        public int index;

        public void Initialize(AceItem item, InventoryUIManager parent, int i)
        {
            inventoryManager = parent;

            // Set item data
            itemData = item;
            index = i;

            // Set item icon in inventory
            itemIcon.sprite = item.icon;
            itemIcon.color = itemIcon.sprite == null ? Color.clear : Color.white;
        }


        /// <summary>
        /// Called by EventTrigger component when pointer enters this option
        /// </summary>
        public void OnPointerEnter(BaseEventData eventData)
        {
            inventoryManager.OnItemHoverEnter(this);
        }


        /// <summary>
        /// Called by EventTrigger component when pointer exits this option
        /// </summary>
        public void OnPointerExit(BaseEventData eventData)
        {
            inventoryManager.OnItemHoverExit(this);
        }


        /// <summary>
        /// Called by EventTrigger component when clicking down on this item
        /// </summary>
        public void OnPointerDown(BaseEventData eventData)
        {
            inventoryManager.OnItemSelected(this);
        }
    }
}