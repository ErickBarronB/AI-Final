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
    
    public void CreateInitialSquads()
    {
        for (int i = 0; i < initialSquadsPerFaction; i++)
        {
            CreateSquad(Faction.Red, redSpawnArea + Random.insideUnitSphere * spawnRadius);
        }
        
        for (int i = 0; i < initialSquadsPerFaction; i++)
        {
            CreateSquad(Faction.Blue, blueSpawnArea + Random.insideUnitSphere * spawnRadius);
        }
    }
    
    void CreateSquad(Faction faction, Vector3 position)
    {
        GameObject leaderPrefab = faction == Faction.Red ? teamALeaderPrefab : teamBLeaderPrefab;
        GameObject unitPrefab = faction == Faction.Red ? teamAUnitPrefab : teamBUnitPrefab;
        
        if (leaderPrefab == null || unitPrefab == null)
        {
            Debug.LogError($"Missing prefabs for {faction} faction! Please assign all prefab references in SquadManager.");
            return;
        }
        
        GameObject squadGO = new GameObject($"Squad_{faction}_{allSquads.Count}");
        Squad squad = squadGO.AddComponent<Squad>();
        squad.maxSquadSize = unitsPerSquad;
        
        GameObject leaderGO = Instantiate(leaderPrefab, position, Quaternion.identity);
        Base_Unit leader = leaderGO.GetComponent<Base_Unit>();
        leader.faction = faction;
        leader.unitType = UnitType.Leader;
        leaderGO.name = $"Leader_{faction}_{allSquads.Count}";
        
        leaderGO.AddComponent<LeaderAI>();
        
        leaderGO.AddComponent<UISpawner>();
        
        squad.SetLeader(leader);
        
        for (int i = 0; i < unitsPerSquad - 1; i++)
        {
            Vector3 memberPos = position + Random.insideUnitSphere * 3f;
            memberPos.y = 0f;
            
            GameObject memberGO = Instantiate(unitPrefab, memberPos, Quaternion.identity);
            Base_Unit member = memberGO.GetComponent<Base_Unit>();
            member.faction = faction;
            member.unitType = UnitType.Base;
            memberGO.name = $"Unit_{faction}_{allSquads.Count}_{i}";
            
            memberGO.AddComponent<UnitAI>();
            
            memberGO.AddComponent<UISpawner>();
            
            squad.TryAddMember(member);
        }
        
        allSquads.Add(squad);
    }
    
    void Update()
    {
        allSquads.RemoveAll(squad => squad == null);
    }
    
    void OnDrawGizmos()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(redSpawnArea, spawnRadius);
        
        Gizmos.color = Color.blue;
        Gizmos.DrawWireSphere(blueSpawnArea, spawnRadius);
    }
}
