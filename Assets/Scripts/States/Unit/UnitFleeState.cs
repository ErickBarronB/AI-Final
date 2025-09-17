using UnityEngine;

public class UnitFleeState : IState
{
    private Base_Unit unit;
    private SteeringBehaviors steering;
    private StateMachine stateMachine;
    
    private Vector3 fleeTarget;
    private float fleeStartTime;
    private float fleeDuration;
    private bool hasSeparatedFromSquad = false;
    private float maxFleeTime = 15f; // Maximum time to flee as failsafe
    
    public UnitFleeState(Base_Unit unit, SteeringBehaviors steering, StateMachine stateMachine)
    {
        this.unit = unit;
        this.steering = steering;
        this.stateMachine = stateMachine;
    }
    
    public void Enter()
    {
        fleeStartTime = Time.time;
        
        // Calculate flee duration based on health (lower health = longer flee)
        float healthPercent = unit.healthPercentage;
        fleeDuration = Mathf.Lerp(8f, 3f, healthPercent); // 8s at 0% health, 3s at 100% health
        
        FindFleeTarget();
    }
    
    public void Update()
    {
        // Separate from squad if not already done
        if (!hasSeparatedFromSquad && unit.CurrentSquad != null)
        {
            unit.CurrentSquad.RemoveMember(unit);
            unit.SetSquad(null, SquadRole.Independent);
            hasSeparatedFromSquad = true;
        }
        
        // Check for nearby healing leaders to join (even while fleeing)
        Base_Unit healingLeader = FindNearbyHealingLeader();
        if (healingLeader != null)
        {
            // Join the healing leader's squad
            if (healingLeader.CurrentSquad != null && !healingLeader.CurrentSquad.IsFull)
            {
                if (healingLeader.CurrentSquad.TryAddMember(unit))
                {
                    stateMachine.ChangeState<UnitFollowState>();
                    return;
                }
            }
        }
        
        // Check if flee duration is over (with failsafe)
        float timeSinceFleeStart = Time.time - fleeStartTime;
        if (timeSinceFleeStart > fleeDuration || timeSinceFleeStart > maxFleeTime)
        {
            // Transition to roam state to look for new squad
            stateMachine.ChangeState<UnitRoamState>();
            return;
        }
        
        // Continue fleeing
        float distanceToFleeTarget = Vector3.Distance(unit.transform.position, fleeTarget);
        if (distanceToFleeTarget < 3f)
        {
            FindFleeTarget(); // Find new flee position
        }
        
        steering.MoveTo(fleeTarget);
    }
    
    public void Exit()
    {
    }
    
    void FindFleeTarget()
    {
        // Find a position far from enemies
        Vector3 fleeDirection = GetFleeDirection();
        fleeTarget = unit.transform.position + fleeDirection * 12f;
        
        // Clamp to pathfinding grid bounds if pathfinder is available
        if (AStarPlus.Instance != null)
        {
            fleeTarget = AStarPlus.Instance.ClampToGrid(fleeTarget);
        }
        else
        {
            // Fallback to reasonable bounds if no pathfinder
            fleeTarget = new Vector3(
                Mathf.Clamp(fleeTarget.x, -25f, 25f),
                0f,
                Mathf.Clamp(fleeTarget.z, -25f, 25f)
            );
        }
    }
    
    Vector3 GetFleeDirection()
    {
        var enemies = unit.GetVisibleEnemies();
        if (enemies.Count == 0)
        {
            return Random.insideUnitCircle.normalized;
        }
        
        Vector3 avgEnemyPosition = Vector3.zero;
        foreach (var enemy in enemies)
        {
            avgEnemyPosition += enemy.transform.position;
        }
        avgEnemyPosition /= enemies.Count;
        
        return (unit.transform.position - avgEnemyPosition).normalized;
    }
    
    Base_Unit FindNearbyHealingLeader()
    {
        // Look for leaders of the same faction who are in healing state
        Base_Unit[] allUnits = Object.FindObjectsOfType<Base_Unit>();
        foreach (Base_Unit potentialLeader in allUnits)
        {
            if (potentialLeader == unit || potentialLeader.faction != unit.faction || !potentialLeader.isAlive) continue;
            if (potentialLeader.unitType != UnitType.Leader) continue;
            if (potentialLeader.CurrentSquad == null || potentialLeader.CurrentSquad.IsFull) continue;
            
            // Check if leader is in healing state
            var leaderStateMachine = potentialLeader.GetComponent<StateMachine>();
            if (leaderStateMachine == null) continue;
            
            if (leaderStateMachine.IsInState<LeaderHealState>())
            {
                // Check if within healing range
                float distance = Vector3.Distance(unit.transform.position, potentialLeader.transform.position);
                if (distance <= potentialLeader.HealingRange)
                {
                    return potentialLeader;
                }
            }
        }
        
        return null;
    }
}
