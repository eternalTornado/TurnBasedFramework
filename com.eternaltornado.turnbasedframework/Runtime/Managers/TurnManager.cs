using UnityEngine;

namespace TurnBasedFramework.TurnManagement
{
    /// <summary>
    /// Main manager that integrates all turn system components
    /// This is the main entry point for turn-based combat
    /// </summary>
    public class TurnManager : MonoBehaviour
    {
        #region Singleton
        public static TurnManager Instance { get; private set; }
        #endregion

        #region Components
        [Header("Core Components")]
        [SerializeField] private TurnQueueManager turnQueueManager;
        [SerializeField] private TurnStateMachine turnStateMachine;
        [SerializeField] private ActionQueueSystem actionQueueSystem;
        #endregion

        #region Properties
        public TurnQueueManager TurnQueue => turnQueueManager;
        public TurnStateMachine StateMachine => turnStateMachine;
        public ActionQueueSystem ActionQueue => actionQueueSystem;
        
        public ITurnBasedUnit CurrentUnit => turnQueueManager?.CurrentTurn?.Unit;
        public TurnState CurrentState => turnStateMachine?.CurrentState ?? TurnState.Idle;
        #endregion

        #region Unity Lifecycle
        private void Awake()
        {
            // Singleton setup
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            // Get or create components
            if (turnQueueManager == null)
                turnQueueManager = GetComponent<TurnQueueManager>() ?? gameObject.AddComponent<TurnQueueManager>();
            
            if (turnStateMachine == null)
                turnStateMachine = GetComponent<TurnStateMachine>() ?? gameObject.AddComponent<TurnStateMachine>();
            
            if (actionQueueSystem == null)
                actionQueueSystem = GetComponent<ActionQueueSystem>() ?? gameObject.AddComponent<ActionQueueSystem>();

            SetupEventListeners();
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }
            
            RemoveEventListeners();
        }
        #endregion

        #region Event Setup
        private void SetupEventListeners()
        {
            if (turnQueueManager != null)
            {
                turnQueueManager.OnTurnStarted += HandleTurnStarted;
                turnQueueManager.OnTurnEnded += HandleTurnEnded;
                turnQueueManager.OnRoundCompleted += HandleRoundCompleted;
            }

            if (turnStateMachine != null)
            {
                turnStateMachine.OnStateChanged += HandleStateChanged;
            }

            if (actionQueueSystem != null)
            {
                actionQueueSystem.OnActionCompleted += HandleActionCompleted;
                actionQueueSystem.OnQueueEmpty += HandleQueueEmpty;
            }
        }

        private void RemoveEventListeners()
        {
            if (turnQueueManager != null)
            {
                turnQueueManager.OnTurnStarted -= HandleTurnStarted;
                turnQueueManager.OnTurnEnded -= HandleTurnEnded;
                turnQueueManager.OnRoundCompleted -= HandleRoundCompleted;
            }

            if (turnStateMachine != null)
            {
                turnStateMachine.OnStateChanged -= HandleStateChanged;
            }

            if (actionQueueSystem != null)
            {
                actionQueueSystem.OnActionCompleted -= HandleActionCompleted;
                actionQueueSystem.OnQueueEmpty -= HandleQueueEmpty;
            }
        }
        #endregion

        #region Combat Flow
        /// <summary>
        /// Initialize and start combat
        /// </summary>
        public void StartCombat(System.Collections.Generic.List<ITurnBasedUnit> units)
        {
            Debug.Log("=== COMBAT START ===");
            
            // Initialize turn queue
            turnQueueManager.Initialize(units);
            
            // Start combat
            turnQueueManager.StartCombat();
        }

        /// <summary>
        /// Execute an action for the current unit
        /// </summary>
        public void ExecuteAction(IAction action)
        {
            if (action == null)
            {
                Debug.LogError("Cannot execute null action");
                return;
            }

            // Change to executing state
            turnStateMachine.ChangeState(TurnState.ExecutingAction);
            
            // Queue the action
            actionQueueSystem.EnqueueAction(action);
        }

        /// <summary>
        /// End current turn manually
        /// </summary>
        public void EndTurn()
        {
            turnQueueManager.ForceEndTurn();
        }
        #endregion

        #region Event Handlers
        private void HandleTurnStarted(TurnQueueEntry entry)
        {
            Debug.Log($"Turn started for: {entry.Unit.UnitName}");
            
            // Notify unit
            entry.Unit.OnTurnStart();
            
            // Change state based on unit type
            if (entry.Unit.IsPlayerControlled)
            {
                turnStateMachine.ChangeState(TurnState.PlayerTurn);
            }
            else
            {
                turnStateMachine.ChangeState(TurnState.EnemyTurn);
            }
        }

        private void HandleTurnEnded(TurnQueueEntry entry)
        {
            Debug.Log($"Turn ended for: {entry.Unit.UnitName}");
            
            // Notify unit
            entry.Unit.OnTurnEnd();
            
            // Transition to next turn
            turnStateMachine.ChangeState(TurnState.TurnTransition);
        }

        private void HandleRoundCompleted()
        {
            Debug.Log("=== Round Completed ===");
            
            // Check win/lose conditions
            // Apply round-based effects
        }

        private void HandleStateChanged(TurnState from, TurnState to)
        {
            Debug.Log($"State changed: {from} -> {to}");
            
            // Handle specific state transitions
            if (to == TurnState.TurnTransition)
            {
                // Small delay then start next turn
                Invoke(nameof(StartNextTurnDelayed), 0.5f);
            }
        }

        private void HandleActionCompleted(IAction action)
        {
            Debug.Log($"Action completed: {action.ActionName}");
        }

        private void HandleQueueEmpty()
        {
            Debug.Log("Action queue empty");
            
            // If we're in ExecutingAction state, return to appropriate turn state
            if (turnStateMachine.CurrentState == TurnState.ExecutingAction)
            {
                if (CurrentUnit != null && CurrentUnit.IsPlayerControlled)
                {
                    turnStateMachine.ChangeState(TurnState.WaitingForInput);
                }
                else
                {
                    // End AI turn
                    EndTurn();
                }
            }
        }

        private void StartNextTurnDelayed()
        {
            turnQueueManager.StartNextTurn();
        }
        #endregion
    }
}