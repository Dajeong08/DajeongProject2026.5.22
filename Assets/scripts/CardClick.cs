using UnityEngine;
using UnityEngine.EventSystems;

public class CardClick : MonoBehaviour, IPointerClickHandler
{
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
    private bool originalPositionSaved = false;  // ✅ 위치 저장 완료 여부

    public static CardClick currentPreviewCard = null;

    void Update()
    {
        // ✅ HandLayoutManager가 위치 잡아준 이후에 originalPosition 저장
        if (!originalPositionSaved && transform.localPosition != Vector3.zero)
        {
            originalPosition = transform.localPosition;
            targetPosition = originalPosition;
            targetScale = originalScale;
            originalPositionSaved = true;
        }

        // 미리보기 아닐때 HandLayoutManager가 위치 바꾸면 원래위치도 같이 업데이트
        if (!isPreviewing && originalPositionSaved)
        {
            originalPosition = transform.localPosition;
            targetPosition = originalPosition;
        }

        transform.localPosition = Vector3.Lerp(transform.localPosition, targetPosition, Time.deltaTime * animSpeed);
        transform.localScale = Vector3.Lerp(transform.localScale, targetScale, Time.deltaTime * animSpeed);
        transform.localRotation = Quaternion.identity;
    }

    //public void OnPointerClick(PointerEventData eventData)
    //{
    //    if (!isPreviewing)
    //    {
    //        if (currentPreviewCard != null && currentPreviewCard != this)
    //        {
    //            currentPreviewCard.CancelPreview();
    //        }
    //        ShowPreview();
    //    }
    //    else
    //    {
    //        UseCard();
    //    }
    //}

    //void ShowPreview()
    //{
    //    isPreviewing = true;
    //    currentPreviewCard = this;
    //    originalSiblingIndex = transform.GetSiblingIndex();
    //    targetPosition = previewPosition;
    //    targetScale = previewScale;
    //    transform.SetAsLastSibling();
    //}

    public void OnPointerClick(PointerEventData eventData)
    {
        if (!isPreviewing)
        {
            if (currentPreviewCard != null && currentPreviewCard != this)
            {
                currentPreviewCard.CancelPreview();
            }
            ShowPreview();
        }
        else
        {
            UseCard();
        }
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
        isPreviewing = false;
        currentPreviewCard = null;
        Debug.Log(gameObject.name + " 카드 사용!");

        HandLayoutManager hand = GetComponentInParent<HandLayoutManager>();
        if (hand != null)
        {
            hand.RemoveCard(gameObject);
        }
    }
}