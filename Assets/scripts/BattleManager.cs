using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public enum TurnState { PlayerTurn, EnemyTurn, Wait }

// ✅ 라운드별 적 설정을 위한 클래스
[System.Serializable]
public class RoundEnemySettings
{
    public string roundDescription;
    public List<EnemyData> enemyPool;

    [Range(1, 10)] // 인스펙터에서 슬라이더로 조절 가능하게 하여 0 방지
    public int spawnCount = 1;
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

    // ✅ 인스펙터에서 라운드 순서대로 설정 (0번=1라운드, 1번=2라운드...)
    [Header("라운드별 적 데이터 설정")]
    public List<RoundEnemySettings> roundSettings = new List<RoundEnemySettings>();

    [HideInInspector]
    public List<EnemyController> activeEnemies = new List<EnemyController>();

    // ✅ 맵 매니저에서 라운드 번호를 받아 전투 준비
    public void PrepareBattle(int roundIndex)
    {
        ClearHand();
        foreach (var e in activeEnemies) { if (e != null) Destroy(e.gameObject); }
        activeEnemies.Clear();

        // 라운드 설정에 맞는 적 스폰
        if (roundIndex < roundSettings.Count)
        {
            RoundEnemySettings settings = roundSettings[roundIndex];

            // 💡 값이 제대로 들어왔는지 콘솔창에서 확인
            Debug.Log($"{settings.roundDescription} 준비 중 - 설정된 소환 수: {settings.spawnCount}");

            // 0이 입력되는 것을 방지하는 코드 (최소 1마리 보장)
            int actualCount = Mathf.Max(1, settings.spawnCount);
            SpawnEnemies(settings.enemyPool, actualCount);
        }

        gamePanel.SetActive(true);
        StartCoroutine(InitGame());
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
            ec.Init();
            activeEnemies.Add(ec);
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
                e.AttackPlayer(player);
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
            MapManager map = FindObjectOfType<MapManager>();
            if (map != null) map.FinishRound();
        }
    }

    public void DrawCards(int count)
    {
        if (deckList.Count == 0) return;
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