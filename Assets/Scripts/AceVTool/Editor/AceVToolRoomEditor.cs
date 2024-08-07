#if UNITY_EDITOR

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditorInternal;
using System.Text.RegularExpressions;
using System.IO;
using System.Linq;
using AceV;


public class AceVToolRoomEditor : EditorWindow
{
    private static EditorWindow wnd;
    private AceVToolEditor parent;
    private AceRoom roomEditing;
    private string storyPath;

    private bool objectsFoldout;
    private bool poisFoldout;

    private List<AceObjectData> objects = new List<AceObjectData>();
    private List<AcePOIData> pois = new List<AcePOIData>();

    private ReorderableList objectsList;
    private ReorderableList poisList;

    private List<Texture2D> objectIcons = new List<Texture2D>();
    private float objectIconPreviewSize = 40f;

    private Texture2D selectedBackgroundTexture;
    private Texture2D blankBackgroundTexture;

    void OnEnable()
    {
        // Could be null on refreshes
        if (wnd == null)
        {
            wnd = GetWindow<AceVToolRoomEditor>();
        }

        objectsList = new ReorderableList(objects, typeof(AceObjectData), true, false, true, true)
        {
            drawElementCallback = DrawObjectsListElement,
            onAddCallback = AddToObjectsList,
            elementHeightCallback = ObjectsListHeight
        };

        poisList = new ReorderableList(pois, typeof(AcePOIData), true, false, true, true)
        {
            drawElementCallback = DrawPOIsListElement,
            onAddCallback = AddToPOIsList
        };

        blankBackgroundTexture = new Texture2D(16, 9);
        blankBackgroundTexture.SetPixels(Enumerable.Repeat(Color.black, 16 * 9).ToArray());
        blankBackgroundTexture.Apply();
    }


    public void ShowEditor(AceVToolEditor parentEditor, AceRoom roomToEdit, string path)
    {
        wnd.titleContent = new GUIContent("Room Editor - " + roomToEdit.id);
        parent = parentEditor;
        roomEditing = roomToEdit;
        storyPath = File.Exists(path) ? Path.GetDirectoryName(path): path;
    }


    void OnGUI()
    {
        if (roomEditing == null)
        {
            return;
        }

        // Draw background (essentially the canvas for the room editor)
        Rect backgroundRect = GUILayoutUtility.GetRect(position.width, position.height * 0.5f);
        GUI.enabled = false;
        GUI.TextArea(backgroundRect, ""); // This isn't for anything in particular, it just looks nice as a background shade
        GUI.enabled = true;
        Texture2D bgTexture = selectedBackgroundTexture == null ? blankBackgroundTexture : selectedBackgroundTexture;
        GUI.DrawTexture(backgroundRect, bgTexture, ScaleMode.ScaleToFit);

        // Draw objects on top of background
        for (int i = 0; i < objects.Count; ++i)
        {
            AceObjectData objData = objects[i];
            Texture2D objIcon = objectIcons[i];

            if (objIcon == null)
            {
                // TODO show something in case the object icon is missing! Like a box or some shit
                continue;
            }

            Rect backgroundImageRect = backgroundRect;
            float rectRatio = backgroundRect.width / backgroundRect.height;
            float bgRatio = (float)bgTexture.width / (float)bgTexture.height;
            if (rectRatio > bgRatio)
            {
                // Wider than image (extra space on left/right side)
                float targetWidth = backgroundRect.height * bgRatio;
                backgroundImageRect.x += (backgroundRect.width - targetWidth) / 2f;
                backgroundImageRect.width = targetWidth;
            }
            else if (rectRatio < bgRatio)
            {
                // Narrower than image (extra space on top/bottom)
                float targetHeight = backgroundRect.width / bgRatio;
                backgroundImageRect.y += (backgroundRect.height - targetHeight) / 2f;
                backgroundImageRect.height = targetHeight;
            }

            Vector2 objPos = WorldPositionOnRect(backgroundImageRect, objData.position);
            Vector2 objSize = GetTextureSizeOnRect(backgroundImageRect, new Vector2(objIcon.width, objIcon.height), new Vector2(480f, 270f));
            objPos.x -= objSize.x/2f;
            objPos.y -= objSize.y/2f;
            Rect objRect = new Rect(objPos, objSize);
            GUI.DrawTexture(objRect, objIcon);
        }

        // Draw object/POI UI (outline boxes)

        // Draw background image selector
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("Background", GUILayout.MaxWidth(100f));
        bool backgroundSelectBtn = GUILayout.Button("...", GUILayout.MaxWidth(20f));
        GUI.enabled = false;
        //EditorGUILayout.TextField(roomEditing.pathToBackground);
        GUI.enabled = true;
        EditorGUILayout.EndHorizontal();
        EditorGUILayout.LabelField("NOTE: Files must be in a subdirectory to the story JSON! All paths will be relative.");
        if (selectedBackgroundTexture != null)
        {
            float bgAspectRatio = (float)selectedBackgroundTexture.width / (float)selectedBackgroundTexture.height;

            if (bgAspectRatio < (16f / 9f))
            {
                EditorGUILayout.HelpBox("Background size is less than 16-by-9! There will be vertical black bars in-game.", MessageType.Warning);
            }
        }

        if (backgroundSelectBtn)
        {
            string chosenBG = EditorUtility.OpenFilePanel("Select Background", storyPath, "png,jpg,bmp,tif");

            if (!string.IsNullOrEmpty(chosenBG))
            {
                if (!chosenBG.StartsWith(storyPath))
                {
                    EditorUtility.DisplayDialog("Warning", "Background must be in the same folder as the story JSON!", "OK");
                }
                else
                {
                    AssetHandler.GetTextureFromPath(chosenBG, out selectedBackgroundTexture);
                    AssetHandler.GetSpriteFromPath(chosenBG, out Sprite bgSprite, new Vector2(0.5f, 0.5f));
                    roomEditing.SetRoomBackground(chosenBG, bgSprite);
                }
            }
        }

        // Objects section
        objectsFoldout = EditorGUILayout.BeginFoldoutHeaderGroup(objectsFoldout, "Objects");
        if (objectsFoldout)
        {
            GUI.enabled = objects.Count > 0;
            bool objIconRefreshBtn = GUILayout.Button("Refresh", GUILayout.Width(100f));
            GUI.enabled = true;

            if (objIconRefreshBtn)
            {
                objectIcons.Clear();
                for (int i = 0; i < objects.Count; ++i)
                {
                    AceObjectData obj = objects[i];
                    string objPath = Path.Combine(storyPath, "Objects", obj.id + ".png");
                    if (!AssetHandler.GetTextureFromPath(objPath, out Texture2D objTexture))
                    {
                        objectIcons.Add(null);
                        continue;
                    }
                    objectIcons.Add(objTexture);
                }
            }
            
            objectsList.DoLayoutList();
        }

        EditorGUILayout.EndFoldoutHeaderGroup();

        // POIs section
        poisFoldout = EditorGUILayout.BeginFoldoutHeaderGroup(poisFoldout, "POIs");
        if (poisFoldout)
        {
            poisList.DoLayoutList();
        }
        EditorGUILayout.EndFoldoutHeaderGroup();
    }


    float ObjectsListHeight(int index)
    {
        return objectIconPreviewSize;
    }


    void AddToObjectsList(ReorderableList list)
    {
        AceObjectData newObj = new AceObjectData()
        {
            id = "newObject",
            // name = "New Object",
            room = roomEditing.id,
            onInteract = "",
            position = Vector3.zero,
            scale = 1f
        };

        objects.Add(newObj);
        objectIcons.Add(null);
    }


    void DrawObjectsListElement(Rect rect, int index, bool isActive, bool isFocused)
    {
        AceObjectData objectData = objects[index];

        float objIDLabelWidth = 20f;
        float objIDTextWidth = 120f;
        float objNodeLabelWidth = 40f;
        float objNodeTextWidth = 120f;
        float objPosLabelWidth = 50f;
        float objPosTextWidth = 80f;
        float objScaleLabelWidth = 40f;
        float objScaleTextWidth = 30f;
        float objLabelPadding = 5f;

        rect.height = EditorGUIUtility.singleLineHeight;

        // Draw object icon (a good test to see if you've set everything up correctly!)
        if (objectIcons[index] == null)
        {
            GUI.Box(new Rect(rect.x, rect.y, objectIconPreviewSize, objectIconPreviewSize), "???");
        }
        else
        {
            GUI.Box(new Rect(rect.x, rect.y, objectIconPreviewSize, objectIconPreviewSize), objectIcons[index]);
        }

        Rect objRect = new Rect(rect.x + objectIconPreviewSize + 10f, rect.y, objIDLabelWidth, rect.height);
        EditorGUI.LabelField(objRect, "ID");

        objRect.x += objIDLabelWidth;
        objRect.width = objIDTextWidth;
        objectData.id = EditorGUI.TextField(objRect, objectData.id);

        objRect.x += objIDTextWidth + objLabelPadding;
        objRect.width = objNodeLabelWidth;
        EditorGUI.LabelField(objRect, "Node");

        objRect.x += objNodeLabelWidth;
        objRect.width = objNodeTextWidth;
        objectData.onInteract = EditorGUI.TextField(objRect, objectData.onInteract);

        objRect.x += objNodeTextWidth + objLabelPadding;
        objRect.width = objPosLabelWidth;
        EditorGUI.LabelField(objRect, "Position");

        objRect.x += objPosLabelWidth;
        objRect.width = objPosTextWidth;
        string newPos = EditorGUI.TextField(objRect, ((Vector2)objectData.position).ToString());
        Regex positionRegex = new Regex("\\-?(\\d)+(\\.\\d+)*");
        MatchCollection positionMatches = positionRegex.Matches(newPos);
        // TODO uh... validate this?
        objectData.position = new Vector2(float.Parse(positionMatches[0].Value), float.Parse(positionMatches[1].Value));

        objRect.x += objPosTextWidth + objLabelPadding;
        objRect.width = objScaleLabelWidth;
        EditorGUI.LabelField(objRect, "Scale");

        objRect.x += objScaleLabelWidth;
        objRect.width = objScaleTextWidth;
        string newScale = EditorGUI.TextField(objRect, objectData.scale.ToString());
        // TODO validate!
        objectData.scale = float.Parse(newScale);

        objects[index] = objectData;
    }


    void AddToPOIsList(ReorderableList list)
    {
        AcePOIData newPOI = new AcePOIData()
        {
            id = "newPOI",
            room = roomEditing.id,
            onInteract = "",
            bounds = new List<Rect>()
        };

        pois.Add(newPOI);
    }


    void DrawPOIsListElement(Rect rect, int index, bool isActive, bool isFocused)
    {
        AcePOIData poiData = pois[index];
    }


    /// <summary>
    /// Converts a Unity world position to an X/Y position on the given rect
    /// (Note: MANY ASSUMPTIONS MADE)
    /// </summary>
    private Vector2 WorldPositionOnRect(Rect rect, Vector2 position)
    {
        Vector2 result = new Vector2(rect.x + (rect.width/2f), rect.y + (rect.height/2f));
        // We assume this constant: a 16x9 screen will have a vertical possibility space of [-13.5, 13.5]
        // Since the background will always fill the screen vertically, we go off of this
        float extentX = 24f, extentY = 13.3f; // Should be 13.5? Not sure why it doesn't line up perfectly?

        // Figure out how far out we are w/ respect to the reference screen size
        float xRatio = (position.x/2f) / extentX;
        float yRatio = (-position.y/2f) / extentY;

        // Convert these "ratios" to position deltas for each axis
        result.x += Mathf.LerpUnclamped(0f, rect.width, xRatio);
        result.y += Mathf.LerpUnclamped(0f, rect.height, yRatio);

        return result;
    }


    private Vector2 GetTextureSizeOnRect(Rect rect, Vector2 textureSize, Vector2 rectReferenceSize)
    {
        return new Vector2(
            (textureSize.x / rectReferenceSize.x) * rect.width,
            (textureSize.y / rectReferenceSize.y) * rect.height);
    }
}

#endif