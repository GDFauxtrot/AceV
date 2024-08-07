using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;


namespace AceV
{
    public class MouseCatcher : Button
    {
        public UnityEvent onClickDown;

        public override void OnPointerDown(PointerEventData eventData)
        {
            base.OnPointerDown(eventData);

            onClickDown.Invoke();
        }
    }
}
