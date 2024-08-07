using System.Collections;
using System.Collections.Generic;
using UnityEngine;


namespace AceV
{
    public class DialogueArrowAnimator : MonoBehaviour
    {
        public CanvasGroup canvasGroup;

        public bool visible;
        public float speed;
        public float maxMoveDistance;

        private float initialXValue;

        void Start()
        {
            initialXValue = transform.position.x;
        }

        // Update is called once per frame
        void Update()
        {
            if (visible)
            {
                transform.position = new Vector3(
                    initialXValue + Mathf.PingPong(Time.timeSinceLevelLoad * speed, 1f) * maxMoveDistance,
                    transform.position.y, transform.position.z);
            }

            canvasGroup.alpha = visible ? 1f : 0f;
        }
    }
}
