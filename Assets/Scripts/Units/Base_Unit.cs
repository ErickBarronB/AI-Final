using System.Collections.Generic;
using UnityEngine;

public class Base_Unit : MonoBehaviour
{
    [Header("Unit Identity")]
    public Faction faction;
    public UnitType unitType;
    
    [Header("Health")]
    [SerializeField] protected int maxHealth = 100;
    [SerializeField] protected int currentHealth;
    public float healthPercentage => (float)currentHealth / maxHealth;
    public bool isAlive => currentHealth > 0;
    
    [Header("Combat")]
    [SerializeField] protected int attackDamage = 15;
    [SerializeField] protected float attackRange = 4f;
    [SerializeField] protected float attackCooldown = 1.5f;
    [SerializeField] protected float optimalCombatDistance = 1.5f;
    protected float lastAttackTime;
    public bool canAttack => Time.time >= lastAttackTime + attackCooldown;
    
    [Header("Vision")]
    [SerializeField] protected float visionRange = 8f;
    [SerializeField] protected float visionAngle = 90f;
    [SerializeField] protected LayerMask obstacleLayerMask = 1;
    
    [Header("Squad")]
    [SerializeField] protected SquadRole squadRole = SquadRole.Independent;
    protected Squad currentSquad;
    public SquadRole SquadRole => squadRole;
    
    [Header("Healing (Leader Only)")]
    [SerializeField] protected float healingRange = 6f;
    [SerializeField] protected float healingRate = 10f; // HP per second
    public float HealingRate => healingRate;
    public float HealingRange => healingRange;
    
    public Squad CurrentSquad => currentSquad;
    public SquadRole Role => squadRole;
    public float VisionRange => unitType == UnitType.Leader ? visionRange * 1.5f : visionRange;
    public float OptimalCombatDistance => optimalCombatDistance;
    
    protected virtual void Awake()
    {
        currentHealth = maxHealth;
    }
    
    public virtual void TakeDamage(int damage)
    {
        if (!isAlive) return;
        
        // Apply fortification damage reduction if in fortified squad
        if (currentSquad != null && currentSquad.IsFortified && squadRole == SquadRole.Member)
        {
            damage = Mathf.RoundToInt(damage * (1f - currentSquad.FortifyDamageReduction));
        }
        
        currentHealth = Mathf.Max(0, currentHealth - damage);
        
        // Trigger combat response for any unit taking damage
        if (isAlive)
        {
            TriggerCombatResponse();
        }
        
        if (!isAlive)
        {
            Die();
        }
    }
    
    protected virtual void TriggerCombatResponse()
    {
        // First try to find visible enemies
        Base_Unit closestEnemy = FindClosestEnemy();
        
        // If no visible enemies, search for ANY nearby enemy (360 degree search)
        if (closestEnemy == null)
        {
            closestEnemy = FindNearbyEnemyForRetaliation();
        }
        
        if (closestEnemy != null)
        {
            var stateMachine = GetComponent<StateMachine>();
            if (stateMachine == null) return;
            
            if (unitType == UnitType.Leader)
            {
                // Leader: Enter attack state and coordinate squad
                // Only trigger if in non-combat state
                if (stateMachine.IsInState<LeaderCommandState>() || 
                    stateMachine.IsInState<LeaderHealState>() ||
                    stateMachine.IsInState<LeaderRecruitState>())
                {
                    stateMachine.ChangeState<LeaderAttackState>();
                }
            }
            else
            {
                // Regular unit: Report to leader if in squad, otherwise attack directly
                if (currentSquad != null && squadRole == SquadRole.Member)
                {
                    // Report enemy contact to leader
                    currentSquad.ReportEnemyContact(this, closestEnemy);
                    
                    // Don't change state - wait for leader's orders
                }
                else if (squadRole == SquadRole.Independent)
                {
                    // Independent unit: attack directly
                    if (stateMachine.IsInState<UnitFollowState>() || 
                        stateMachine.IsInState<UnitRecruitState>())
                    {
                        stateMachine.ChangeState<UnitAttackState>();
                    }
                }
            }
        }
    }
    
    private Base_Unit FindNearbyEnemyForRetaliation()
    {
        // Search for enemies in a larger radius without vision restrictions
        float searchRadius = VisionRange * 1.5f; // Larger search when under attack
        Collider[] nearbyColliders = Physics.OverlapSphere(transform.position, searchRadius);
        
        Base_Unit closestEnemy = null;
        float closestDistance = float.MaxValue;
        
        foreach (Collider col in nearbyColliders)
        {
            Base_Unit unit = col.GetComponent<Base_Unit>();
            if (unit != null && unit.faction != this.faction && unit.isAlive)
            {
                float distance = Vector3.Distance(transform.position, unit.transform.position);
                if (distance < closestDistance)
                {
                    closestDistance = distance;
                    closestEnemy = unit;
                }
            }
        }
        
        return closestEnemy;
    }
    
    public virtual void Heal(int healAmount)
    {
        if (!isAlive) return;
        currentHealth = Mathf.Min(maxHealth, currentHealth + healAmount);
    }
    
    protected virtual void Die()
    {
        if (currentSquad != null)
        {
            if (squadRole == SquadRole.Leader)
                currentSquad.RemoveLeader();
            else
                currentSquad.RemoveMember(this);
        }
        
        Destroy(gameObject);
    }
    
    public virtual void Attack(Base_Unit target)
    {
        if (!canAttack || target == null || !target.isAlive) return;
        
        target.TakeDamage(attackDamage);
        lastAttackTime = Time.time;
    }
    
    public virtual bool CanSeeTarget(Base_Unit target)
    {
        if (target == null || !target.isAlive) return false;
        
        Vector3 directionToTarget = target.transform.position - transform.position;
        float distanceToTarget = directionToTarget.magnitude;
        
        if (distanceToTarget > VisionRange) return false;
        
        float angleToTarget = Vector3.Angle(transform.forward, directionToTarget);
        if (angleToTarget > visionAngle * 0.5f) return false;
        
        RaycastHit hit;
        if (Physics.Raycast(transform.position + Vector3.up * 0.5f, directionToTarget.normalized, 
                           out hit, distanceToTarget, obstacleLayerMask))
        {
            return false;
        }
        
        return true;
    }
    
    public virtual bool IsInAttackRange(Base_Unit target)
    {
        if (target == null || !target.isAlive) return false;
        return Vector3.Distance(transform.position, target.transform.position) <= attackRange;
    }
    
    public virtual List<Base_Unit> GetVisibleEnemies()
    {
        List<Base_Unit> enemies = new List<Base_Unit>();
        Base_Unit[] allUnits = FindObjectsOfType<Base_Unit>();
        
        foreach (Base_Unit unit in allUnits)
        {
            if (unit.faction != this.faction && unit.isAlive && CanSeeTarget(unit))
            {
                enemies.Add(unit);
            }
        }
        
        return enemies;
    }
    
    public virtual Base_Unit FindClosestEnemy()
    {
        List<Base_Unit> enemies = GetVisibleEnemies();
        if (enemies.Count == 0) return null;
        
        Base_Unit closest = null;
        float closestDistance = float.MaxValue;
        
        foreach (Base_Unit enemy in enemies)
        {
            float distance = Vector3.Distance(transform.position, enemy.transform.position);
            if (distance < closestDistance)
            {
                closestDistance = distance;
                closest = enemy;
            }
        }
        
        return closest;
    }
    
    // Alias for backward compatibility
    public virtual Base_Unit GetClosestEnemy()
    {
        return FindClosestEnemy();
    }
    
    // Additional helper methods
    public virtual bool HasEnemiesInSight()
    {
        return GetVisibleEnemies().Count > 0;
    }
    
    public virtual Base_Unit FindRecruitable()
    {
        if (currentSquad == null || currentSquad.IsFull) return null;
        
        Base_Unit[] allUnits = FindObjectsOfType<Base_Unit>();
        
        foreach (Base_Unit unit in allUnits)
        {
            if (unit == this || unit.faction != this.faction || !unit.isAlive) continue;
            if (unit.squadRole != SquadRole.Independent) continue;
            
            float distance = Vector3.Distance(transform.position, unit.transform.position);
            if (distance <= currentSquad.recruitmentRange)
            {
                return unit;
            }
        }
        
        return null;
    }
    
    public virtual void SetSquad(Squad squad, SquadRole role)
    {
        currentSquad = squad;
        squadRole = role;
    }
    
    private float lastHealTime = 0f;
    private float healInterval = 1f; // Heal every 1 second
    
    public virtual void HealNearbyAllies()
    {
        if (unitType != UnitType.Leader) return;
        
        // Only heal every 1 second
        if (Time.time < lastHealTime + healInterval) return;
        lastHealTime = Time.time;
        
        // Heal nearby friendly units (both squad members and nearby independents)
        Base_Unit[] allUnits = Object.FindObjectsOfType<Base_Unit>();
        
        foreach (Base_Unit unit in allUnits)
        {
            if (unit == this || !unit.isAlive) continue;
            if (unit.faction != this.faction) continue; // Only heal same faction
            
            float distance = Vector3.Distance(transform.position, unit.transform.position);
            if (distance <= healingRange)
            {
                // Heal a substantial amount every second
                int healAmount = Mathf.RoundToInt(healingRate * 0.5f); // Half the healing rate per second
                if (healAmount < 1) healAmount = 1; // Minimum 1 HP per second
                unit.Heal(healAmount);
            }
        }
    }
    
    protected virtual void OnDrawGizmosSelected()
    {
        // Vision range
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, VisionRange);
        
        // Attack range
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, attackRange);
        
        // Optimal combat distance
        Gizmos.color = Color.magenta;
        Gizmos.DrawWireSphere(transform.position, optimalCombatDistance);
        
        // Healing range (leaders only)
        if (unitType == UnitType.Leader)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(transform.position, healingRange);
        }
        
        // Vision cone
        Gizmos.color = Color.cyan;
        Vector3 leftBoundary = Quaternion.Euler(0, -visionAngle * 0.5f, 0) * transform.forward * VisionRange;
        Vector3 rightBoundary = Quaternion.Euler(0, visionAngle * 0.5f, 0) * transform.forward * VisionRange;
        
        Gizmos.DrawLine(transform.position, transform.position + leftBoundary);
        Gizmos.DrawLine(transform.position, transform.position + rightBoundary);
    }
}
