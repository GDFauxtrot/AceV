using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class AceVButtonHandler : MonoBehaviour
{
    public Selectable attachedButton;
    public Color enabledColor;
    public Color disabledColor;
    public bool isDisabledAppearanceForcedEnabled;

    void Awake()
    {
        // In case the inspector isn't filled in
        if (!attachedButton)
        {
            attachedButton = GetComponent<Button>();

            // Last resort?
            if (!attachedButton)
            {
                attachedButton = GetComponentInChildren<Button>();
            }
        }
    }

    void Update()
    {
        if (isDisabledAppearanceForcedEnabled && attachedButton.IsInteractable() == false)
        {
            ColorBlock buttonColors = attachedButton.colors;
            buttonColors.disabledColor = enabledColor;
            attachedButton.colors = buttonColors;
        }
    }


    /// <summary>
    /// Set the button's "interactable" flag. Has an additional argument to force
    /// the appearance of the button to look enabled but not be interactable.
    /// </summary>
    public void SetButtonInteractable(bool interactable)
    {
        attachedButton.interactable = interactable;

        ColorBlock buttonColors = attachedButton.colors;

        buttonColors.disabledColor = 
            (isDisabledAppearanceForcedEnabled && !interactable) ?
            enabledColor : disabledColor;

        attachedButton.colors = buttonColors;
    }
}
