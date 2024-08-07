using System.Collections;
using System.Collections.Generic;
using UnityEngine;


namespace AceV
{
    /// <summary>
    /// FSM states for player actions (and what UI is open) while in a room.
    /// Driven entirely by player action and what UI buttons they press.
    /// </summary>
    public enum PlayerActionState {
        NULL, ROOM_OPTIONS, ROOM_TALK, ROOM_INVESTIGATE, ROOM_TRAVEL,
        ITEMS, DIALOGUE, DIALOGUE_CHOICE, DIALOGUE_PRESENT };

    public class GameManager : MonoBehaviour
    {
        public static GameManager Instance;

        private Stack<PlayerActionState> stateStack;


        void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);

            stateStack = new Stack<PlayerActionState>();
        }


        void Update()
        {
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                Application.Quit();
            }
        }


        public void PushState(PlayerActionState newState)
        {
            // Checking this just in case? Probably not needed
            if (newState == PlayerActionState.NULL)
            {
                Debug.LogError("Changing game state to NULL! Something terrible happened");
                return;
            }
            
            // Update state stack values
            if (!stateStack.TryPeek(out PlayerActionState oldState))
            {
                oldState = PlayerActionState.NULL;
            }
            stateStack.Push(newState);

            // Inform UI to transition to the new state from the old state
            UIManager.Instance.MoveToNewState(newState, oldState);
        }


        public void PopState()
        {
            PlayerActionState oldState = stateStack.Pop();
            if (!stateStack.TryPeek(out PlayerActionState newState))
            {
                newState = PlayerActionState.NULL;
            }

            // Inform UI to transition to the new state
            UIManager.Instance.MoveToNewState(newState, oldState);
        }


        // Completely overwrite the state stack with the specified states.
        // Limit its use as much as possible!
        public void ForceState(params PlayerActionState[] states)
        {
            stateStack.Clear();

            for (int i = 0; i < states.Length-1; ++i)
            {
                stateStack.Push(states[i]);
            }

            // Make the last state change a full UI shift
            PushState(states[states.Length-1]);
        }


        public PlayerActionState GetState()
        {
            if (stateStack.Count > 0)
            {
                return stateStack.Peek();
            }
            else
            {
                return PlayerActionState.NULL;
            }
        }
    }
}
