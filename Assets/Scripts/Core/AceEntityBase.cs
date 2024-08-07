using System.Collections;
using System.Collections.Generic;
using UnityEngine;


namespace AceV
{
    /// The base class for all objects, points of interest, characters, and other interactibles
    // in AceV that have some kind of in-world presence and association with Yarn Spinner.
    public class AceEntityBase : MonoBehaviour
    {
        public virtual void Interact() {}
    }
}
