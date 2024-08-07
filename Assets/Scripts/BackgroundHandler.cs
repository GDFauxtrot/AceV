using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace AceV
{
    public class BackgroundHandler : MonoBehaviour
    {
        public SpriteRenderer backgroundRenderer;


        public void SetBackground(Sprite newBackground)
        {
            backgroundRenderer.sprite = newBackground;
        }
    }
}
