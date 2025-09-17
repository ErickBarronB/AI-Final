using UnityEngine;
using UnityEngine.UI;

public class HealthBar : MonoBehaviour
{
    [Header("UI References")]
    public Canvas canvas;
    public Slider healthSlider;
    public Image fillImage;
    
    [Header("Colors")]
    public Color fullHealthColor = Color.green;
    public Color lowHealthColor = Color.red;
    
    private Base_Unit unit;
    private Camera mainCamera;
    
    void Awake()
    {
        unit = GetComponentInParent<Base_Unit>();
        mainCamera = Camera.main;
        
        // Set up canvas to face camera
        if (canvas != null)
        {
            canvas.worldCamera = mainCamera;
            canvas.sortingOrder = 100;
        }
    }
    
    void Start()
    {
        if (healthSlider != null)
        {
            healthSlider.maxValue = 1f;
            healthSlider.value = 1f;
        }
        
        UpdateHealthBar();
    }
    
    void Update()
    {
        UpdateHealthBar();
        FaceCamera();
    }
    
    void UpdateHealthBar()
    {
        if (unit == null || healthSlider == null) return;
        
        float healthPercent = unit.healthPercentage;
        healthSlider.value = healthPercent;
        
        // Update color based on health
        if (fillImage != null)
        {
            fillImage.color = Color.Lerp(lowHealthColor, fullHealthColor, healthPercent);
        }
        
        // Always show health bar
        canvas.gameObject.SetActive(true);
    }
    
    void FaceCamera()
    {
        if (mainCamera != null && canvas != null)
        {
            Vector3 directionToCamera = mainCamera.transform.position - transform.position;
            directionToCamera.y = 0; // Keep health bar upright
            
            if (directionToCamera != Vector3.zero)
            {
                transform.rotation = Quaternion.LookRotation(-directionToCamera);
            }
        }
    }
}
