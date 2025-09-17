using UnityEngine;

[RequireComponent(typeof(Base_Unit))]
[RequireComponent(typeof(SteeringBehaviors))]
public class UnitAI : MonoBehaviour
{
    private Base_Unit unit;
    private SteeringBehaviors steering;
    private StateMachine stateMachine;
    
    // Unit States
    private UnitFollowState followState;
    private UnitAttackState attackState;
    private UnitFleeState fleeState;
    private UnitRecruitState recruitState;
    private UnitRoamState roamState;
    
    void Awake()
    {
        unit = GetComponent<Base_Unit>();
        steering = GetComponent<SteeringBehaviors>();
        stateMachine = gameObject.AddComponent<StateMachine>();
    }
    
    void Start()
    {
        InitializeStates();
        
        // Start in follow state
        stateMachine.ChangeState<UnitFollowState>();
    }
    
    void InitializeStates()
    {
        followState = new UnitFollowState(unit, steering, stateMachine);
        attackState = new UnitAttackState(unit, steering, stateMachine);
        fleeState = new UnitFleeState(unit, steering, stateMachine);
        recruitState = new UnitRecruitState(unit, steering, stateMachine);
        roamState = new UnitRoamState(unit, steering, stateMachine);
        
        stateMachine.AddState(followState);
        stateMachine.AddState(attackState);
        stateMachine.AddState(fleeState);
        stateMachine.AddState(recruitState);
        stateMachine.AddState(roamState);
    }
}
