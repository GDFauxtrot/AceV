using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Yarn.Unity;
using TMPro;
using DG.Tweening;


namespace AceV
{
    public class AceVDialogueOptionsView : DialogueViewBase
    {
        [SerializeField]
        CanvasGroup canvasGroup;

        [SerializeField]
        OptionView optionPrefab;

        // A cached pool of OptionView objects so that we can reuse them
        List<OptionView> optionViews = new List<OptionView>();

        // The method we should call when an option has been selected.
        Action<int> OnOptionSelected;


        public void Start()
        {
            canvasGroup.alpha = 0;
        }


        public override void RunOptions(DialogueOption[] dialogueOptions, Action<int> onOptionSelected)
        {
            // If we don't already have enough option views, create more
            while (dialogueOptions.Length > optionViews.Count)
            {
                var optionView = CreateNewOptionView();
                optionView.gameObject.SetActive(false);
            }

            // Set up all of the option views
            int optionViewsCreated = 0;

            for (int i = 0; i < dialogueOptions.Length; i++)
            {
                var optionView = optionViews[i];
                var option = dialogueOptions[i];

                if (option.IsAvailable == false)
                {
                    // Don't show this option.
                    continue;
                }

                optionView.gameObject.SetActive(true);

                // optionView.palette = this.palette;
                optionView.Option = option;

                // The first available option is selected by default
                if (optionViewsCreated == 0)
                {
                    optionView.Select();
                }

                optionViewsCreated += 1;
            }

            // Note the delegate to call when an option is selected
            OnOptionSelected = onOptionSelected;

            // sometimes (not always) the TMP layout in conjunction with the
            // content size fitters doesn't update the rect transform
            // until the next frame, and you get a weird pop as it resizes
            // just forcing this to happen now instead of then
            Relayout();

            // Fade it all in
            canvasGroup.alpha = 1f;
            UIManager.Instance.SetUIInteractable(false);

            foreach (var optionView in optionViews)
            {
                if (optionView.gameObject.activeInHierarchy)
                {
                    optionView.gameObject.GetComponent<DialogueOptionRead>().SetRead(
                        StoryManager.Instance.GetDialogueOptionRead(optionView.Option.DialogueOptionID)
                        );
                    optionView.GetComponent<RectTransform>().pivot = new Vector2(0f, 0.5f);
                    optionView.transform.localScale = new Vector3(0f, 1f, 1f);
                    optionView.transform.DOScaleX(1f, 0.5f).SetEase(Ease.OutCubic);
                    Utils.RunFunctionDelayed(0.5f, () => {
                        optionView.GetComponent<RectTransform>().pivot = new Vector2(0.5f, 0.5f);
                    });
                }
            }
            Utils.RunFunctionDelayed(0.5f, () => {
                UIManager.Instance.SetUIInteractable(true);
            });

            /// <summary>
            /// Creates and configures a new <see cref="OptionView"/>, and adds
            /// it to <see cref="optionViews"/>.
            /// </summary>
            OptionView CreateNewOptionView()
            {
                var optionView = Instantiate(optionPrefab);
                optionView.transform.SetParent(transform, false);
                optionView.transform.SetAsLastSibling();

                optionView.OnOptionSelected = OptionViewWasSelected;
                optionViews.Add(optionView);

                return optionView;
            }

            /// <summary>
            /// Called by <see cref="OptionView"/> objects.
            /// </summary>
            void OptionViewWasSelected(DialogueOption option)
            {
                StoryManager.Instance.SetDialogueOptionRead(option.DialogueOptionID);
                StartCoroutine(SelectOptionsView(option));
            }
        }


        private IEnumerator SelectOptionsView(DialogueOption option)
        {
            UIManager.Instance.SetUIInteractable(false);

            List<OptionView> activeOptionViews = new List<OptionView>();
            foreach (var optionView in optionViews)
            {
                if (optionView.gameObject.activeInHierarchy)
                {
                    activeOptionViews.Add(optionView);
                }
            }

            GameObject selectedOption = null;
            foreach (var optionView in activeOptionViews)
            {
                if (optionView.Option == option)
                {
                    selectedOption = optionView.gameObject;
                }
            }

            CanvasGroup selectedCanvas = selectedOption.GetComponent<CanvasGroup>();
            selectedCanvas.alpha = 1f;

            // Flash animation (TODO is this the best way to do this???)
            {
                Utils.RunFunctionDelayed(0.1f, () => { selectedCanvas.alpha = 0f; });
                Utils.RunFunctionDelayed(0.2f, () => { selectedCanvas.alpha = 1f; });
                Utils.RunFunctionDelayed(0.3f, () => { selectedCanvas.alpha = 0f; });
                Utils.RunFunctionDelayed(0.4f, () => { selectedCanvas.alpha = 1f; });
                Utils.RunFunctionDelayed(0.5f, () => { selectedCanvas.alpha = 0f; });
                Utils.RunFunctionDelayed(0.6f, () => { selectedCanvas.alpha = 1f; });
                yield return new WaitForSeconds(0.7f);
            }

            for (int i = 0; i < activeOptionViews.Count; ++i)
            {
                OptionView optionView = activeOptionViews[i];
                optionView.GetComponent<RectTransform>().pivot = new Vector2(0f, 0.5f);
                optionView.transform.localScale = new Vector3(1f, 1f, 1f);
                optionView.transform.DOScaleX(0f, 0.5f).SetEase(Ease.InCubic);
                Utils.RunFunctionDelayed(0.5f, () => { optionView.GetComponent<RectTransform>().pivot = new Vector2(0.5f, 0.5f); });
                yield return new WaitForSeconds(i == activeOptionViews.Count-1 ? 0.5f : 0.25f);
            }
            UIManager.Instance.SetUIInteractable(true);
            GameManager.Instance.PushState(PlayerActionState.DIALOGUE);
            OnOptionSelected(option.DialogueOptionID);

            // Disable everything at the end
            canvasGroup.alpha = 0f;
            foreach (var optionView in activeOptionViews)
            {
                optionView.gameObject.SetActive(false);
            }
        }


        /// <inheritdoc />
        /// <remarks>
        /// If options are still shown dismisses them.
        /// </remarks>
        public override void DialogueComplete()
        {   
            // Don't run if the canvas group isn't visible (happens in some cases)
            if (canvasGroup.alpha <= 0f)
            {
                return;
            }

            CloseDialogueOptions();
        }


        public void OnEnable()
        {
            Relayout();
        }


        private void Relayout()
        {
            // Force re-layout
            var layouts = GetComponentsInChildren<LayoutGroup>();

            // Perform the first pass of re-layout. This will update the inner horizontal group's sizing, based on the text size.
            foreach (var layout in layouts)
            {
                LayoutRebuilder.ForceRebuildLayoutImmediate(layout.GetComponent<RectTransform>());
            }
            
            // Perform the second pass of re-layout. This will update the outer vertical group's positioning of the individual elements.
            foreach (var layout in layouts)
            {
                LayoutRebuilder.ForceRebuildLayoutImmediate(layout.GetComponent<RectTransform>());
            }
        }


        public void CloseDialogueOptions()
        {
            UIManager.Instance.SetUIInteractable(false);

            OnOptionSelected = null;

            // Close each option view via animation
            List<OptionView> activeOptionViews = new List<OptionView>();
            foreach (var optionView in optionViews)
            {
                if (optionView.gameObject.activeInHierarchy)
                {
                    activeOptionViews.Add(optionView);
                }
            }
            float closeTime = 0.5f;
            foreach (OptionView optionView in activeOptionViews)
            {
                optionView.GetComponent<RectTransform>().pivot = new Vector2(0f, 0.5f);
                optionView.transform.localScale = new Vector3(1f, 1f, 1f);
                optionView.transform.DOScaleX(0f, closeTime).SetEase(Ease.InCubic);
                Utils.RunFunctionDelayed(closeTime, () => {
                    optionView.GetComponent<RectTransform>().pivot = new Vector2(0.5f, 0.5f);
                    optionView.gameObject.SetActive(false);
                });
            }
            Utils.RunFunctionDelayed(closeTime, () => {
                canvasGroup.alpha = 0f;
                UIManager.Instance.SetBackButtonAnimationFinished();
                UIManager.Instance.SetUIInteractable(true);
            });
        }
    }
}
