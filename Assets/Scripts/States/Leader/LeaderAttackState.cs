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
        
        // Set squad target for coordinated attack (only if we have a squad)
        if (leader.CurrentSquad != null && primaryTarget != null)
        {
            leader.CurrentSquad.SetSquadTarget(primaryTarget);
        }
        
        // Chance to fortify at start of combat
        if (Random.Range(0f, 1f) < 0.3f) // 30% chance
        {
            stateMachine.ChangeState<LeaderFortifyState>();
            return;
        }
        
        UpdatePathToTarget();
    }
    
    public void Update()
    {
        // Check retreat conditions
        if (ShouldRetreat())
        {
            stateMachine.ChangeState<LeaderFleeState>();
            return;
        }
        
        // Update target
        if (primaryTarget == null || !primaryTarget.isAlive || !leader.CanSeeTarget(primaryTarget))
        {
            primaryTarget = leader.FindClosestEnemy();
            if (primaryTarget == null)
            {
                // Clear squad target when no enemies found
                if (leader.CurrentSquad != null)
                {
                    leader.CurrentSquad.ClearSquadTarget();
                }
                stateMachine.ChangeState<LeaderCommandState>();
                return;
            }
            else
            {
                // Update squad target when leader switches targets (only if we have a squad)
                if (leader.CurrentSquad != null)
                {
                    leader.CurrentSquad.SetSquadTarget(primaryTarget);
                }
            }
        }
        
        // Attack if in range
        if (leader.IsInAttackRange(primaryTarget) && leader.canAttack)
        {
            leader.Attack(primaryTarget);
            return;
        }
        
        // Move towards target
        MoveTowardsTarget();
    }
    
    public void Exit()
    {
        pathToTarget = null;
        currentPathIndex = 0;
        
        // Clear squad target when leader exits combat (only if we have a squad)
        if (leader.CurrentSquad != null)
        {
            leader.CurrentSquad.ClearSquadTarget();
        }
    }
    
    bool ShouldRetreat()
    {
        // Solo leader retreat conditions
        if (leader.CurrentSquad == null)
        {
            return leader.healthPercentage < 0.25f; // Solo leaders retreat at 25% health
        }
        
        // Squad leader retreat conditions - be more strategic
        float avgHealth = leader.CurrentSquad.GetAverageHealthPercentage();
        int aliveCount = leader.CurrentSquad.GetAliveCount();
        float leaderHealth = leader.healthPercentage;
        
        // Only retreat when:
        // 1. Squad is mostly gone (only leader left)
        // 2. Squad health is critically low (20% or less)
        // 3. Leader is very low on health (<15%) and squad is weakened
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
        
        // Use direct steering for close targets
        if (distanceToTarget <= 8f)
        {
            Vector3 directionToTarget = (primaryTarget.transform.position - leader.transform.position).normalized;
            float optimalDistance = 1.5f; // Much closer than attack range (4f)
            
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
            // Use pathfinding for longer distances
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
