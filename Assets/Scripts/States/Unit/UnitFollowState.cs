using UnityEngine;

public class UnitFollowState : IState
{
    private Base_Unit unit;
    private SteeringBehaviors steering;
    private StateMachine stateMachine;
    
    private float lastReportTime = 0f;
    private float reportCooldown = 1f; // Don't spam reports
    
    public UnitFollowState(Base_Unit unit, SteeringBehaviors steering, StateMachine stateMachine)
    {
        this.unit = unit;
        this.steering = steering;
        this.stateMachine = stateMachine;
    }
    
    public void Enter()
    {
    }
    
    public void Update()
    {
        // Independent units should not be in FollowState - they should be in RoamState
        if (unit.SquadRole == SquadRole.Independent)
        {
            stateMachine.ChangeState<UnitRoamState>();
            return;
        }
        
        // Check if we have squad attack orders first (regardless of visible enemies)
        if (unit.CurrentSquad != null && unit.CurrentSquad.HasSquadAttackOrder())
        {
            stateMachine.ChangeState<UnitAttackState>();
            return;
        }
        
        // Check for enemies to report to leader or attack
        var enemies = unit.GetVisibleEnemies();
        if (enemies.Count > 0)
        {
            if (unit.CurrentSquad != null && unit.SquadRole == SquadRole.Member)
            {
                // Squad members report enemy contact to leader (with cooldown to prevent spam)
                if (Time.time >= lastReportTime + reportCooldown)
                {
                    Base_Unit closestEnemy = unit.FindClosestEnemy();
                    if (closestEnemy != null)
                    {
                        unit.CurrentSquad.ReportEnemyContact(unit, closestEnemy);
                        lastReportTime = Time.time;
                    }
                }
                // Stay in follow state and wait for orders
                return;
            }
            else
            {
                // Independent units or leaders can attack directly
                stateMachine.ChangeState<UnitAttackState>();
                return;
            }
        }
        
        // Check if we should flee
        if (unit.healthPercentage <= 0.25f)
        {
            stateMachine.ChangeState<UnitFleeState>();
            return;
        }
        
        // Follow formation or wander
        if (unit.CurrentSquad != null && unit.CurrentSquad.Leader != null)
        {
            Vector3 formationPosition = unit.CurrentSquad.GetFormationPosition(unit);
            steering.MoveTo(formationPosition);
        }
        else
        {
            // Wander if no squad
            Vector2 wanderDirection = Random.insideUnitCircle.normalized * 5f;
            Vector3 wanderTarget = unit.transform.position + new Vector3(wanderDirection.x, 0, wanderDirection.y);
            steering.MoveTo(wanderTarget);
        }
    }
    
    public void Exit()
    {
    }
}
