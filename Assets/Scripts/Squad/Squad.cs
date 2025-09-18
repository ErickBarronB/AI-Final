using System.Collections.Generic;
using UnityEngine;

public class Squad : MonoBehaviour
{
    [Header("Squad Settings")]
    public int maxSquadSize = 4;
    public float recruitmentRange = 10f;
    public float cohesionRange = 5f;
    
    private Base_Unit leader;
    private List<Base_Unit> members = new List<Base_Unit>();
    private bool isFortified = false;
    private float fortifyDamageReduction = 0.3f;
    
    public Base_Unit Leader => leader;
    public List<Base_Unit> Members => new List<Base_Unit>(members);
    public int SquadSize => members.Count + (leader != null ? 1 : 0);
    public bool IsFull => SquadSize >= maxSquadSize;
    public bool IsFortified => isFortified;
    public float FortifyDamageReduction => fortifyDamageReduction;
    
    [Header("Combat Coordination")]
    private Base_Unit currentSquadTarget;
    private bool isInCombat = false;
    private float lastReportTime = 0f;
    private float reportCooldown = 2f;
    
    public Base_Unit CurrentSquadTarget => currentSquadTarget;
    public bool IsInCombat => isInCombat;
    
    public void SetLeader(Base_Unit unit)
    {
        leader = unit;
        if (leader != null)
        {
            leader.SetSquad(this, SquadRole.Leader);
        }
    }
    
    public bool TryAddMember(Base_Unit unit)
    {
        if (IsFull || members.Contains(unit) || unit == leader) return false;
        
        members.Add(unit);
        unit.SetSquad(this, SquadRole.Member);
        return true;
    }
    
    public void RemoveMember(Base_Unit unit)
    {
        if (members.Remove(unit))
        {
            unit.SetSquad(null, SquadRole.Independent);
        }
    }
    
    public void RemoveLeader()
    {
        if (leader != null)
        {
            leader.SetSquad(null, SquadRole.Independent);
            leader = null;
            
            if (members.Count > 0)
            {
                Base_Unit newLeader = members[0];
                members.RemoveAt(0);
                SetLeader(newLeader);
            }
        }
    }
    
    public float GetAverageHealthPercentage()
    {
        float totalHealth = 0f;
        int count = 0;
        
        if (leader != null && leader.isAlive)
        {
            totalHealth += leader.healthPercentage;
            count++;
        }
        
        foreach (Base_Unit member in members)
        {
            if (member != null && member.isAlive)
            {
                totalHealth += member.healthPercentage;
                count++;
            }
        }
        
        return count > 0 ? totalHealth / count : 0f;
    }
    
    public int GetAliveCount()
    {
        int count = 0;
        if (leader != null && leader.isAlive) count++;
        
        foreach (Base_Unit member in members)
        {
            if (member != null && member.isAlive) count++;
        }
        
        return count;
    }
    
    public List<Base_Unit> GetLowHealthMembers(float threshold = 0.2f)
    {
        List<Base_Unit> lowHealthUnits = new List<Base_Unit>();
        
        if (leader != null && leader.isAlive && leader.healthPercentage <= threshold)
        {
            lowHealthUnits.Add(leader);
        }
        
        foreach (Base_Unit member in members)
        {
            if (member != null && member.isAlive && member.healthPercentage <= threshold)
            {
                lowHealthUnits.Add(member);
            }
        }
        
        return lowHealthUnits;
    }
    
    public void SetFortified(bool fortified)
    {
        isFortified = fortified;
    }
    
    public Vector3 GetFormationPosition(Base_Unit unit)
    {
        if (leader == null) return unit.transform.position;
        
        int index = members.IndexOf(unit);
        if (index == -1) return unit.transform.position;
        
        float angle = (index * 90f) * Mathf.Deg2Rad;
        Vector3 offset = new Vector3(Mathf.Cos(angle), 0, Mathf.Sin(angle)) * cohesionRange;
        return leader.transform.position + offset;
    }
    
    void Update()
    {
        CleanupDeadMembers();
    }
    
    void CleanupDeadMembers()
    {
        if (leader != null && !leader.isAlive)
        {
            RemoveLeader();
        }
        
        for (int i = members.Count - 1; i >= 0; i--)
        {
            if (members[i] == null || !members[i].isAlive)
            {
                members.RemoveAt(i);
            }
        }
        
        if (SquadSize == 0)
        {
            Destroy(gameObject);
        }
    }
    
    public void ReportEnemyContact(Base_Unit reporter, Base_Unit enemy)
    {
        if (leader == null || enemy == null) return;
        
        if (Time.time < lastReportTime + reportCooldown) return;
        
        lastReportTime = Time.time;
        
        var leaderStateMachine = leader.GetComponent<StateMachine>();
        if (leaderStateMachine == null) return;
        
        if (leaderStateMachine.IsInState<LeaderCommandState>() || 
            leaderStateMachine.IsInState<LeaderHealState>() ||
            leaderStateMachine.IsInState<LeaderRecruitState>())
        {
            SetSquadTarget(enemy);
            leaderStateMachine.ChangeState<LeaderAttackState>();
        }
        else if (leaderStateMachine.IsInState<LeaderAttackState>() && 
                 (currentSquadTarget == null || currentSquadTarget != enemy))
        {
            SetSquadTarget(enemy);
        }
    }
    
    public void SetSquadTarget(Base_Unit target)
    {
        currentSquadTarget = target;
        isInCombat = target != null;
        
        if (isInCombat)
        {
            Debug.Log($"[Squad] Squad target set to: {target.name}");
            
            NotifySquadAttackOrder(target);
        }
        else
        {
            Debug.Log($"[Squad] Squad combat ended");
        }
    }
    
    public void NotifySquadAttackOrder(Base_Unit target)
    {
        foreach (Base_Unit member in members)
        {
            if (member != null && member.isAlive)
            {
                var memberStateMachine = member.GetComponent<StateMachine>();
                if (memberStateMachine != null)
                {
                    if (memberStateMachine.IsInState<UnitFollowState>() || 
                        memberStateMachine.IsInState<UnitRecruitState>())
                    {
                        memberStateMachine.ChangeState<UnitAttackState>();
                    }
                }
            }
        }
    }
    
    public void ClearSquadTarget()
    {
        SetSquadTarget(null);
    }
    
    public bool HasSquadAttackOrder()
    {
        return isInCombat && currentSquadTarget != null && currentSquadTarget.isAlive;
    }
    
    public bool HasValidSquadTarget()
    {
        if (isInCombat && (currentSquadTarget == null || !currentSquadTarget.isAlive))
        {
            ClearSquadTarget();
            return false;
        }
        return isInCombat && currentSquadTarget != null && currentSquadTarget.isAlive;
    }
}
