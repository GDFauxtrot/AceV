using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;


namespace AceV
{
    public class RoomOptionsManager : MonoBehaviour
    {
        public AceVButtonHandler talkButton;
        public AceVButtonHandler investigateButton;
        public AceVButtonHandler presentButton;
        public AceVButtonHandler travelButton;

        public void SetCharacterInRoom(bool isCharacterInRoom)
        {
            talkButton.isDisabledAppearanceForcedEnabled = isCharacterInRoom;
            presentButton.isDisabledAppearanceForcedEnabled = isCharacterInRoom;
            
            talkButton.SetButtonInteractable(isCharacterInRoom);
            presentButton.SetButtonInteractable(isCharacterInRoom);
        }

        public void OnTalkPressed()
        {
            // TODO play some kind of UI go-away animation
            StoryManager.Instance.BeginTalkToCharacter();
        }

        public void OnInvestigatePressed()
        {
            // TODO play some kind of UI go-away animation
            StoryManager.Instance.BeginInvestigatingRoom();
        }

        public void OnPresentPressed()
        {
            // TODO play some kind of UI go-away animation
            StoryManager.Instance.OpenPresentUI();
        }

        public void OnTravelPressed()
        {
            // TODO play some kind of UI go-away animation
            StoryManager.Instance.OpenTravelUI();
        }
    }
}
