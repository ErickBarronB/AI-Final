using System.Collections.Generic;
using UnityEngine;

public class SquadManager : MonoBehaviour
{
    [Header("Squad Settings")]
    public int initialSquadsPerFaction = 2;
    public int unitsPerSquad = 4;
    
    [Header("Team A Prefabs (Red)")]
    public GameObject teamALeaderPrefab;
    public GameObject teamAUnitPrefab;
    
    [Header("Team B Prefabs (Blue)")]
    public GameObject teamBLeaderPrefab;
    public GameObject teamBUnitPrefab;
    
    [Header("Spawn Settings")]
    public Vector3 redSpawnArea = new Vector3(-20, 0, 0);
    public Vector3 blueSpawnArea = new Vector3(20, 0, 0);
    public float spawnRadius = 5f;
    
    private List<Squad> allSquads = new List<Squad>();
    
    void Start()
    {
        CreateInitialSquads();
    }
    
    void CreateInitialSquads()
    {
        // Create Red squads
        for (int i = 0; i < initialSquadsPerFaction; i++)
        {
            CreateSquad(Faction.Red, redSpawnArea + Random.insideUnitSphere * spawnRadius);
        }
        
        // Create Blue squads
        for (int i = 0; i < initialSquadsPerFaction; i++)
        {
            CreateSquad(Faction.Blue, blueSpawnArea + Random.insideUnitSphere * spawnRadius);
        }
    }
    
    void CreateSquad(Faction faction, Vector3 position)
    {
        // Get the correct prefabs for this faction
        GameObject leaderPrefab = faction == Faction.Red ? teamALeaderPrefab : teamBLeaderPrefab;
        GameObject unitPrefab = faction == Faction.Red ? teamAUnitPrefab : teamBUnitPrefab;
        
        // Validate prefabs
        if (leaderPrefab == null || unitPrefab == null)
        {
            Debug.LogError($"Missing prefabs for {faction} faction! Please assign all prefab references in SquadManager.");
            return;
        }
        
        // Create squad GameObject
        GameObject squadGO = new GameObject($"Squad_{faction}_{allSquads.Count}");
        Squad squad = squadGO.AddComponent<Squad>();
        squad.maxSquadSize = unitsPerSquad; // Set the correct max size
        
        // Create leader
        GameObject leaderGO = Instantiate(leaderPrefab, position, Quaternion.identity);
        Base_Unit leader = leaderGO.GetComponent<Base_Unit>();
        leader.faction = faction;
        leader.unitType = UnitType.Leader;
        leaderGO.name = $"Leader_{faction}_{allSquads.Count}";
        
        // Add LeaderAI component
        leaderGO.AddComponent<LeaderAI>();
        
        // Add UI spawner for health bar and squad counter
        leaderGO.AddComponent<UISpawner>();
        
        squad.SetLeader(leader);
        
        // Create squad members
        for (int i = 0; i < unitsPerSquad - 1; i++) // -1 because leader counts as one
        {
            Vector3 memberPos = position + Random.insideUnitSphere * 3f;
            memberPos.y = 0f;
            
            GameObject memberGO = Instantiate(unitPrefab, memberPos, Quaternion.identity);
            Base_Unit member = memberGO.GetComponent<Base_Unit>();
            member.faction = faction;
            member.unitType = UnitType.Base;
            memberGO.name = $"Unit_{faction}_{allSquads.Count}_{i}";
            
            // Add UnitAI component
            memberGO.AddComponent<UnitAI>();
            
            // Add UI spawner for health bar
            memberGO.AddComponent<UISpawner>();
            
            squad.TryAddMember(member);
        }
        
        allSquads.Add(squad);
    }
    
    void Update()
    {
        // Clean up destroyed squads
        allSquads.RemoveAll(squad => squad == null);
    }
    
    void OnDrawGizmos()
    {
        // Draw spawn areas
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(redSpawnArea, spawnRadius);
        
        Gizmos.color = Color.blue;
        Gizmos.DrawWireSphere(blueSpawnArea, spawnRadius);
    }
}
