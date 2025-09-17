using System.Collections.Generic;
using UnityEngine;

public class SteeringBehaviors : MonoBehaviour
{
    [Header("Movement")]
    [SerializeField] private float maxSpeed = 5f;
    [SerializeField] private float maxForce = 3f;
    [SerializeField] private float rotationSpeed = 5f;
    
    [Header("Obstacle Avoidance")]
    [SerializeField] private float avoidanceRadius = 2f;
    [SerializeField] private LayerMask obstacleLayerMask = 1;
    
    [Header("Flocking")]
    [SerializeField] private float flockRadius = 5f;
    [SerializeField] private float separationWeight = 1.5f;
    [SerializeField] private float alignmentWeight = 1f;
    [SerializeField] private float cohesionWeight = 1f;
    
    private Vector3 velocity;
    private Base_Unit unit;
    
    void Awake()
    {
        unit = GetComponent<Base_Unit>();
    }
    
    public Vector3 GetVelocity() => velocity;
    
    [Header("Pathfinding")]
    [SerializeField] private float pathfindingDistance = 10f; // Use pathfinding for distances greater than this
    private List<Vector3> currentPath;
    private int currentPathIndex;
    private float lastPathUpdate;
    private Vector3 lastPathTarget;
    
    public void MoveTo(Vector3 targetPosition)
    {
        targetPosition.y = transform.position.y;
        float distanceToTarget = Vector3.Distance(transform.position, targetPosition);
        
        Vector3 steering = Vector3.zero;
        
        // Use pathfinding for longer distances, direct steering for short distances
        if (distanceToTarget > pathfindingDistance && AStarPlus.Instance != null)
        {
            Vector3 nextWaypoint = GetNextPathWaypoint(targetPosition);
            steering += Seek(nextWaypoint) * 1f;
        }
        else
        {
            // Direct movement for close targets
            steering += Seek(targetPosition) * 1f;
        }
        
        steering += ObstacleAvoidance() * 2f;
        steering += SquadFlocking() * 0.8f;
        
        ApplySteering(steering);
    }
    
    public void Flee(Vector3 dangerPosition)
    {
        dangerPosition.y = transform.position.y;
        
        Vector3 steering = Vector3.zero;
        steering += Seek(dangerPosition) * -1.5f;
        steering += ObstacleAvoidance() * 2f;
        steering += SquadFlocking() * 0.5f;
        
        ApplySteering(steering);
    }
    
    public void Stop()
    {
        velocity = Vector3.Lerp(velocity, Vector3.zero, Time.deltaTime * 3f);
        
        if (velocity.magnitude > 0.1f)
        {
            Vector3 horizontalVelocity = new Vector3(velocity.x, 0, velocity.z);
            transform.position += horizontalVelocity * Time.deltaTime;
        }
    }
    
    void ApplySteering(Vector3 steering)
    {
        velocity += steering * Time.deltaTime;
        velocity = Vector3.ClampMagnitude(velocity, maxSpeed);
        
        if (velocity.magnitude > 0.1f)
        {
            Vector3 horizontalVelocity = new Vector3(velocity.x, 0, velocity.z);
            transform.position += horizontalVelocity * Time.deltaTime;
            
            if (horizontalVelocity.magnitude > 0.1f)
            {
                Quaternion targetRotation = Quaternion.LookRotation(horizontalVelocity.normalized);
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * rotationSpeed);
            }
        }
    }
    
    Vector3 Seek(Vector3 targetPosition)
    {
        Vector3 desired = (targetPosition - transform.position).normalized * maxSpeed;
        Vector3 steering = desired - velocity;
        return Vector3.ClampMagnitude(steering, maxForce);
    }
    
    Vector3 ObstacleAvoidance()
    {
        Vector3 avoidance = Vector3.zero;
        
        Vector3[] rayDirections = {
            transform.forward,
            transform.forward + transform.right * 0.5f,
            transform.forward - transform.right * 0.5f
        };
        
        foreach (Vector3 direction in rayDirections)
        {
            RaycastHit hit;
            if (Physics.Raycast(transform.position, direction, out hit, avoidanceRadius, obstacleLayerMask))
            {
                Vector3 avoidDirection = Vector3.Cross(Vector3.up, direction);
                if (Vector3.Dot(avoidDirection, transform.right) < 0)
                    avoidDirection = -avoidDirection;
                
                float force = (avoidanceRadius - hit.distance) / avoidanceRadius;
                avoidance += avoidDirection * force * maxForce;
            }
        }
        
        return avoidance;
    }
    
    Vector3 SquadFlocking()
    {
        if (unit == null || unit.CurrentSquad == null) return Vector3.zero;
        
        List<Base_Unit> squadMembers = GetSquadMembers();
        if (squadMembers.Count == 0) return Vector3.zero;
        
        Vector3 separation = Separation(squadMembers);
        Vector3 alignment = Alignment(squadMembers);
        Vector3 cohesion = Cohesion(squadMembers);
        
        return separation * separationWeight + alignment * alignmentWeight + cohesion * cohesionWeight;
    }
    
    Vector3 Separation(List<Base_Unit> squadMembers)
    {
        Vector3 steering = Vector3.zero;
        int count = 0;
        
        foreach (Base_Unit member in squadMembers)
        {
            if (member == unit) continue;
            
            float distance = Vector3.Distance(transform.position, member.transform.position);
            if (distance > 0 && distance < flockRadius * 0.5f)
            {
                Vector3 diff = transform.position - member.transform.position;
                diff.Normalize();
                diff /= distance;
                steering += diff;
                count++;
            }
        }
        
        if (count > 0)
        {
            steering /= count;
            steering = steering.normalized * maxSpeed - velocity;
            steering = Vector3.ClampMagnitude(steering, maxForce);
        }
        
        return steering;
    }
    
    Vector3 Alignment(List<Base_Unit> squadMembers)
    {
        Vector3 sum = Vector3.zero;
        int count = 0;
        
        foreach (Base_Unit member in squadMembers)
        {
            if (member == unit) continue;
            
            SteeringBehaviors memberBehavior = member.GetComponent<SteeringBehaviors>();
            if (memberBehavior != null)
            {
                sum += memberBehavior.GetVelocity();
                count++;
            }
        }
        
        if (count > 0)
        {
            sum /= count;
            sum = sum.normalized * maxSpeed;
            Vector3 steering = sum - velocity;
            return Vector3.ClampMagnitude(steering, maxForce);
        }
        
        return Vector3.zero;
    }
    
    Vector3 Cohesion(List<Base_Unit> squadMembers)
    {
        Vector3 sum = Vector3.zero;
        int count = 0;
        
        foreach (Base_Unit member in squadMembers)
        {
            if (member == unit) continue;
            
            sum += member.transform.position;
            count++;
        }
        
        if (count > 0)
        {
            sum /= count;
            return Seek(sum);
        }
        
        return Vector3.zero;
    }
    
    List<Base_Unit> GetSquadMembers()
    {
        List<Base_Unit> members = new List<Base_Unit>();
        
        if (unit.CurrentSquad != null)
        {
            if (unit.CurrentSquad.Leader != null && unit.CurrentSquad.Leader != unit)
            {
                float distance = Vector3.Distance(transform.position, unit.CurrentSquad.Leader.transform.position);
                if (distance <= flockRadius)
                    members.Add(unit.CurrentSquad.Leader);
            }
            
            foreach (Base_Unit member in unit.CurrentSquad.Members)
            {
                if (member != unit)
                {
                    float distance = Vector3.Distance(transform.position, member.transform.position);
                    if (distance <= flockRadius)
                        members.Add(member);
                }
            }
        }
        
        return members;
    }
    
    Vector3 GetNextPathWaypoint(Vector3 targetPosition)
    {
        // Check if we need to recalculate path
        bool needNewPath = currentPath == null || 
                          currentPath.Count == 0 || 
                          Vector3.Distance(lastPathTarget, targetPosition) > 2f ||
                          Time.time > lastPathUpdate + 1f; // Recalculate every second
        
        if (needNewPath)
        {
            currentPath = AStarPlus.Instance.FindPath(transform.position, targetPosition);
            currentPathIndex = 0;
            lastPathUpdate = Time.time;
            lastPathTarget = targetPosition;
        }
        
        // Get next waypoint from path
        if (currentPath != null && currentPath.Count > 0)
        {
            // Check if we've reached current waypoint
            if (currentPathIndex < currentPath.Count)
            {
                Vector3 currentWaypoint = currentPath[currentPathIndex];
                float distanceToWaypoint = Vector3.Distance(transform.position, currentWaypoint);
                
                if (distanceToWaypoint < 1.5f && currentPathIndex < currentPath.Count - 1)
                {
                    currentPathIndex++;
                }
                
                if (currentPathIndex < currentPath.Count)
                    return currentPath[currentPathIndex];
            }
        }
        
        // Fallback to direct movement if pathfinding fails
        return targetPosition;
    }
}
