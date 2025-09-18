using UnityEngine;

public class UnitFollowState : IState
{
    private Base_Unit unit;
    private SteeringBehaviors steering;
    private StateMachine stateMachine;
    
    private float lastReportTime = 0f;
    private float reportCooldown = 1f;
    
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
        if (unit.SquadRole == SquadRole.Independent)
        {
            stateMachine.ChangeState<UnitRoamState>();
            return;
        }
        
        if (unit.CurrentSquad != null && unit.CurrentSquad.HasSquadAttackOrder())
        {
            stateMachine.ChangeState<UnitAttackState>();
            return;
        }
        
        var enemies = unit.GetVisibleEnemies();
        if (enemies.Count > 0)
        {
            if (unit.CurrentSquad != null && unit.SquadRole == SquadRole.Member)
            {
                if (Time.time >= lastReportTime + reportCooldown)
                {
                    Base_Unit closestEnemy = unit.FindClosestEnemy();
                    if (closestEnemy != null)
                    {
                        unit.CurrentSquad.ReportEnemyContact(unit, closestEnemy);
                        lastReportTime = Time.time;
                    }
                }
                return;
            }
            else
            {
                stateMachine.ChangeState<UnitAttackState>();
                return;
            }
        }
        
        if (unit.healthPercentage <= 0.25f)
        {
            stateMachine.ChangeState<UnitFleeState>();
            return;
        }
        
        if (unit.CurrentSquad != null && unit.CurrentSquad.Leader != null)
        {
            Vector3 formationPosition = unit.CurrentSquad.GetFormationPosition(unit);
            steering.MoveTo(formationPosition);
        }
        else
        {
            Vector2 wanderDirection = Random.insideUnitCircle.normalized * 5f;
            Vector3 wanderTarget = unit.transform.position + new Vector3(wanderDirection.x, 0, wanderDirection.y);
            steering.MoveTo(wanderTarget);
        }
    }
    
    public void Exit()
    {
    }
}
