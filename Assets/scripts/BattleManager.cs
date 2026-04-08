using System.Collections.Generic;
using UnityEngine;

public class BattleManager : MonoBehaviour
{
    [Header("설정")]
    public GameObject cardPrefab;
    public HandLayoutManager handLayout; 

    [Header("카드 뭉치")]
    public List<CardData> deckList = new List<CardData>();

    void Start()
    {
        DrawCards(7);
    }

    public void DrawCards(int count)
    {
        for (int i = 0; i < count; i++)
        {
            if (deckList.Count > 0)
            {
                int randomIndex = Random.Range(0, deckList.Count);
                CardData data = deckList[randomIndex];

                GameObject newCard = Instantiate(cardPrefab, handLayout.transform);
                CardDisplay display = newCard.GetComponent<CardDisplay>();
                if (display != null)
                {
                    display.cardData = data;
                    display.UpdateUI();
                }

                handLayout.AddCard(newCard);  // ✅ 이 줄 추가
            }
        }
    }
}