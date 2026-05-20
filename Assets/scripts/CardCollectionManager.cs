using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class CardCollectionManager : MonoBehaviour
{
    [Header("References")]
    public BattleManager battleManager;
    public GameObject collectionPanel;
    public Transform cardParent;
    public GameObject cardPrefab;
    public Button openButton;
    public Button closeButton;

    [Header("Layout")]
    public bool applyGridLayoutSettings = true;
    public bool stretchContentToPanel = true;
    public float contentTopOffset = 80f;
    public int columnCount = 4;
    public Vector2 cellSize = new Vector2(170f, 230f);
    public Vector2 spacing = new Vector2(120f, 45f);
    public int paddingLeft = 30;
    public int paddingRight = 30;
    public int paddingTop = 30;
    public int paddingBottom = 30;
    [Range(0.3f, 1.5f)] public float cardScale = 0.8f;

    [Header("Count Badge")]
    public Vector2 badgeSize = new Vector2(48f, 32f);
    public Vector2 badgePosition = new Vector2(-6f, -8f);
    public float badgeFontSize = 22f;
    public Color badgeColor = new Color(0.08f, 0.08f, 0.08f, 0.85f);
    public Color badgeTextColor = Color.white;

    private List<GameObject> spawnedCards;

    void Awake()
    {
        if (spawnedCards == null)
            spawnedCards = new List<GameObject>();

        if (collectionPanel != null)
            collectionPanel.SetActive(false);

        if (openButton != null)
            openButton.onClick.AddListener(ToggleCollection);

        if (closeButton != null)
            closeButton.onClick.AddListener(CloseCollection);
    }

    public void ToggleCollection()
    {
        if (collectionPanel != null && collectionPanel.activeSelf)
            CloseCollection();
        else
            OpenCollection();
    }

    public void OpenCollection()
    {
        if (battleManager == null)
            battleManager = FindObjectOfType<BattleManager>();

        if (collectionPanel == null || cardParent == null || cardPrefab == null || battleManager == null)
            return;

        RebuildCollection();
        collectionPanel.SetActive(true);

        if (GamePresentationManager.Instance != null)
            GamePresentationManager.Instance.RegisterButtonSounds();
    }

    public void CloseCollection()
    {
        if (collectionPanel != null)
            collectionPanel.SetActive(false);
    }

    void RebuildCollection()
    {
        ClearSpawnedCards();
        ApplyLayoutSettings();

        Dictionary<CardData, int> ownedCardCounts = battleManager.GetOwnedCardCounts();
        foreach (KeyValuePair<CardData, int> pair in ownedCardCounts)
        {
            if (pair.Key == null) continue;

            GameObject cardObject = Instantiate(cardPrefab, cardParent);
            cardObject.transform.localScale = Vector3.one * cardScale;
            spawnedCards.Add(cardObject);

            CardDisplay display = cardObject.GetComponent<CardDisplay>();
            if (display != null)
            {
                display.cardData = pair.Key;
                display.UpdateUI();
                display.SetPlayableVisual(true);
            }

            DisableGameplayInteraction(cardObject);
            AddCountBadge(cardObject, pair.Value);
        }
    }

    void ApplyLayoutSettings()
    {
        if (!applyGridLayoutSettings || cardParent == null) return;

        GridLayoutGroup gridLayout = cardParent.GetComponent<GridLayoutGroup>();
        if (gridLayout == null) return;

        if (stretchContentToPanel)
            StretchCardParentToPanel();

        gridLayout.cellSize = cellSize;
        gridLayout.spacing = spacing;
        gridLayout.padding = new RectOffset(paddingLeft, paddingRight, paddingTop, paddingBottom);
        gridLayout.childAlignment = TextAnchor.UpperCenter;
        gridLayout.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
        gridLayout.constraintCount = Mathf.Max(1, columnCount);
    }

    void StretchCardParentToPanel()
    {
        RectTransform parentRect = cardParent as RectTransform;
        if (parentRect == null) return;

        parentRect.anchorMin = Vector2.zero;
        parentRect.anchorMax = Vector2.one;
        parentRect.pivot = new Vector2(0.5f, 0.5f);
        parentRect.offsetMin = Vector2.zero;
        parentRect.offsetMax = new Vector2(0f, -contentTopOffset);
    }

    void DisableGameplayInteraction(GameObject cardObject)
    {
        CardClick cardClick = cardObject.GetComponent<CardClick>();
        if (cardClick != null)
            Destroy(cardClick);

        Button button = cardObject.GetComponent<Button>();
        if (button != null)
        {
            button.onClick.RemoveAllListeners();
            button.interactable = false;
        }
    }

    void AddCountBadge(GameObject cardObject, int count)
    {
        GameObject badgeObject = new GameObject("CountBadge");
        badgeObject.transform.SetParent(cardObject.transform, false);

        Image badgeImage = badgeObject.AddComponent<Image>();
        badgeImage.color = badgeColor;
        badgeImage.raycastTarget = false;

        RectTransform badgeRect = badgeImage.rectTransform;
        badgeRect.anchorMin = new Vector2(1f, 1f);
        badgeRect.anchorMax = new Vector2(1f, 1f);
        badgeRect.pivot = new Vector2(1f, 1f);
        badgeRect.anchoredPosition = badgePosition;
        badgeRect.sizeDelta = badgeSize;

        GameObject textObject = new GameObject("CountText");
        textObject.transform.SetParent(badgeObject.transform, false);

        TextMeshProUGUI countText = textObject.AddComponent<TextMeshProUGUI>();
        countText.text = $"x{count}";
        countText.fontSize = badgeFontSize;
        countText.color = badgeTextColor;
        countText.alignment = TextAlignmentOptions.Center;
        countText.raycastTarget = false;

        RectTransform textRect = countText.rectTransform;
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = Vector2.zero;
        textRect.offsetMax = Vector2.zero;
    }

    void ClearSpawnedCards()
    {
        if (spawnedCards == null)
            spawnedCards = new List<GameObject>();

        foreach (GameObject card in spawnedCards)
        {
            if (card != null)
                Destroy(card);
        }

        spawnedCards.Clear();
    }
}
