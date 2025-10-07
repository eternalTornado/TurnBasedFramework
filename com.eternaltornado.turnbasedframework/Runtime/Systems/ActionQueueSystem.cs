using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace TurnBasedFramework.TurnManagement
{
    /// <summary>
    /// System for queuing and executing actions in order
    /// Supports both instant and animated actions
    /// </summary>
    public class ActionQueueSystem : MonoBehaviour
    {
        #region Events
        public event Action<IAction> OnActionQueued;
        public event Action<IAction> OnActionStarted;
        public event Action<IAction> OnActionCompleted;
        public event Action<IAction, string> OnActionFailed;
        public event Action OnQueueEmpty;
        public event Action OnQueueCleared;
        #endregion

        #region Configuration
        [Header("Action Queue Settings")]
        [SerializeField] private int maxQueueSize = 50;
        [SerializeField] private bool autoExecuteQueue = true;
        [SerializeField] private float actionDelaySeconds = 0.1f;
        [SerializeField] private bool allowActionCancellation = true;
        #endregion

        #region Private Fields
        private Queue<ActionQueueEntry> actionQueue = new Queue<ActionQueueEntry>();
        private ActionQueueEntry currentAction = null;
        private bool isExecuting = false;
        private bool isPaused = false;
        private Coroutine executionCoroutine = null;
        #endregion

        #region Properties
        public bool IsExecuting => isExecuting;
        public bool IsPaused => isPaused;
        public int QueuedActionCount => actionQueue.Count;
        public IAction CurrentAction => currentAction?.Action;
        public bool IsQueueFull => actionQueue.Count >= maxQueueSize;
        #endregion

        #region Queue Management
        /// <summary>
        /// Add an action to the queue
        /// </summary>
        public bool EnqueueAction(IAction action, int priority = 0)
        {
            if (action == null)
            {
                Debug.LogError("Cannot enqueue null action");
                return false;
            }

            if (IsQueueFull)
            {
                Debug.LogWarning($"Action queue is full ({maxQueueSize} actions). Cannot add more.");
                return false;
            }

            var entry = new ActionQueueEntry(action, priority);
            actionQueue.Enqueue(entry);

            Debug.Log($"Action queued: {action.ActionName} (Priority: {priority})");
            OnActionQueued?.Invoke(action);

            // Auto-start execution if enabled
            if (autoExecuteQueue && !isExecuting && !isPaused)
            {
                StartExecution();
            }

            return true;
        }

        /// <summary>
        /// Add multiple actions at once
        /// </summary>
        public void EnqueueActions(IEnumerable<IAction> actions, int priority = 0)
        {
            foreach (var action in actions)
            {
                EnqueueAction(action, priority);
            }
        }

        /// <summary>
        /// Clear all queued actions (does not stop current action)
        /// </summary>
        public void ClearQueue()
        {
            int clearedCount = actionQueue.Count;
            actionQueue.Clear();

            Debug.Log($"Action queue cleared ({clearedCount} actions removed)");
            OnQueueCleared?.Invoke();
        }

        /// <summary>
        /// Clear queue and stop current action
        /// </summary>
        public void ClearAll()
        {
            ClearQueue();

            if (currentAction != null && allowActionCancellation)
            {
                CancelCurrentAction();
            }
        }
        #endregion

        #region Execution Control
        /// <summary>
        /// Start processing the action queue
        /// </summary>
        public void StartExecution()
        {
            if (isExecuting)
            {
                Debug.LogWarning("Action queue is already executing");
                return;
            }

            if (actionQueue.Count == 0)
            {
                Debug.LogWarning("Cannot start execution - queue is empty");
                return;
            }

            isExecuting = true;
            isPaused = false;

            executionCoroutine = StartCoroutine(ExecuteQueueCoroutine());
        }

        /// <summary>
        /// Pause queue execution
        /// </summary>
        public void PauseExecution()
        {
            if (!isExecuting)
            {
                Debug.LogWarning("Cannot pause - queue is not executing");
                return;
            }

            isPaused = true;
            Debug.Log("Action queue paused");
        }

        /// <summary>
        /// Resume queue execution
        /// </summary>
        public void ResumeExecution()
        {
            if (!isPaused)
            {
                Debug.LogWarning("Cannot resume - queue is not paused");
                return;
            }

            isPaused = false;
            Debug.Log("Action queue resumed");
        }

        /// <summary>
        /// Stop queue execution
        /// </summary>
        public void StopExecution()
        {
            if (!isExecuting) return;

            isExecuting = false;
            isPaused = false;

            if (executionCoroutine != null)
            {
                StopCoroutine(executionCoroutine);
                executionCoroutine = null;
            }

            Debug.Log("Action queue execution stopped");
        }

        /// <summary>
        /// Cancel current action
        /// </summary>
        public void CancelCurrentAction()
        {
            if (currentAction == null)
            {
                Debug.LogWarning("No current action to cancel");
                return;
            }

            if (!allowActionCancellation)
            {
                Debug.LogWarning("Action cancellation is disabled");
                return;
            }

            Debug.Log($"Cancelling action: {currentAction.Action.ActionName}");

            currentAction.Action.Cancel();
            currentAction.Status = ActionStatus.Cancelled;
            currentAction = null;
        }
        #endregion

        #region Execution Logic
        /// <summary>
        /// Main coroutine for executing queued actions
        /// </summary>
        private IEnumerator ExecuteQueueCoroutine()
        {
            Debug.Log("=== Action Queue Execution Started ===");

            while (actionQueue.Count > 0 && isExecuting)
            {
                // Wait if paused
                while (isPaused)
                {
                    yield return null;
                }

                // Get next action
                currentAction = actionQueue.Dequeue();
                currentAction.Status = ActionStatus.Executing;
                currentAction.StartTime = Time.time;

                Debug.Log($"Executing action: {currentAction.Action.ActionName}");
                OnActionStarted?.Invoke(currentAction.Action);

                // Validate action
                if (!currentAction.Action.CanExecute())
                {
                    string failReason = "Action cannot be executed (validation failed)";
                    Debug.LogWarning($"{currentAction.Action.ActionName}: {failReason}");

                    currentAction.Status = ActionStatus.Failed;
                    OnActionFailed?.Invoke(currentAction.Action, failReason);

                    currentAction = null;
                    continue;
                }

                // Execute action
                bool success = false;
                string errorMessage = "";

                yield return currentAction.Action.Execute();
                success = true;

                currentAction.EndTime = Time.time;

                // Handle result
                if (success && currentAction.Status != ActionStatus.Cancelled)
                {
                    currentAction.Status = ActionStatus.Completed;
                    Debug.Log($"Action completed: {currentAction.Action.ActionName}");
                    OnActionCompleted?.Invoke(currentAction.Action);
                }
                else if (currentAction.Status == ActionStatus.Cancelled)
                {
                    Debug.Log($"Action cancelled: {currentAction.Action.ActionName}");
                }
                else
                {
                    currentAction.Status = ActionStatus.Failed;
                    OnActionFailed?.Invoke(currentAction.Action, errorMessage);
                }

                currentAction = null;

                // Delay between actions
                if (actionDelaySeconds > 0)
                {
                    yield return new WaitForSeconds(actionDelaySeconds);
                }
            }

            // Queue is empty
            isExecuting = false;
            Debug.Log("=== Action Queue Execution Completed ===");
            OnQueueEmpty?.Invoke();
        }
        #endregion

        #region Query Methods
        /// <summary>
        /// Get list of upcoming actions
        /// </summary>
        public List<IAction> GetUpcomingActions(int count = -1)
        {
            var result = new List<IAction>();
            int limit = count < 0 ? actionQueue.Count : Mathf.Min(count, actionQueue.Count);

            int index = 0;
            foreach (var entry in actionQueue)
            {
                if (index >= limit) break;
                result.Add(entry.Action);
                index++;
            }

            return result;
        }

        /// <summary>
        /// Check if action queue contains specific action type
        /// </summary>
        public bool ContainsActionType<T>() where T : IAction
        {
            foreach (var entry in actionQueue)
            {
                if (entry.Action is T) return true;
            }
            return false;
        }
        #endregion
    }

    #region Supporting Classes
    /// <summary>
    /// Entry in action queue
    /// </summary>
    public class ActionQueueEntry
    {
        public IAction Action { get; private set; }
        public int Priority { get; private set; }
        public ActionStatus Status { get; set; }
        public float StartTime { get; set; }
        public float EndTime { get; set; }
        public float Duration => EndTime - StartTime;

        public ActionQueueEntry(IAction action, int priority = 0)
        {
            Action = action;
            Priority = priority;
            Status = ActionStatus.Queued;
        }
    }

    /// <summary>
    /// Action execution status
    /// </summary>
    public enum ActionStatus
    {
        Queued,
        Executing,
        Completed,
        Failed,
        Cancelled
    }
    #endregion
}