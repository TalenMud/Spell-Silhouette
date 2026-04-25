using TMPro;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(PlayerHealth))]
public class HealthBarUI : MonoBehaviour
{
    [SerializeField] Vector2 barSize = new Vector2(220f, 28f);
    [SerializeField] Vector2 barOffset = new Vector2(20f, 20f);
    [SerializeField] Color fullColor = new Color(0.2f, 0.85f, 0.2f);
    [SerializeField] Color lowColor = new Color(0.9f, 0.15f, 0.15f);
    [SerializeField] float lowThreshold = 0.3f;

    PlayerHealth health;
    Image fillImage;
    TextMeshProUGUI label;

    void Awake()
    {
        health = GetComponent<PlayerHealth>();
        BuildUI();
    }

    void OnEnable() => health.OnHealthChanged += Refresh;
    void OnDisable() => health.OnHealthChanged -= Refresh;

    void Start() => Refresh(health.Normalized);

    void BuildUI()
    {
        var canvasGO = new GameObject("HUD Canvas");
        DontDestroyOnLoad(canvasGO);

        var canvas = canvasGO.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 10;

        var scaler = canvasGO.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);

        canvasGO.AddComponent<GraphicRaycaster>();

        // Root panel anchored to bottom-left
        var panel = new GameObject("HP Panel");
        panel.transform.SetParent(canvasGO.transform, false);
        var panelRect = panel.AddComponent<RectTransform>();
        panelRect.anchorMin = panelRect.anchorMax = panelRect.pivot = Vector2.zero;
        panelRect.anchoredPosition = barOffset;
        panelRect.sizeDelta = barSize + new Vector2(0, 20f); // extra height for label

        // Label
        var labelGO = new GameObject("Label");
        labelGO.transform.SetParent(panel.transform, false);
        var labelRect = labelGO.AddComponent<RectTransform>();
        labelRect.anchorMin = new Vector2(0, 1);
        labelRect.anchorMax = new Vector2(1, 1);
        labelRect.pivot = new Vector2(0, 1);
        labelRect.anchoredPosition = Vector2.zero;
        labelRect.sizeDelta = new Vector2(0, 18f);
        label = labelGO.AddComponent<TextMeshProUGUI>();
        label.text = "HP";
        label.fontSize = 13;
        label.color = Color.white;

        // Bar background
        var bg = new GameObject("BG");
        bg.transform.SetParent(panel.transform, false);
        var bgRect = bg.AddComponent<RectTransform>();
        bgRect.anchorMin = Vector2.zero;
        bgRect.anchorMax = new Vector2(1, 0);
        bgRect.pivot = Vector2.zero;
        bgRect.anchoredPosition = Vector2.zero;
        bgRect.sizeDelta = new Vector2(0, barSize.y);
        var bgImg = bg.AddComponent<Image>();
        bgImg.color = new Color(0f, 0f, 0f, 0.65f);

        // Fill
        var fillGO = new GameObject("Fill");
        fillGO.transform.SetParent(bg.transform, false);
        var fillRect = fillGO.AddComponent<RectTransform>();
        fillRect.anchorMin = Vector2.zero;
        fillRect.anchorMax = Vector2.one;
        fillRect.offsetMin = new Vector2(3f, 3f);
        fillRect.offsetMax = new Vector2(-3f, -3f);
        fillImage = fillGO.AddComponent<Image>();
        fillImage.type = Image.Type.Filled;
        fillImage.fillMethod = Image.FillMethod.Horizontal;
        fillImage.fillOrigin = (int)Image.OriginHorizontal.Left;
        fillImage.color = fullColor;
    }

    void Refresh(float normalized)
    {
        if (fillImage == null) return;
        fillImage.fillAmount = normalized;
        fillImage.color = Color.Lerp(lowColor, fullColor,
            Mathf.Clamp01((normalized - lowThreshold) / (1f - lowThreshold)));

        if (label != null)
            label.text = $"HP  {Mathf.CeilToInt(health.Current)} / {Mathf.CeilToInt(health.MaxHealth)}";
    }
}
