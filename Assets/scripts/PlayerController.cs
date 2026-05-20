using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System.Collections.Generic;
using System.Collections;
using System;

[System.Serializable]
public class ShieldBuff
{
    public int amount;
    public int remainingTurns;
}

public class PlayerController : MonoBehaviour
{
    public float HP = 2000;
    public float maxHP = 2000;
    public int maxEnergy = 3;
    public int currentEnergy;

    [Header("방어력 리스트")]
    public List<ShieldBuff> activeShields = new List<ShieldBuff>();

    [Header("UI 연결")]
    public TextMeshProUGUI energyText;
    public TextMeshProUGUI hpText;
    public Slider hpSlider;
    public Slider shieldSlider;

    [Header("공격 이동")]
    public float attackMoveDistance = 1.5f;
    public float attackMoveOutDuration = 0.12f;
    public float attackHoldDuration = 1.5f;
    public float attackMoveBackDuration = 0.16f;

    private Animator anim;
    private bool isDead;
    private bool isActing;
    private Vector3 originalPos;
    private Coroutine attackMoveRoutine;

    public bool IsActing => isActing;

    void Awake()
    {
        anim = GetComponent<Animator>();
        originalPos = transform.position;
        currentEnergy = maxEnergy;
        UpdateEnergyUI();
        UpdateHpUI();
    }

    public void AddShield(int amount, int duration)
    {
        activeShields.Add(new ShieldBuff { amount = amount, remainingTurns = 1 });
        UpdateHpUI();
        Debug.Log($"방어력 획득: {amount}");
    }

    public void TickShieldTurns()
    {
        ClearShields();
    }

    public void ClearShields()
    {
        activeShields.Clear();
        EndDefenseAnimation();
        UpdateHpUI();
    }

    public void EndDefenseAnimation()
    {
        if (anim != null)
        {
            anim.SetBool("Shield", false);
            anim.SetBool("CrouchShield", false);
            Debug.Log("모든 방어 애니메이션 종료");
        }
    }

    public void TakeDamage(int damage)
    {
        float beforeHealthAndShield = HP + GetTotalShield();
        int totalShield = GetTotalShield();
        if (totalShield > 0)
        {
            int damageToRemove = Mathf.Min(totalShield, damage);
            damage -= damageToRemove;

            // 리스트의 실제 수치 깎기
            for (int i = 0; i < activeShields.Count && damageToRemove > 0; i++)
            {
                int subtract = Mathf.Min(activeShields[i].amount, damageToRemove);
                activeShields[i].amount -= subtract;
                damageToRemove -= subtract;
            }
            activeShields.RemoveAll(s => s.amount <= 0);
        }

        if (damage > 0)
        {
            HP -= damage;
            HP = Mathf.Max(0, HP);
        }

        UpdateHpUI();
        int lostAmount = Mathf.RoundToInt(beforeHealthAndShield - (HP + GetTotalShield()));
        DamagePopupManager.ShowDamage(transform.position + Vector3.up * 0.6f, lostAmount);

        if (HP <= 0 && !isDead)
        {
            isDead = true;
            Debug.Log("게임오버!");
            if (GamePresentationManager.Instance != null)
                GamePresentationManager.Instance.ShowDefeat();
        }
    }

    public void Heal(int amount)
    {
        if (amount <= 0 || isDead) return;

        float beforeHp = HP;
        HP = Mathf.Min(HP + amount, maxHP);
        UpdateHpUI();

        int healedAmount = Mathf.RoundToInt(HP - beforeHp);
        if (healedAmount > 0)
            DamagePopupManager.ShowDamage(transform.position + Vector3.up * 0.6f, healedAmount);
    }

    public int GetTotalShield()
    {
        int sum = 0;
        foreach (var s in activeShields) sum += s.amount;
        return sum;
    }

    void UpdateHpUI()
    {
        int totalShield = GetTotalShield();

        // 1. 텍스트 업데이트
        if (hpText != null)
        {
            if (totalShield > 0)
                hpText.text = $"{HP}+{totalShield}/{maxHP}";
            else
                hpText.text = $"{HP}/{maxHP}";
        }

        // 2. HP 슬라이더 업데이트
        if (hpSlider != null)
        {
            hpSlider.maxValue = maxHP;
            hpSlider.value = HP;
        }

        // 3. 방어력 슬라이더 업데이트 (상시 활성화)
        if (shieldSlider != null)
        {
            shieldSlider.maxValue = maxHP;
            shieldSlider.value = totalShield;
            shieldSlider.gameObject.SetActive(true);
        }
    }

    public bool CanUseCard(int cardCost) => currentEnergy >= cardCost;
    public void UseEnergy(int amount) { currentEnergy -= amount; UpdateEnergyUI(); }
    public void ResetEnergy() { currentEnergy = maxEnergy; UpdateEnergyUI(); }
    void UpdateEnergyUI() { if (energyText != null) energyText.text = $"{currentEnergy} / {maxEnergy}"; }

    public void PlayCardAnimation(CardData data)
    {
        if (anim == null || string.IsNullOrEmpty(data.animationParameter)) return;
        if (data.animType == AnimType.Trigger) anim.SetTrigger(data.animationParameter);
        else anim.SetBool(data.animationParameter, true);
    }

    public void PlayAttackMovement(Action onHit)
    {
        if (attackMoveRoutine != null)
        {
            StopCoroutine(attackMoveRoutine);
            transform.position = originalPos;
            isActing = false;
        }

        attackMoveRoutine = StartCoroutine(AttackMoveRoutine(onHit));
    }

    IEnumerator AttackMoveRoutine(Action onHit)
    {
        isActing = true;
        Vector3 startPosition = originalPos;
        Vector3 attackPosition = startPosition + Vector3.right * attackMoveDistance;

        float elapsed = 0f;
        while (elapsed < attackMoveOutDuration)
        {
            transform.position = Vector3.Lerp(startPosition, attackPosition, elapsed / attackMoveOutDuration);
            elapsed += Time.deltaTime;
            yield return null;
        }
        transform.position = attackPosition;

        onHit?.Invoke();
        yield return new WaitForSeconds(attackHoldDuration);

        elapsed = 0f;
        while (elapsed < attackMoveBackDuration)
        {
            transform.position = Vector3.Lerp(attackPosition, startPosition, elapsed / attackMoveBackDuration);
            elapsed += Time.deltaTime;
            yield return null;
        }
        transform.position = startPosition;
        attackMoveRoutine = null;
        isActing = false;
    }
}
