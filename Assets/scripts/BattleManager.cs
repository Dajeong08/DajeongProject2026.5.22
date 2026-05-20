using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public enum TurnState { PlayerTurn, EnemyTurn, Wait }

// ✅ 라운드별 적 설정을 위한 클래스
[System.Serializable]
public class RoundEnemySettings
{
    public string roundDescription;
    public List<EnemyData> enemyPool;
}

[System.Serializable]
public class EnemyUiSlot
{
    public GameObject namePlateObject;
    public TextMeshProUGUI nameText;
    public TextMeshProUGUI hpText;
    public Slider hpSlider;
    public Slider shieldSlider;
}

public class BattleManager : MonoBehaviour
{
    public TurnState currentState;
    public bool IsPlayerTurn => currentState == TurnState.PlayerTurn;

    [Header("참조")]
    public PlayerController player;
    public HandLayoutManager handLayout;
    public GameObject cardPrefab;
    public List<CardData> deckList = new List<CardData>();
    public CardRewardManager cardRewardManager;

    [Header("UI")]
    public Button endTurnButton;
    public GameObject gamePanel;

    [Header("적 스폰 설정")]
    public GameObject enemyPrefab;
    public Transform[] enemySpawnPoints;

    [Header("적 UI 슬롯")]
    public EnemyUiSlot[] enemyUiSlots;

    // ✅ 인스펙터에서 라운드 순서대로 설정 (0번=1라운드, 1번=2라운드...)
    [Header("라운드별 적 데이터 설정")]
    public List<RoundEnemySettings> roundSettings = new List<RoundEnemySettings>();

    [HideInInspector]
    public List<EnemyController> activeEnemies = new List<EnemyController>();
    private readonly Dictionary<CardData, int> usedOnceCardCountsThisBattle = new Dictionary<CardData, int>();
    private readonly Dictionary<CardData, int> rewardCardCounts = new Dictionary<CardData, int>();
    private readonly Dictionary<CardData, int> startingCardCounts = new Dictionary<CardData, int>();
    private Coroutine autoEndTurnRoutine;
    private NodeType currentNodeType;

    void Awake()
    {
        CaptureStartingCards();
    }

    // ✅ 맵 매니저에서 라운드 번호를 받아 전투 준비
    public void PrepareBattle(int roundIndex, NodeType nodeType)
    {
        currentNodeType = nodeType;
        usedOnceCardCountsThisBattle.Clear();
        if (autoEndTurnRoutine != null)
        {
            StopCoroutine(autoEndTurnRoutine);
            autoEndTurnRoutine = null;
        }
        ClearHand();
        foreach (var e in activeEnemies) { if (e != null) Destroy(e.gameObject); }
        activeEnemies.Clear();
        HideEnemyUiSlots();

        // 라운드 설정에 맞는 적 스폰
        if (roundIndex < roundSettings.Count)
        {
            RoundEnemySettings settings = roundSettings[roundIndex];
            int spawnCount = GetSpawnCount(nodeType);

            // 💡 값이 제대로 들어왔는지 콘솔창에서 확인
            Debug.Log($"{settings.roundDescription} 준비 중 - 노드 타입: {nodeType}, 소환 수: {spawnCount}");

            SpawnEnemies(settings.enemyPool, spawnCount);
        }

        gamePanel.SetActive(true);
        StartCoroutine(InitGame());
    }

    int GetSpawnCount(NodeType nodeType)
    {
        if (nodeType == NodeType.HardBattle) return 2;
        return 1;
    }

    void SpawnEnemies(List<EnemyData> pool, int count)
    {
        if (pool == null || pool.Count == 0) return;

        for (int i = 0; i < count; i++)
        {
            if (i >= enemySpawnPoints.Length) break;

            GameObject go = Instantiate(enemyPrefab, enemySpawnPoints[i].position, Quaternion.identity);
            go.transform.SetParent(gamePanel.transform, true);

            EnemyController ec = go.GetComponent<EnemyController>();
            ec.enemyData = pool[Random.Range(0, pool.Count)];
            AssignEnemyUiSlot(ec, i);
            ec.Init();
            activeEnemies.Add(ec);
        }
    }

    void AssignEnemyUiSlot(EnemyController enemy, int index)
    {
        if (enemy == null || enemyUiSlots == null || index >= enemyUiSlots.Length) return;

        EnemyUiSlot slot = enemyUiSlots[index];
        enemy.SetUiReferences(slot.namePlateObject, slot.nameText, slot.hpText, slot.hpSlider, slot.shieldSlider);
    }

    void HideEnemyUiSlots()
    {
        if (enemyUiSlots == null) return;

        foreach (EnemyUiSlot slot in enemyUiSlots)
        {
            if (slot == null) continue;

            if (slot.namePlateObject != null) slot.namePlateObject.SetActive(false);
            if (slot.nameText != null) slot.nameText.gameObject.SetActive(false);
            if (slot.hpText != null) slot.hpText.gameObject.SetActive(false);
            if (slot.hpSlider != null) slot.hpSlider.gameObject.SetActive(false);
            if (slot.shieldSlider != null) slot.shieldSlider.gameObject.SetActive(false);
        }
    }

    IEnumerator InitGame()
    {
        yield return new WaitForSeconds(0.1f);
        StartPlayerTurn();
    }

    public void StartPlayerTurn()
    {
        currentState = TurnState.PlayerTurn;
        player.ResetEnergy();
        player.TickShieldTurns();
        DrawCards(7);
        if (endTurnButton != null) endTurnButton.interactable = true;
        RequestAutoEndTurnCheck();
    }

    public void OnEndTurnButtonClicked()
    {
        if (currentState != TurnState.PlayerTurn) return;
        if (autoEndTurnRoutine != null)
        {
            StopCoroutine(autoEndTurnRoutine);
            autoEndTurnRoutine = null;
        }
        currentState = TurnState.Wait;
        if (endTurnButton != null) endTurnButton.interactable = false;
        StartCoroutine(EnemyTurnRoutine());
    }

    IEnumerator EnemyTurnRoutine()
    {
        currentState = TurnState.EnemyTurn;
        if (endTurnButton != null) endTurnButton.interactable = false;

        ClearHand();
        yield return new WaitForSeconds(1.0f);

        foreach (var e in activeEnemies)
        {
            if (e != null && e.IsAlive)
            {
                e.TakeTurn(player);
                if (player.HP <= 0) yield break;
                yield return new WaitUntil(() => e == null || !e.IsActing);
                yield return new WaitForSeconds(1.2f);
            }
        }

        yield return new WaitForSeconds(0.5f);
        if (player.GetTotalShield() <= 0) player.EndDefenseAnimation();

        StartPlayerTurn();
    }

    public void CheckBattleEnd()
    {
        bool allDead = true;
        foreach (var e in activeEnemies)
        {
            if (e != null && e.IsAlive) { allDead = false; break; }
        }

        if (allDead)
        {
            if (GamePresentationManager.Instance != null)
            {
                GamePresentationManager.Instance.ShowVictory();
            }
            else
            {
                MapManager map = FindObjectOfType<MapManager>();
                if (map != null) map.FinishRound();
            }
        }
    }

    public void ContinueAfterBattleVictory()
    {
        if (cardRewardManager == null)
            cardRewardManager = FindObjectOfType<CardRewardManager>(true);

        if (cardRewardManager != null && cardRewardManager.ShowReward(currentNodeType))
            return;

        FinishBattleRound();
    }

    public void FinishBattleRound()
    {
        MapManager map = FindObjectOfType<MapManager>();
        if (map != null) map.FinishRound();
    }

    public void AddCardToDeck(CardData card)
    {
        if (card == null) return;
        deckList.Add(card);

        if (!rewardCardCounts.ContainsKey(card))
            rewardCardCounts[card] = 0;

        rewardCardCounts[card]++;
    }

    public Dictionary<CardData, int> GetOwnedCardCounts()
    {
        Dictionary<CardData, int> ownedCardCounts = new Dictionary<CardData, int>();

        foreach (KeyValuePair<CardData, int> pair in startingCardCounts)
        {
            if (pair.Key == null) continue;
            ownedCardCounts[pair.Key] = Mathf.Max(1, pair.Value);
        }

        foreach (KeyValuePair<CardData, int> pair in rewardCardCounts)
        {
            if (pair.Key == null) continue;

            if (!ownedCardCounts.ContainsKey(pair.Key))
                ownedCardCounts[pair.Key] = 0;

            ownedCardCounts[pair.Key] += pair.Value;
        }

        return ownedCardCounts;
    }

    public void MarkCardUsedThisBattle(CardData card)
    {
        if (card == null) return;

        if (!usedOnceCardCountsThisBattle.ContainsKey(card))
            usedOnceCardCountsThisBattle[card] = 0;

        usedOnceCardCountsThisBattle[card]++;
    }

    public void RequestAutoEndTurnCheck()
    {
        if (autoEndTurnRoutine != null)
            StopCoroutine(autoEndTurnRoutine);

        autoEndTurnRoutine = StartCoroutine(AutoEndTurnCheckRoutine());
    }

    IEnumerator AutoEndTurnCheckRoutine()
    {
        yield return null;

        while (player != null && player.IsActing)
            yield return null;

        autoEndTurnRoutine = null;

        if (currentState != TurnState.PlayerTurn) yield break;
        if (!HasAliveEnemy()) yield break;
        if (player == null) yield break;

        bool shouldEndTurn = player.currentEnergy <= 0 || !HasPlayableCardInHand();
        if (shouldEndTurn)
            OnEndTurnButtonClicked();
    }

    public void DrawCards(int count)
    {
        List<CardData> drawPool = GetAvailableDrawPool();
        if (drawPool.Count == 0) return;
        Dictionary<CardData, int> rewardCardsAlreadyDrawn = GetRewardCardCountsInHand();

        if (GamePresentationManager.Instance != null)
            GamePresentationManager.Instance.PlayCardDraw();

        for (int i = 0; i < count; i++)
        {
            List<CardData> candidates = GetDrawCandidates(drawPool, rewardCardsAlreadyDrawn);
            if (candidates.Count == 0) break;

            int randomIndex = Random.Range(0, candidates.Count);
            CardData data = candidates[randomIndex];
            GameObject newCard = Instantiate(cardPrefab, handLayout.transform);
            CardDisplay display = newCard.GetComponent<CardDisplay>();
            if (display != null) { display.cardData = data; display.UpdateUI(); }
            handLayout.AddCard(newCard);

            if (rewardCardCounts.ContainsKey(data))
            {
                if (!rewardCardsAlreadyDrawn.ContainsKey(data))
                    rewardCardsAlreadyDrawn[data] = 0;

                rewardCardsAlreadyDrawn[data]++;
            }
        }
    }

    List<CardData> GetAvailableDrawPool()
    {
        List<CardData> drawPool = new List<CardData>();
        Dictionary<CardData, int> deckCounts = GetDeckCardCounts();
        Dictionary<CardData, int> includedOnceCardCounts = new Dictionary<CardData, int>();

        foreach (CardData card in deckList)
        {
            if (card == null) continue;
            if (card.oncePerBattle)
            {
                int usedCount = usedOnceCardCountsThisBattle.ContainsKey(card)
                    ? usedOnceCardCountsThisBattle[card]
                    : 0;
                int includedCount = includedOnceCardCounts.ContainsKey(card)
                    ? includedOnceCardCounts[card]
                    : 0;
                int availableCopies = deckCounts[card] - usedCount;

                if (includedCount >= availableCopies) continue;

                includedOnceCardCounts[card] = includedCount + 1;
            }

            drawPool.Add(card);
        }
        return drawPool;
    }

    Dictionary<CardData, int> GetDeckCardCounts()
    {
        Dictionary<CardData, int> deckCounts = new Dictionary<CardData, int>();
        foreach (CardData card in deckList)
        {
            if (card == null) continue;

            if (!deckCounts.ContainsKey(card))
                deckCounts[card] = 0;

            deckCounts[card]++;
        }

        return deckCounts;
    }

    void CaptureStartingCards()
    {
        startingCardCounts.Clear();

        foreach (CardData card in deckList)
        {
            if (card == null || startingCardCounts.ContainsKey(card)) continue;
            startingCardCounts[card] = 1;
        }
    }

    List<CardData> GetDrawCandidates(List<CardData> drawPool, Dictionary<CardData, int> rewardCardsAlreadyDrawn)
    {
        List<CardData> candidates = new List<CardData>();
        foreach (CardData card in drawPool)
        {
            if (card == null) continue;
            if (HasDrawnMaxRewardCopies(card, rewardCardsAlreadyDrawn)) continue;
            candidates.Add(card);
        }

        return candidates;
    }

    bool HasDrawnMaxRewardCopies(CardData card, Dictionary<CardData, int> rewardCardsAlreadyDrawn)
    {
        if (!rewardCardCounts.ContainsKey(card)) return false;

        int drawnCount = rewardCardsAlreadyDrawn.ContainsKey(card)
            ? rewardCardsAlreadyDrawn[card]
            : 0;

        return drawnCount >= rewardCardCounts[card];
    }

    Dictionary<CardData, int> GetRewardCardCountsInHand()
    {
        Dictionary<CardData, int> cardsInHand = new Dictionary<CardData, int>();
        if (handLayout == null || handLayout.cards == null) return cardsInHand;

        foreach (GameObject cardObject in handLayout.cards)
        {
            if (cardObject == null) continue;

            CardDisplay display = cardObject.GetComponent<CardDisplay>();
            if (display == null || display.cardData == null) continue;
            if (!rewardCardCounts.ContainsKey(display.cardData)) continue;

            if (!cardsInHand.ContainsKey(display.cardData))
                cardsInHand[display.cardData] = 0;

            cardsInHand[display.cardData]++;
        }

        return cardsInHand;
    }

    bool HasPlayableCardInHand()
    {
        if (player == null || handLayout == null || handLayout.cards == null) return false;

        foreach (GameObject cardObject in handLayout.cards)
        {
            if (cardObject == null) continue;

            CardDisplay display = cardObject.GetComponent<CardDisplay>();
            if (display == null || display.cardData == null) continue;
            if (player.CanUseCard(display.cardData.cost)) return true;
        }

        return false;
    }

    bool HasAliveEnemy()
    {
        foreach (EnemyController enemy in activeEnemies)
        {
            if (enemy != null && enemy.IsAlive)
                return true;
        }

        return false;
    }

    void ClearHand()
    {
        List<GameObject> toRemove = new List<GameObject>(handLayout.cards);
        foreach (GameObject card in toRemove)
        {
            if (card != null) Destroy(card);
        }
        handLayout.cards.Clear();
    }
}
