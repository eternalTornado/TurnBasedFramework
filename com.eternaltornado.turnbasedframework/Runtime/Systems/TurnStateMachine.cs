using System;
using UnityEngine;

namespace TurnBasedFramework.TurnManagement
{
    /// <summary>
    /// State Machine managing turn system states
    /// </summary>
    public class TurnStateMachine : MonoBehaviour
    {
        #region Events
        public event Action<TurnState, TurnState> OnStateChanged;
        #endregion

        #region States
        private TurnState currentState;
        private ITurnState[] states;
        #endregion

        #region Properties
        public TurnState CurrentState => currentState;
        public bool CanTransition { get; set; } = true;
        #endregion

        #region Unity Lifecycle
        private void Awake()
        {
            InitializeStates();
        }

        private void Update()
        {
            states[(int)currentState]?.Update();
        }

        private void FixedUpdate()
        {
            states[(int)currentState]?.FixedUpdate();
        }
        #endregion

        #region Initialization
        private void InitializeStates()
        {
            states = new ITurnState[System.Enum.GetValues(typeof(TurnState)).Length];
            
            states[(int)TurnState.Idle] = new IdleState(this);
            states[(int)TurnState.WaitingForInput] = new WaitingForInputState(this);
            states[(int)TurnState.PlayerTurn] = new PlayerTurnState(this);
            states[(int)TurnState.EnemyTurn] = new EnemyTurnState(this);
            states[(int)TurnState.ExecutingAction] = new ExecutingActionState(this);
            states[(int)TurnState.TurnTransition] = new TurnTransitionState(this);
            states[(int)TurnState.Victory] = new VictoryState(this);
            states[(int)TurnState.Defeat] = new DefeatState(this);

            currentState = TurnState.Idle;
        }
        #endregion

        #region State Management
        /// <summary>
        /// Transition to a new state
        /// </summary>
        public void ChangeState(TurnState newState)
        {
            if (!CanTransition)
            {
                Debug.LogWarning($"Cannot transition to {newState} - transitions are locked");
                return;
            }

            if (currentState == newState)
            {
                Debug.LogWarning($"Already in {newState} state");
                return;
            }

            // Validate transition
            if (!IsValidTransition(currentState, newState))
            {
                Debug.LogError($"Invalid state transition from {currentState} to {newState}");
                return;
            }

            TurnState previousState = currentState;
            
            // Exit current state
            states[(int)currentState]?.Exit();
            
            // Change state
            currentState = newState;
            
            // Enter new state
            states[(int)currentState]?.Enter();
            
            Debug.Log($"State changed: {previousState} -> {currentState}");
            OnStateChanged?.Invoke(previousState, currentState);
        }

        /// <summary>
        /// Check if state transition is valid
        /// </summary>
        private bool IsValidTransition(TurnState from, TurnState to)
        {
            // Define valid transitions
            switch (from)
            {
                case TurnState.Idle:
                    return to == TurnState.PlayerTurn || to == TurnState.EnemyTurn;

                case TurnState.PlayerTurn:
                    return to == TurnState.WaitingForInput || 
                           to == TurnState.ExecutingAction || 
                           to == TurnState.TurnTransition ||
                           to == TurnState.Victory ||
                           to == TurnState.Defeat;

                case TurnState.WaitingForInput:
                    return to == TurnState.ExecutingAction || 
                           to == TurnState.TurnTransition ||
                           to == TurnState.PlayerTurn;

                case TurnState.EnemyTurn:
                    return to == TurnState.ExecutingAction || 
                           to == TurnState.TurnTransition ||
                           to == TurnState.Victory ||
                           to == TurnState.Defeat;

                case TurnState.ExecutingAction:
                    return to == TurnState.PlayerTurn || 
                           to == TurnState.EnemyTurn || 
                           to == TurnState.WaitingForInput ||
                           to == TurnState.TurnTransition ||
                           to == TurnState.Victory ||
                           to == TurnState.Defeat;

                case TurnState.TurnTransition:
                    return to == TurnState.PlayerTurn || 
                           to == TurnState.EnemyTurn ||
                           to == TurnState.Victory ||
                           to == TurnState.Defeat;

                case TurnState.Victory:
                case TurnState.Defeat:
                    return to == TurnState.Idle; // Can restart

                default:
                    return false;
            }
        }

        /// <summary>
        /// Force state change without validation (use carefully!)
        /// </summary>
        public void ForceChangeState(TurnState newState)
        {
            bool wasLocked = !CanTransition;
            CanTransition = true;
            ChangeState(newState);
            CanTransition = !wasLocked;
        }
        #endregion

        #region Query Methods
        public bool IsInState(TurnState state) => currentState == state;
        
        public bool IsInAnyState(params TurnState[] checkStates)
        {
            foreach (var state in checkStates)
            {
                if (currentState == state) return true;
            }
            return false;
        }
        #endregion
    }

    #region State Enum
    public enum TurnState
    {
        Idle,               // No active turn
        WaitingForInput,    // Waiting for player input
        PlayerTurn,         // Player's turn active
        EnemyTurn,          // Enemy's turn active
        ExecutingAction,    // Currently executing an action
        TurnTransition,     // Transitioning between turns
        Victory,            // Combat won
        Defeat              // Combat lost
    }
    #endregion

    #region State Interface
    public interface ITurnState
    {
        void Enter();
        void Update();
        void FixedUpdate();
        void Exit();
    }
    #endregion

    #region Base State Class
    public abstract class BaseTurnState : ITurnState
    {
        protected TurnStateMachine stateMachine;

        public BaseTurnState(TurnStateMachine stateMachine)
        {
            this.stateMachine = stateMachine;
        }

        public virtual void Enter() { }
        public virtual void Update() { }
        public virtual void FixedUpdate() { }
        public virtual void Exit() { }
    }
    #endregion

    #region Concrete State Implementations
    
    // Idle State - No active combat
    public class IdleState : BaseTurnState
    {
        public IdleState(TurnStateMachine stateMachine) : base(stateMachine) { }

        public override void Enter()
        {
            Debug.Log("Entering Idle State");
        }

        public override void Update()
        {
            // Wait for combat to start
        }
    }

    // Waiting For Input State - Player needs to choose action
    public class WaitingForInputState : BaseTurnState
    {
        private float waitTimer = 0f;
        private const float MAX_WAIT_TIME = 60f; // Auto-skip after 60 seconds

        public WaitingForInputState(TurnStateMachine stateMachine) : base(stateMachine) { }

        public override void Enter()
        {
            Debug.Log("Waiting for player input...");
            waitTimer = 0f;
            
            // Enable UI input
            // Show action menu
        }

        public override void Update()
        {
            waitTimer += Time.deltaTime;

            // Auto-skip turn if player takes too long
            if (waitTimer >= MAX_WAIT_TIME)
            {
                Debug.LogWarning("Player took too long - auto-skipping turn");
                stateMachine.ChangeState(TurnState.TurnTransition);
            }
        }

        public override void Exit()
        {
            // Disable UI input elements
        }
    }

    // Player Turn State
    public class PlayerTurnState : BaseTurnState
    {
        public PlayerTurnState(TurnStateMachine stateMachine) : base(stateMachine) { }

        public override void Enter()
        {
            Debug.Log("Player Turn Started");
            
            // Initialize player turn
            // Enable player controls
            // Show available actions
            
            // Transition to waiting for input
            stateMachine.ChangeState(TurnState.WaitingForInput);
        }

        public override void Update()
        {
            // Update player turn UI
        }

        public override void Exit()
        {
            Debug.Log("Player Turn Ended");
            // Cleanup player turn
        }
    }

    // Enemy Turn State
    public class EnemyTurnState : BaseTurnState
    {
        private float thinkingTime = 0f;
        private const float MIN_THINKING_TIME = 0.5f; // Minimum delay for visual feedback

        public EnemyTurnState(TurnStateMachine stateMachine) : base(stateMachine) { }

        public override void Enter()
        {
            Debug.Log("Enemy Turn Started");
            thinkingTime = 0f;
            
            // Trigger AI behavior tree
            // Calculate enemy action
        }

        public override void Update()
        {
            thinkingTime += Time.deltaTime;

            // Add small delay for visual feedback
            if (thinkingTime >= MIN_THINKING_TIME)
            {
                // AI has decided on action
                stateMachine.ChangeState(TurnState.ExecutingAction);
            }
        }

        public override void Exit()
        {
            Debug.Log("Enemy Turn Ended");
        }
    }

    // Executing Action State
    public class ExecutingActionState : BaseTurnState
    {
        public ExecutingActionState(TurnStateMachine stateMachine) : base(stateMachine) { }

        public override void Enter()
        {
            Debug.Log("Executing Action...");
            
            // Lock input during action execution
            stateMachine.CanTransition = false;
            
            // Execute queued actions
            // Play animations
            // Apply effects
        }

        public override void Update()
        {
            // Wait for action execution to complete
            // This will be controlled by ActionQueueSystem
        }

        public override void Exit()
        {
            Debug.Log("Action Execution Complete");
            stateMachine.CanTransition = true;
        }
    }

    // Turn Transition State
    public class TurnTransitionState : BaseTurnState
    {
        private float transitionTimer = 0f;
        private const float TRANSITION_DURATION = 0.3f;

        public TurnTransitionState(TurnStateMachine stateMachine) : base(stateMachine) { }

        public override void Enter()
        {
            Debug.Log("Transitioning to next turn...");
            transitionTimer = 0f;
            
            // Apply end-of-turn effects
            // Update status effects
            // Check win/lose conditions
        }

        public override void Update()
        {
            transitionTimer += Time.deltaTime;

            if (transitionTimer >= TRANSITION_DURATION)
            {
                // Determine next turn
                // This should be controlled by TurnQueueManager
                // For now, just placeholder
                stateMachine.ChangeState(TurnState.PlayerTurn);
            }
        }

        public override void Exit()
        {
            // Clean up transition
        }
    }

    // Victory State
    public class VictoryState : BaseTurnState
    {
        public VictoryState(TurnStateMachine stateMachine) : base(stateMachine) { }

        public override void Enter()
        {
            Debug.Log("=== VICTORY ===");
            
            // Show victory screen
            // Calculate rewards
            // Play victory animation/sound
        }

        public override void Update()
        {
            // Wait for player to acknowledge victory
        }
    }

    // Defeat State
    public class DefeatState : BaseTurnState
    {
        public DefeatState(TurnStateMachine stateMachine) : base(stateMachine) { }

        public override void Enter()
        {
            Debug.Log("=== DEFEAT ===");
            
            // Show defeat screen
            // Offer retry/quit options
            // Play defeat animation/sound
        }

        public override void Update()
        {
            // Wait for player to choose retry/quit
        }
    }
    #endregion
}