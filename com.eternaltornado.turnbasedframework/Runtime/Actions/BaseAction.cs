using System.Collections;
using UnityEngine;

namespace TurnBasedFramework.TurnManagement
{
    /// <summary>
    /// Base abstract class for all actions
    /// </summary>
    public abstract class BaseAction : IAction
    {
        public virtual string ActionName { get; protected set; }
        public virtual string Description { get; protected set; }
        public virtual ActionType ActionType { get; protected set; }

        protected ITurnBasedUnit actor;
        protected bool isCancelled = false;

        public BaseAction(ITurnBasedUnit actor)
        {
            this.actor = actor;
        }

        public abstract bool CanExecute();
        public abstract IEnumerator Execute();

        public virtual void Cancel()
        {
            isCancelled = true;
            Debug.Log($"Action {ActionName} cancelled");
        }

        public virtual void Undo()
        {
            Debug.LogWarning($"Undo not implemented for {ActionName}");
        }
    }
}