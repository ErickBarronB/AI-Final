using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class StateDisplay : MonoBehaviour
{
    [Header("UI References")]
    public Canvas canvas;
    public TextMeshProUGUI stateText;
    
    [Header("Settings")]
    public Color textColor = Color.white;
    public float fontSize = 0.1f;
    
    private Base_Unit unit;
    private StateMachine stateMachine;
    private Camera mainCamera;
    
    void Awake()
    {
        unit = GetComponentInParent<Base_Unit>();
        mainCamera = Camera.main;
        
        // Set up canvas to face camera
        if (canvas != null)
        {
            canvas.worldCamera = mainCamera;
            canvas.sortingOrder = 101; // Above health bar
        }
    }
    
    void Start()
    {
        // Get the StateMachine component (it's added by AI controllers)
        if (unit != null)
            stateMachine = unit.GetComponent<StateMachine>();
        
        if (stateText != null)
        {
            stateText.text = "Initializing";
            stateText.color = textColor;
            stateText.fontSize = fontSize;
        }
        
        UpdateStateDisplay();
    }
    
    void Update()
    {
        UpdateStateDisplay();
        FaceCamera();
    }
    
    void UpdateStateDisplay()
    {
        if (stateMachine == null || stateText == null) return;
        
        string currentState = stateMachine.GetCurrentStateName();
        stateText.text = currentState;
        
        // Always show state display
        canvas.gameObject.SetActive(true);
    }
    
    void FaceCamera()
    {
        if (mainCamera != null && canvas != null)
        {
            Vector3 directionToCamera = mainCamera.transform.position - transform.position;
            directionToCamera.y = 0; // Keep state display upright
            
            if (directionToCamera != Vector3.zero)
            {
                transform.rotation = Quaternion.LookRotation(-directionToCamera);
            }
        }
    }
}
