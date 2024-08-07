using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using UnityEngine;


namespace AceV
{
    [Serializable]
    public class AcePOIData
    {
        public string id;
        public bool visible;
        public AcePOI poiObject;

        public string room;
        public string onInteract;
        public List<Rect> bounds;
    }

    public class AcePOI : AceEntityBase
    {
        /// <summary>
        /// The internal data of this POI.
        /// </summary>
        public AcePOIData data;

        /// <summary>
        /// The unique ID of this POI.
        /// </summary>
        public string id { get { return data.id; } }

        /// <summary>
        /// The room ID this POI belongs to.
        /// </summary>
        public string room { get { return data.room; } }

        /// <summary>
        /// The Yarn Spinner node that is activated when this POI is interacted with.
        /// </summary>
        public string onInteract { get { return data.onInteract; } }

        /// <summary>
        /// The collection of collider rect data for this POI.
        /// </summary>
        public List<Rect> bounds { get { return data.bounds; } }

        /// <summary>
        /// The collection of colliders generated for this POI.
        /// </summary>
        public List<BoxCollider> boundsColliders = new List<BoxCollider>();

        public void Initialize(string poiID, string poiRoom, string poiInteract, List<Rect> poiBounds)
        {
            gameObject.name = poiID;

            boundsColliders.Clear();
            // Initialize BoxColliders for each Rect passed in for the POI bounds
            foreach (Rect rect in poiBounds)
            {
                BoxCollider rectBox = gameObject.AddComponent<BoxCollider>();
                rectBox.center = transform.InverseTransformPoint(new Vector3(rect.x, rect.y, 0f));
                rectBox.size = new Vector3(rect.width, rect.height, 0f);
                boundsColliders.Add(rectBox);
            }

            data = new AcePOIData()
            {
                id = poiID,
                visible = true,
                poiObject = this,

                room = poiRoom,
                onInteract = poiInteract,
                bounds = poiBounds
            };
        }


        public override void Interact()
        {
            // Switch state to DIALOGUE and play node dialogue
            GameManager.Instance.PushState(PlayerActionState.DIALOGUE);
            StoryManager.Instance.PlayNode(onInteract);
        }
    }
}
