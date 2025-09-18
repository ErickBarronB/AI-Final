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
        if (recruitTarget == null || !recruitTarget.isAlive || 
            leader.CurrentSquad == null || leader.CurrentSquad.IsFull ||
            recruitTarget.SquadRole != SquadRole.Independent)
        {
            stateMachine.ChangeState<LeaderCommandState>();
            return;
        }
        
        float distance = Vector3.Distance(leader.transform.position, recruitTarget.transform.position);
        
        if (distance <= leader.CurrentSquad.recruitmentRange)
        {
            if (leader.CurrentSquad.TryAddMember(recruitTarget))
            {
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
