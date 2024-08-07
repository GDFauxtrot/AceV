using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;


public class DialogueOptionRead : MonoBehaviour
{
    public Image readImage;

    public void SetRead(bool read)
    {
        readImage.enabled = read;
    }
}