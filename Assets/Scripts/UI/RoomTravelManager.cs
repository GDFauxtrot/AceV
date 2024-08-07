using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Yarn.Unity;

namespace AceV
{
    public class RoomTravelManager : MonoBehaviour
    {
        public GameObject travelOptionPrefab;

        public GameObject travelOptionsParent;
        public Image travelIconImage;

        /// <summary>
        /// Update the available options presented for the current room.
        /// Typically ran just as we're displaying the Travel menu.
        /// </summary>
        public void Opened()
        {
            // Clear existing options (if there are any)
            // TODO these could be pooled and reused! Maybe a max predefined?
            foreach (Transform child in travelOptionsParent.transform)
            {
                Destroy(child.gameObject);
            }
        
            // Get the list of all rooms we can traverse to from here
            List<AceRoom> visibleConnectedRooms = new List<AceRoom>();
            AceRoom currentRoom = StoryManager.Instance.allRooms[StoryManager.Instance.currentRoom];
            foreach (string connectedRoomID in currentRoom.connectedRoomIDs)
            {
                if (!StoryManager.Instance.allRooms.ContainsKey(connectedRoomID))
                {
                    Debug.LogError($"Connected room '{connectedRoomID}' does not exist " +
                            $"(while parsing Travel option {currentRoom.id})!");
                    continue;
                }
                AceRoom connectedRoom = StoryManager.Instance.allRooms[connectedRoomID];
                if (connectedRoom.visible)
                {
                    visibleConnectedRooms.Add(connectedRoom);
                }
            }

            if (visibleConnectedRooms.Count == 0)
            {
                Debug.LogWarning("ERROR - this room has no visible connected rooms! We can't travel anywhere!");
                return;
            }

            // Create an option entry for each connected room
            foreach (AceRoom connectedRoom in visibleConnectedRooms)
            {
                GameObject newTravelOption = Instantiate(travelOptionPrefab, travelOptionsParent.transform);
                newTravelOption.GetComponent<RoomTravelOption>().Initialize(connectedRoom, this);
            }
        }


        public void OnOptionHoverEnter(RoomTravelOption option)
        {
            Sprite iconSprite = option.roomData.icon;
            if (iconSprite != null)
            {
                travelIconImage.sprite = iconSprite;
                travelIconImage.color = Color.white;
            }
            else
            {
                travelIconImage.sprite = null;
                travelIconImage.color = Color.black;
            }
        }


        public void OnOptionHoverExit(RoomTravelOption option)
        {
            // TODO show some kind of animation or default image here!
            travelIconImage.sprite = null;
            travelIconImage.color = Color.black;
        }


        public void OnOptionSelected(RoomTravelOption option)
        {
            // TODO begin room transition!
            StoryManager.LoadRoom(option.roomData.id);
        }
    }
}
