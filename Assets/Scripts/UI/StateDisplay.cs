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
        
        if (canvas != null)
        {
            canvas.worldCamera = mainCamera;
            canvas.sortingOrder = 101;
        }
    }
    
    void Start()
    {
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
        
        canvas.gameObject.SetActive(true);
    }
    
    void FaceCamera()
    {
        if (mainCamera != null && canvas != null)
        {
            Vector3 directionToCamera = mainCamera.transform.position - transform.position;
            directionToCamera.y = 0;
            
            if (directionToCamera != Vector3.zero)
            {
                transform.rotation = Quaternion.LookRotation(-directionToCamera);
            }
        }
    }
}
