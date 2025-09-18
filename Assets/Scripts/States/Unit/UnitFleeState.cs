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
    private float maxFleeTime = 15f;
    
    public UnitFleeState(Base_Unit unit, SteeringBehaviors steering, StateMachine stateMachine)
    {
        this.unit = unit;
        this.steering = steering;
        this.stateMachine = stateMachine;
    }
    
    public void Enter()
    {
        fleeStartTime = Time.time;
        
        float healthPercent = unit.healthPercentage;
        fleeDuration = Mathf.Lerp(8f, 3f, healthPercent);
        
        FindFleeTarget();
    }
    
    public void Update()
    {
        if (!hasSeparatedFromSquad && unit.CurrentSquad != null)
        {
            unit.CurrentSquad.RemoveMember(unit);
            unit.SetSquad(null, SquadRole.Independent);
            hasSeparatedFromSquad = true;
        }
        
        Base_Unit healingLeader = FindNearbyHealingLeader();
        if (healingLeader != null)
        {
            if (healingLeader.CurrentSquad != null && !healingLeader.CurrentSquad.IsFull)
            {
                if (healingLeader.CurrentSquad.TryAddMember(unit))
                {
                    stateMachine.ChangeState<UnitFollowState>();
                    return;
                }
            }
        }
        
        float timeSinceFleeStart = Time.time - fleeStartTime;
        if (timeSinceFleeStart > fleeDuration || timeSinceFleeStart > maxFleeTime)
        {
            stateMachine.ChangeState<UnitRoamState>();
            return;
        }
        
        float distanceToFleeTarget = Vector3.Distance(unit.transform.position, fleeTarget);
        if (distanceToFleeTarget < 3f)
        {
            FindFleeTarget();
        }
        
        steering.MoveTo(fleeTarget);
    }
    
    public void Exit()
    {
    }
    
    void FindFleeTarget()
    {
        Vector3 fleeDirection = GetFleeDirection();
        fleeTarget = unit.transform.position + fleeDirection * 12f;
        
        if (AStarPlus.Instance != null)
        {
            fleeTarget = AStarPlus.Instance.ClampToGrid(fleeTarget);
        }
        else
        {
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
        Base_Unit[] allUnits = Object.FindObjectsOfType<Base_Unit>();
        foreach (Base_Unit potentialLeader in allUnits)
        {
            if (potentialLeader == unit || potentialLeader.faction != unit.faction || !potentialLeader.isAlive) continue;
            if (potentialLeader.unitType != UnitType.Leader) continue;
            if (potentialLeader.CurrentSquad == null || potentialLeader.CurrentSquad.IsFull) continue;
            
            var leaderStateMachine = potentialLeader.GetComponent<StateMachine>();
            if (leaderStateMachine == null) continue;
            
            if (leaderStateMachine.IsInState<LeaderHealState>())
            {
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
