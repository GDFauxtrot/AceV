using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Yarn.Unity;
using DG.Tweening;


namespace AceV
{
    [Serializable]
    public struct AceCharacterData
    {
        public string id;
        public string name;
        public string onInteract;
        public string onPresent;
        public AceCharacter obj;
    }

    public class AceCharacter : AceEntityBase
    {
        public SpriteRenderer spriteRenderer;
        public CharacterAnimator animator;

        public AceCharacterData data;

        /// <summary>
        /// The ID of this character.
        /// Must match the asset folder and Yarn Spinner name as well.
        /// </summary>
        public string id { get { return data.id; } }

        /// <summary>
        /// The name of this character. This is displayed to the player instead of the ID
        /// (which can't contain some symbols due to file name limits)
        /// </summary>
        public string fullName { get { return data.name; } }

        /// <summary>
        /// The Yarn Spinner node this character's dialogue options are contained in.
        /// </summary>
        public string onInteract { get { return data.onInteract; } }


        /// <summary>
        /// The Yarn Spinner node this character's present options are contained in.
        /// </summary>
        public string onPresent { get { return data.onPresent; } }

        /// <summary>
        /// The name of the emotion this character is currently expressing.
        /// </summary>
        public string emotion { get; private set; }

        /// <summary>
        /// The name of the room this character is currently in.
        /// </summary>
        [ReadOnly]
        public string currentRoom;

        public void Initialize(string charID, string charName, string charInteract, string charPresent)
        {
            gameObject.name = charID;

            data = new AceCharacterData()
            {
                id = charID,
                name = charName,
                onInteract = charInteract,
                onPresent = charPresent,
                obj = this
            };

            // All character sprites align with the bottom of the screen
            transform.position = new Vector3(0f, -Camera.main.orthographicSize, 0f);

            emotion = "neutral";
            animator.UpdateEmotion();
            animator.StopAnimating();
        }


        public void SetEmotion(string newEmotion)
        {
            bool emotionChanged = emotion != newEmotion;

            emotion = newEmotion;

            if (emotionChanged)
            {
                animator.UpdateEmotion();
                animator.ResetAnimation();
            }
        }


        public Coroutine PlayAnimation(string animName)
        {
            return animator.PlayAnimation(animName);
        }


        public float EnterInvestigateMode()
        {
            float time = 0.5f;
            float alpha = 0.2f;
            spriteRenderer.DOColor(new Color(1f, 1f, 1f, alpha), time).SetEase(Ease.InCubic);
            transform.DOMoveY(transform.position.y - spriteRenderer.size.y, time);
            return time;
        }


        public float ExitInvestigateMode()
        {
            float time = 0.5f;
            spriteRenderer.DOColor(new Color(1f, 1f, 1f, 1f), time).SetEase(Ease.OutCubic);
            transform.DOMoveY(transform.position.y + spriteRenderer.size.y, time);
            return time;
        }
    }
}
