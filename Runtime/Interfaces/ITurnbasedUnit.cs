using UnityEngine;

namespace TurnBasedFramework.TurnManagement
{
    /// <summary>
    /// Interface for all units that can participate in turn-based combat
    /// </summary>
    public interface ITurnBasedUnit
    {
        string UnitName { get; }
        int Speed { get; }
        bool IsAlive { get; }
        bool IsPlayerControlled { get; }

        GameObject GameObject { get; }
        Transform Transform { get; }

        void OnTurnStart();
        void OnTurnEnd();
    }
}