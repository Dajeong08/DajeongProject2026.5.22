using UnityEngine;
using UnityEngine.EventSystems;

public class CardClick : MonoBehaviour, IPointerClickHandler
{
    // ... 미리보기 관련 변수들 (기존과 동일) ...
    [Header("미리보기 설정")]
    public Vector3 previewPosition = new Vector3(0, 150, 0);
    public Vector3 previewScale = new Vector3(1.5f, 1.5f, 1.5f);
    public float animSpeed = 10f;
    private Vector3 originalPosition;
    private Vector3 originalScale = Vector3.one;
    private bool isPreviewing = false;
    private Vector3 targetPosition;
    private Vector3 targetScale;
    private int originalSiblingIndex;
    private bool originalPositionSaved = false;
    public static CardClick currentPreviewCard = null;

    void Update()
    {
        if (!originalPositionSaved && transform.localPosition != Vector3.zero)
        {
            originalPosition = transform.localPosition;
            targetPosition = originalPosition;
            targetScale = originalScale;
            originalPositionSaved = true;
        }
        if (!isPreviewing && originalPositionSaved)
        {
            originalPosition = transform.localPosition;
            targetPosition = originalPosition;
        }
        transform.localPosition = Vector3.Lerp(transform.localPosition, targetPosition, Time.deltaTime * animSpeed);
        transform.localScale = Vector3.Lerp(transform.localScale, targetScale, Time.deltaTime * animSpeed);
        transform.localRotation = Quaternion.identity;
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (!isPreviewing)
        {
            if (currentPreviewCard != null && currentPreviewCard != this) currentPreviewCard.CancelPreview();
            ShowPreview();
        }
        else UseCard();
    }

    void ShowPreview()
    {
        isPreviewing = true;
        currentPreviewCard = this;
        originalSiblingIndex = transform.GetSiblingIndex();
        targetPosition = previewPosition;
        targetScale = previewScale;
        transform.SetAsLastSibling();
    }

    public void CancelPreview()
    {
        isPreviewing = false;
        if (currentPreviewCard == this) currentPreviewCard = null;
        targetPosition = originalPosition;
        targetScale = originalScale;
        transform.SetSiblingIndex(originalSiblingIndex);
    }

    void UseCard()
    {
        CardDisplay display = GetComponent<CardDisplay>();
        PlayerController player = FindObjectOfType<PlayerController>();
        EnemyController enemy = FindObjectOfType<EnemyController>();

        if (display != null && display.cardData != null && player != null)
        {
            int cardCost = display.cardData.cost;
            if (player.CanUseCard(cardCost))
            {
                player.UseEnergy(cardCost);
                player.PlayCardAnimation(display.cardData);

                if (display.cardData.damage > 0 && enemy != null)
                    enemy.TakeDamage(display.cardData.damage);

                // ✅ 수정: 방어력과 지속 시간을 함께 전달
                if (display.cardData.shield > 0)
                    player.AddShield(display.cardData.shield, display.cardData.shieldDuration);

                isPreviewing = false;
                currentPreviewCard = null;
                HandLayoutManager hand = GetComponentInParent<HandLayoutManager>();
                if (hand != null) hand.RemoveCard(gameObject);
            }
            else CancelPreview();
        }
    }
}