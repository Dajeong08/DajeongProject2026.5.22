using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class CardDisplay : MonoBehaviour
{
    public CardData cardData;
    public TextMeshProUGUI nameText;
    public TextMeshProUGUI descriptionText;
    public TextMeshProUGUI costText;
    public Image artworkImage;

    public void UpdateUI()
    {
        if (cardData == null) return; 

        nameText.text = cardData.cardName;
        descriptionText.text = cardData.description;
        costText.text = cardData.cost.ToString();
        artworkImage.sprite = cardData.cardImage;
    }
}