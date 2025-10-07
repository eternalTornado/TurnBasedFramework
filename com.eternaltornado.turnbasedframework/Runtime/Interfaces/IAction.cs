using System.Collections;
using UnityEngine;

namespace TurnBasedFramework.TurnManagement
{
    /// <summary>
    /// Interface for all executable actions
    /// </summary>
    public interface IAction
    {
        string ActionName { get; }
        string Description { get; }
        ActionType ActionType { get; }
        
        /// <summary>
        /// Check if action can be executed
        /// </summary>
        bool CanExecute();
        
        /// <summary>
        /// Execute the action (can be coroutine for animations)
        /// </summary>
        IEnumerator Execute();
        
        /// <summary>
        /// Cancel the action mid-execution
        /// </summary>
        void Cancel();
        
        /// <summary>
        /// Undo the action (for action replay/undo systems)
        /// </summary>
        void Undo();
    }

    /// <summary>
    /// Action types for categorization
    /// </summary>
    public enum ActionType
    {
        Movement,
        Attack,
        Skill,
        Item,
        Defend,
        Wait,
        Special
    }
}