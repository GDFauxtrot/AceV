using System.Collections.Generic;
using UnityEngine;


namespace AceV
{
    public class TagManager
    {
        public static float HandleTag(string tagName)
        {
            float timeTaken = 0f;
            string[] tagSplit = tagName.Split(':');
            string tagKey = tagSplit[0].Trim();
            string tagValue = "";
            if (tagSplit.Length > 1)
            {
                tagValue = tagSplit[1].Trim();
            }
            
            switch (tagKey)
            {
                case "noshow":
                    StoryManager.Instance.SetIgnoreCharacterChanges(true);
                    break;
                case "emotion":
                    StoryManager.Instance.characterInCurrentRoom?.SetEmotion(tagValue);
                    break;
                case "continue":
                    StoryManager.Instance.SetContinueLineDelay(string.IsNullOrEmpty(tagValue) ? -1 : float.Parse(tagValue));
                    StoryManager.Instance.SetContinueOnNextLine(true);
                    break;
                case "anim":
                    StoryManager.Instance.characterInCurrentRoom?.PlayAnimation(tagValue);
                    break;
            }

            return timeTaken;
        }
    }
}