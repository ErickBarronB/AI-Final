using UnityEngine;

public class LeaderFortifyState : IState
{
    private Base_Unit leader;
    private SteeringBehaviors steering;
    private StateMachine stateMachine;
    
    private float fortifyDuration = 2f;
    private float fortifyStartTime;
    
    public LeaderFortifyState(Base_Unit leader, SteeringBehaviors steering, StateMachine stateMachine)
    {
        this.leader = leader;
        this.steering = steering;
        this.stateMachine = stateMachine;
    }
    
    public void Enter()
    {
        fortifyStartTime = Time.time;
        
        // Activate fortification for the squad
        if (leader.CurrentSquad != null)
        {
            leader.CurrentSquad.SetFortified(true);
        }
        
        // Stop moving during fortification
        steering.Stop();
    }
    
    public void Update()
    {
        // Check if we should retreat (only in extreme cases during fortify)
        if (ShouldEmergencyRetreat())
        {
            stateMachine.ChangeState<LeaderFleeState>();
            return;
        }
        
        // Look for enemies to attack while fortified
        Base_Unit enemy = leader.FindClosestEnemy();
        if (enemy != null && leader.IsInAttackRange(enemy) && leader.canAttack)
        {
            leader.Attack(enemy);
        }
        
        // Fortification takes time
        if (Time.time > fortifyStartTime + fortifyDuration)
        {
            stateMachine.ChangeState<LeaderAttackState>();
            return;
        }
        
        // Stay still during fortification but can still attack
        steering.Stop();
    }
    
    bool ShouldEmergencyRetreat()
    {
        if (leader.CurrentSquad == null) return true;
        
        // Only retreat in extreme emergencies during fortification
        float avgHealth = leader.CurrentSquad.GetAverageHealthPercentage();
        int aliveCount = leader.CurrentSquad.GetAliveCount();
        
        // Emergency retreat: squad is nearly wiped out or critically low health
        return avgHealth < 0.15f || aliveCount == 0 || (leader.healthPercentage < 0.1f && aliveCount <= 1);
    }
    
    public void Exit()
    {
    }
}
