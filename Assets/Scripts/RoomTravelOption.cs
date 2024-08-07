using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace AceV
{
    public class RoomTravelOption : MonoBehaviour
    {
        private RoomTravelManager travelManager;

        public Button attachedButton;
        public TextMeshProUGUI attachedText;
        
        public AceRoom roomData;

        /// <summary>
        /// Initializes this RoomTravelOption button with information, callbacks, etc
        /// </summary>
        public void Initialize(AceRoom room, RoomTravelManager parent)
        {
            travelManager = parent;

            // Set room data
            roomData = room;
            
            // Set room name text
            attachedText.text = roomData.name;
        }

        
        /// <summary>
        /// Called by EventTrigger component when pointer enters this option
        /// </summary>
        public void OnPointerEnter(BaseEventData eventData)
        {
            travelManager.OnOptionHoverEnter(this);
        }


        /// <summary>
        /// Called by EventTrigger component when pointer exits this option
        /// </summary>
        public void OnPointerExit(BaseEventData eventData)
        {
            travelManager.OnOptionHoverExit(this);
        }


        /// <summary>
        /// Called by EventTrigger component when clicking down on this option
        /// </summary>
        public void OnPointerDown(BaseEventData eventData)
        {
            travelManager.OnOptionSelected(this);
        }
    }
}
