using UnityEngine;

public class UnitRecruitState : IState
{
    private Base_Unit unit;
    private SteeringBehaviors steering;
    private StateMachine stateMachine;
    
    private Base_Unit recruitTarget;
    
    public UnitRecruitState(Base_Unit unit, SteeringBehaviors steering, StateMachine stateMachine)
    {
        this.unit = unit;
        this.steering = steering;
        this.stateMachine = stateMachine;
    }
    
    public void Enter()
    {
        recruitTarget = unit.FindRecruitable();
    }
    
    public void Update()
    {
        if (unit.SquadRole != SquadRole.Independent ||
            recruitTarget == null || !recruitTarget.isAlive ||
            recruitTarget.SquadRole != SquadRole.Independent)
        {
            stateMachine.ChangeState<UnitFollowState>();
            return;
        }
        
        float distance = Vector3.Distance(unit.transform.position, recruitTarget.transform.position);
        
        if (distance <= 3f)
        {
            if (recruitTarget.CurrentSquad != null && !recruitTarget.CurrentSquad.IsFull)
            {
                if (recruitTarget.CurrentSquad.TryAddMember(unit))
                {
                    stateMachine.ChangeState<UnitFollowState>();
                    return;
                }
            }
            else if (unit.unitType == UnitType.Leader)
            {
                GameObject squadGO = new GameObject("Squad");
                Squad newSquad = squadGO.AddComponent<Squad>();
                newSquad.SetLeader(unit);
                newSquad.TryAddMember(recruitTarget);
                
                stateMachine.ChangeState<UnitFollowState>();
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
