using UnityEngine;

public class UnitRoamState : IState
{
    private Base_Unit unit;
    private SteeringBehaviors steering;
    private StateMachine stateMachine;
    
    private Vector3 roamTarget;
    private float lastRoamUpdate;
    private float roamUpdateInterval = 3f;
    private float lastRecruitCheck;
    private float recruitCheckInterval = 2f;
    
    public UnitRoamState(Base_Unit unit, SteeringBehaviors steering, StateMachine stateMachine)
    {
        this.unit = unit;
        this.steering = steering;
        this.stateMachine = stateMachine;
    }
    
    public void Enter()
    {
        // Set unit as independent when entering roam state
        if (unit.CurrentSquad != null)
        {
            unit.CurrentSquad.RemoveMember(unit);
        }
        unit.SetSquad(null, SquadRole.Independent);
        
        GetNewRoamTarget();
        lastRecruitCheck = Time.time;
    }
    
    public void Update()
    {
        // Check if we should flee
        if (unit.healthPercentage <= 0.2f)
        {
            stateMachine.ChangeState<UnitFleeState>();
            return;
        }
        
        // Check for enemies to attack (independent units can attack directly)
        if (unit.GetVisibleEnemies().Count > 0)
        {
            stateMachine.ChangeState<UnitAttackState>();
            return;
        }
        
        // Safety check: if no enemies exist at all, just wander around
        if (!AreThereAnyEnemies())
        {
            // No enemies left - just wander around peacefully
            if (Time.time > lastRoamUpdate + roamUpdateInterval || HasReachedRoamTarget())
            {
                GetNewRoamTarget();
                lastRoamUpdate = Time.time;
            }
            steering.MoveTo(roamTarget);
            return;
        }
        
        // Check for squads to join (independent units look for leaders to recruit them)
        if (Time.time > lastRecruitCheck + recruitCheckInterval)
        {
            // Look for leaders of the same faction who can recruit us
            Base_Unit[] allUnits = Object.FindObjectsOfType<Base_Unit>();
            foreach (Base_Unit potentialLeader in allUnits)
            {
                if (potentialLeader == unit || potentialLeader.faction != unit.faction || !potentialLeader.isAlive) continue;
                if (potentialLeader.unitType != UnitType.Leader) continue;
                if (potentialLeader.CurrentSquad == null || potentialLeader.CurrentSquad.IsFull) continue;
                
                float distance = Vector3.Distance(unit.transform.position, potentialLeader.transform.position);
                if (distance <= potentialLeader.CurrentSquad.recruitmentRange)
                {
                    stateMachine.ChangeState<UnitRecruitState>();
                    return;
                }
            }
            lastRecruitCheck = Time.time;
        }
        
        // Update roam target periodically
        if (Time.time > lastRoamUpdate + roamUpdateInterval || HasReachedRoamTarget())
        {
            GetNewRoamTarget();
            lastRoamUpdate = Time.time;
        }
        
        // Move towards roam target
        steering.MoveTo(roamTarget);
    }
    
    public void Exit()
    {
    }
    
    void GetNewRoamTarget()
    {
        // Create random roam points within pathfinding grid
        float roamRange = 20f;
        Vector3 basePosition = unit.transform.position;
        
        Vector2 randomDirection = Random.insideUnitCircle.normalized;
        roamTarget = basePosition + new Vector3(randomDirection.x * roamRange, 0, randomDirection.y * roamRange);
        
        // Clamp to pathfinding grid bounds if pathfinder is available
        if (AStarPlus.Instance != null)
        {
            roamTarget = AStarPlus.Instance.ClampToGrid(roamTarget);
        }
        else
        {
            // Fallback to reasonable bounds if no pathfinder
            roamTarget = new Vector3(
                Mathf.Clamp(roamTarget.x, -25f, 25f),
                0f,
                Mathf.Clamp(roamTarget.z, -25f, 25f)
            );
        }
    }
    
    bool HasReachedRoamTarget()
    {
        return Vector3.Distance(unit.transform.position, roamTarget) < 3f;
    }
    
    bool AreThereAnyEnemies()
    {
        // Check if there are any enemy units alive at all
        Base_Unit[] allUnits = Object.FindObjectsOfType<Base_Unit>();
        foreach (Base_Unit otherUnit in allUnits)
        {
            if (otherUnit.faction != unit.faction && otherUnit.isAlive)
            {
                return true; // Found at least one enemy
            }
        }
        return false; // No enemies found
    }
}
