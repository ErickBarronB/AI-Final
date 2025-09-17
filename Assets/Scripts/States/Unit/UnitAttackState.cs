using System.Collections.Generic;
using UnityEngine;

public class UnitAttackState : IState
{
    private Base_Unit unit;
    private SteeringBehaviors steering;
    private StateMachine stateMachine;
    
    private Base_Unit currentTarget;
    private List<Vector3> pathToTarget;
    private int currentPathIndex;
    private float lastPathUpdate;
    
    public UnitAttackState(Base_Unit unit, SteeringBehaviors steering, StateMachine stateMachine)
    {
        this.unit = unit;
        this.steering = steering;
        this.stateMachine = stateMachine;
    }
    
    public void Enter()
    {
        // Prioritize squad target if in a squad, otherwise find closest enemy
        if (unit.CurrentSquad != null && unit.CurrentSquad.HasSquadAttackOrder())
        {
            currentTarget = unit.CurrentSquad.CurrentSquadTarget;
        }
        else
        {
            currentTarget = unit.FindClosestEnemy();
        }
        
        UpdatePathToTarget();
    }
    
    public void Update()
    {
        // Check if we should flee
        if (unit.healthPercentage <= 0.25f)
        {
            stateMachine.ChangeState<UnitFleeState>();
            return;
        }
        
        // Check if we still have valid targets to attack
        bool hasSquadOrders = unit.CurrentSquad != null && unit.CurrentSquad.HasValidSquadTarget();
        bool hasVisibleEnemies = unit.GetVisibleEnemies().Count > 0;
        
        if (!hasSquadOrders && !hasVisibleEnemies)
        {
            // Return to appropriate state based on squad role
            if (unit.SquadRole == SquadRole.Independent)
            {
                stateMachine.ChangeState<UnitRoamState>();
            }
            else
            {
                stateMachine.ChangeState<UnitFollowState>();
            }
            return;
        }
        
        // Update target if current one is gone
        if (currentTarget == null || !currentTarget.isAlive || !unit.CanSeeTarget(currentTarget))
        {
            // Prioritize squad target if available
            if (unit.CurrentSquad != null && unit.CurrentSquad.HasSquadAttackOrder())
            {
                currentTarget = unit.CurrentSquad.CurrentSquadTarget;
            }
            else
            {
                currentTarget = unit.FindClosestEnemy();
            }
            
            if (currentTarget == null)
            {
                // Return to appropriate state based on squad role
                if (unit.SquadRole == SquadRole.Independent)
                {
                    stateMachine.ChangeState<UnitRoamState>();
                }
                else
                {
                    stateMachine.ChangeState<UnitFollowState>();
                }
                return;
            }
        }
        
        // Attack if in range
        if (unit.IsInAttackRange(currentTarget) && unit.canAttack)
        {
            unit.Attack(currentTarget);
            // Stop moving when attacking
            Vector3 directionToTarget = (currentTarget.transform.position - unit.transform.position).normalized;
            if (directionToTarget.magnitude > 0.1f)
            {
                Quaternion targetRotation = Quaternion.LookRotation(directionToTarget);
                unit.transform.rotation = Quaternion.Slerp(unit.transform.rotation, targetRotation, Time.deltaTime * 5f);
            }
            return;
        }
        
        // Move towards target if not in attack range
        if (!unit.IsInAttackRange(currentTarget))
        {
            MoveTowardsTarget();
        }
    }
    
    public void Exit()
    {
        currentTarget = null;
        pathToTarget = null;
        currentPathIndex = 0;
    }
    
    void UpdatePathToTarget()
    {
        if (currentTarget != null && Time.time > lastPathUpdate + 0.5f)
        {
            pathToTarget = AStarPlus.Instance.FindPath(unit.transform.position, currentTarget.transform.position);
            currentPathIndex = 0;
            lastPathUpdate = Time.time;
        }
    }
    
    void MoveTowardsTarget()
    {
        if (currentTarget == null) return;
        
        float distanceToTarget = Vector3.Distance(unit.transform.position, currentTarget.transform.position);
        
        // Use direct steering for close targets
        if (distanceToTarget <= 8f)
        {
            Vector3 directionToTarget = (currentTarget.transform.position - unit.transform.position).normalized;
            float optimalDistance = 1.5f; // Much closer than attack range (4f)
            
            if (distanceToTarget > optimalDistance + 0.5f)
            {
                Vector3 optimalPosition = currentTarget.transform.position - directionToTarget * optimalDistance;
                steering.MoveTo(optimalPosition);
            }
            else if (distanceToTarget < optimalDistance - 0.5f)
            {
                Vector3 backPosition = unit.transform.position - directionToTarget * 0.5f;
                steering.MoveTo(backPosition);
            }
        }
        else
        {
            // Use pathfinding for longer distances
            UpdatePathToTarget();
            
            if (pathToTarget != null && pathToTarget.Count > 0)
            {
                if (currentPathIndex < pathToTarget.Count)
                {
                    Vector3 targetWaypoint = pathToTarget[currentPathIndex];
                    float distanceToWaypoint = Vector3.Distance(unit.transform.position, targetWaypoint);
                    
                    if (distanceToWaypoint < 1f)
                    {
                        currentPathIndex++;
                    }
                    
                    if (currentPathIndex < pathToTarget.Count)
                    {
                        steering.MoveTo(pathToTarget[currentPathIndex]);
                    }
                }
            }
            else
            {
                steering.MoveTo(currentTarget.transform.position);
            }
        }
    }
}
