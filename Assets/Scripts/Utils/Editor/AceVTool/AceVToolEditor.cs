#if UNITY_EDITOR

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditorInternal;
using System.IO;
using SimpleJSON;
using Unity.EditorCoroutines.Editor;
using UnityEngine.UIElements;
using AceV;


public class AceVToolEditor : EditorWindow
{
    private static EditorWindow wnd;

    [SerializeField]
    private TextAsset selectedJSONAsset;
    private string selectedJSONAssetPath;

    [SerializeField]
    private bool roomsFoldout;
    [SerializeField]
    private bool charactersFoldout;

    private List<AceRoom> rooms = new List<AceRoom>();
    ReorderableList roomsList;
    int currentlyEditingRoomIndex = -1;
    bool focusNewRoomName = false;

    private List<AceCharacterData> characters = new List<AceCharacterData>();
    ReorderableList charactersList;
    private List<Texture2D> characterIcons = new List<Texture2D>();
    private float characterIconPreviewSize = 40f;

    private EditorCoroutine savedAlertCoroutine;
    private bool savedAlertShown = false;

    private AceVToolRoomEditor roomEditor;

    [MenuItem("AceV/AceVTool")]
    public static void ShowWindow()
    {
        // This method is called when the user selects the menu item in the Editor
        wnd = GetWindow<AceVToolEditor>();
        wnd.titleContent = new GUIContent("AceVTool Editor");
    }


    [MenuItem("Assets/Create/Empty JSON", false, 1)]
    private static void CreateEmptyJSON()
    {
        ProjectWindowUtil.CreateAssetWithContent(
            "Default Name.json",
            "{\n\n}");
    }


    void OnEnable()
    {
        // Could be null on refreshes
        if (wnd == null)
        {
            wnd = GetWindow<AceVToolEditor>();
        }

        roomsList = new ReorderableList(rooms, typeof(AceRoom), true, false, true, true)
        {
            drawElementCallback = DrawRoomsListElement,
            onAddCallback = AddToRoomsList
        };
        
        charactersList = new ReorderableList(characters, typeof(AceCharacterData), true, false, true, true)
        {
            drawElementCallback = DrawCharactersListElement,
            onAddCallback = AddToCharactersList,
            elementHeightCallback = CharactersListHeight
        };

        // If we've selected a new asset (or OnGUI for the firs time), load its contents!
        if (selectedJSONAsset != null)
        {
            ParseSelectedJSONAsset();
        }
    }


    void OnGUI()
    {
        TextAsset oldSelectedAsset = selectedJSONAsset;
        selectedJSONAsset = (TextAsset)EditorGUILayout.ObjectField("Story JSON", selectedJSONAsset, typeof(TextAsset), false);

        if (Path.GetExtension(AssetDatabase.GetAssetPath(selectedJSONAsset)) != ".json")
        {
            selectedJSONAsset = oldSelectedAsset;
        }

        if (oldSelectedAsset != selectedJSONAsset)
        {
            ParseSelectedJSONAsset();
        }

        if (selectedJSONAsset == null)
        {
            EditorGUILayout.HelpBox("No JSON asset loaded! Make one in the project first (Assets -> Create -> Empty JSON)", MessageType.Error);
            return;
        }

        EditorGUILayout.BeginHorizontal();
        {
            EditorGUILayout.LabelField("File path:", GUILayout.Width(65f));
            GUI.enabled = false;
            selectedJSONAssetPath = "";
            if (selectedJSONAsset != null)
            {
                selectedJSONAssetPath = Path.Combine(Path.GetFullPath(Application.dataPath + "/.."), AssetDatabase.GetAssetPath(selectedJSONAsset));
            }
            EditorGUILayout.TextField(string.IsNullOrEmpty(selectedJSONAssetPath) ? "NONE SELECTED" : selectedJSONAssetPath);
            GUI.enabled = true;
        }
        EditorGUILayout.EndHorizontal();

        // Defined rooms
        roomsFoldout = EditorGUILayout.BeginFoldoutHeaderGroup(roomsFoldout, "Rooms");
        if (roomsFoldout)
        {
            EditorGUI.indentLevel++;

            roomsList.DoLayoutList();

            EditorGUI.indentLevel--;
        }
        
        EditorGUILayout.EndFoldoutHeaderGroup();

        // Defined characters
        charactersFoldout = EditorGUILayout.BeginFoldoutHeaderGroup(charactersFoldout, "Characters");
        if (charactersFoldout)
        {
            EditorGUI.indentLevel++;

            GUI.enabled = characters.Count > 0;
            bool iconRefreshBtn = GUILayout.Button("Refresh Icons", GUILayout.Width(100f));
            GUI.enabled = true;

            if (iconRefreshBtn)
            {
                characterIcons.Clear();
                // Go through each character ID and pull the "neutral" face for them
                for (int i = 0; i < characters.Count; ++i)
                {
                    AceCharacterData charData = characters[i];
                    CharacterEmotion charNeutral = AssetHandler.GetCharacterEmotion(Path.GetDirectoryName(selectedJSONAssetPath), charData.id, "neutral");
                    if (charNeutral == null)
                    {
                        characterIcons.Add(null);
                        continue;
                    }
                    if (charNeutral.emotionFrames.Length == 0)
                    {
                        characterIcons.Add(null);
                        continue;
                    }

                    characterIcons.Add(charNeutral.emotionFrames[0].texture);
                }
            }

            charactersList.DoLayoutList();

            EditorGUI.indentLevel--;
        }
        EditorGUILayout.EndFoldoutHeaderGroup();

        
        // Show save button, but disable if any errors occur (eg. duplicate ID's, no room, etc)
        bool foundDuplicateRoomName = false;
        HashSet<string> names = new HashSet<string>();
        foreach (AceRoom room in rooms)
        {
            if (names.Contains(room.id))
            {
                foundDuplicateRoomName = true;
            }
            names.Add(room.id);
        }
        bool anyRoomsMade = rooms.Count > 0;
        bool foundDuplicateCharName = false;
        names.Clear();
        foreach (AceCharacterData character in characters)
        {
            if (names.Contains(character.id))
            {
                foundDuplicateCharName = true;
            }
            names.Add(character.id);
        }

        bool saveJsonBtnEnabled = true;
        saveJsonBtnEnabled &= !foundDuplicateRoomName;
        saveJsonBtnEnabled &= anyRoomsMade;
        saveJsonBtnEnabled &= !foundDuplicateCharName;

        GUI.enabled = saveJsonBtnEnabled;
        bool saveJsonBtn = GUILayout.Button("Save JSON");
        GUI.enabled = true;

        if (!anyRoomsMade)
        {
            EditorGUILayout.HelpBox("No rooms made!", MessageType.Error);
        }
        if (foundDuplicateRoomName)
        {
            EditorGUILayout.HelpBox("Duplicate room ID's found!", MessageType.Error);
        }
        if (foundDuplicateCharName)
        {
            EditorGUILayout.HelpBox("Duplicate character ID's found!", MessageType.Error);
        }

        // Stop editing name if we get an unfocus or Enter key pressed
        if (currentlyEditingRoomIndex != -1 && focusNewRoomName == false)
        {
            bool enterKeyPressed = Event.current.isKey && Event.current.keyCode == KeyCode.Return;
            if (GUI.GetNameOfFocusedControl() != $"RoomName{currentlyEditingRoomIndex}" || enterKeyPressed)
            {
                EditorGUI.FocusTextInControl(null);
                currentlyEditingRoomIndex = -1;
            }
        }

        if (saveJsonBtn)
        {
            saveJsonBtn = false;
            SaveToCurrentJSON();
            savedAlertShown = true;

            if (savedAlertCoroutine != null)
            {
                EditorCoroutineUtility.StopCoroutine(savedAlertCoroutine);
            }
            savedAlertCoroutine = EditorCoroutineUtility.StartCoroutine(SavedAlertCoroutine(), this);
        }
        if (savedAlertShown)
        {
            EditorGUILayout.HelpBox("Saved!", MessageType.Info);
        }
    }


    IEnumerator SavedAlertCoroutine()
    {
        yield return new WaitForSecondsRealtime(3f);
        savedAlertShown = false;
        wnd.Repaint();
    }


    /// <summary>
    /// A very involved function that parses the current JSON asset for all story data
    /// and gets the editor ready for... editing.
    /// </summary>
    private void ParseSelectedJSONAsset()
    {
        Debug.Log("JSON ASSET CHANGED");

        JSONNode parsedJSON = JSON.Parse(selectedJSONAsset.text);

        // Start with rooms
        if (!parsedJSON.HasKey("Rooms"))
        {
            Debug.LogWarning("This JSON asset is missing 'Rooms'! Assuming a blank file.");
        }
        else
        {
            JSONNode parsedJSONRooms = parsedJSON["Rooms"];

            rooms.Clear();

            // Rooms are simple - just an ID and a background image URL
            foreach (JSONNode jsonRoom in parsedJSONRooms)
            {
                AceRoom room = new AceRoom();
                // TODO update this
                // room.Initialize(jsonRoom["id"], jsonRoom["background"]);
                rooms.Add(room);
            }
        }

        // TODO parse everything else! We aint done yet here
    }

    
    void AddToRoomsList(ReorderableList list)
    {
        AceRoom newRoom = new AceRoom();
        // TODO update this
        // newRoom.Initialize("New Room", "");
        rooms.Add(newRoom);

        // Mark that we're in edit mode
        currentlyEditingRoomIndex = rooms.Count-1;
        focusNewRoomName = true;
    }


    void DrawRoomsListElement(Rect rect, int index, bool isActive, bool isFocused)
    {
        AceRoom room = rooms[index];

        bool roomEditBtnPressed = GUI.Button(new Rect(rect.x, rect.y, 50f, rect.height), "Edit");
        
        if (roomEditBtnPressed)
        {
            if (roomEditor == null)
            {
                roomEditor = GetWindow<AceVToolRoomEditor>();
                roomEditor.ShowEditor(this, room, selectedJSONAssetPath);
            }
        }
        // TODO editor for room in separate window

        bool editingThisRoomName = currentlyEditingRoomIndex == index;
        Rect roomNameRect = new Rect(rect.x + 70f, rect.y, rect.width - 70f, rect.height);
        if (editingThisRoomName)
        {
            GUI.SetNextControlName($"RoomName{index}");
            string newRoomName = EditorGUI.TextField(roomNameRect, room.id);
            // TODO validating room name (alphanumeric, dashes, underscores and spaces only)
            room.SetRoomName(newRoomName);

            // Trigger this ONCE! This sets the focus to the text field after element create
            if (focusNewRoomName)
            {
                focusNewRoomName = false;
                EditorGUI.FocusTextInControl($"RoomName{index}");
            }
        }
        else
        {
            GUI.enabled = false;
            EditorGUI.TextField(roomNameRect, room.id);
            GUI.enabled = true;
        }
    }


    void AddToCharactersList(ReorderableList list)
    {
        AceCharacterData newChar = new AceCharacterData()
        {
            id = "newCharId",
            name = "New Character",
            onInteract = "characterDialogueNode"
        };

        characters.Add(newChar);
        characterIcons.Add(null);
    }

    float CharactersListHeight(int index)
    {
        return characterIconPreviewSize;
    }

    void DrawCharactersListElement(Rect rect, int index, bool isActive, bool isFocused)
    {
        AceCharacterData character = characters[index];

        float charIDLabelWidth = 20f;
        float charIDTextWidth = 100f;
        float charNameLabelWidth = 40f;
        float charNameTextWidth = 130f;
        float charNodeLabelWidth = 40f;
        float charNodeTextWidth = 180f;

        rect.height = EditorGUIUtility.singleLineHeight;

        // Draw character icon (a good test to see if you've set everything up correctly!)
        if (characterIcons[index] == null)
        {
            GUI.Box(new Rect(rect.x, rect.y, characterIconPreviewSize, characterIconPreviewSize), "???");
        }
        else
        {
            GUI.Box(new Rect(rect.x, rect.y, characterIconPreviewSize, characterIconPreviewSize), characterIcons[index]);
        }

        Rect charRect = new Rect(rect.x + characterIconPreviewSize + 10f, rect.y + characterIconPreviewSize/4f, charIDLabelWidth, rect.height);
        EditorGUI.LabelField(charRect, "ID");

        charRect.x += charIDLabelWidth;
        charRect.width = charIDTextWidth;
        character.id = EditorGUI.TextField(charRect, character.id);

        charRect.x += charIDTextWidth + 5;
        charRect.width = charNameLabelWidth;
        EditorGUI.LabelField(charRect, "Name");

        charRect.x += charNameLabelWidth;
        charRect.width = charNameTextWidth;
        string newCharName = EditorGUI.TextField(charRect, character.name);
        // TODO validating character name
        character.name = newCharName;

        charRect.x += charNameTextWidth + 5;
        charRect.width = charNodeLabelWidth;
        EditorGUI.LabelField(charRect, "Node");

        charRect.x += charNodeLabelWidth;
        charRect.width = charNodeTextWidth;
        string newCharNode = EditorGUI.TextField(charRect, character.onInteract);
        character.onInteract = newCharNode;

        // Structs are pass-by-copy! Must apply changes back in order for them to stick
        characters[index] = character;
    }


    private void SaveToCurrentJSON()
    {
        if (selectedJSONAsset == null)
        {
            Debug.LogError("ERROR: Saving to JSON with no selected JSON asset!");
            return;
        }

        JSONObject outputJSON = new JSONObject();

        // Write rooms to JSON
        JSONArray roomsJSON = new JSONArray();
        foreach (AceRoom room in rooms)
        {
            JSONObject roomObj = new JSONObject();
            roomObj.Add("id", room.id);
            roomObj.Add("background", room.backgroundRelativePath);

            roomsJSON.Add(roomObj);
        }
        outputJSON.Add("Rooms", roomsJSON);

        // Write objects and POIs to JSON
        JSONArray objectsJSON = new JSONArray();
        JSONArray poisJSON = new JSONArray();
        foreach (AceRoom room in rooms)
        {
            foreach (AceObjectData objData in room.objectsInRoom)
            {
                JSONObject objObj = new JSONObject();

                objObj.Add("id", objData.id);
                objObj.Add("room", objData.room);
                objObj.Add("oninteract", objData.onInteract);
                objObj.Add("position", objData.position.ToString());
                if (objData.scale != 1f)
                {
                    objObj.Add("scale", objData.scale);
                }

                objectsJSON.Add(objObj);
            }
            foreach (AcePOIData poiData in room.poisInRoom)
            {
                JSONObject poiObj = new JSONObject();

                poiObj.Add("id", poiData.id);
                poiObj.Add("room", poiData.room);
                poiObj.Add("oninteract", poiData.onInteract);

                JSONArray boundsArray = new JSONArray();
                foreach (Rect bound in poiData.bounds)
                {
                    boundsArray.Add(bound.ToString());
                }
                poiObj.Add("bounds", boundsArray);

                poisJSON.Add(poiObj);
            }
        }
        outputJSON.Add("Objects", objectsJSON);
        outputJSON.Add("POIs", poisJSON);

        // Write characters to JSON
        JSONArray charactersJSON = new JSONArray();
        foreach (AceCharacterData character in characters)
        {
            JSONObject charObj = new JSONObject();
            charObj.Add("id", character.id);
            charObj.Add("name", character.name);
            charObj.Add("oninteract", character.onInteract);
            charactersJSON.Add(charObj);
        }
        outputJSON.Add("Characters", charactersJSON);

        File.WriteAllText(AssetDatabase.GetAssetPath(selectedJSONAsset), outputJSON.ToStringIndented());
        EditorUtility.SetDirty(selectedJSONAsset);
    }


    public void SetRoomDataFromRoomEditor(AceRoom newRoom)
    {
        bool roomFound = false;
        for (int i = 0; i < rooms.Count; ++i)
        {
            AceRoom room = rooms[i];

            if (room.id == newRoom.id)
            {
                roomFound = true;
                rooms[i] = newRoom;
            }
        }

        // Maybe the room was removed? Let's preserve data by adding this room to the end of the list
        if (!roomFound)
        {
            rooms.Add(newRoom);
        }
    }
}

#endif