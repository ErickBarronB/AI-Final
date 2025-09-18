using UnityEngine;
using UnityEngine.SceneManagement;

public class MenuManager : MonoBehaviour
{
    [Header("Menu Panels")]
    [SerializeField] private GameObject mainMenuPanel;
    [SerializeField] private GameObject pauseMenuPanel;
    [SerializeField] private GameObject gameUI;
    
    [Header("Win Screen Panels")]
    [SerializeField] private GameObject redTeamWinPanel;
    [SerializeField] private GameObject blueTeamWinPanel;
    [SerializeField] private GameObject tieWinPanel;
    
    [Header("Main Menu Buttons")]
    [SerializeField] private UnityEngine.UI.Button playButton;
    [SerializeField] private UnityEngine.UI.Button exitButton;
    
    [Header("Pause Menu Buttons")]
    [SerializeField] private UnityEngine.UI.Button resumeButton;
    [SerializeField] private UnityEngine.UI.Button mainMenuButton;
    [SerializeField] private UnityEngine.UI.Button quitButton;
    
    [Header("Win Screen Buttons")]
    [SerializeField] private UnityEngine.UI.Button playAgainButton;
    [SerializeField] private UnityEngine.UI.Button mainMenuWinButton;
    [SerializeField] private UnityEngine.UI.Button quitWinButton;
    
    [Header("Settings")]
    [SerializeField] private bool pauseGameOnWin = true;
    [SerializeField] private bool hideOtherPanelsOnWin = true;
    
    private bool isPaused = false;
    private bool gameStarted = false;
    private bool gameEnded = false;
    
    public static MenuManager Instance { get; private set; }
    
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
        SetupButtons();
        ShowMainMenu();
        
        PauseGameAtStart();
    }
    
    void PauseGameAtStart()
    {
        Time.timeScale = 0f;
        gameStarted = false;
        gameEnded = false;
        isPaused = false;
        
        if (GameManager.Instance != null)
        {
        }
        
        Debug.Log("Juego pausado al inicio - Presiona Play para comenzar");
    }
    
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape) && gameStarted && !gameEnded)
        {
            if (isPaused)
            {
                ResumeGame();
            }
            else
            {
                PauseGame();
            }
        }
    }
    
    void SetupButtons()
    {
        if (playButton != null)
            playButton.onClick.AddListener(StartGame);
        if (exitButton != null)
            exitButton.onClick.AddListener(QuitGame);
        
        if (resumeButton != null)
            resumeButton.onClick.AddListener(ResumeGame);
        if (mainMenuButton != null)
            mainMenuButton.onClick.AddListener(ReturnToMainMenu);
        if (quitButton != null)
            quitButton.onClick.AddListener(QuitGame);
        
        if (playAgainButton != null)
            playAgainButton.onClick.AddListener(PlayAgain);
        if (mainMenuWinButton != null)
            mainMenuWinButton.onClick.AddListener(ReturnToMainMenu);
        if (quitWinButton != null)
            quitWinButton.onClick.AddListener(QuitGame);
    }
    
    
    public void ShowMainMenu()
    {
        HideAllPanels();
        if (mainMenuPanel != null)
            mainMenuPanel.SetActive(true);
        
        isPaused = false;
        gameStarted = false;
        gameEnded = false;
        Time.timeScale = 0f;
        
        Debug.Log("Mostrando menú principal");
    }
    
    public void StartGame()
    {
        HideAllPanels();
        if (gameUI != null)
            gameUI.SetActive(true);
        
        isPaused = false;
        gameStarted = true;
        gameEnded = false;
        Time.timeScale = 1f;
        
        Debug.Log("¡Juego iniciado!");
    }
    
    
    public void PauseGame()
    {
        if (!gameStarted || gameEnded) return;
        
        HideAllPanels();
        if (pauseMenuPanel != null)
            pauseMenuPanel.SetActive(true);
        
        isPaused = true;
        Time.timeScale = 0f;
        
        Debug.Log("Juego pausado");
    }
    
    public void ResumeGame()
    {
        if (!gameStarted) return;
        
        HideAllPanels();
        if (gameUI != null)
            gameUI.SetActive(true);
        
        isPaused = false;
        Time.timeScale = 1f;
        
        Debug.Log("Juego reanudado");
    }
    
    
    public void ShowRedTeamWin()
    {
        ShowWinScreen(redTeamWinPanel);
        Debug.Log("¡Equipo Rojo Gana!");
    }
    
    public void ShowBlueTeamWin()
    {
        ShowWinScreen(blueTeamWinPanel);
        Debug.Log("¡Equipo Azul Gana!");
    }
    
    public void ShowTieWin()
    {
        ShowWinScreen(tieWinPanel);
        Debug.Log("¡Empate!");
    }
    
    void ShowWinScreen(GameObject winPanel)
    {
        if (winPanel == null)
        {
            Debug.LogWarning("Panel de victoria no asignado!");
            return;
        }
        
        if (hideOtherPanelsOnWin)
        {
            HideAllPanels();
        }
        
        winPanel.SetActive(true);
        
        if (pauseGameOnWin)
        {
            Time.timeScale = 0f;
        }
        
        gameEnded = true;
        isPaused = false;
    }
    
    public void ShowWinScreen(string winner)
    {
        switch (winner.ToLower())
        {
            case "equipo rojo":
            case "red team":
            case "rojo":
                ShowRedTeamWin();
                break;
            case "equipo azul":
            case "blue team":
            case "azul":
                ShowBlueTeamWin();
                break;
            case "empate":
            case "tie":
            case "draw":
                ShowTieWin();
                break;
            default:
                Debug.LogWarning($"Ganador no reconocido: {winner}");
                break;
        }
    }
    
    public void ShowWinScreen(Faction winningFaction)
    {
        switch (winningFaction)
        {
            case Faction.Red:
                ShowRedTeamWin();
                break;
            case Faction.Blue:
                ShowBlueTeamWin();
                break;
            default:
                ShowTieWin();
                break;
        }
    }
    
    
    public void PlayAgain()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
        
        Debug.Log("Reiniciando juego...");
    }
    
    public void RestartGame()
    {
        Time.timeScale = 1f;
        gameStarted = false;
        gameEnded = false;
        isPaused = false;
        
        if (GameManager.Instance != null)
        {
            GameManager.Instance.RestartGame();
        }
        
        ShowMainMenu();
        
        Debug.Log("Juego reiniciado");
    }
    
    public void ReturnToMainMenu()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
        
        Debug.Log("Volviendo al menú principal - Reiniciando escena");
    }
    
    public void QuitGame()
    {
        Debug.Log("Saliendo del juego...");
        Application.Quit();
        
        #if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
        #endif
    }
    
    
    void HideAllPanels()
    {
        if (mainMenuPanel != null)
            mainMenuPanel.SetActive(false);
        if (pauseMenuPanel != null)
            pauseMenuPanel.SetActive(false);
        if (gameUI != null)
            gameUI.SetActive(false);
        if (redTeamWinPanel != null)
            redTeamWinPanel.SetActive(false);
        if (blueTeamWinPanel != null)
            blueTeamWinPanel.SetActive(false);
        if (tieWinPanel != null)
            tieWinPanel.SetActive(false);
    }
    
    
    public bool IsGamePaused()
    {
        return isPaused;
    }
    
    public bool IsGameStarted()
    {
        return gameStarted;
    }
    
    public bool IsGameEnded()
    {
        return gameEnded;
    }
    
    
    public void SetPauseGameOnWin(bool pause)
    {
        pauseGameOnWin = pause;
    }
    
    public void SetHideOtherPanelsOnWin(bool hide)
    {
        hideOtherPanelsOnWin = hide;
    }
    
    
    [ContextMenu("Test Red Win")]
    public void TestRedWin()
    {
        ShowRedTeamWin();
    }
    
    [ContextMenu("Test Blue Win")]
    public void TestBlueWin()
    {
        ShowBlueTeamWin();
    }
    
    [ContextMenu("Test Tie")]
    public void TestTie()
    {
        ShowTieWin();
    }
    
    [ContextMenu("Test Pause")]
    public void TestPause()
    {
        if (gameStarted)
            PauseGame();
    }
}
