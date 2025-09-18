using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class UISpawner : MonoBehaviour
{
    [Header("Settings")]
    public Vector3 healthBarOffset = new Vector3(0, 2f, 0);
    
    private Base_Unit unit;
    
    void Start()
    {
        StartCoroutine(DelayedUICreation());
    }
    
    System.Collections.IEnumerator DelayedUICreation()
    {
        yield return null;
        yield return null;
        yield return null;
        
        unit = GetComponent<Base_Unit>();
        if (unit != null)
        {
            SpawnUI();
        }
    }
    
    void SpawnUI()
    {
        if (unit == null) return;
        
        CreateUIFromCode();
    }
    
    void CreateUIFromCode()
    {
        CreateHealthBarFromCode();
        CreateStateDisplayFromCode();
        
    }
    
    void CreateHealthBarFromCode()
    {
        GameObject healthBarGO = new GameObject("HealthBar");
        healthBarGO.transform.SetParent(transform);
        healthBarGO.transform.localPosition = healthBarOffset;
        
        Canvas canvas = healthBarGO.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;
        canvas.worldCamera = Camera.main;
        
        CanvasScaler scaler = healthBarGO.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ConstantPhysicalSize;
        
        GameObject bgGO = new GameObject("Background");
        bgGO.transform.SetParent(healthBarGO.transform);
        
        Image bgImage = bgGO.AddComponent<Image>();
        bgImage.color = Color.black;
        
        RectTransform bgRect = bgGO.GetComponent<RectTransform>();
        bgRect.sizeDelta = new Vector2(1f, 0.2f);
        bgRect.anchoredPosition = Vector2.zero;
        
        GameObject sliderGO = new GameObject("HealthSlider");
        sliderGO.transform.SetParent(healthBarGO.transform);
        
        Slider slider = sliderGO.AddComponent<Slider>();
        slider.transition = Slider.Transition.None;
        
        RectTransform sliderRect = sliderGO.GetComponent<RectTransform>();
        sliderRect.sizeDelta = new Vector2(0.9f, 0.15f);
        sliderRect.anchoredPosition = Vector2.zero;
        
        GameObject fillAreaGO = new GameObject("Fill Area");
        fillAreaGO.transform.SetParent(sliderGO.transform);
        
        RectTransform fillAreaRect = fillAreaGO.AddComponent<RectTransform>();
        fillAreaRect.anchorMin = Vector2.zero;
        fillAreaRect.anchorMax = Vector2.one;
        fillAreaRect.sizeDelta = Vector2.zero;
        fillAreaRect.anchoredPosition = Vector2.zero;
        
        GameObject fillGO = new GameObject("Fill");
        fillGO.transform.SetParent(fillAreaGO.transform);
        
        Image fillImage = fillGO.AddComponent<Image>();
        fillImage.color = Color.green;
        fillImage.type = Image.Type.Filled;
        
        RectTransform fillRect = fillGO.GetComponent<RectTransform>();
        fillRect.anchorMin = Vector2.zero;
        fillRect.anchorMax = Vector2.one;
        fillRect.sizeDelta = Vector2.zero;
        fillRect.anchoredPosition = Vector2.zero;
        
        slider.fillRect = fillRect;
        slider.maxValue = 1f;
        slider.value = 1f;
        
        HealthBar healthBar = healthBarGO.AddComponent<HealthBar>();
        healthBar.canvas = canvas;
        healthBar.healthSlider = slider;
        healthBar.fillImage = fillImage;
    }
    
    
    void CreateStateDisplayFromCode()
    {
        GameObject stateDisplayGO = new GameObject("StateDisplay");
        stateDisplayGO.transform.SetParent(transform);
        stateDisplayGO.transform.localPosition = healthBarOffset + new Vector3(0, -0.4f, 0);
        
        Canvas canvas = stateDisplayGO.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;
        canvas.worldCamera = Camera.main;
        
        CanvasScaler scaler = stateDisplayGO.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ConstantPhysicalSize;
        
        GameObject textGO = new GameObject("StateText");
        textGO.transform.SetParent(stateDisplayGO.transform);
        
        TextMeshProUGUI text = textGO.AddComponent<TextMeshProUGUI>();
        text.text = "Initializing";
        text.fontSize = 0.3f;
        text.color = Color.cyan;
        text.alignment = TextAlignmentOptions.Center;
        text.fontStyle = FontStyles.Normal;
        
        RectTransform textRect = textGO.GetComponent<RectTransform>();
        textRect.sizeDelta = new Vector2(1.5f, 0.3f);
        textRect.anchoredPosition = Vector2.zero;
        
        StateDisplay stateDisplay = stateDisplayGO.AddComponent<StateDisplay>();
        stateDisplay.canvas = canvas;
        stateDisplay.stateText = text;
        stateDisplay.textColor = Color.cyan;
        stateDisplay.fontSize = 0.3f;
    }
    
}
