using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Yarn.Unity;


namespace AceV
{
    public class CharacterAnimator : MonoBehaviour
    {
        public AceCharacter character;

        private int currentFrame;
        private bool isPaused;

        private Coroutine fadeCoroutine;
        private Coroutine animCoroutine;

        private CharacterEmotion currentEmotionData;

        private bool isOneOffAnimationPlaying;

        private bool _isTalking;
        public bool isTalking
        {
            get
            {
                return _isTalking;
            }
            set
            {
                if (value != isTalking)
                {
                    _isTalking = value;
                    ResetAnimation();
                }
                _isTalking = value;
            }
        }


        public void StartAnimating()
        {
            StopAnimating();
            isPaused = false;
            animCoroutine = StartCoroutine(AnimationCoroutine());
        }


        public void StopAnimating()
        {
            isPaused = true;
            if (animCoroutine != null)
            {
                StopCoroutine(animCoroutine);
                animCoroutine = null;
            }
            // Update current frame being shown
            ShowCharacterFrame(currentFrame, isTalking);
        }


        public void ResetAnimation()
        {
            currentFrame = 0;

            if (isPaused)
            {
                StopAnimating();
            }
            else
            {
                StartAnimating();
            }
        }


        public Coroutine FadeIn(float fadeTime = 0.5f)
        {
            if (fadeCoroutine != null)
            {
                StopCoroutine(fadeCoroutine);
                fadeCoroutine = null;
            }

            return StartCoroutine(FadeCoroutine(0f, 1f, fadeTime));
        }


        public Coroutine FadeOut(float fadeTime = 1f)
        {
            if (fadeCoroutine != null)
            {
                StopCoroutine(fadeCoroutine);
                fadeCoroutine = null;
            }

            return StartCoroutine(FadeCoroutine(1f, 0f, fadeTime));
        }


        private IEnumerator AnimationCoroutine()
        {
            if (isTalking && isOneOffAnimationPlaying == false)
            {
                // Talking anim loops always
                while (true)
                {
                    for (int i = 0; i < currentEmotionData.framesTalking.Count; ++i)
                    {
                        currentFrame = i;
                        int frameDelay = ShowCharacterFrame(currentFrame, true);
                        yield return new WaitForSeconds(frameDelay / 60f);
                    }
                }
            }
            else
            {
                // Animation "loop": If loopable, it will run forever according to anim data. If not, will end after playing through its entirety
                while (currentFrame < currentEmotionData.frames.Count)
                {
                    int frameDelay = ShowCharacterFrame(currentFrame, false);
                    yield return new WaitForSeconds(frameDelay / 60f);

                    // Increment frame
                    ++currentFrame;

                    // If we're supposed to loop and at the end, jump back to the loopIndex
                    if (currentEmotionData.loop && currentFrame == currentEmotionData.frames.Count)
                    {
                        currentFrame = currentEmotionData.loopIndex;
                    }
                }
            }
            
            animCoroutine = null;
            if (isOneOffAnimationPlaying)
            {
                isOneOffAnimationPlaying = false;
                UpdateEmotion();
                animCoroutine = StartCoroutine(AnimationCoroutine());
            }
            // yield break;
        }


        private IEnumerator FadeCoroutine(float from, float to, float fadeTime)
        {
            character.spriteRenderer.color = new Color(1f, 1f, 1f, from);

            float t = 0f;
            while (t < fadeTime)
            {
                float currentAlpha = Mathf.Lerp(from, to, t / fadeTime);

                character.spriteRenderer.color = new Color(1f, 1f, 1f, currentAlpha);

                yield return new WaitForSeconds(0f);
                t += Time.deltaTime;
            }

            character.spriteRenderer.color = new Color(1f, 1f, 1f, to);
        }


        public void UpdateEmotion()
        {
            currentEmotionData = AssetHandler.GetCharacterEmotion(StoryManager.Instance.currentStoryPath, character.id, character.emotion);
        }


        /// <summary>
        /// Play a one-time animation, then go back to showing whatever last animation was playing
        /// </summary>
        public Coroutine PlayAnimation(string animName)
        {
            if (animCoroutine != null)
            {
                StopCoroutine(animCoroutine);
                animCoroutine = null;
            }
            currentEmotionData = AssetHandler.GetCharacterEmotion(StoryManager.Instance.currentStoryPath, character.id, animName);
            isOneOffAnimationPlaying = true;
            animCoroutine = StartCoroutine(AnimationCoroutine());
            return animCoroutine;
        }


        /// <summary>
        /// Shows the Sprite corresponding to this character's current emotion on the attached SpriteRenderer,
        /// returning the amount in frames to wait until the next frame should be shown.
        /// </summary>
        int ShowCharacterFrame(int frame, bool isTalking)
        {
            (int, int) frameData;
            if (isTalking)
            {
                frameData = currentEmotionData.framesTalking[frame];
            }
            else
            {
                frameData = currentEmotionData.frames[frame];
            }
            character.spriteRenderer.sprite = currentEmotionData.emotionFrames[frameData.Item1];
            return frameData.Item2;
        }


        public IEnumerator FadeoutCommandCoroutine()
        {
            yield return FadeCoroutine(1f, 0f, 1f);
        }
    }
}
