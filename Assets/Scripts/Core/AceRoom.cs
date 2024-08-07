using System.Collections;
using System.Collections.Generic;
using UnityEngine;


namespace AceV
{
    public class AceRoom
    {
        public string id { get; private set; }
        public string name { get; private set; }
        public bool visible { get; private set; }
        public string entranceNode { get; private set; }
        public Sprite background { get; private set; }
        public string backgroundRelativePath { get; private set; }
        public Sprite icon { get; private set; }
        public string iconRelativePath { get; private set; }
        public List<string> connectedRoomIDs { get; private set; }
        public List<AceObjectData> objectsInRoom { get; private set; } = new List<AceObjectData>();
        public List<AcePOIData> poisInRoom { get; private set; } = new List<AcePOIData>();
        public AceCharacter currentCharacterInRoom { get; private set; } // Monobehaviour ref


        /// <summary>
        /// Initializes this AceRoom.
        /// </summary>
        public void Initialize(string roomID, string roomName, bool startsVisible, string roomEntranceNode, string[] connectedRooms, string bgPath, string iconPath, Sprite roomBackground = null, Sprite roomIcon = null)
        {
            id = roomID;
            name = roomName;
            visible = startsVisible;
            entranceNode = roomEntranceNode;
            backgroundRelativePath = bgPath;
            iconRelativePath = iconPath;
            background = roomBackground;
            icon = roomIcon;
            connectedRoomIDs = new List<string>(connectedRooms);
        }

        /// <summary>
        /// Changes the room ID.
        /// (Do not use unless you know what you're doing!)
        /// </summary>
        public void SetRoomName(string newRoomID)
        {
            id = newRoomID;
        }


        /// <summary>
        /// Changes the room background.
        /// (Do not use unless you know what you're doing!)
        /// </summary>
        public void SetRoomBackground(string bgPath, Sprite newBG = null)
        {
            backgroundRelativePath = bgPath;
            background = newBG;
        }


        /// <summary>
        /// Changes the "enter dialogue" node for this room.
        /// (Do not use unless you know what you're doing!)
        /// </summary>
        public void SetEntranceNode(string newEntranceNode)
        {
            entranceNode = newEntranceNode;
        }


        public void SetVisible(bool newVisibility)
        {
            visible = newVisibility;
        }


        public void SetCurrentCharacterInRoom(AceCharacter newChar)
        {
            if (currentCharacterInRoom)
            {
                currentCharacterInRoom.currentRoom = null;
            }
            
            currentCharacterInRoom = newChar;
            if (newChar != null)
            {
                newChar.currentRoom = id;
            }
        }
    }
}
