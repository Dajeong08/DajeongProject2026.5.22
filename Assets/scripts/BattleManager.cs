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

    [Header("참조")]
    public PlayerController player;
    public HandLayoutManager handLayout;
    public GameObject cardPrefab;
    public List<CardData> deckList = new List<CardData>();

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

    // ✅ 맵 매니저에서 라운드 번호를 받아 전투 준비
    public void PrepareBattle(int roundIndex, NodeType nodeType)
    {
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
        DrawCards(5);
        if (endTurnButton != null) endTurnButton.interactable = true;
    }

    public void OnEndTurnButtonClicked()
    {
        if (currentState != TurnState.PlayerTurn) return;
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
            if (e != null && e.gameObject.activeSelf)
            {
                e.TakeTurn(player);
                if (player.HP <= 0) yield break;
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
            if (e != null && e.gameObject.activeSelf) { allDead = false; break; }
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

    public void DrawCards(int count)
    {
        if (deckList.Count == 0) return;
        if (GamePresentationManager.Instance != null)
            GamePresentationManager.Instance.PlayCardDraw();

        for (int i = 0; i < count; i++)
        {
            int randomIndex = Random.Range(0, deckList.Count);
            CardData data = deckList[randomIndex];
            GameObject newCard = Instantiate(cardPrefab, handLayout.transform);
            CardDisplay display = newCard.GetComponent<CardDisplay>();
            if (display != null) { display.cardData = data; display.UpdateUI(); }
            handLayout.AddCard(newCard);
        }
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
