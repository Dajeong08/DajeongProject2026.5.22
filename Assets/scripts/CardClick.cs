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
        if (GamePresentationManager.Instance != null)
            GamePresentationManager.Instance.PlayCardClick();

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
        // 기존의 단일 적 참조 대신 BattleManager를 통해 적 리스트에 접근합니다.
        BattleManager bm = FindObjectOfType<BattleManager>();
        EnemyController targetEnemy = null;

        if (bm == null || !bm.IsPlayerTurn || player == null || player.IsActing)
        {
            CancelPreview();
            return;
        }

        // 1. 현재 필드에서 살아있는 첫 번째 적을 타겟으로 설정
        if (bm != null && bm.activeEnemies != null)
        {
            foreach (var e in bm.activeEnemies)
            {
                if (e != null && e.IsAlive)
                {
                    targetEnemy = e;
                    break;
                }
            }
        }

        if (display != null && display.cardData != null && player != null)
        {
            int cardCost = display.cardData.cost;
            if (player.CanUseCard(cardCost))
            {
                if (display.cardData.damage > 0 && targetEnemy == null)
                {
                    CancelPreview();
                    return;
                }

                // 에너지 소모 및 애니메이션 실행
                player.UseEnergy(cardCost);
                player.PlayCardAnimation(display.cardData);

                // 2. 데미지 로직: 타겟팅된 적이 있을 때만 데미지 입힘
                if (display.cardData.damage > 0 && targetEnemy != null)
                {
                    EnemyController enemyToHit = targetEnemy;
                    int damage = display.cardData.damage;
                    player.PlayAttackMovement(() =>
                    {
                        if (enemyToHit != null && enemyToHit.IsAlive)
                            enemyToHit.TakeDamage(damage);
                    });
                }

                if (display.cardData.heal > 0)
                {
                    player.Heal(display.cardData.heal);
                }

                // 3. 방어력 로직: 방어력과 지속 시간을 함께 전달
                if (display.cardData.shield > 0)
                {
                    player.AddShield(display.cardData.shield, display.cardData.shieldDuration);
                }

                if (display.cardData.oncePerBattle && bm != null)
                {
                    bm.MarkCardUsedThisBattle(display.cardData);
                }

                // 4. 후처리: 프리뷰 해제 및 카드 파괴
                isPreviewing = false;
                currentPreviewCard = null;
                HandLayoutManager hand = GetComponentInParent<HandLayoutManager>();
                if (hand != null)
                {
                    hand.RemoveCard(gameObject);
                }

                if (bm != null)
                    bm.RequestAutoEndTurnCheck();
            }
            else
            {
                // 에너지가 부족하면 사용 취소
                CancelPreview();
            }
        }
    }
}
