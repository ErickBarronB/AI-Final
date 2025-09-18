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
        
        if (leader.CurrentSquad != null)
        {
            leader.CurrentSquad.SetFortified(true);
        }
        
        steering.Stop();
    }
    
    public void Update()
    {
        if (ShouldEmergencyRetreat())
        {
            stateMachine.ChangeState<LeaderFleeState>();
            return;
        }
        
        Base_Unit enemy = leader.FindClosestEnemy();
        if (enemy != null && leader.IsInAttackRange(enemy) && leader.canAttack)
        {
            leader.Attack(enemy);
        }
        
        if (Time.time > fortifyStartTime + fortifyDuration)
        {
            stateMachine.ChangeState<LeaderAttackState>();
            return;
        }
        
        steering.Stop();
    }
    
    bool ShouldEmergencyRetreat()
    {
        if (leader.CurrentSquad == null) return true;
        
        float avgHealth = leader.CurrentSquad.GetAverageHealthPercentage();
        int aliveCount = leader.CurrentSquad.GetAliveCount();
        
        return avgHealth < 0.15f || aliveCount == 0 || (leader.healthPercentage < 0.1f && aliveCount <= 1);
    }
    
    public void Exit()
    {
    }
}
