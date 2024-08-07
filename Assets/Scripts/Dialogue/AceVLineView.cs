using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Yarn.Unity;
using TMPro;
using Febucci.UI;
using UnityEngine.SearchService;
using System.IO;


namespace AceV
{
    public class AceVLineView : DialogueViewBase
    {
        [SerializeField]
        CanvasGroup canvasGroup;

        /// <summary>
        /// The <see cref="TextMeshProUGUI"/> object that displays the text of
        /// dialogue lines.
        /// </summary>
        [SerializeField]
        internal TextMeshProUGUI lineText = null;

        /// <summary>
        /// The <see cref="TextMeshProUGUI"/> object that displays the character
        /// names found in dialogue lines.
        /// </summary>
        /// <remarks>
        /// If the <see cref="LineView"/> receives a line that does not contain
        /// a character name, this object will be left blank.
        /// </remarks>
        [SerializeField]
        internal TextMeshProUGUI characterNameText = null;

        /// <summary>
        /// The gameobject that holds the <see cref="characterNameText"/> textfield.
        /// </summary>
        /// <remarks>
        /// This is needed in situations where the character name is contained within an entirely different game object.
        /// Most of the time this will just be the same gameobject as <see cref="characterNameText"/>.
        /// </remarks>
        [SerializeField]
        internal GameObject characterNameContainer = null;

        /// <summary>
        /// The current <see cref="LocalizedLine"/> that this line view is
        /// displaying.
        /// </summary>
        LocalizedLine currentLine = null;

        [SerializeField]
        TextAnimator_TMP lineTextAnimator;

        [SerializeField]
        public TypewriterByCharacter lineTextTypewriter;

        /// <summary>
        /// Controls whether this Line View will wait for user input before
        /// indicating that it has finished presenting a line.
        /// </summary>
        /// <remarks>
        /// <para>
        /// If this value is true, the Line View will not report that it has
        /// finished presenting its lines. Instead, it will wait until the <see
        /// cref="UserRequestedViewAdvancement"/> method is called.
        /// </para>
        /// <para style="note"><para>The <see cref="DialogueRunner"/> will not
        /// proceed to the next piece of content (e.g. the next line, or the
        /// next options) until all Dialogue Views have reported that they have
        /// finished presenting their lines. If a <see cref="LineView"/> doesn't
        /// report that it's finished until it receives input, the <see
        /// cref="DialogueRunner"/> will end up pausing.</para>
        /// <para>
        /// This is useful for games in which you want the player to be able to
        /// read lines of dialogue at their own pace, and give them control over
        /// when to advance to the next line.</para></para>
        /// </remarks>
        [SerializeField]
        internal bool autoAdvance = false;

        /// <summary>
        /// The game object that represents an on-screen button that the user
        /// can click to continue to the next piece of dialogue.
        /// </summary>
        /// <remarks>
        /// <para>This game object will be made inactive when a line begins
        /// appearing, and active when the line has finished appearing.</para>
        /// <para>
        /// This field will generally refer to an object that has a <see
        /// cref="Button"/> component on it that, when clicked, calls <see
        /// cref="OnContinueClicked"/>. However, if your game requires specific
        /// UI needs, you can provide any object you need.</para>
        /// </remarks>
        /// <seealso cref="autoAdvance"/>
        [SerializeField]
        internal GameObject mouseAdvanceCatcher = null;

        [SerializeField]
        internal DialogueArrowAnimator arrowAnimator;

        private bool onTextShowedCallbackAdded;
        private Coroutine onTextShowedCoroutine;


        private void Awake()
        {
            canvasGroup.alpha = 0;
        }


        private void Reset()
        {
            canvasGroup = GetComponentInParent<CanvasGroup>();
        }


        // RunLine receives a localized line, and is in charge of displaying it to
        // the user. When the view is done with the line, it should call
        // onDialogueLineFinished.
        //
        // Unless the line gets interrupted, the Dialogue Runner will wait until all
        // views have called their onDialogueLineFinished, before telling them to
        // dismiss the line and proceeding on to the next one. This means that if
        // you want to keep a line on screen for a while, simply don't call
        // onDialogueLineFinished until you're ready.
        public override void RunLine(LocalizedLine dialogueLine, Action onDialogueLineFinished)
        {
            // We shouldn't do anything if we're not active.
            if (gameObject.activeInHierarchy == false)
            {
                canvasGroup.alpha = 1f;

                // This line view isn't active; it should immediately report that
                // it's finished presenting.
                onDialogueLineFinished();
                return;
            }

            // Check dialogue if character name is present. Show/hide appropriately
            if (StoryManager.Instance.GetPerformedContinueOnNextLine() == false)
            {
                characterNameContainer.SetActive(!string.IsNullOrEmpty(dialogueLine.CharacterName));
            }

            // Run through dialogue line!
            StartCoroutine(RunTagsAndSetText(dialogueLine, onDialogueLineFinished));
        }


        private IEnumerator RunTagsAndSetText(LocalizedLine dialogueLine, Action onDialogueLineFinished)
        {
            if (dialogueLine.Metadata != null)
            {
                foreach (string tag in dialogueLine.Metadata)
                {
                    float timeToWait = TagManager.HandleTag(tag);
                    yield return new WaitForSeconds(timeToWait);
                }
            }

            // Turn on canvas group AFTER processing tags (ie. fade in)
            canvasGroup.alpha = 1;

            arrowAnimator.visible = false;

            // Animate character talking (only talk if character is on screen!)
            StoryManager.Instance.ShowCharacterByName(dialogueLine.CharacterName);
            bool nameMatches =
                StoryManager.Instance.characterOnScreen != null && 
                StoryManager.Instance.characterOnScreen.fullName == dialogueLine.CharacterName;
            bool nameValidOnContinueLine =
                StoryManager.Instance.characterOnScreen != null &&
                StoryManager.Instance.GetPerformedContinueOnNextLine() &&
                (string.IsNullOrEmpty(dialogueLine.CharacterName) || nameMatches);
            if (nameMatches || nameValidOnContinueLine)
            {
                StoryManager.Instance.SetCurrentCharacterTalking(true);
            }

            currentLine = dialogueLine;

            if (StoryManager.Instance.GetPerformedContinueOnNextLine())
            {
                string appendText = dialogueLine.TextWithoutCharacterName.Text;
                // TODO what if we don't want a space?
                if (!appendText.StartsWith(' '))
                {
                    appendText = " " + appendText;
                }
                lineTextTypewriter.AppendText(appendText);
            }
            else
            {
                characterNameText.text = dialogueLine.CharacterName;
                lineTextTypewriter.ShowText(dialogueLine.TextWithoutCharacterName.Text);
            }
            // We only want to add this callback once!
            if (!onTextShowedCallbackAdded)
            {
                lineTextTypewriter.onTextShowed.AddListener(() =>
                {
                    if (onTextShowedCoroutine != null)
                    {
                        StopCoroutine(onTextShowedCoroutine);
                        onTextShowedCoroutine = null;
                    }
                    onTextShowedCoroutine = StartCoroutine(OnTextShowedCoroutine(onDialogueLineFinished));
                });
            }
        }

        private IEnumerator OnTextShowedCoroutine(Action onDialogueLineFinished)
        {
            onTextShowedCallbackAdded = true;
            StoryManager.Instance.SetCurrentCharacterTalking(false);

            // Delay line run if we're continuing a line from #continue in Yarn
            if (StoryManager.Instance.GetContinueOnNextLine())
            {
                yield return new WaitForSeconds(StoryManager.Instance.continueLineDelay);
            }

            OnTextShowedCoroutineFinished(onDialogueLineFinished);

            onTextShowedCoroutine = null;
        }


        private void OnTextShowedCoroutineFinished(Action onDialogueLineFinished)
        {
            arrowAnimator.visible = true;

            bool doAutoAdvance = autoAdvance || StoryManager.Instance.GetContinueOnNextLine();

            // Set these before callback invoke - the next line runs basically immediately
            StoryManager.Instance.SetPerformedContinueOnNextLine(StoryManager.Instance.GetContinueOnNextLine());
            StoryManager.Instance.SetContinueOnNextLine(false);

            if (doAutoAdvance)
            {
                onDialogueLineFinished?.Invoke();
            }

            StoryManager.Instance.SetIgnoreCharacterChanges(false);
        }


        // InterruptLine is called when the dialogue runner indicates that the
        // line's presentation should be interrupted. This is a 'hurry up' signal -
        // the view should finish whatever presentation it needs to do as quickly as
        // possible.
        public override void InterruptLine(LocalizedLine dialogueLine, Action onDialogueLineFinished)
        {
            canvasGroup.alpha = 1;
            currentLine = dialogueLine;

            if (gameObject.activeInHierarchy == false)
            {
                // This line view isn't active; it should immediately report that
                // it's finished presenting.
                onDialogueLineFinished();
                return;
            }

            lineTextTypewriter.SkipTypewriter();
            StoryManager.Instance.SetCurrentCharacterTalking(false);
            arrowAnimator.visible = true;

            // If "continue" applies to this line, make sure we quickly get through it!
            if (StoryManager.Instance.GetContinueOnNextLine() == true)
            {
                StoryManager.Instance.SetContinueLineDelay(0f);
            }
            if (onTextShowedCoroutine != null)
            {
                StopCoroutine(onTextShowedCoroutine);
                onTextShowedCoroutine = null;
                OnTextShowedCoroutineFinished(onDialogueLineFinished);
            }

            // Indicate that we've finished presenting the line.
            onDialogueLineFinished();
        }


        // DismissLine is called when the dialogue runner has instructed us to get
        // rid of the line. This is our view's opportunity to do whatever animations
        // we need to to get rid of the line. When we're done, we call
        // onDismissalComplete. When all line views have called their
        // onDismissalComplete, the dialogue runner moves on to the next line.
        public override void DismissLine(Action onDismissalComplete)
        {
            // If we're "continuing" this line (#continue in Yarn), we want to completely skip this function
            // and not dismiss it at all
            if (StoryManager.Instance.GetPerformedContinueOnNextLine())
            {
                onDismissalComplete?.Invoke();
                return;
            }

            currentLine = null;
            canvasGroup.alpha = 0;
            
            if (lineTextTypewriter.isShowingText && StoryManager.Instance.GetPerformedContinueOnNextLine() == false)
            {
                lineTextTypewriter.StopShowingText();
                characterNameText.text = "";
                lineTextTypewriter.ShowText("");
                StoryManager.Instance.SetCurrentCharacterTalking(false);
            }

            onDismissalComplete?.Invoke();
        }


        /// <inheritdoc />
        /// <remarks>
        /// If a line is still being shown dismisses it.
        /// </remarks>
        public override void DialogueComplete()
        {
            // do we still have a line lying around?
            if (currentLine != null)
            {
                currentLine = null;
                DismissLine(null);
            }
        }


        // UserRequestedViewAdvancement is called by other parts of your game to
        // indicate that the user wants to proceed to the 'next' step of seeing the
        // line. What 'next' means is up to your view - in this view, it means to
        // either skip the current animation, or if no animation is happening,
        // interrupt the line.
        public override void UserRequestedViewAdvancement()
        {
            // We received a request to advance the view. If we're in the middle of
            // an animation, skip to the end of it. If we're not current in an
            // animation, interrupt the line so we can skip to the next one.

            // we have no line, so the user just mashed randomly
            if (currentLine == null)
            {
                return;
            }

            if (lineTextTypewriter.isShowingText)
            {
                lineTextTypewriter.SkipTypewriter();
                StoryManager.Instance.SetCurrentCharacterTalking(false);
                arrowAnimator.visible = true;
            }
            else
            {
                // No animation is now running. Signal that we want to
                // interrupt the line instead.
                requestInterrupt?.Invoke();
            }
        }


        /// <summary>
        /// Called when the <see cref="continueButton"/> is clicked.
        /// </summary>
        public void OnContinueClicked()
        {
            // When the Continue button is clicked, we'll do the same thing as
            // if we'd received a signal from any other part of the game (for
            // example, if a DialogueAdvanceInput had signalled us.)
            UserRequestedViewAdvancement();
        }
    }
}
