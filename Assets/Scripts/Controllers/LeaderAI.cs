using UnityEngine;

[RequireComponent(typeof(Base_Unit))]
[RequireComponent(typeof(SteeringBehaviors))]
public class LeaderAI : MonoBehaviour
{
    private Base_Unit unit;
    private SteeringBehaviors steering;
    private StateMachine stateMachine;
    
    private LeaderCommandState commandState;
    private LeaderAttackState attackState;
    private LeaderFleeState fleeState;
    private LeaderHealState healState;
    private LeaderFortifyState fortifyState;
    private LeaderRecruitState recruitState;
    
    void Awake()
    {
        unit = GetComponent<Base_Unit>();
        steering = GetComponent<SteeringBehaviors>();
        stateMachine = gameObject.AddComponent<StateMachine>();
    }
    
    void Start()
    {
        InitializeStates();
        
        GameObject squadGO = new GameObject($"Squad_{unit.faction}_{unit.name}");
        Squad squad = squadGO.AddComponent<Squad>();
        squad.SetLeader(unit);
        
        stateMachine.ChangeState<LeaderCommandState>();
    }
    
    void InitializeStates()
    {
        commandState = new LeaderCommandState(unit, steering, stateMachine);
        attackState = new LeaderAttackState(unit, steering, stateMachine);
        fleeState = new LeaderFleeState(unit, steering, stateMachine);
        healState = new LeaderHealState(unit, steering, stateMachine);
        fortifyState = new LeaderFortifyState(unit, steering, stateMachine);
        recruitState = new LeaderRecruitState(unit, steering, stateMachine);
        
        stateMachine.AddState(commandState);
        stateMachine.AddState(attackState);
        stateMachine.AddState(fleeState);
        stateMachine.AddState(healState);
        stateMachine.AddState(fortifyState);
        stateMachine.AddState(recruitState);
    }
}
