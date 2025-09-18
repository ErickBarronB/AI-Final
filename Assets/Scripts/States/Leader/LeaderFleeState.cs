using UnityEngine;

public class LeaderFleeState : IState
{
    private Base_Unit leader;
    private SteeringBehaviors steering;
    private StateMachine stateMachine;
    
    private Vector3 fleeTarget;
    private float safetyCheckTime;
    private float safetyDuration = 3f;
    
    public LeaderFleeState(Base_Unit leader, SteeringBehaviors steering, StateMachine stateMachine)
    {
        this.leader = leader;
        this.steering = steering;
        this.stateMachine = stateMachine;
    }
    
    public void Enter()
    {
        FindFleeTarget();
        safetyCheckTime = Time.time;
    }
    
    public void Update()
    {
        if (IsSafe() && Time.time > safetyCheckTime + safetyDuration)
        {
            stateMachine.ChangeState<LeaderHealState>();
            return;
        }
        
        float distanceToFleeTarget = Vector3.Distance(leader.transform.position, fleeTarget);
        if (distanceToFleeTarget < 3f)
        {
            FindFleeTarget();
        }
        
        steering.MoveTo(fleeTarget);
        
        if (leader.GetVisibleEnemies().Count > 0)
        {
            safetyCheckTime = Time.time;
        }
    }
    
    public void Exit()
    {
    }
    
    bool IsSafe()
    {
        return leader.GetVisibleEnemies().Count == 0;
    }
    
    void FindFleeTarget()
    {
        Vector3 fleeDirection = GetFleeDirection();
        fleeTarget = leader.transform.position + fleeDirection * 15f;
        
        if (AStarPlus.Instance != null)
        {
            fleeTarget = AStarPlus.Instance.ClampToGrid(fleeTarget);
        }
        else
        {
            fleeTarget = new Vector3(
                Mathf.Clamp(fleeTarget.x, -25f, 25f),
                0f,
                Mathf.Clamp(fleeTarget.z, -25f, 25f)
            );
        }
    }
    
    Vector3 GetFleeDirection()
    {
        var enemies = leader.GetVisibleEnemies();
        if (enemies.Count == 0)
        {
            return Random.insideUnitCircle.normalized;
        }
        
        Vector3 avgEnemyPosition = Vector3.zero;
        foreach (var enemy in enemies)
        {
            avgEnemyPosition += enemy.transform.position;
        }
        avgEnemyPosition /= enemies.Count;
        
        return (leader.transform.position - avgEnemyPosition).normalized;
    }
}
