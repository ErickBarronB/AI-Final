using UnityEngine;

public class LeaderRecruitState : IState
{
    private Base_Unit leader;
    private SteeringBehaviors steering;
    private StateMachine stateMachine;
    
    private Base_Unit recruitTarget;
    
    public LeaderRecruitState(Base_Unit leader, SteeringBehaviors steering, StateMachine stateMachine)
    {
        this.leader = leader;
        this.steering = steering;
        this.stateMachine = stateMachine;
    }
    
    public void Enter()
    {
        recruitTarget = leader.FindRecruitable();
    }
    
    public void Update()
    {
        // Check if recruitment is no longer possible
        if (recruitTarget == null || !recruitTarget.isAlive || 
            leader.CurrentSquad == null || leader.CurrentSquad.IsFull ||
            recruitTarget.SquadRole != SquadRole.Independent)
        {
            stateMachine.ChangeState<LeaderCommandState>();
            return;
        }
        
        // Move towards recruit target
        float distance = Vector3.Distance(leader.transform.position, recruitTarget.transform.position);
        
        if (distance <= leader.CurrentSquad.recruitmentRange)
        {
            // Attempt recruitment
            if (leader.CurrentSquad.TryAddMember(recruitTarget))
            {
                // Recruitment successful
                stateMachine.ChangeState<LeaderCommandState>();
                return;
            }
        }
        
        steering.MoveTo(recruitTarget.transform.position);
    }
    
    public void Exit()
    {
        recruitTarget = null;
    }
}
