using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

// ✅ 이 부분이 누락되어서 에러가 나는 것입니다. 이 위치에 복사해 넣으세요!
public enum TurnState { PlayerTurn, EnemyTurn, Wait }

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

    [Header("적 연결")]
    public EnemyController enemy;

    void Start() { StartCoroutine(InitGame()); }

    IEnumerator InitGame()
    {
        yield return new WaitForSeconds(0.1f);
        if (enemy != null) enemy.Init();
        StartPlayerTurn();
    }

    public void StartPlayerTurn()
    {
        currentState = TurnState.PlayerTurn;
        player.ResetEnergy();

        // ✅ 턴마다 초기화 대신, 지속 시간을 깎음
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

        if (enemy != null && enemy.gameObject.activeSelf)
        {
            enemy.AttackPlayer(player);
        }

        yield return new WaitForSeconds(1.5f);
        // 적 공격 직후에도 방어력이 다 깎였다면 애니메이션 체크를 위해 한 번 더 호출 가능
        if (player.GetTotalShield() <= 0) player.EndDefenseAnimation();

        StartPlayerTurn();
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
        foreach (GameObject card in toRemove) handLayout.RemoveCard(card);
    }
}