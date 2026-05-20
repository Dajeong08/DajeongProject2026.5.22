using System.Collections.Generic;
using UnityEngine;

public class HandLayoutManager : MonoBehaviour
{
    public List<GameObject> cards = new List<GameObject>();

    [Header("레이아웃 설정")]
    public float cardSpacing = 130f;

    public void AddCard(GameObject card)
    {
        cards.Add(card);
        ArrangeCards();
        RefreshCardPlayableStates();
    }

    public void RemoveCard(GameObject card)
    {
        cards.Remove(card);
        Destroy(card);
        ArrangeCards();
        RefreshCardPlayableStates();
    }

    public void RefreshCardPlayableStates(PlayerController player = null)
    {
        if (player == null)
            player = FindObjectOfType<PlayerController>();

        if (player == null) return;

        foreach (GameObject card in cards)
        {
            if (card == null) continue;

            CardDisplay display = card.GetComponent<CardDisplay>();
            if (display == null || display.cardData == null) continue;

            display.SetPlayableVisual(player.CanUseCard(display.cardData.cost));
        }
    }

    //void ArrangeCards()
    //{
    //    int count = cards.Count;
    //    if (count == 0) return;

    //    float totalWidth = (count - 1) * cardSpacing;
    //    float startX = -totalWidth / 2f;

    //    for (int i = 0; i < count; i++)
    //    {
    //        if (cards[i] == null) continue;

    //        Vector3 targetPos = new Vector3(startX + i * cardSpacing, 0f, 0f);

    //        cards[i].transform.localPosition = Vector3.Lerp(
    //            cards[i].transform.localPosition,
    //            targetPos,
    //            Time.deltaTime * 10f
    //        );
    //    }
    //}

    void ArrangeCards()
    {
        int count = cards.Count;
        if (count == 0) return;

        float totalWidth = (count - 1) * cardSpacing;
        float startX = -totalWidth / 2f;

        for (int i = 0; i < count; i++)
        {
            if (cards[i] == null) continue;

            Vector3 targetPos = new Vector3(startX + i * cardSpacing, 0f, 0f);

            cards[i].transform.localPosition = Vector3.Lerp(
                cards[i].transform.localPosition,
                targetPos,
                Time.deltaTime * 10f
            );
        }
    }

    void Update()
    {
        ArrangeCards();
    }
}
