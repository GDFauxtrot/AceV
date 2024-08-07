using System.Collections;
using System.Collections.Generic;
using UnityEngine;


namespace AceV
{
    public class InteractionManager : MonoBehaviour
    {
        public static InteractionManager Instance;


        void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }


        void Update()
        {
            if (Input.GetMouseButtonDown(0) &&
                GameManager.Instance.GetState() == PlayerActionState.ROOM_INVESTIGATE &&
                UIManager.Instance.GetUIInteractable())
            {
                Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
                RaycastHit2D hit = Physics2D.Raycast(ray.origin,ray.direction);
                if (hit) {
                    AceEntityBase aceEntity = hit.collider.gameObject.GetComponent<AceEntityBase>();
                    if (aceEntity)
                    {
                        aceEntity.Interact();
                    }
                }
            }
        }
    }
}
