using System.Collections.Generic;
using UnityEngine;

public class LeaderAttackState : IState
{
    private Base_Unit leader;
    private SteeringBehaviors steering;
    private StateMachine stateMachine;
    
    private Base_Unit primaryTarget;
    private List<Vector3> pathToTarget;
    private int currentPathIndex;
    private float lastPathUpdate;
    private bool hasFortified;
    
    public LeaderAttackState(Base_Unit leader, SteeringBehaviors steering, StateMachine stateMachine)
    {
        this.leader = leader;
        this.steering = steering;
        this.stateMachine = stateMachine;
    }
    
    public void Enter()
    {
        primaryTarget = leader.FindClosestEnemy();
        hasFortified = false;
        
        if (leader.CurrentSquad != null && primaryTarget != null)
        {
            leader.CurrentSquad.SetSquadTarget(primaryTarget);
        }
        
        if (Random.Range(0f, 1f) < 0.3f)
        {
            stateMachine.ChangeState<LeaderFortifyState>();
            return;
        }
        
        UpdatePathToTarget();
    }
    
    public void Update()
    {
        if (ShouldRetreat())
        {
            stateMachine.ChangeState<LeaderFleeState>();
            return;
        }
        
        if (primaryTarget == null || !primaryTarget.isAlive || !leader.CanSeeTarget(primaryTarget))
        {
            primaryTarget = leader.FindClosestEnemy();
            if (primaryTarget == null)
            {
                if (leader.CurrentSquad != null)
                {
                    leader.CurrentSquad.ClearSquadTarget();
                }
                stateMachine.ChangeState<LeaderCommandState>();
                return;
            }
            else
            {
                if (leader.CurrentSquad != null)
                {
                    leader.CurrentSquad.SetSquadTarget(primaryTarget);
                }
            }
        }
        
        if (leader.IsInAttackRange(primaryTarget) && leader.canAttack)
        {
            leader.Attack(primaryTarget);
            return;
        }
        
        MoveTowardsTarget();
    }
    
    public void Exit()
    {
        pathToTarget = null;
        currentPathIndex = 0;
        
        if (leader.CurrentSquad != null)
        {
            leader.CurrentSquad.ClearSquadTarget();
        }
    }
    
    bool ShouldRetreat()
    {
        if (leader.CurrentSquad == null)
        {
            return leader.healthPercentage < 0.25f;
        }
        
        float avgHealth = leader.CurrentSquad.GetAverageHealthPercentage();
        int aliveCount = leader.CurrentSquad.GetAliveCount();
        float leaderHealth = leader.healthPercentage;
        
        return aliveCount == 0 || 
               avgHealth < 0.20f || 
               (leaderHealth < 0.15f && avgHealth < 0.35f);
    }
    
    void UpdatePathToTarget()
    {
        if (primaryTarget != null && Time.time > lastPathUpdate + 0.5f)
        {
            pathToTarget = AStarPlus.Instance.FindPath(leader.transform.position, primaryTarget.transform.position);
            currentPathIndex = 0;
            lastPathUpdate = Time.time;
        }
    }
    
    void MoveTowardsTarget()
    {
        if (primaryTarget == null) return;
        
        float distanceToTarget = Vector3.Distance(leader.transform.position, primaryTarget.transform.position);
        
        if (distanceToTarget <= 8f)
        {
            Vector3 directionToTarget = (primaryTarget.transform.position - leader.transform.position).normalized;
            float optimalDistance = 1.5f;
            
            if (distanceToTarget > optimalDistance + 0.5f)
            {
                Vector3 optimalPosition = primaryTarget.transform.position - directionToTarget * optimalDistance;
                steering.MoveTo(optimalPosition);
            }
            else if (distanceToTarget < optimalDistance - 0.5f)
            {
                Vector3 backPosition = leader.transform.position - directionToTarget * 0.5f;
                steering.MoveTo(backPosition);
            }
        }
        else
        {
            UpdatePathToTarget();
            
            if (pathToTarget != null && pathToTarget.Count > 0)
            {
                if (currentPathIndex < pathToTarget.Count)
                {
                    Vector3 targetWaypoint = pathToTarget[currentPathIndex];
                    float distanceToWaypoint = Vector3.Distance(leader.transform.position, targetWaypoint);
                    
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
                steering.MoveTo(primaryTarget.transform.position);
            }
        }
    }
}
