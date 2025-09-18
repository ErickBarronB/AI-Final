using UnityEngine;
using System.Collections.Generic;
using System.Linq;

public enum WinConditionType
{
    EliminateAll,
    EliminateHalf,
    EliminateLeaders,
    Custom
}

public class GameManager : MonoBehaviour
{
    [Header("Game Settings")]
    [SerializeField] private float gameTimeLimit = 300f;
    [SerializeField] private bool useTimeLimit = false;
    
    [Header("Win Conditions")]
    [SerializeField] private WinConditionType winConditionType = WinConditionType.EliminateAll;
    [SerializeField] private int redUnitsToWin = 0;
    [SerializeField] private int blueUnitsToWin = 0;
    
    [Header("Auto Detection")]
    [SerializeField] private bool autoDetectSpawnSettings = true;
    [SerializeField] private SquadManager squadManager;
    
    private float gameStartTime;
    private bool gameEnded = false;
    
    public static GameManager Instance { get; private set; }
    
    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }
    
    void Start()
    {
        gameStartTime = Time.time;
        
        if (autoDetectSpawnSettings)
        {
            AutoDetectSquadManager();
        }
        
        CalculateWinConditions();
    }
    
    void Update()
    {
        if (gameEnded) return;
        
        CheckWinConditions();
        CheckTimeLimit();
    }
    
    void CheckWinConditions()
    {
        Base_Unit[] allUnits = FindObjectsOfType<Base_Unit>();
        List<Base_Unit> redUnits = new List<Base_Unit>();
        List<Base_Unit> blueUnits = new List<Base_Unit>();
        List<Base_Unit> redLeaders = new List<Base_Unit>();
        List<Base_Unit> blueLeaders = new List<Base_Unit>();
        
        foreach (Base_Unit unit in allUnits)
        {
            if (unit.isAlive)
            {
                if (unit.faction == Faction.Red)
                {
                    redUnits.Add(unit);
                    if (unit.unitType == UnitType.Leader)
                        redLeaders.Add(unit);
                }
                else if (unit.faction == Faction.Blue)
                {
                    blueUnits.Add(unit);
                    if (unit.unitType == UnitType.Leader)
                        blueLeaders.Add(unit);
                }
            }
        }
        
        Faction winnerFaction = Faction.Red;
        bool gameWon = false;
        
        switch (winConditionType)
        {
            case WinConditionType.EliminateAll:
                gameWon = CheckEliminateAll(redUnits, blueUnits, out winnerFaction);
                break;
            case WinConditionType.EliminateHalf:
                gameWon = CheckEliminateHalf(redUnits, blueUnits, out winnerFaction);
                break;
            case WinConditionType.EliminateLeaders:
                gameWon = CheckEliminateLeaders(redLeaders, blueLeaders, out winnerFaction);
                break;
            case WinConditionType.Custom:
                gameWon = CheckCustomWinConditions(redUnits, blueUnits, out winnerFaction);
                break;
        }
        
        if (gameWon)
        {
            EndGame(winnerFaction);
        }
    }
    
    void CheckTimeLimit()
    {
        if (useTimeLimit && Time.time - gameStartTime >= gameTimeLimit)
        {
            Base_Unit[] allUnits = FindObjectsOfType<Base_Unit>();
            int redCount = allUnits.Count(u => u.isAlive && u.faction == Faction.Red);
            int blueCount = allUnits.Count(u => u.isAlive && u.faction == Faction.Blue);
            
            Faction winnerFaction;
            if (redCount > blueCount)
                winnerFaction = Faction.Red;
            else if (blueCount > redCount)
                winnerFaction = Faction.Blue;
            else
                winnerFaction = Faction.Red;
            
            EndGame(winnerFaction);
        }
    }
    
    void EndGame(Faction winnerFaction)
    {
        if (gameEnded) return;
        
        gameEnded = true;
        Debug.Log($"¡Juego terminado! Ganador: {winnerFaction}");
        
        if (MenuManager.Instance != null)
        {
            MenuManager.Instance.ShowWinScreen(winnerFaction);
        }
        else
        {
            Debug.LogWarning("MenuManager no encontrado! Asigna el script a un GameObject.");
        }
    }
    
    void EndGame(string winner)
    {
        if (gameEnded) return;
        
        gameEnded = true;
        Debug.Log($"¡Juego terminado! Ganador: {winner}");
        
        if (MenuManager.Instance != null)
        {
            MenuManager.Instance.ShowWinScreen(winner);
        }
        else
        {
            Debug.LogWarning("MenuManager no encontrado! Asigna el script a un GameObject.");
        }
    }
    
    public void SetTimeLimit(float timeLimit)
    {
        gameTimeLimit = timeLimit;
        useTimeLimit = true;
    }
    
    public void SetWinConditions(int redUnits, int blueUnits)
    {
        redUnitsToWin = redUnits;
        blueUnitsToWin = blueUnits;
    }
    
    public void RestartGame()
    {
        gameEnded = false;
        gameStartTime = Time.time;
    }
    
    public bool IsGameEnded()
    {
        return gameEnded;
    }
    
    public float GetGameTime()
    {
        return Time.time - gameStartTime;
    }
    
    public float GetTimeRemaining()
    {
        if (!useTimeLimit) return -1f;
        return Mathf.Max(0f, gameTimeLimit - GetGameTime());
    }
    
    void AutoDetectSquadManager()
    {
        if (squadManager == null)
        {
            squadManager = FindObjectOfType<SquadManager>();
            if (squadManager != null)
            {
                Debug.Log("SquadManager detectado automáticamente");
            }
            else
            {
                Debug.LogWarning("SquadManager no encontrado. Las condiciones de victoria se calcularán manualmente.");
            }
        }
    }
    
    void CalculateWinConditions()
    {
        if (squadManager == null) return;
        
        int totalRedUnits = squadManager.initialSquadsPerFaction * squadManager.unitsPerSquad;
        int totalBlueUnits = squadManager.initialSquadsPerFaction * squadManager.unitsPerSquad;
        
        switch (winConditionType)
        {
            case WinConditionType.EliminateHalf:
                redUnitsToWin = totalRedUnits / 2;
                blueUnitsToWin = totalBlueUnits / 2;
                break;
            case WinConditionType.EliminateLeaders:
                redUnitsToWin = squadManager.initialSquadsPerFaction;
                blueUnitsToWin = squadManager.initialSquadsPerFaction;
                break;
        }
        
        Debug.Log($"Condiciones de victoria calculadas: Red={redUnitsToWin}, Blue={blueUnitsToWin}");
    }
    
    bool CheckEliminateAll(List<Base_Unit> redUnits, List<Base_Unit> blueUnits, out Faction winner)
    {
        winner = Faction.Red;
        
        if (redUnits.Count == 0 && blueUnits.Count > 0)
        {
            winner = Faction.Blue;
            return true;
        }
        else if (blueUnits.Count == 0 && redUnits.Count > 0)
        {
            winner = Faction.Red;
            return true;
        }
        else if (redUnits.Count == 0 && blueUnits.Count == 0)
        {
            winner = Faction.Red;
            return true;
        }
        
        return false;
    }
    
    bool CheckEliminateHalf(List<Base_Unit> redUnits, List<Base_Unit> blueUnits, out Faction winner)
    {
        winner = Faction.Red;
        
        if (redUnits.Count <= redUnitsToWin && blueUnits.Count > blueUnitsToWin)
        {
            winner = Faction.Blue;
            return true;
        }
        else if (blueUnits.Count <= blueUnitsToWin && redUnits.Count > redUnitsToWin)
        {
            winner = Faction.Red;
            return true;
        }
        
        return false;
    }
    
    bool CheckEliminateLeaders(List<Base_Unit> redLeaders, List<Base_Unit> blueLeaders, out Faction winner)
    {
        winner = Faction.Red;
        
        if (redLeaders.Count == 0 && blueLeaders.Count > 0)
        {
            winner = Faction.Blue;
            return true;
        }
        else if (blueLeaders.Count == 0 && redLeaders.Count > 0)
        {
            winner = Faction.Red;
            return true;
        }
        else if (redLeaders.Count == 0 && blueLeaders.Count == 0)
        {
            winner = Faction.Red;
            return true;
        }
        
        return false;
    }
    
    bool CheckCustomWinConditions(List<Base_Unit> redUnits, List<Base_Unit> blueUnits, out Faction winner)
    {
        winner = Faction.Red;
        
        if (redUnits.Count <= redUnitsToWin && blueUnits.Count > blueUnitsToWin)
        {
            winner = Faction.Blue;
            return true;
        }
        else if (blueUnits.Count <= blueUnitsToWin && redUnits.Count > redUnitsToWin)
        {
            winner = Faction.Red;
            return true;
        }
        
        return false;
    }
    
    public void SetWinConditionType(WinConditionType type)
    {
        winConditionType = type;
        CalculateWinConditions();
    }
    
    public void SetCustomWinConditions(int redUnits, int blueUnits)
    {
        winConditionType = WinConditionType.Custom;
        redUnitsToWin = redUnits;
        blueUnitsToWin = blueUnits;
    }
    
    public WinConditionType GetWinConditionType()
    {
        return winConditionType;
    }
    
    public int GetRedUnitsToWin()
    {
        return redUnitsToWin;
    }
    
    public int GetBlueUnitsToWin()
    {
        return blueUnitsToWin;
    }
}
