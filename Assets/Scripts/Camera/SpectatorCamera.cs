using UnityEngine;

public class SpectatorCamera : MonoBehaviour
{
    [Header("Movement Settings")]
    [SerializeField] private float moveSpeed = 10f;
    [SerializeField] private float fastMoveSpeed = 20f;
    [SerializeField] private float smoothTime = 0.1f;
    
    [Header("Mouse Look Settings")]
    [SerializeField] private float mouseSensitivity = 2f;
    [SerializeField] private bool invertY = false;
    
    [Header("Zoom Settings")]
    [SerializeField] private float zoomSpeed = 10f;
    [SerializeField] private float minZoomDistance = 2f;
    [SerializeField] private float maxZoomDistance = 50f;
    
    [Header("Pan Settings")]
    [SerializeField] private float panSpeed = 2f;
    
    [Header("Focus Settings")]
    [SerializeField] private float focusSpeed = 5f;
    
    private Vector3 velocity = Vector3.zero;
    private Vector3 targetPosition;
    private float xRotation = 0f;
    private float yRotation = 0f;
    private bool isMouseLookActive = false;
    private bool isPanning = false;
    private Vector3 lastMousePosition;
    
    private Camera cam;
    
    void Start()
    {
        cam = GetComponent<Camera>();
        targetPosition = transform.position;
        
        Vector3 currentRotation = transform.eulerAngles;
        xRotation = currentRotation.x;
        yRotation = currentRotation.y;
        
        Cursor.lockState = CursorLockMode.None;
    }
    
    void Update()
    {
        HandleInput();
        HandleMovement();
        HandleMouseLook();
        HandlePanning();
        HandleZoom();
        HandleFocus();
    }
    
    void HandleInput()
    {
        if (Input.GetKey(KeyCode.LeftAlt) && Input.GetMouseButton(0))
        {
            if (!isMouseLookActive)
            {
                isMouseLookActive = true;
                Cursor.lockState = CursorLockMode.Locked;
            }
        }
        else
        {
            if (isMouseLookActive)
            {
                isMouseLookActive = false;
                Cursor.lockState = CursorLockMode.None;
            }
        }
        
        if (Input.GetMouseButtonDown(2))
        {
            isPanning = true;
            lastMousePosition = Input.mousePosition;
        }
        else if (Input.GetMouseButtonUp(2))
        {
            isPanning = false;
        }
    }
    
    void HandleMovement()
    {
        Vector3 inputDirection = Vector3.zero;
        
        if (Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.UpArrow))
            inputDirection += transform.forward;
        if (Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.DownArrow))
            inputDirection -= transform.forward;
        if (Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.LeftArrow))
            inputDirection -= transform.right;
        if (Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow))
            inputDirection += transform.right;
        if (Input.GetKey(KeyCode.Q))
            inputDirection -= transform.up;
        if (Input.GetKey(KeyCode.E))
            inputDirection += transform.up;
        
        
        float currentSpeed = Input.GetKey(KeyCode.LeftShift) ? fastMoveSpeed : moveSpeed;
        
        if (inputDirection != Vector3.zero)
        {
            targetPosition += inputDirection.normalized * currentSpeed * Time.deltaTime;
        }
        
        
        transform.position = Vector3.SmoothDamp(transform.position, targetPosition, ref velocity, smoothTime);
    }
    
    void HandleMouseLook()
    {
        if (!isMouseLookActive) return;
        
        
        float mouseX = Input.GetAxis("Mouse X") * mouseSensitivity;
        float mouseY = Input.GetAxis("Mouse Y") * mouseSensitivity;
        
        
        if (invertY)
            mouseY = -mouseY;
        
        
        yRotation += mouseX;
        
        
        xRotation -= mouseY;
        xRotation = Mathf.Clamp(xRotation, -90f, 90f);
        
        
        transform.rotation = Quaternion.Euler(xRotation, yRotation, 0f);
    }
    
    void HandlePanning()
    {
        if (!isPanning) return;
        
        Vector3 mouseDelta = Input.mousePosition - lastMousePosition;
        
        
        Vector3 panDirection = (-transform.right * mouseDelta.x + -transform.up * mouseDelta.y) * panSpeed * Time.deltaTime;
        
        targetPosition += panDirection;
        lastMousePosition = Input.mousePosition;
    }
    
    void HandleZoom()
    {
        
        float scroll = Input.GetAxis("Mouse ScrollWheel");
        
        if (Mathf.Abs(scroll) > 0.01f)
        {
            Vector3 zoomDirection = transform.forward * scroll * zoomSpeed;
            targetPosition += zoomDirection;
            
            
            float distanceFromOrigin = Vector3.Distance(targetPosition, Vector3.zero);
            if (distanceFromOrigin < minZoomDistance)
            {
                targetPosition = Vector3.zero + (targetPosition - Vector3.zero).normalized * minZoomDistance;
            }
            else if (distanceFromOrigin > maxZoomDistance)
            {
                targetPosition = Vector3.zero + (targetPosition - Vector3.zero).normalized * maxZoomDistance;
            }
        }
    }
    
    void HandleFocus()
    {
        
        if (Input.GetKeyDown(KeyCode.F))
        {
            FocusOnSelection();
        }
        
        
        if (Input.GetKeyDown(KeyCode.Home))
        {
            FocusOnPoint(Vector3.zero);
        }
    }
    
    void FocusOnSelection()
    {
        
        GameObject target = FindClosestUnit();
        
        if (target != null)
        {
            FocusOnPoint(target.transform.position);
        }
    }
    
    GameObject FindClosestUnit()
    {
        Base_Unit[] allUnits = FindObjectsOfType<Base_Unit>();
        if (allUnits.Length == 0) return null;
        
        GameObject closest = null;
        float closestDistance = float.MaxValue;
        
        foreach (Base_Unit unit in allUnits)
        {
            float distance = Vector3.Distance(transform.position, unit.transform.position);
            if (distance < closestDistance)
            {
                closestDistance = distance;
                closest = unit.gameObject;
            }
        }
        
        return closest;
    }
    
    void FocusOnPoint(Vector3 point)
    {
        
        Vector3 offset = transform.forward * -10f + Vector3.up * 5f;
        targetPosition = point + offset;
        
                                                        
        Vector3 direction = (point - targetPosition).normalized;
        if (direction != Vector3.zero)
        {
            Quaternion targetRotation = Quaternion.LookRotation(direction);
            Vector3 eulerAngles = targetRotation.eulerAngles;
            
            xRotation = eulerAngles.x;
            yRotation = eulerAngles.y;
            
            if (xRotation > 180f)
                xRotation -= 360f;
        }
    }
    
    void OnGUI()
    {
        if (Input.GetKey(KeyCode.H))
        {
            GUIStyle style = new GUIStyle();
            style.normal.textColor = Color.white;
            style.fontSize = 14;
            
            string helpText = "SPECTATOR CAMERA CONTROLS:\n\n" +
                             "WASD / Arrow Keys - Move\n" +
                             "QE - Move Up/Down\n" +
                             "Shift - Fast Movement\n" +
                             "Alt + Left Mouse - Look Around\n" +
                             "Middle Mouse - Pan\n" +
                             "Scroll Wheel - Zoom\n" +
                             "F - Focus on Closest Unit\n" +
                             "Home - Focus on Origin\n" +
                             "H - Show/Hide This Help";
            
            GUI.Label(new Rect(10, 10, 300, 300), helpText, style);
        }
        else
        {
            GUIStyle style = new GUIStyle();
            style.normal.textColor = Color.yellow;
            style.fontSize = 12;
            GUI.Label(new Rect(10, 10, 200, 20), "Hold H for Camera Controls", style);
        }
    }
}
