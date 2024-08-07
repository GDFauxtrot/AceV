using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

namespace AceV
{
    public class UIManager : MonoBehaviour
    {
        public static UIManager Instance;

        public CanvasGroup canvas; // Used to control interactability with the UI

        public GameObject dialogueBoxParent;
        public GameObject inventoryParent;
        public GameObject dialogueOptions;
        public GameObject roomOptions;
        public GameObject travelOptions;

        public GameObject backButton;
        public GameObject itemsButton;
        public GameObject presentButton;

        public MouseCatcher mouseCatcher;

        private Coroutine transitionOutCoroutine, transitionInCoroutine;

        public bool showPresentButton;

        /// <summary>
        /// A special flag for states to flip to hold off on finishing UI transitions while animations finish.
        /// Used primarily for ROOM_TALK state since Yarn Runner more closely handles that state
        /// </summary>
        private bool backButtonCloseIsAnimating;


        void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);

            SetActiveAndVisible(inventoryParent, false);
            SetActiveAndVisible(roomOptions, false);
            SetActiveAndVisible(travelOptions, false);
            SetActiveAndVisible(backButton, false);
            SetActiveAndVisible(itemsButton, false);
            SetActiveAndVisible(presentButton, false);
        }


        public void MoveToNewState(PlayerActionState newState, PlayerActionState lastState)
        {
            // Let's transition out of the last state
            if (lastState != PlayerActionState.NULL && transitionOutCoroutine == null)
            {
                transitionOutCoroutine = StartCoroutine(TransitionUIOutOfState(lastState, newState));
            }

            // And let's transition into the new state
            if (transitionInCoroutine != null)
            {
                StopCoroutine(transitionInCoroutine); // Hmm... this might cause issues
            }
            transitionInCoroutine = StartCoroutine(TransitionUIIntoState(newState, lastState));
        }


        private IEnumerator TransitionUIOutOfState(PlayerActionState state, PlayerActionState newState)
        {
            // TODO animate state change! For now let's turn OFF whatever menus we have to
            switch (state)
            {
                case PlayerActionState.ROOM_OPTIONS:
                    if (StoryManager.Instance.roomLoadedOnThisFrame)
                    {
                        break; // Special case
                    }

                    SetUIInteractable(false);
                    SetInteractable(roomOptions, false);

                    roomOptions.transform.localScale = new Vector3(1f, 1f, 1f);
                    roomOptions.transform.DOScaleY(0f, 0.5f).SetEase(Ease.InCubic);

                    yield return new WaitForSeconds(0.5f);

                    SetVisible(roomOptions, false);
                    break;
                case PlayerActionState.ROOM_TALK:
                    // AceVDialogueOptionsView handles a lot of this state
                    // due to how it's tied closely to Yarn Spinner
                    while (backButtonCloseIsAnimating)
                    {
                        yield return new WaitForSeconds(0f);
                    }
                    break;
                case PlayerActionState.ROOM_INVESTIGATE:
                    if (newState == PlayerActionState.ITEMS)
                    {
                        break; // Nah don't do anything
                    }
                    SetUIInteractable(false);
                    float t = (StoryManager.Instance.characterInCurrentRoom?.ExitInvestigateMode()).GetValueOrDefault();
                    yield return new WaitForSeconds(t);
                    StoryManager.Instance.PlayQueuedNodeIfPresent();
                    break;
                case PlayerActionState.ROOM_TRAVEL:
                    SetActiveAndVisible(travelOptions, false);
                    break;
                case PlayerActionState.ITEMS:
                    // SetUIInteractable(false);
                    SetActiveAndVisible(presentButton, false);
                    SetActiveAndVisible(itemsButton, true); // TODO keep an eye on this one
                    SetActiveAndVisible(inventoryParent, false);
                    break;
                default:
                    yield return null; // Big shrug
                    break;
            }
            
            transitionOutCoroutine = null;
        }


        private IEnumerator TransitionUIIntoState(PlayerActionState state, PlayerActionState lastState)
        {
            // Wait for out coroutine to finish first!
            while (transitionOutCoroutine != null)
            {
                yield return null;
            }

            // TODO animate state change! For now let's turn ON whatever menus we have to
            switch (state)
            {
                case PlayerActionState.ROOM_OPTIONS:
                    SetVisible(backButton, false);
                    SetVisible(itemsButton, true);
                    SetVisible(roomOptions, true);
                    SetUIInteractable(false);

                    roomOptions.GetComponent<RoomOptionsManager>().SetCharacterInRoom(
                            StoryManager.Instance.characterInCurrentRoom != null);
                    
                    roomOptions.transform.localScale = new Vector3(1f, 0f, 1f);
                    roomOptions.transform.DOScaleY(1f, 0.5f).SetEase(Ease.OutCubic);

                    yield return new WaitForSeconds(0.5f);

                    SetInteractable(itemsButton, true);
                    SetInteractable(roomOptions, true);
                    SetUIInteractable(true);
                    break;
                case PlayerActionState.ROOM_TALK:
                    SetInteractable(dialogueOptions, true);
                    SetActiveAndVisible(backButton, true);
                    // Start Yarn Spinner dialogue options node
                    if (lastState != PlayerActionState.ITEMS)
                    {
                        StoryManager.Instance.yarnDialogueRunner.StartDialogue(StoryManager.Instance.allRooms[StoryManager.Instance.currentRoom].currentCharacterInRoom.onInteract);
                    }
                    break;
                case PlayerActionState.ROOM_INVESTIGATE:
                    if (lastState == PlayerActionState.ITEMS)
                    {
                        break; // Don't do anything
                    }
                    SetUIInteractable(false);
                    SetActiveAndVisible(backButton, true);
                    float t = (StoryManager.Instance.characterInCurrentRoom?.EnterInvestigateMode()).GetValueOrDefault();
                    yield return new WaitForSeconds(t);
                    SetUIInteractable(true);
                    break;
                case PlayerActionState.ROOM_TRAVEL:
                    SetUIInteractable(true);
                    SetActiveAndVisible(travelOptions, true);
                    SetActiveAndVisible(backButton, true);
                    travelOptions.GetComponent<RoomTravelManager>().Opened();
                    break;
                case PlayerActionState.DIALOGUE:
                    SetActiveAndVisible(backButton, false);
                    SetUIInteractable(true);
                    break;
                case PlayerActionState.ITEMS:
                    SetUIInteractable(true);
                    SetActiveAndVisible(presentButton, showPresentButton);
                    SetActiveAndVisible(backButton, true);
                    SetActiveAndVisible(itemsButton, false);
                    SetActiveAndVisible(inventoryParent, true);
                    inventoryParent.GetComponent<InventoryUIManager>().Opened();
                    break;
                default:
                    yield return null;
                    break;
            }

            transitionInCoroutine = null;
        }


        public void BackButtonPressed()
        {
            // Stop talk options dialogue if we're leaving the Talk menu
            if (GameManager.Instance.GetState() == PlayerActionState.ROOM_TALK)
            {
                StoryManager.Instance.yarnDialogueRunner.Stop();

                backButtonCloseIsAnimating = true;
                dialogueOptions.GetComponent<AceVDialogueOptionsView>().CloseDialogueOptions();
            }

            // Go back to last state if we're in the Options menu and going back.
            // Otherwise, just assume we should be going back to ROOM_OPTIONS
            if (GameManager.Instance.GetState() == PlayerActionState.ITEMS)
            {
                // Use back button as a way to step out of detail view, if we're in it
                if (inventoryParent.GetComponent<InventoryUIManager>().isInDetailMode)
                {
                    inventoryParent.GetComponent<InventoryUIManager>().CloseDetailMode();
                }
                else
                {
                    GameManager.Instance.PopState();
                }
            }
            else
            {
                GameManager.Instance.PopState();
            }
        }


        public void ItemsButtonPressed()
        {
            showPresentButton = false;
            GameManager.Instance.PushState(PlayerActionState.ITEMS);
        }


        public void PresentButtonPressed()
        {
            InventoryUIManager inventory = inventoryParent.GetComponent<InventoryUIManager>();

            if (!inventory.isInDetailMode)
            {
                inventory.SelectItemByIndex(inventory.highlightedItemIndex);
            }
            else
            {
                AceCharacter currentChar = StoryManager.Instance.characterInCurrentRoom;
                AceItem selectedItem = inventory.GetAceItemForIndex(inventory.selectedItemIndex);
                inventory.CloseDetailMode();
                StoryManager.Instance.StoreCurrentSelectedItem(selectedItem.id);
                GameManager.Instance.PushState(PlayerActionState.DIALOGUE);
                StoryManager.Instance.PlayNode(currentChar.onPresent);

            }
        }


        public static void SetActiveAndVisible(GameObject go, bool enable, bool setBlocksRaycast = true)
        {
            SetVisible(go, enable);
            SetInteractable(go, enable, setBlocksRaycast);
        }


        public static void SetVisible(GameObject go, bool visible)
        {
            CanvasGroup cg = go.GetComponent<CanvasGroup>();
            if (cg != null)
            {
                cg.alpha = visible ? 1f : 0f;
            }
            else
            {
                go.SetActive(visible);
            }
        }


        public static void SetInteractable(GameObject go, bool interactable, bool setBlocksRaycast = true)
        {
            CanvasGroup cg = go.GetComponent<CanvasGroup>();
            if (cg != null)
            {
                cg.interactable = interactable;
                if (setBlocksRaycast)
                {
                    cg.blocksRaycasts = interactable;
                }
            }
            else
            {
                go.SetActive(interactable);
            }
        }


        public void SetBackButtonAnimationFinished()
        {
            backButtonCloseIsAnimating = false;
        }


        public void SetUIInteractable(bool interactable)
        {
            canvas.interactable = interactable;
            canvas.blocksRaycasts = interactable;
        }


        public bool GetUIInteractable()
        {
            return canvas.interactable;
        }
    }
}
