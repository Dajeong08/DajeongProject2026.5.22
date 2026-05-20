using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

[System.Serializable]
public class CardRewardSlot
{
    public GameObject rootObject;
    public Button button;
    public CardDisplay cardDisplay;
}

public class CardRewardManager : MonoBehaviour
{
    [Header("References")]
    public GameObject rewardPanel;
    public BattleManager battleManager;
    public CardRewardSlot[] rewardSlots = new CardRewardSlot[3];

    [Header("Reward Pools")]
    public List<CardData> lowCostRewards = new List<CardData>();
    public List<CardData> highCostRewards = new List<CardData>();
    public List<CardData> bossRewards = new List<CardData>();

    private readonly List<CardData> currentChoices = new List<CardData>();

    void Awake()
    {
        SetPanel(false);
        ClearSlots();
    }

    public bool ShowReward(NodeType nodeType)
    {
        List<CardData> rewardPool = GetRewardPool(nodeType);
        if (rewardPool == null || rewardPool.Count == 0)
            return false;

        currentChoices.Clear();
        List<CardData> candidates = new List<CardData>();
        foreach (CardData card in rewardPool)
        {
            if (card != null)
                candidates.Add(card);
        }

        if (candidates.Count == 0)
            return false;

        int choiceCount = Mathf.Min(3, candidates.Count);
        for (int i = 0; i < choiceCount; i++)
        {
            int randomIndex = Random.Range(0, candidates.Count);
            currentChoices.Add(candidates[randomIndex]);
            candidates.RemoveAt(randomIndex);
        }

        ApplyChoicesToSlots();
        SetPanel(true);
        return true;
    }

    public void SelectReward(int index)
    {
        if (index < 0 || index >= currentChoices.Count) return;

        if (battleManager == null)
            battleManager = FindObjectOfType<BattleManager>();

        if (battleManager != null)
        {
            battleManager.AddCardToDeck(currentChoices[index]);
        }

        currentChoices.Clear();
        ClearSlots();
        SetPanel(false);

        if (battleManager != null)
            battleManager.FinishBattleRound();
    }

    List<CardData> GetRewardPool(NodeType nodeType)
    {
        if (nodeType == NodeType.Battle) return lowCostRewards;
        if (nodeType == NodeType.HardBattle) return highCostRewards;
        if (nodeType == NodeType.Boss) return bossRewards;
        return null;
    }

    void ApplyChoicesToSlots()
    {
        ClearSlots();

        for (int i = 0; i < rewardSlots.Length; i++)
        {
            CardRewardSlot slot = rewardSlots[i];
            if (slot == null) continue;

            bool hasChoice = i < currentChoices.Count;
            if (slot.rootObject != null)
                slot.rootObject.SetActive(hasChoice);

            if (!hasChoice) continue;

            if (slot.cardDisplay != null)
            {
                slot.cardDisplay.cardData = currentChoices[i];
                slot.cardDisplay.UpdateUI();
            }

            if (slot.button != null)
            {
                int capturedIndex = i;
                slot.button.onClick.RemoveAllListeners();
                slot.button.onClick.AddListener(() => SelectReward(capturedIndex));
                slot.button.interactable = true;
            }
        }

        if (GamePresentationManager.Instance != null)
            GamePresentationManager.Instance.RegisterButtonSounds();
    }

    void ClearSlots()
    {
        if (rewardSlots == null) return;

        foreach (CardRewardSlot slot in rewardSlots)
        {
            if (slot == null) continue;

            if (slot.rootObject != null)
                slot.rootObject.SetActive(false);

            if (slot.button != null)
            {
                slot.button.onClick.RemoveAllListeners();
                slot.button.interactable = false;
            }
        }
    }

    void SetPanel(bool isActive)
    {
        if (rewardPanel != null)
            rewardPanel.SetActive(isActive);
    }
}
