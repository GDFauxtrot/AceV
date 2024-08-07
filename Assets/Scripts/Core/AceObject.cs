using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;


namespace AceV
{
    [Serializable]
    public class AceObjectData
    {
        public string id;
        public bool visible;
        public AceObject aceObject;

        public string room;
        public string onInteract;
        public Vector3 position;
        public float scale;
    }

    public class AceObject : AceEntityBase
    {
        public SpriteRenderer spriteRenderer;
        public PolygonCollider2D spriteCollider;
        
        [Tooltip("Measured in pixels!")]
        public uint objectSelectionPadding;

        /// <summary>
        /// The internal data of this object.
        /// </summary>
        [SerializeField] [ReadOnly]
        public AceObjectData data;

        /// <summary>
        /// The unique ID of this object.
        /// </summary>
        public string id { get { return data.id; } }

        /// <summary>
        /// The room ID this object belongs to.
        /// </summary>
        public string room { get { return data.room; } }

        /// <summary>
        /// The Yarn Spinner node that is activated when this object is interacted with.
        /// </summary>
        public string onInteract { get { return data.onInteract; } }

        public void Initialize(string objID, Sprite objSprite, string objRoom, string objInteract, Vector2 objPosition, float objScale)
        {
            gameObject.name = objID;

            data = new AceObjectData()
            {
                id = objID,
                visible = true,
                aceObject = this,

                room = objRoom,
                onInteract = objInteract,
                position = objPosition,
                scale = objScale
            };

            transform.position = objPosition;
            transform.localScale = Vector3.one * objScale;

            if (objSprite != null)
            {
                spriteRenderer.sprite = objSprite;
                
                // We requested a sprite physics shape to generate on load.
                // Update collider shape to fit the sprite's generated shape
                spriteCollider.enabled = true;
                spriteCollider.pathCount = objSprite.GetPhysicsShapeCount();
                List<Vector2> path = new List<Vector2>();
                for (int i = 0; i < spriteCollider.pathCount; ++i)
                {
                    path.Clear();
                    objSprite.GetPhysicsShape(i, path);
                    spriteCollider.SetPath(i, path);
                }
            }
            else
            {
                // Hide collider since there's no need for one
                spriteCollider.enabled = false;
            }
            
        }


        public override void Interact()
        {
            // Switch state to DIALOGUE and play node dialogue
            StoryManager.Instance.QueueNodeToPlay(onInteract);
            GameManager.Instance.PushState(PlayerActionState.DIALOGUE);
        }
    }
}
