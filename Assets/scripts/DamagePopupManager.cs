using System.Collections;
using TMPro;
using UnityEngine;

public class DamagePopupManager : MonoBehaviour
{
    public static DamagePopupManager Instance { get; private set; }

    [Header("Target")]
    public Canvas targetCanvas;

    [Header("Style")]
    public Color damageColor = new Color(1f, 0.22f, 0.16f);
    public float fontSize = 42f;
    public float duration = 0.75f;
    public float riseDistance = 70f;
    public Vector2 randomOffset = new Vector2(20f, 12f);

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    public static void ShowDamage(Vector3 worldPosition, int amount)
    {
        if (amount <= 0) return;

        DamagePopupManager manager = GetOrCreateInstance();
        if (manager == null) return;

        manager.Show(worldPosition, $"-{amount}", manager.damageColor);
    }

    static DamagePopupManager GetOrCreateInstance()
    {
        if (Instance != null) return Instance;

        GameObject go = new GameObject("DamagePopupManager");
        return go.AddComponent<DamagePopupManager>();
    }

    void Show(Vector3 worldPosition, string text, Color color)
    {
        Canvas canvas = GetCanvas();
        if (canvas == null) return;

        GameObject go = new GameObject("DamagePopupText");
        go.transform.SetParent(canvas.transform, false);

        TextMeshProUGUI textUi = go.AddComponent<TextMeshProUGUI>();
        textUi.text = text;
        textUi.fontSize = fontSize;
        textUi.color = color;
        textUi.alignment = TextAlignmentOptions.Center;
        textUi.raycastTarget = false;

        RectTransform rect = textUi.rectTransform;
        rect.sizeDelta = new Vector2(180f, 70f);
        rect.anchoredPosition = WorldToCanvasPosition(canvas, worldPosition) + GetRandomOffset();

        StartCoroutine(AnimatePopup(textUi, rect, color));
    }

    Canvas GetCanvas()
    {
        if (targetCanvas != null) return targetCanvas;

        targetCanvas = FindObjectOfType<Canvas>();
        return targetCanvas;
    }

    Vector2 WorldToCanvasPosition(Canvas canvas, Vector3 worldPosition)
    {
        RectTransform canvasRect = canvas.transform as RectTransform;
        if (canvasRect == null) return Vector2.zero;

        Camera worldCamera = Camera.main;
        Vector2 screenPoint = worldCamera != null
            ? RectTransformUtility.WorldToScreenPoint(worldCamera, worldPosition)
            : new Vector2(worldPosition.x, worldPosition.y);

        Camera uiCamera = canvas.renderMode == RenderMode.ScreenSpaceOverlay
            ? null
            : canvas.worldCamera;

        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            canvasRect,
            screenPoint,
            uiCamera,
            out Vector2 localPoint);

        return localPoint;
    }

    Vector2 GetRandomOffset()
    {
        return new Vector2(
            Random.Range(-randomOffset.x, randomOffset.x),
            Random.Range(-randomOffset.y, randomOffset.y));
    }

    IEnumerator AnimatePopup(TextMeshProUGUI textUi, RectTransform rect, Color baseColor)
    {
        Vector2 startPosition = rect.anchoredPosition;
        Vector2 endPosition = startPosition + Vector2.up * riseDistance;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            float t = elapsed / duration;
            rect.anchoredPosition = Vector2.Lerp(startPosition, endPosition, t);

            Color color = baseColor;
            color.a = 1f - t;
            textUi.color = color;

            elapsed += Time.deltaTime;
            yield return null;
        }

        Destroy(textUi.gameObject);
    }
}
