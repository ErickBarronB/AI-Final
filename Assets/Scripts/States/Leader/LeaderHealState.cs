using UnityEngine;

public class LeaderHealState : IState
{
    private Base_Unit leader;
    private SteeringBehaviors steering;
    private StateMachine stateMachine;
    
    private float healingStartTime;
    
    public LeaderHealState(Base_Unit leader, SteeringBehaviors steering, StateMachine stateMachine)
    {
        this.leader = leader;
        this.steering = steering;
        this.stateMachine = stateMachine;
    }
    
    public void Enter()
    {
        healingStartTime = Time.time;
        steering.Stop();
    }
    
    public void Update()
    {
        if (leader.GetVisibleEnemies().Count > 0)
        {
            stateMachine.ChangeState<LeaderCommandState>();
            return;
        }
        
        if (IsHealingComplete())
        {
            stateMachine.ChangeState<LeaderCommandState>();
            return;
        }
        
        steering.Stop();
        
        leader.HealNearbyAllies();
        
        if (leader.healthPercentage < 1f)
        {
            float healRate = leader.HealingRate;
            
            if (leader.healthPercentage < 0.3f)
                healRate *= 2f;
                
            int healAmount = Mathf.RoundToInt(healRate * Time.deltaTime);
            if (healAmount < 1) healAmount = 1;
            leader.Heal(healAmount);
        }
    }
    
    public void Exit()
    {
    }
    
    bool IsHealingComplete()
    {
        if (leader.CurrentSquad == null) 
        {
            return leader.healthPercentage >= 0.95f;
        }
        
        float avgHealth = leader.CurrentSquad.GetAverageHealthPercentage();
        float leaderHealth = leader.healthPercentage;
        
        return avgHealth >= 0.90f && leaderHealth >= 0.90f;
    }
}
