using UnityEngine;
using UnityEngine.EventSystems;

public class ClickOutsideHandler : MonoBehaviour, IPointerClickHandler
{
    public void OnPointerClick(PointerEventData eventData)
    {
        // 카드 클릭인지 확인
        if (eventData.pointerPress != null)
        {
            CardClick card = eventData.pointerPress.GetComponent<CardClick>();
            if (card != null) return;  // 카드 클릭이면 무시
        }

        // 카드 외 클릭이면 미리보기 취소
        if (CardClick.currentPreviewCard != null)
        {
            CardClick.currentPreviewCard.CancelPreview();
        }
    }
}
