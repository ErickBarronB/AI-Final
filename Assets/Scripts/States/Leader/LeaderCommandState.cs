using System.Collections.Generic;
using UnityEngine;

public class LeaderCommandState : IState
{
    private Base_Unit leader;
    private SteeringBehaviors steering;
    private StateMachine stateMachine;
    
    private float lastDecisionTime;
    private float decisionInterval = 1f;
    private Vector3 currentPatrolTarget;
    private float lastPatrolUpdate;
    private float patrolUpdateInterval = 5f;
    
    public LeaderCommandState(Base_Unit leader, SteeringBehaviors steering, StateMachine stateMachine)
    {
        this.leader = leader;
        this.steering = steering;
        this.stateMachine = stateMachine;
    }
    
    public void Enter()
    {
        lastDecisionTime = Time.time;
        lastPatrolUpdate = Time.time;
        currentPatrolTarget = GetNewPatrolTarget();
    }
    
    public void Update()
    {
        if (leader.CurrentSquad != null && !leader.CurrentSquad.IsFull)
        {
            Base_Unit recruitable = leader.FindRecruitable();
            if (recruitable != null)
            {
                stateMachine.ChangeState<LeaderRecruitState>();
                return;
            }
        }
        
        if (leader.healthPercentage <= 0.15f)
        {
            stateMachine.ChangeState<LeaderFleeState>();
            return;
        }
        
        List<Base_Unit> immediateEnemies = leader.GetVisibleEnemies();
        if (immediateEnemies.Count > 0)
        {
            MakeTacticalDecision();
            return;
        }
        
        if (Time.time > lastDecisionTime + decisionInterval)
        {
            MakeTacticalDecision();
            lastDecisionTime = Time.time;
        }
        
        if (Time.time > lastPatrolUpdate + patrolUpdateInterval || HasReachedPatrolTarget())
        {
            currentPatrolTarget = GetNewPatrolTarget();
            lastPatrolUpdate = Time.time;
        }
        
        steering.MoveTo(currentPatrolTarget);
    }
    
    public void Exit()
    {
    }
    
    void MakeTacticalDecision()
    {
        List<Base_Unit> enemies = leader.GetVisibleEnemies();
        
        if (enemies.Count > 0)
        {
            List<WeightedOption<LeaderDecision>> options = new List<WeightedOption<LeaderDecision>>();
            
            float squadHealth = leader.CurrentSquad.GetAverageHealthPercentage();
            float squadSize = leader.CurrentSquad.GetAliveCount();
            
            float aggressiveWeight = squadHealth > 0.7f ? 40f : 10f;
            float cautiousWeight = squadHealth > 0.4f ? 30f : 20f;
            float retreatWeight = squadHealth < 0.3f ? 50f : 5f;
            
            options.Add(new WeightedOption<LeaderDecision>(LeaderDecision.AttackAggressive, aggressiveWeight));
            options.Add(new WeightedOption<LeaderDecision>(LeaderDecision.AttackCautious, cautiousWeight));
            options.Add(new WeightedOption<LeaderDecision>(LeaderDecision.Retreat, retreatWeight));
            
            LeaderDecision decision = WeightedRouletteWheel.SelectOption(options);
            
            switch (decision)
            {
                case LeaderDecision.AttackAggressive:
                case LeaderDecision.AttackCautious:
                    stateMachine.ChangeState<LeaderAttackState>();
                    break;
                case LeaderDecision.Retreat:
                    stateMachine.ChangeState<LeaderFleeState>();
                    break;
            }
        }
        else
        {
            if (leader.CurrentSquad != null)
            {
                leader.CurrentSquad.HasValidSquadTarget();
            }
            
            bool needsHealing = false;
            
            if (leader.CurrentSquad != null)
            {
                float avgHealth = leader.CurrentSquad.GetAverageHealthPercentage();
                needsHealing = avgHealth < 0.8f;
            }
            else
            {
                needsHealing = leader.healthPercentage < 0.6f;
            }
            
            if (needsHealing)
            {
                stateMachine.ChangeState<LeaderHealState>();
            }
        }
    }
    
    Vector3 GetNewPatrolTarget()
    {
        float patrolRange = 15f;
        Vector3 basePosition = leader.transform.position;
        
        Vector2 randomDirection = Random.insideUnitCircle.normalized;
        Vector3 patrolTarget = basePosition + new Vector3(randomDirection.x * patrolRange, 0, randomDirection.y * patrolRange);
        
        if (AStarPlus.Instance != null)
        {
            patrolTarget = AStarPlus.Instance.ClampToGrid(patrolTarget);
        }
        else
        {
            patrolTarget = new Vector3(
                Mathf.Clamp(patrolTarget.x, -25f, 25f),
                0f,
                Mathf.Clamp(patrolTarget.z, -25f, 25f)
            );
        }
        
        return patrolTarget;
    }
    
    bool HasReachedPatrolTarget()
    {
        return Vector3.Distance(leader.transform.position, currentPatrolTarget) < 3f;
    }
}
