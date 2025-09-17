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
        // Stop healing if enemies appear
        if (leader.GetVisibleEnemies().Count > 0)
        {
            stateMachine.ChangeState<LeaderCommandState>();
            return;
        }
        
        // Check if healing is complete
        if (IsHealingComplete())
        {
            stateMachine.ChangeState<LeaderCommandState>();
            return;
        }
        
        // Stay still and heal nearby allies
        steering.Stop();
        
        // Heal squad members first
        leader.HealNearbyAllies();
        
        // Self-heal the leader too (prioritize leader healing if critically low)
        if (leader.healthPercentage < 1f)
        {
            float healRate = leader.HealingRate;
            
            // Increase healing rate if leader is critically low
            if (leader.healthPercentage < 0.3f)
                healRate *= 2f;
                
            // Heal every frame for faster self-healing
            int healAmount = Mathf.RoundToInt(healRate * Time.deltaTime);
            if (healAmount < 1) healAmount = 1; // Minimum 1 HP per frame
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
            // If no squad, just heal leader to full
            return leader.healthPercentage >= 0.95f;
        }
        
        float avgHealth = leader.CurrentSquad.GetAverageHealthPercentage();
        float leaderHealth = leader.healthPercentage;
        
        // Complete when both leader and squad are well healed
        return avgHealth >= 0.90f && leaderHealth >= 0.90f;
    }
}
