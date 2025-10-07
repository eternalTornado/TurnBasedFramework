using UnityEngine;
using System.Collections.Generic;
using TurnBasedFramework.TurnManagement;

public class CombatExample : MonoBehaviour
{
    [SerializeField] private List<GameObject> playerUnits;
    [SerializeField] private List<GameObject> enemyUnits;

    private void Start()
    {
        StartExampleCombat();
    }

    private void StartExampleCombat()
    {
        // Collect all units
        var allUnits = new List<ITurnBasedUnit>();
        
        foreach (var playerUnit in playerUnits)
        {
            var unit = playerUnit.GetComponent<ITurnBasedUnit>();
            if (unit != null) allUnits.Add(unit);
        }
        
        foreach (var enemyUnit in enemyUnits)
        {
            var unit = enemyUnit.GetComponent<ITurnBasedUnit>();
            if (unit != null) allUnits.Add(unit);
        }

        // Start combat
        TurnManager.Instance.StartCombat(allUnits);
    }

    // Example: Player selects attack action
    public void OnPlayerSelectAttack(ITurnBasedUnit target)
    {
        var currentUnit = TurnManager.Instance.CurrentUnit;
        if (currentUnit == null || !currentUnit.IsPlayerControlled) return;

        // Create attack action
        var attackAction = new AttackAction(currentUnit, target);
        
        // Execute through TurnManager
        TurnManager.Instance.ExecuteAction(attackAction);
    }

    // Example attack action implementation
    public class AttackAction : BaseAction
    {
        private ITurnBasedUnit target;

        public AttackAction(ITurnBasedUnit actor, ITurnBasedUnit target) : base(actor)
        {
            this.target = target;
            ActionName = "Attack";
            ActionType = ActionType.Attack;
        }

        public override bool CanExecute()
        {
            return actor != null && actor.IsAlive && 
                   target != null && target.IsAlive;
        }

        public override System.Collections.IEnumerator Execute()
        {
            Debug.Log($"{actor.UnitName} attacks {target.UnitName}!");
            
            // Play attack animation
            yield return new WaitForSeconds(0.5f);
            
            // Deal damage (simplified)
            // target.TakeDamage(actor.AttackPower);
            
            Debug.Log("Attack completed!");
        }
    }
}