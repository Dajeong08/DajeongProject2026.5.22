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

    [Header("Playable Visual")]
    [Range(0.2f, 1f)] public float unavailableAlpha = 0.55f;

    private CanvasGroup canvasGroup;

    public void UpdateUI()
    {
        if (cardData == null) return; 

        nameText.text = cardData.cardName;
        descriptionText.text = cardData.description;
        costText.text = cardData.cost.ToString();
        artworkImage.sprite = cardData.cardImage;
    }

    public void SetPlayableVisual(bool isPlayable)
    {
        if (canvasGroup == null)
        {
            canvasGroup = GetComponent<CanvasGroup>();
            if (canvasGroup == null)
                canvasGroup = gameObject.AddComponent<CanvasGroup>();
        }

        canvasGroup.alpha = isPlayable ? 1f : unavailableAlpha;
    }
}
