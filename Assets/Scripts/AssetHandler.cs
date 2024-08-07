using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using SimpleJSON;


namespace AceV
{
    public class CharacterEmotion
    {
        public Sprite[] emotionFrames;
        public List<(int, int)> frames;
        public List<(int, int)> framesTalking; // Always loops
        public bool loop;
        public int loopIndex;
    }

    public class AssetHandler : MonoBehaviour
    {
        /// Cached paths over the lifetime of the game
        private static Dictionary<string, string> cachedCharacterPaths = new Dictionary<string, string>();

        /// Cached loaded emotion data over the lifetime of the game
        private static Dictionary<string, CharacterEmotion> cachedLoadedEmotions = new Dictionary<string, CharacterEmotion>();

        /// Cached loaded JSON emotion data over the lifetime of the game (eliminating need for contant IO operations)
        private static Dictionary<string, JSONNode> cachedEmotionJsons = new Dictionary<string, JSONNode>();


        void Awake()
        {
            DontDestroyOnLoad(gameObject);
        }

        
        /// Returns the absolute path on this system for a character's graphics assets
        public static string GetSpritePathForCharacterName(string storyPath, string characterName)
        {
            if (!cachedCharacterPaths.ContainsKey(characterName))
            {
                string path = Path.Combine(storyPath, "Characters", characterName);
                cachedCharacterPaths[characterName] = path;
            }

            return cachedCharacterPaths[characterName];
        }


        /// Returns the CharacterEmotion data for a given character's emotion
        public static CharacterEmotion GetCharacterEmotion(string storyPath, string characterName, string characterEmotion)
        {
            // Reduce to directory only if we erroneously passed in a file name instead
            if (File.Exists(storyPath))
            {
                storyPath = Path.GetDirectoryName(storyPath);
            }

            // Check if character exists
            string charPath = GetSpritePathForCharacterName(storyPath, characterName);
            if (!Directory.Exists(charPath) || string.IsNullOrEmpty(characterName))
            {
                return null;
            }

            string emotionKey = string.Concat(characterName, '-', characterEmotion);
            if (!cachedLoadedEmotions.ContainsKey(emotionKey))
            {
                // Load data
                CharacterEmotion loadedEmotion = LoadCharacterEmotionData(charPath, characterName, characterEmotion);

                if (loadedEmotion == null)
                {
                    return null;
                }
                
                cachedLoadedEmotions[emotionKey] = loadedEmotion;
            }
            
            return cachedLoadedEmotions[emotionKey];
        }


        /// The IO function for loading a character emotion JSON, utilizing caching when possible
        private static JSONNode LoadCharacterEmotionJSON(string path)
        {
            if (!cachedEmotionJsons.ContainsKey(path))
            {
                // Load data
                string jsonFromFile = File.ReadAllText(path);
                JSONNode jsonResult = JSON.Parse(jsonFromFile);
                cachedEmotionJsons[path] = jsonResult;
            }

            return cachedEmotionJsons[path];
        }


        /// The IO function for loading character emotion graphics and creating a CharacterEmotion object
        private static CharacterEmotion LoadCharacterEmotionData(string charPath, string charName, string charEmotion)
        {
            // Start by getting and parsing the JSON
            string jsonPath = Path.Combine(charPath, string.Concat(charName, ".json"));

            if (!File.Exists(jsonPath))
            {
                return null;
            }

            JSONNode jsonEmotions = LoadCharacterEmotionJSON(jsonPath);

            // Now that we have the list of emotions, get the one we're looking for and make a new CharacterEmotion
            CharacterEmotion newEmotion = new CharacterEmotion();
            foreach (var emotion in jsonEmotions.Keys)
            {
                if (emotion != charEmotion)
                {
                    continue;
                }

                JSONNode emotionJson = jsonEmotions[emotion];
                newEmotion.loop = emotionJson["loop"];
                newEmotion.loopIndex = emotionJson["loopIndex"];
                newEmotion.frames = new List<(int, int)>();
                int maxSeenFrameIndex = 0;
                foreach (JSONNode emotionFrame in emotionJson["frames"])
                {
                    string emotionFrameString = emotionFrame.Value;
                    string[] frameSplit = emotionFrameString.Split(':');
                    int frameIndex = int.Parse(frameSplit[0]);
                    int frameTiming = int.Parse(frameSplit[1]);
                    newEmotion.frames.Add((frameIndex, frameTiming));
                    maxSeenFrameIndex = Mathf.Max(maxSeenFrameIndex, frameIndex);
                }
                newEmotion.framesTalking = new List<(int, int)>();
                foreach (JSONNode emotionFrame in emotionJson["talkingFrames"])
                {
                    string emotionFrameString = emotionFrame.Value;
                    string[] frameSplit = emotionFrameString.Split(':');
                    int frameIndex = int.Parse(frameSplit[0]);
                    int frameTiming = int.Parse(frameSplit[1]);
                    newEmotion.framesTalking.Add((frameIndex, frameTiming));
                    maxSeenFrameIndex = Mathf.Max(maxSeenFrameIndex, frameIndex);
                }

                // Load files from disk into an array
                newEmotion.emotionFrames = new Sprite[maxSeenFrameIndex+1];
                for (int i = 0; i <= maxSeenFrameIndex; ++i)
                {
                    string frameFileName = string.Concat(charName, '-', charEmotion, '-', i, ".png");
                    string framePath = Path.Combine(charPath, frameFileName);
                    Sprite loadedSprite;
                    GetSpriteFromPath(framePath, out loadedSprite, new Vector2(0.5f, 0f));
                    newEmotion.emotionFrames[i] = loadedSprite;
                }
                break;
            }

            return newEmotion;
        }


        public static bool GetSpriteFromPath(string path, out Sprite result, Vector2 pivot, bool generatePhysics = false, uint colliderPadding = 0)
        {
            if (GetTextureFromPath(path, out Texture2D t2d))
            {
                float width = t2d.width;
                float height = t2d.height;
                Sprite loadedSprite;

                if (generatePhysics)
                {
                    loadedSprite = Sprite.Create(t2d, new Rect(0, 0, width, height), pivot, 10, colliderPadding, SpriteMeshType.Tight, Vector4.zero, true);
                }
                else
                {
                    loadedSprite = Sprite.Create(t2d, new Rect(0, 0, width, height), pivot, 10);
                }

                result = loadedSprite;
                return true;
            }
            else
            {
                result = null;
                return false;
            }
        }

        
        public static bool GetTextureFromPath(string path, out Texture2D result)
        {
            try
            {
                byte[] loadedFileBytes = File.ReadAllBytes(path);
                Texture2D t2d = new Texture2D(4, 4);
                t2d.LoadImage(loadedFileBytes);
                t2d.filterMode = FilterMode.Point;
                result = t2d;
                return true;
            }
            catch (Exception e)
            {
                Debug.LogError($"ERROR '{e.GetType()}' while loading texture from disk! Path: '{path}'");
                result = null;
                return false;
            }
        }
    }
}
