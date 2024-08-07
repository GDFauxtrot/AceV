using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Text.RegularExpressions;
using SimpleJSON;
using UnityEngine;
using UnityEngine.Analytics;
using Yarn.Unity;
using Yarn.Compiler;


namespace AceV
{
    public class StoryManager : MonoBehaviour
    {
        // Use this bool like an inspector button for now lmao
        public bool temp_ActivateStoryButton;

        public static StoryManager Instance;

        public GameObject objectPrefab;
        public GameObject poiPrefab;
        public GameObject characterPrefab;

        public BackgroundHandler background;
        public ForegroundHandler foreground;

        public DialogueRunner yarnDialogueRunner;
        private string yarnStoryStartRoom;

        public Dictionary<string, AceItem> playerInventory;

        [SerializeField]
        public List<string> internalStoryJSONNames;

        [ReadOnly]
        public string currentStoryPath;

        [ReadOnly]
        public string currentRoom;

        public AceCharacter characterOnScreen;
        public bool isCharacterTalking;
        private bool dialogueEndedOnThisFrame;
        public bool roomLoadedOnThisFrame;

        private bool ignoreCharacterChangesFlag;
        private bool continueOnNextLineFlag, performedContinueOnNextLineFlag;

        private string currentItemToPresent;
        private string roomNodeToPlay;

        private bool titleCoroutine_TextIsRunning;
        private bool titleCoroutine_WaitForInput;
        private float titleCoroutine_MiddleSpeed;
        private float titleCorutine_LongSpeed;

        private const float CONTINUE_DELAY = 0.25f;
        public float continueLineDelay = CONTINUE_DELAY;

        // A set of all dialogue ID's - when displaying dialogue options, we show a mark if we've read it
        private HashSet<int> readDialogueIDs = new HashSet<int>();

        public AceCharacter characterInCurrentRoom
        {
            get { return allRooms[currentRoom].currentCharacterInRoom; }
        }

        // Ace stories are contained in "rooms", where all information is held.
        // This includes objects, interactable points of interest, and characters.
        public Dictionary<string, AceRoom> allRooms
                = new Dictionary<string, AceRoom>();

        public Dictionary<string, AceCharacter> allCharacters
                = new Dictionary<string, AceCharacter>();

        public Dictionary<string, AceItem> allItems
                = new Dictionary<string, AceItem>();

        void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);

            playerInventory = new Dictionary<string, AceItem>();

            yarnDialogueRunner.onDialogueComplete.AddListener(OnDialogueComplete);
        }


        void Update()
        {
            if (temp_ActivateStoryButton)
            {
                temp_ActivateStoryButton = false;

                // Initialize the story! JSON is loaded up and assets are generated
                // Just load the first entry in interalStoryJSONNames for now!
                string storyPath = Path.Combine(Application.dataPath,
                        "Assets", "Internal", internalStoryJSONNames[0]);
                currentStoryPath = Path.GetDirectoryName(storyPath);
                yarnStoryStartRoom = InitializeStory(storyPath);

                // TEST - Compile yarn code LIVE before we begin!
                YarnRuntimeStoryImporter.RunAllStoriesImport(yarnDialogueRunner);
                
                // Run node "declvars" for all declared variables in the story
                yarnDialogueRunner.StartDialogue("declvars");
            }

            // We may be here because "declvars" has completed. Start the story node now!
            if (!string.IsNullOrEmpty(yarnStoryStartRoom) && !yarnDialogueRunner.Dialogue.IsActive)
            {
                // Initialize start room!
                LoadRoom(yarnStoryStartRoom);

                // TODO load save files on this step! Just before Dialogue Runner is started

                // Start Yarn Spinner once all assets are loaded
                // yarnDialogueRunner.StartDialogue(yarnStoryStartNode);
                yarnStoryStartRoom = null;
            }
        }


        void LateUpdate()
        {
            if (dialogueEndedOnThisFrame)
            {
                dialogueEndedOnThisFrame = false;
            }
            if (roomLoadedOnThisFrame)
            {
                roomLoadedOnThisFrame = false;
            }
        }


        [YarnCommand]
        public static void LoadRoom(string roomName)
        {
            StoryManager.Instance.StartCoroutine(StoryManager.Instance.LoadRoomInternal(roomName));
        }


        private IEnumerator LoadRoomInternal(string roomName)
        {
            // START - fade to black, wait until it's finished to continue!
            if (yarnStoryStartRoom != null)
            {
                ScreenCover.FadeToBlack(0f);
                
            }
            else
            {
                UIManager.Instance.SetUIInteractable(false);
                ScreenCover.FadeToBlack(0.5f);
                yield return new WaitForSeconds(0.5f);
            }

            // Travel menu is always open when we LoadRoom - make sure it's closed!
            UIManager.SetActiveAndVisible(UIManager.Instance.travelOptions, false);

            AceRoom roomObj;
            if (!string.IsNullOrEmpty(Instance.currentRoom))
            {
                roomObj = Instance.allRooms[Instance.currentRoom];

                // Turn off everything in the current room
                foreach (AceObjectData objData in roomObj.objectsInRoom)
                {
                    objData.aceObject.gameObject.SetActive(false);
                }
                foreach (AcePOIData poiData in roomObj.poisInRoom)
                {
                    poiData.poiObject.gameObject.SetActive(false);
                }
                if (roomObj.currentCharacterInRoom != null)
                {
                    roomObj.currentCharacterInRoom.gameObject.SetActive(false);
                    roomObj.currentCharacterInRoom.animator.StopAnimating();
                }
            }

            // Load new room!
            Instance.currentRoom = roomName;
            roomObj = Instance.allRooms[roomName];

            // Set background in new room
            Instance.background.SetBackground(roomObj.background);

            // Turn on everything in the new room
            foreach (AceObjectData objData in roomObj.objectsInRoom)
            {
                objData.aceObject.gameObject.SetActive(objData.visible);
            }
            foreach (AcePOIData poiData in roomObj.poisInRoom)
            {
                poiData.poiObject.gameObject.SetActive(poiData.visible);
            }
            if (roomObj.currentCharacterInRoom != null)
                {
                    roomObj.currentCharacterInRoom.gameObject.SetActive(true);
                    roomObj.currentCharacterInRoom.animator.StartAnimating();
                }

            // Loading finished - switch state and then fade from black!

            // We want to check if the new room has a "enterdialogue" node. If so,
            // we run it first! Otherwise enter ROOM_OPTIONS immediately.
            if (string.IsNullOrEmpty(roomObj.entranceNode))
            {
                GameManager.Instance.ForceState(PlayerActionState.ROOM_OPTIONS);
                
                ScreenCover.FadeFromBlack(0.5f);
                yield return new WaitForSeconds(0.5f);
            }
            else
            {
                roomLoadedOnThisFrame = true;
                GameManager.Instance.ForceState(PlayerActionState.ROOM_OPTIONS, PlayerActionState.DIALOGUE);

                ScreenCover.FadeFromBlack(0.5f);
                yield return new WaitForSeconds(0.5f);

                if (yarnDialogueRunner.IsDialogueRunning)
                {
                    yarnDialogueRunner.Stop();
                }
                yarnDialogueRunner.StartDialogue(roomObj.entranceNode);
            }
        }


        [YarnCommand]
        public static void UnlockRoom(string roomName)
        {
            StoryManager.Instance.allRooms[roomName].SetVisible(true);
        }


        [YarnCommand]
        public static void LockRoom(string roomName)
        {
            StoryManager.Instance.allRooms[roomName].SetVisible(false);
        }


        public string InitializeStory(string storyPath)
        {
            string jsonStory = File.ReadAllText(storyPath);
            JSONNode jsonStoryLoaded = JSON.Parse(jsonStory);

            // Create AceRoom instances for each room defined in the JSON
            foreach (JSONNode jsonRoomNode in jsonStoryLoaded["rooms"])
            {
                AceRoom newRoom = GetAceRoomFromJSON(jsonRoomNode);
                
                if (newRoom == null)
                {
                    continue;
                }

                allRooms.Add(jsonRoomNode["id"], newRoom);
            }

            // Iterate through JSON and create all entities
            List<AceObject> loadedObjects = new List<AceObject>();
            List<AcePOI> loadedPOIs = new List<AcePOI>();
            // List<AceCharacter> loadedCharacters = new List<AceCharacter>();
            foreach (JSONNode jsonObjectNode in jsonStoryLoaded["objects"])
            {
                AceObject newObject = GetAceObjectFromJSON(jsonObjectNode);

                if (newObject == null)
                {
                    continue;
                }

                newObject.gameObject.transform.SetParent(foreground.objectsParent, false);
                newObject.gameObject.SetActive(false);
                loadedObjects.Add(newObject);

                // Add object reference to the room it belongs in
                allRooms[newObject.room].objectsInRoom.Add(newObject.data);
            }
            foreach (JSONNode jsonPOINode in jsonStoryLoaded["pois"])
            {
                AcePOI newPOI = GetAcePOIFromJSON(jsonPOINode);

                if (newPOI == null)
                {
                    continue;
                }

                newPOI.gameObject.transform.SetParent(foreground.poisParent, false);
                newPOI.gameObject.SetActive(false);
                loadedPOIs.Add(newPOI);

                // Add POI reference to the room it belongs in
                allRooms[newPOI.room].poisInRoom.Add(newPOI.data);
            }
            foreach (JSONNode jsonCharNode in jsonStoryLoaded["characters"])
            {
                AceCharacter newChar = GetAceCharacterFromJSON(jsonCharNode);

                if (newChar == null)
                {
                    continue;
                }

                newChar.gameObject.transform.SetParent(foreground.charactersParent, false);
                newChar.gameObject.SetActive(false);
                // loadedCharacters.Add(newChar);
                allCharacters.Add(newChar.id, newChar);
            }
            foreach (JSONNode jsonItemNode in jsonStoryLoaded["items"])
            {
                AceItem newItem = GetAceItemFromJSON(jsonItemNode);
                
                if (newItem == null)
                {
                    continue;
                }

                allItems.Add(newItem.id, newItem);
            }

            // The story assets should now be all loaded - rooms, objects, POI's, characters
            string startRoom = jsonStoryLoaded["startroom"];
            return startRoom;
        }


        public void PlayNode(string nodeToPlay)
        {
            if (!yarnDialogueRunner.IsDialogueRunning)
            {
                yarnDialogueRunner.StartDialogue(nodeToPlay);
            }
        }


        public void QueueNodeToPlay(string nodeToPlay)
        {
            roomNodeToPlay = nodeToPlay;
        }


        public void PlayQueuedNodeIfPresent()
        {
            if (roomNodeToPlay != null)
            {
                string n = roomNodeToPlay;
                roomNodeToPlay = null;
                PlayNode(n);
            }
        }


        [YarnCommand]
        public static Coroutine RoomIntro(string title, string subtitle)
        {
            GameObject dialogueBox = UIManager.Instance.dialogueBoxParent;
            CanvasGroup dialogueCanvasGroup = dialogueBox.GetComponent<CanvasGroup>();
            AceVLineView dialogueLineView = dialogueBox.GetComponent<AceVLineView>();

            dialogueBox.SetActive(true);
            dialogueCanvasGroup.alpha = 1f;
            dialogueLineView.lineTextTypewriter.ShowText($"<style=Title>{title}</style>\n" +
                $"<style=Subtitle>{subtitle}</style>");
            dialogueLineView.lineTextTypewriter.SetTypewriterSpeed(0.3f);
            dialogueLineView.characterNameContainer.SetActive(false);
            dialogueLineView.arrowAnimator.visible = false;

            StoryManager.Instance.titleCoroutine_MiddleSpeed = dialogueLineView.lineTextTypewriter.waitMiddle;
            StoryManager.Instance.titleCorutine_LongSpeed = dialogueLineView.lineTextTypewriter.waitLong;
            dialogueLineView.lineTextTypewriter.waitMiddle = dialogueLineView.lineTextTypewriter.waitForNormalChars;
            dialogueLineView.lineTextTypewriter.waitLong = dialogueLineView.lineTextTypewriter.waitForNormalChars;

            dialogueLineView.lineTextTypewriter.onTextShowed.AddListener(StoryManager.Instance.RoomIntroOnTextShowed);
            UIManager.Instance.mouseCatcher.onClickDown.AddListener(StoryManager.Instance.RoomIntroOnClickPressed);

            return StoryManager.Instance.StartCoroutine(StoryManager.Instance.RoomIntroWaitCoroutine());
        }


        private IEnumerator RoomIntroWaitCoroutine()
        {
            GameObject dialogueBox = UIManager.Instance.dialogueBoxParent;
            AceVLineView dialogueLineView = dialogueBox.GetComponent<AceVLineView>();

            titleCoroutine_TextIsRunning = true;
            while (titleCoroutine_TextIsRunning)
            {
                yield return new WaitForSeconds(0f);
            }

            dialogueLineView.lineTextTypewriter.SetTypewriterSpeed(1f);
            dialogueLineView.lineTextTypewriter.waitMiddle = titleCoroutine_MiddleSpeed;
            dialogueLineView.lineTextTypewriter.waitLong = titleCorutine_LongSpeed;
            dialogueLineView.arrowAnimator.visible = true;

            while (!titleCoroutine_WaitForInput)
            {
                yield return new WaitForSeconds(0f);
            }

            dialogueLineView.lineTextTypewriter.ShowText("");
            dialogueLineView.arrowAnimator.visible = false;

            titleCoroutine_TextIsRunning = false;
            titleCoroutine_WaitForInput = false;

            yield return new WaitForSeconds(0.5f);
        }


        private void RoomIntroOnTextShowed()
        {
            titleCoroutine_TextIsRunning = false;

            GameObject dialogueBox = UIManager.Instance.dialogueBoxParent;
            AceVLineView dialogueLineView = dialogueBox.GetComponent<AceVLineView>();
            dialogueLineView.lineTextTypewriter.onTextShowed.RemoveListener(RoomIntroOnTextShowed);
        }


        private void RoomIntroOnClickPressed()
        {
            if (!titleCoroutine_TextIsRunning)
            {
                titleCoroutine_WaitForInput = true;

                GameObject dialogueBox = UIManager.Instance.dialogueBoxParent;
                AceVLineView dialogueLineView = dialogueBox.GetComponent<AceVLineView>();
                UIManager.Instance.mouseCatcher.onClickDown.RemoveListener(RoomIntroOnClickPressed);
            }
        }


        public void StoreCurrentSelectedItem(string itemID)
        {
            currentItemToPresent = itemID;
        }
        

        [YarnFunction]
        public static string GetItemToPresent()
        {
            return StoryManager.Instance.currentItemToPresent;
        }


        private AceRoom GetAceRoomFromJSON(JSONNode jsonRoomNode)
        {
            // Get strings from JSON
            string roomIDStr = jsonRoomNode["id"]; // Required
            string roomNameStr = jsonRoomNode["name"]; // Required
            string roomStartsVisibleStr = jsonRoomNode["startsvisible"]; // Optional - true by default
            string roomEntranceNodeStr = jsonRoomNode["entrancenode"]; // Required
            string roomBGStr = jsonRoomNode["background"]; // Optional - black screen by default
            string roomIconStr = jsonRoomNode["icon"]; // Optional - black icon by default
            JSONArray roomConnectedRoomsArray = jsonRoomNode["connectedrooms"].AsArray; // Required

            // Validation
            if (string.IsNullOrEmpty(roomIDStr))
            {
                Debug.LogError("ERROR while parsing story JSON! A room " +
                        "was not defined correctly (missing 'id'). Skipping.");
                return null;
            }
            if (string.IsNullOrEmpty(roomNameStr))
            {
                Debug.LogError($"ERROR while parsing story JSON! Room '{roomIDStr}' " +
                        "was not defined correctly (missing 'name'). Skipping.");
                return null;
            }
            if (string.IsNullOrEmpty(roomEntranceNodeStr))
            {
                Debug.LogError($"ERROR while parsing story JSON! Room '{roomIDStr}' " +
                        "was not defined correctly (missing 'entrancenode'). Skipping.");
                return null;
            }
            if (roomConnectedRoomsArray.IsNull || !roomConnectedRoomsArray.IsArray)
            {
                Debug.LogError($"ERROR while parsing story JSON! Room '{roomIDStr}' " +
                        "was not defined correctly (missing/mangled 'connectedrooms'). Skipping.");
                return null;
            }
            // Load room connections - must be an array of strings
            List<string> connectedRooms = new List<string>();
            bool connectedRoomsIsValid = true;
            foreach (JSONNode connectedRoomNode in roomConnectedRoomsArray.Values)
            {
                connectedRoomsIsValid &= connectedRoomNode.IsString;
                if (connectedRoomsIsValid == false)
                {
                    break;
                }
                connectedRooms.Add(connectedRoomNode);
            }
            if (!connectedRoomsIsValid)
            {
                Debug.LogError($"ERROR while parsing story JSON! Room '{roomIDStr}' " +
                        "was not defined correctly ('connectedrooms' must be a string array). Skipping.");
                return null;
            }

            // The rest of the strings we can assume defaults to if they're undefined
            
            // Load startsvisible if it was defined (true otherwise)
            bool roomStartsVisible = true;
            if (!string.IsNullOrEmpty(roomStartsVisibleStr))
            {
                roomStartsVisible = bool.TryParse(
                    roomStartsVisibleStr,
                    out roomStartsVisible) ? roomStartsVisible : true;
            }

            // Load room sprite if it was defined (null otherwise)
            string bgPath = Path.Combine(currentStoryPath, roomBGStr);
            AssetHandler.GetSpriteFromPath(bgPath, out Sprite bgSprite, new Vector2(0.5f, 0.5f));

            // Load room icon sprite if it was defined (null otherwise)
            string iconPath = Path.Combine(currentStoryPath, roomIconStr);
            AssetHandler.GetSpriteFromPath(iconPath, out Sprite iconSprite, new Vector2(0.5f, 0.5f));

            // Instantiate room!
            AceRoom newRoom = new AceRoom();
            newRoom.Initialize(roomIDStr, roomNameStr, roomStartsVisible, roomEntranceNodeStr, connectedRooms.ToArray(), roomBGStr, roomIconStr, bgSprite, iconSprite);
            return newRoom;
        }


        private AceObject GetAceObjectFromJSON(JSONNode jsonObjectNode)
        {
            // Get strings from JSON
            string objIDStr = jsonObjectNode["id"]; // Required
            string objImageStr = jsonObjectNode["image"]; // Required
            string objRoomStr = jsonObjectNode["room"]; // Required
            string objInteractStr = jsonObjectNode["oninteract"]; // Required
            string objPositionStr = jsonObjectNode.GetValueOrDefault("position", "(0, 0)");
            string objScaleStr = jsonObjectNode.GetValueOrDefault("scale", "1");

            // Validation
            if (string.IsNullOrEmpty(objIDStr))
            {
                Debug.LogError("ERROR while parsing story JSON! An Object " +
                        "was not defined correctly (missing 'id'). Skipping.");
                return null;
            }
            if (string.IsNullOrEmpty(objImageStr))
            {
                Debug.LogError($"ERROR while parsing story JSON! Object '{objIDStr}' " +
                        "was not defined correctly (missing 'image'). Skipping.");
                return null;
            }
            if (string.IsNullOrEmpty(objRoomStr))
            {
                Debug.LogError($"ERROR while parsing story JSON! Object '{objIDStr}' " +
                        "was not defined correctly (missing 'room'). Skipping.");
                return null;
            }
            if (string.IsNullOrEmpty(objInteractStr))
            {
                Debug.LogError($"ERROR while parsing story JSON! Object '{objIDStr}' " +
                        "was not defined correctly (missing 'node'). Skipping.");
                return null;
            }

            // Load image
            string spritePath = Path.Combine(StoryManager.Instance.currentStoryPath, objImageStr);
            uint spritePadding = objectPrefab.GetComponent<AceObject>().objectSelectionPadding;
            AssetHandler.GetSpriteFromPath(spritePath, out Sprite objSprite, new Vector2(0.5f, 0.5f), true, spritePadding);

            // Parse position
            Regex positionRegex = new Regex("\\-?(\\d)+(\\.\\d+)*");
            MatchCollection positionMatches = positionRegex.Matches(objPositionStr);
            Vector2 objPosition = Vector2.zero;
            if (positionMatches.Count != 2)
            {
                Debug.LogError($"ERROR while parsing story JSON! Object '{objIDStr} " +
                        "has an improperly assigned value for 'position'.");
            }
            else
            {
                objPosition = new Vector2(
                    float.Parse(positionMatches[0].Value),
                    float.Parse(positionMatches[1].Value));
            }
            
            // Parse scale
            float objScale = 1f;
            try
            {
                objScale = float.Parse(objScaleStr);
            }
            catch
            {
                Debug.LogWarning($"ERROR while parsing story JSON! Object '{objIDStr} " +
                        "has an improperly assigned value for 'scale'.");
            }

            // Instantiate object!
            GameObject go = Instantiate(objectPrefab);
            AceObject newObject = go.GetComponent<AceObject>();
            newObject.Initialize(objIDStr, objSprite, objRoomStr, objInteractStr, objPosition, objScale);
            return newObject;
        }


        private AcePOI GetAcePOIFromJSON(JSONNode jsonPOINode)
        {
            // Get strings from JSON
            string poiIDStr = jsonPOINode["id"]; // Required
            string poiRoomStr = jsonPOINode["room"]; // Required
            string poiInteractStr = jsonPOINode["oninteract"]; // Required

            // Validation
            if (string.IsNullOrEmpty(poiIDStr))
            {
                Debug.LogError($"ERROR while parsing story JSON! POI '{poiIDStr}' " +
                        "was not defined correctly (missing 'id'). Skipping.");
                return null;
            }
            if (string.IsNullOrEmpty(poiRoomStr))
            {
                Debug.LogError($"ERROR while parsing story JSON! POI '{poiIDStr}' " +
                        "was not defined correctly (missing 'room'). Skipping.");
                return null;
            }
            if (string.IsNullOrEmpty(poiInteractStr))
            {
                Debug.LogError($"ERROR while parsing story JSON! POI '{poiIDStr}' " +
                        "was not defined correctly (missing 'node'). Skipping.");
                return null;
            }

            // Parse bounds. It may either be one Rect or an array
            List<Rect> poiBounds = new List<Rect>();
            Regex boundsRegex = new Regex("\\-?(\\d)+(\\.\\d+)*");
            // Array bounds check
            if (jsonPOINode["bounds"].IsArray)
            {
                foreach (JSONNode jsonBoundsNode in jsonPOINode["bounds"])
                {
                    MatchCollection boundsMatches = boundsRegex.Matches(jsonBoundsNode);
                    if (boundsMatches.Count != 4)
                    {
                        Debug.LogError($"ERROR while parsing story JSON! POI '{poiIDStr} " +
                                "has an improperly assigned value for 'bounds'.");
                        poiBounds.Add(Rect.zero);
                    }
                    else
                    {
                        poiBounds.Add(new Rect(
                            float.Parse(boundsMatches[0].Value),
                            float.Parse(boundsMatches[1].Value),
                            float.Parse(boundsMatches[2].Value),
                            float.Parse(boundsMatches[3].Value)));
                    }
                }
            }
            // Just one Rect bounds check (also checks if no bounds were defined)
            else
            {
                if (!string.IsNullOrEmpty(jsonPOINode["bounds"]))
                {
                    MatchCollection boundsMatches = boundsRegex.Matches(jsonPOINode["bounds"]);
                    if (boundsMatches.Count != 4)
                    {
                        Debug.LogError($"ERROR while parsing story JSON! POI '{poiIDStr} " +
                                "has an improperly assigned value for 'bounds'.");
                        poiBounds.Add(Rect.zero);
                    }
                    else
                    {
                        poiBounds.Add(new Rect(
                            float.Parse(boundsMatches[0].Value),
                            float.Parse(boundsMatches[1].Value),
                            float.Parse(boundsMatches[2].Value),
                            float.Parse(boundsMatches[3].Value)));
                    }
                }
                else
                {
                    Debug.LogError($"ERROR while parsing story JSON! POI '{poiIDStr} " +
                            "has no assigned value for 'bounds' when there should be one.");
                    poiBounds.Add(Rect.zero);
                }
            }

            // Instantiate POI!
            GameObject go = Instantiate(poiPrefab);
            AcePOI newPOI = go.GetComponent<AcePOI>();
            newPOI.Initialize(poiIDStr, poiRoomStr, poiInteractStr, poiBounds);
            return newPOI;
        }


        private AceCharacter GetAceCharacterFromJSON(JSONNode jsonCharNode)
        {
            // Get strings from JSON
            string charIDStr = jsonCharNode["id"]; // Required
            string charNameStr = jsonCharNode["name"]; // Required
            string charInteractStr = jsonCharNode["oninteract"]; // Required
            string charPresentStr = jsonCharNode["onpresent"]; // Required

            // Validation
            if (string.IsNullOrEmpty(charIDStr))
            {
                Debug.LogError($"ERROR while parsing story JSON! A character " +
                        "was not defined correctly (missing 'id'). Skipping.");
                return null;
            }
            if (string.IsNullOrEmpty(charNameStr))
            {
                Debug.LogError($"ERROR while parsing story JSON! Character '{charIDStr}' " +
                        "was not defined correctly (missing 'name'). Skipping.");
                return null;
            }
            if (string.IsNullOrEmpty(charInteractStr))
            {
                Debug.LogError($"ERROR while parsing story JSON! Character '{charIDStr}' " +
                        "was not defined correctly (missing 'oninteract'). Skipping.");
                return null;
            }
            if (string.IsNullOrEmpty(charPresentStr))
            {
                Debug.LogError($"ERROR while parsing story JSON! Character '{charIDStr}' " +
                        "was not defined correctly (missing 'onpresent'). Skipping.");
                return null;
            }

            // Instantiate character!
            GameObject go = Instantiate(characterPrefab);
            AceCharacter newChar = go.GetComponent<AceCharacter>();
            newChar.Initialize(charIDStr, charNameStr, charInteractStr, charPresentStr);
            return newChar;
        }


        private AceItem GetAceItemFromJSON(JSONNode jsonItemNode)
        {
            // Get strings from JSON
            string itemIDStr = jsonItemNode["id"]; // Required
            string itemNameStr = jsonItemNode["name"]; // Required
            string itemDescriptionStr = jsonItemNode["description"]; // Required
            string itemIconStr = jsonItemNode["icon"]; // Required

            // Validation
            if (string.IsNullOrEmpty(itemIDStr))
            {
                Debug.LogError($"ERROR while parsing story JSON! An item " +
                        "was not defined correctly (missing 'id'). Skipping.");
                return null;
            }
            if (string.IsNullOrEmpty(itemNameStr))
            {
                Debug.LogError($"ERROR while parsing story JSON! Item '{itemIDStr}' " +
                        "was not defined correctly (missing 'name'). Skipping.");
                return null;
            }
            if (string.IsNullOrEmpty(itemDescriptionStr))
            {
                Debug.LogError($"ERROR while parsing story JSON! Item '{itemIDStr}' " +
                        "was not defined correctly (missing 'description'). Skipping.");
                return null;
            }
            if (string.IsNullOrEmpty(itemIconStr))
            {
                Debug.LogWarning($"ERROR while parsing story JSON! Item '{itemIDStr}' " +
                        "was not defined correctly (missing 'icon'). Icon loading WILL fail!");
            }

            // Load room sprite if it was defined (null otherwise)
            string iconPath = Path.Combine(currentStoryPath, itemIconStr);
            AssetHandler.GetSpriteFromPath(iconPath, out Sprite iconSprite, new Vector2(0.5f, 0.5f));

            // Instantiate item!
            AceItem newItem = new AceItem();
            newItem.Initialize(itemIDStr, itemNameStr, itemDescriptionStr, iconSprite);
            return newItem;
        }


        void OnDialogueComplete()
        {
            // Ignore if not in a room or finished "declvars" (reserved node name)
            if (string.IsNullOrEmpty(currentRoom) || yarnDialogueRunner.CurrentNodeName == "declvars")
            {
                return;
            }

            // Only run once for all dialogue views! We don't want this running multiple times.
            if (dialogueEndedOnThisFrame)
            {
                return;
            }

            dialogueEndedOnThisFrame = true;

            // Clear text from text box
            foreach (DialogueViewBase view in yarnDialogueRunner.dialogueViews)
            {
                if (view is AceVLineView)
                {
                    ((AceVLineView)view).lineTextTypewriter.ShowText("");
                }
            }

            // We just ended a conversation (DIALOGUE). Go back to whatever last state we were in
            if (/*!roomLoadedOnThisFrame && */GameManager.Instance.GetState() == PlayerActionState.DIALOGUE)
            {
                // Change UI state to whatever the last one was
                GameManager.Instance.PopState();
            }

            // If there's supposed to be a character on screen, show it (and vice versa)
            AceRoom room = allRooms[currentRoom];
            AceCharacter character = room.currentCharacterInRoom;

            // Make sure all characters are gone
            if (character == null)
            {
                foreach (var c in allCharacters)
                {
                    if (c.Value.currentRoom == currentRoom)
                    {
                        c.Value.currentRoom = null;
                    }
                    c.Value.gameObject.SetActive(false);
                }
            }
            else
            // Make sure ONLY the current character is showing
            {
                foreach (var c in allCharacters)
                {
                    if (c.Value.fullName != character.fullName)
                    {
                        c.Value.gameObject.SetActive(false);
                    }
                }
                ShowCharacterByName(character.fullName);
            }
        }


        public void ShowCharacterByName(string characterFullName)
        {
            if (ignoreCharacterChangesFlag)
            {
                return;
            }

            // If name doesn't exist or is "Player" (reserved name), ignore
            if (string.IsNullOrEmpty(characterFullName) || characterFullName.ToLower() == "player")
            {
                return;
            }
            // If character doesn't exist or is already on screen, ignore
            AceCharacter character = null;
            foreach (var c in allCharacters)
            {
                if (c.Value.fullName == characterFullName)
                {
                    character = c.Value;
                    break;
                }
            }
            if (character == null)
            {
                return;
            }
            // If it's a new character (not the one already on screen),
            // perform some animation functions
            if (character != characterOnScreen)
            {
                // Character found. Turn off the one on screen if it exists
                // (stop animating), and turn the new one on
                if (characterOnScreen != null)
                {
                    characterOnScreen.animator.StopAnimating();
                    characterOnScreen.gameObject.SetActive(false);
                }
                character.gameObject.SetActive(true);
                SetCurrentCharacterTalking(false); // By default
                character.animator.StartAnimating();
            }
            else
            {
                character.gameObject.SetActive(true);
                character.animator.StartAnimating();
            }
            
            characterOnScreen = character;
        }


        public void SetCurrentCharacterTalking(bool isTalking)
        {
            if (ignoreCharacterChangesFlag)
            {
                return;
            }

            // Don't do anything
            if (characterOnScreen == null)
            {
                isCharacterTalking = false;
                return;
            }

            // Set current character talking. Also has a side effect of resetting their animation
            // if the talking value is different than what it was before
            characterOnScreen.animator.isTalking = isTalking;
            isCharacterTalking = isTalking;

            // Play dialogue beeps for talking
            if (isTalking)
            {
                // TODO ADD DEFAULT AND PER-CHARACTER SUPPORT BEEP SFX
                // AudioManager.Instance.StartDialogueBeeping();
            }
            else
            {
                // AudioManager.Instance.StopDialogueBeeping();
            }
        }


        public void SetContinueLineDelay(float delay)
        {
            if (delay < 0f)
            {
                continueLineDelay = CONTINUE_DELAY;
            }
            else
            {
                continueLineDelay = delay;
            }
        }


        /// <summary>
        /// Set by "#noshow" Yarn spinner tag when a line's character change & talking should be ignored
        /// </summary>
        public void SetIgnoreCharacterChanges(bool ignore)
        {
            ignoreCharacterChangesFlag = ignore;
        }


        /// <summary>
        /// Set by "#continue" Yarn spinner tag when the current dialogue line should be auto-advanced,
        /// and the next dialogue line will be auto-appended to this one.
        /// </summary>
        public void SetContinueOnNextLine(bool continueLine)
        {
            continueOnNextLineFlag = continueLine;
        }


        public bool GetContinueOnNextLine()
        {
            return continueOnNextLineFlag;
        }


        public void SetPerformedContinueOnNextLine(bool continuedLine)
        {
            performedContinueOnNextLineFlag = continuedLine;
        }


        public bool GetPerformedContinueOnNextLine()
        {
            return performedContinueOnNextLineFlag;
        }


        public bool SetDialogueOptionRead(int dialogueID)
        {
            return readDialogueIDs.Add(dialogueID);
        }


        public bool GetDialogueOptionRead(int dialogueID)
        {
            return readDialogueIDs.Contains(dialogueID);
        }


        [YarnCommand]
        public static void EnterCharacterInRoom(string characterName, string roomName)
        {
            AceRoom room = Instance.allRooms[roomName];

            if (room == null)
            {
                Debug.LogError("Can't enter character in an empty room!");
                return;
            }

            AceCharacter character = Instance.allCharacters[characterName.ToLower()];

            if (character == null)
            {
                Debug.LogError($"Can't find character '{characterName.ToLower()}' to enter!");
                return;
            }

            room.SetCurrentCharacterInRoom(character);
            if (roomName == StoryManager.Instance.currentRoom)
            {
                StoryManager.Instance.ShowCharacterByName(characterName);
            }
        }


        [YarnCommand]
        public static void EnterCharacter(string characterName)
        {
            EnterCharacterInRoom(characterName, Instance.currentRoom);
        }


        [YarnCommand]
        public static void ExitCharacterFromRoom(string roomName)
        {
            AceRoom room = Instance.allRooms[roomName];

            if (room == null)
            {
                Debug.LogError("Can't exit character in an empty room!");
                return;
            }

            room.SetCurrentCharacterInRoom(null);
        }


        [YarnCommand]
        public static void ExitCharacter()
        {
            ExitCharacterFromRoom(Instance.currentRoom);
        }


        [YarnCommand]
        public static Coroutine PlayAnimationBlocking(string characterName, string animName)
        {
            StoryManager.Instance.ShowCharacterByName(characterName);
            return StoryManager.Instance.characterInCurrentRoom?.PlayAnimation(animName);
        }


        [YarnFunction]
        public static bool InventoryHasItem(string itemID)
        {
            return StoryManager.Instance.playerInventory.ContainsKey(itemID);
        }


        [YarnCommand]
        public static void AddInventoryItem(string itemID)
        {
            if (!InventoryHasItem(itemID))
            {
                StoryManager.Instance.playerInventory.Add(itemID, StoryManager.Instance.allItems[itemID]);
            }
        }


        [YarnCommand]
        public static void RemoveInventoryItem(string itemID)
        {
            if (InventoryHasItem(itemID))
            {
                StoryManager.Instance.playerInventory.Remove(itemID);
            }
        }


        [YarnCommand]
        public static void ShowObject(string objectID)
        {
            bool foundObject = false;
            AceRoom currentRoom = StoryManager.Instance.allRooms[StoryManager.Instance.currentRoom];
            foreach (AceObjectData objData in currentRoom.objectsInRoom)
            {
                if (objData.id == objectID)
                {
                    foundObject = true;
                    objData.aceObject.gameObject.SetActive(true);
                    objData.aceObject.data.visible = true;
                    break;
                }
            }
            if (!foundObject)
            {
                Debug.LogWarning("ShowObject called, but no object ID '{objectID}' exists in the current room!");
            }
        }


        [YarnCommand]
        public static void HideObject(string objectID)
        {
            bool foundObject = false;
            AceRoom currentRoom = StoryManager.Instance.allRooms[StoryManager.Instance.currentRoom];
            foreach (AceObjectData objData in currentRoom.objectsInRoom)
            {
                if (objData.id == objectID)
                {
                    foundObject = true;
                    objData.aceObject.gameObject.SetActive(false);
                    objData.aceObject.data.visible = false;
                    break;
                }
            }
            if (!foundObject)
            {
                Debug.LogWarning("HideObject called, but no object ID '{objectID}' exists in the current room!");
            }
        }

        [YarnCommand]
        public static Coroutine FadeInCharacter(string charName, float duration)
        {
            StoryManager.Instance.ShowCharacterByName(charName);
            return StoryManager.Instance.characterOnScreen.animator.FadeIn(duration);
        }


        [YarnCommand]
        public static Coroutine FadeOutCharacter(string charName, float duration)
        {
            StoryManager.Instance.ShowCharacterByName(charName);
            return StoryManager.Instance.characterOnScreen.animator.FadeOut(duration);
        }


        /// <summary>
        /// Begins opening the dialogue options for the current AceCharacter in the current AceRoom.
        /// </summary>
        public void BeginTalkToCharacter()
        {
            AceRoom room = allRooms[currentRoom];

            if (room == null)
            {
                Debug.LogError("Can't talk to character in an empty room!");
                return;
            }
            if (room.currentCharacterInRoom == null)
            {
                Debug.LogError("Can't talk to non-existent character in the room!");
                return;
            }
            // This might trip? idk why I have a bad feeling about this
            if (yarnDialogueRunner.IsDialogueRunning)
            {
                Debug.LogError("Can't talk to character - a dialogue is currently happening!");
                return;
            }
            if (string.IsNullOrEmpty(room.currentCharacterInRoom.onInteract))
            {
                Debug.LogError("Conversation can't happen! Conversation node for " +
                        $"'{room.currentCharacterInRoom.id}' is empty.");
            }

            // Turn on and off UI elements
            GameManager.Instance.PushState(PlayerActionState.ROOM_TALK);
        }


        public void BeginInvestigatingRoom()
        {
            GameManager.Instance.PushState(PlayerActionState.ROOM_INVESTIGATE);
        }


        public void OpenPresentUI()
        {
            UIManager.Instance.showPresentButton = true;
            GameManager.Instance.PushState(PlayerActionState.ITEMS);
        }


        public void OpenTravelUI()
        {
            GameManager.Instance.PushState(PlayerActionState.ROOM_TRAVEL);
        }
    }
}
