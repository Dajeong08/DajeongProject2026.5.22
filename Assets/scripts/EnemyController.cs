using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

public class EnemyController : MonoBehaviour
{
    [Header("Data")]
    public EnemyData enemyData;

    [Header("UI")]
    public GameObject namePlateObject;
    public TextMeshProUGUI nameText;
    public TextMeshProUGUI hpText;
    public Slider hpSlider;
    public Slider shieldSlider;

    [Header("Dash Attack")]
    public float dashDistance = 150f;
    public float dashOutDuration = 0.08f;
    public float dashBackDuration = 0.12f;

    [Header("Attack Movement")]
    public float attackMoveDistance = 1.5f;
    public float attackMoveOutDuration = 0.12f;
    public float attackHoldDuration = 1.5f;
    public float attackMoveBackDuration = 0.16f;

    private int currentHp;
    private Animator anim;
    private Vector3 originalPos;
    private bool isAlive;
    private bool isActing;
    private int currentShield;
    private int shieldTurnsRemaining;
    private int healCooldownRemaining;
    private int defenseCooldownRemaining;

    public bool IsAlive => isAlive;
    public bool IsActing => isActing;

    public void Init()
    {
        if (enemyData == null) return;

        anim = GetComponent<Animator>();
        if (anim != null && enemyData.animatorController != null)
            anim.runtimeAnimatorController = enemyData.animatorController;

        ApplyDirection();

        currentHp = enemyData.Hp;
        currentShield = 0;
        shieldTurnsRemaining = 0;
        healCooldownRemaining = 0;
        defenseCooldownRemaining = 0;
        isAlive = currentHp > 0;
        originalPos = transform.position;
        UpdateUI();
        Debug.Log($"Enemy initialized. HP: {enemyData.Hp}");
    }

    public void SetUiReferences(GameObject namePlate, TextMeshProUGUI nameLabel, TextMeshProUGUI hpLabel, Slider hpBar, Slider shieldBar)
    {
        namePlateObject = namePlate;
        nameText = nameLabel;
        hpText = hpLabel;
        hpSlider = hpBar;
        shieldSlider = shieldBar;
        SetUiVisible(true);
        UpdateUI();
    }

    public void SetUiVisible(bool isVisible)
    {
        if (namePlateObject != null) namePlateObject.SetActive(isVisible);
        if (nameText != null) nameText.gameObject.SetActive(isVisible);
        if (hpText != null) hpText.gameObject.SetActive(isVisible);
        if (hpSlider != null) hpSlider.gameObject.SetActive(isVisible);
        if (shieldSlider != null) shieldSlider.gameObject.SetActive(isVisible);
    }

    void ApplyDirection()
    {
        SpriteRenderer[] spriteRenderers = GetComponentsInChildren<SpriteRenderer>(true);
        foreach (SpriteRenderer spriteRenderer in spriteRenderers)
        {
            spriteRenderer.flipX = enemyData.flipX;
        }
    }

    public void TakeDamage(int damage)
    {
        if (!isAlive) return;

        int realDamage = Mathf.Max(0, damage - enemyData.defense);
        int shieldDamage = Mathf.Min(currentShield, realDamage);
        currentShield -= shieldDamage;
        realDamage -= shieldDamage;

        currentHp -= realDamage;
        currentHp = Mathf.Max(0, currentHp);
        UpdateUI();

        DamagePopupManager.ShowDamage(transform.position + Vector3.up * 0.6f, shieldDamage + realDamage);
        PlayAnim(enemyData.hurtAnim);
        Debug.Log($"Enemy took damage. Input: {damage}, Defense: {enemyData.defense}, Shield: {shieldDamage}, HP Damage: {realDamage}");

        if (currentHp <= 0) Die();
    }

    public void TakeTurn(PlayerController player)
    {
        if (!isAlive) return;

        TickTurnCounters();

        if (TryHeal()) return;
        if (TryDefend()) return;

        EnemyAttackData selectedAttack = ChooseAttack();
        int finalDamage = CalculateAttackDamage(selectedAttack);

        if (enemyData.useDashAttack)
            StartCoroutine(DashAttack(player, selectedAttack, finalDamage));
        else
            StartCoroutine(AttackMoveRoutine(player, selectedAttack, finalDamage));
    }

    public void AttackPlayer(PlayerController player)
    {
        TakeTurn(player);
    }

    IEnumerator DashAttack(PlayerController player, EnemyAttackData attackData, int finalDamage)
    {
        isActing = true;
        Vector3 dashTarget = originalPos + Vector3.left * dashDistance;

        float elapsed = 0f;
        while (elapsed < dashOutDuration)
        {
            transform.position = Vector3.Lerp(originalPos, dashTarget, elapsed / dashOutDuration);
            elapsed += Time.deltaTime;
            yield return null;
        }
        transform.position = dashTarget;

        PlayAttackAnim(attackData);
        player.TakeDamage(finalDamage);

        yield return new WaitForSeconds(attackHoldDuration);

        elapsed = 0f;
        while (elapsed < dashBackDuration)
        {
            transform.position = Vector3.Lerp(dashTarget, originalPos, elapsed / dashBackDuration);
            elapsed += Time.deltaTime;
            yield return null;
        }
        transform.position = originalPos;
        isActing = false;
    }

    IEnumerator AttackMoveRoutine(PlayerController player, EnemyAttackData attackData, int finalDamage)
    {
        isActing = true;
        Vector3 startPosition = originalPos;
        Vector3 attackPosition = startPosition + Vector3.left * attackMoveDistance;

        float elapsed = 0f;
        while (elapsed < attackMoveOutDuration)
        {
            transform.position = Vector3.Lerp(startPosition, attackPosition, elapsed / attackMoveOutDuration);
            elapsed += Time.deltaTime;
            yield return null;
        }
        transform.position = attackPosition;

        AttackWithoutDash(player, attackData, finalDamage);
        yield return new WaitForSeconds(attackHoldDuration);

        elapsed = 0f;
        while (elapsed < attackMoveBackDuration)
        {
            transform.position = Vector3.Lerp(attackPosition, startPosition, elapsed / attackMoveBackDuration);
            elapsed += Time.deltaTime;
            yield return null;
        }
        transform.position = startPosition;
        isActing = false;
    }

    void AttackWithoutDash(PlayerController player, EnemyAttackData attackData, int finalDamage)
    {
        PlayAttackAnim(attackData);
        player.TakeDamage(finalDamage);
    }

    EnemyAttackData ChooseAttack()
    {
        bool canUseAttack1 = enemyData.attack1 != null && enemyData.attack1.canUse;
        bool canUseAttack2 = enemyData.attack2 != null && enemyData.attack2.canUse;

        if (canUseAttack1 && canUseAttack2)
            return Random.value < 0.5f ? enemyData.attack1 : enemyData.attack2;

        if (canUseAttack2) return enemyData.attack2;
        if (canUseAttack1) return enemyData.attack1;

        return null;
    }

    int CalculateAttackDamage(EnemyAttackData attackData)
    {
        if (enemyData.critical != null && enemyData.critical.canUse &&
            Random.Range(0, 100) < enemyData.critical.chancePercent)
        {
            int criticalDamage = enemyData.critical.damage > 0 ? enemyData.critical.damage : enemyData.Damage;
            Debug.Log($"Critical attack! Damage: {criticalDamage}");
            return criticalDamage;
        }

        int finalDamage = attackData != null ? attackData.damage : enemyData.Damage;
        Debug.Log($"Enemy attack. Damage: {finalDamage}");
        return finalDamage;
    }

    void PlayAttackAnim(EnemyAttackData attackData)
    {
        if (attackData == null) return;
        PlayAnim(attackData.animParameter, attackData.animType);
    }

    public void Defend()
    {
        if (enemyData == null || enemyData.defenseAction == null) return;

        currentShield += enemyData.defenseAction.shieldAmount;
        shieldTurnsRemaining = Mathf.Max(1, enemyData.defenseAction.durationTurns);
        defenseCooldownRemaining = Mathf.Max(0, enemyData.defenseAction.cooldownTurns);
        UpdateUI();
        DamagePopupManager.ShowShield(transform.position + Vector3.up * 0.6f, enemyData.defenseAction.shieldAmount);
        Debug.Log($"{enemyData.enemyName} gained shield: {enemyData.defenseAction.shieldAmount}");
    }

    void TickTurnCounters()
    {
        if (healCooldownRemaining > 0)
            healCooldownRemaining--;

        if (defenseCooldownRemaining > 0)
            defenseCooldownRemaining--;

        if (shieldTurnsRemaining > 0)
        {
            shieldTurnsRemaining--;
            if (shieldTurnsRemaining <= 0)
                currentShield = 0;
        }

        UpdateUI();
    }

    bool TryHeal()
    {
        EnemyHealData healData = enemyData.heal;
        if (healData == null || !healData.canUse) return false;
        if (healCooldownRemaining > 0) return false;
        if (healData.healAmount <= 0) return false;
        if (currentHp >= enemyData.Hp) return false;
        if (!IsHpUnderThreshold(healData.hpThresholdPercent)) return false;

        int beforeHp = currentHp;
        currentHp = Mathf.Min(currentHp + healData.healAmount, enemyData.Hp);
        healCooldownRemaining = Mathf.Max(0, healData.cooldownTurns);
        UpdateUI();

        int healedAmount = currentHp - beforeHp;
        DamagePopupManager.ShowHeal(transform.position + Vector3.up * 0.6f, healedAmount);
        Debug.Log($"{enemyData.enemyName} healed: {healedAmount}");
        return true;
    }

    bool TryDefend()
    {
        EnemyDefenseData defenseData = enemyData.defenseAction;
        if (defenseData == null || !defenseData.canUse) return false;
        if (defenseCooldownRemaining > 0) return false;
        if (defenseData.shieldAmount <= 0) return false;
        if (!IsHpUnderThreshold(defenseData.hpThresholdPercent)) return false;

        Defend();
        return true;
    }

    bool IsHpUnderThreshold(int thresholdPercent)
    {
        if (thresholdPercent <= 0) return true;

        float currentPercent = enemyData.Hp > 0
            ? (currentHp / (float)enemyData.Hp) * 100f
            : 0f;

        return currentPercent <= thresholdPercent;
    }

    void Die()
    {
        if (!isAlive) return;

        isAlive = false;
        isActing = false;
        PlayAnim(enemyData.deathAnim);
        StartCoroutine(DieAfterAnim());
    }

    IEnumerator DieAfterAnim()
    {
        yield return new WaitForSeconds(1.0f);
        SetUiVisible(false);
        gameObject.SetActive(false);

        FindObjectOfType<BattleManager>().CheckBattleEnd();
    }

    void PlayAnim(EnemyAnimData animData)
    {
        if (animData == null) return;
        PlayAnim(animData.animParameter, animData.animType);
    }

    void PlayAnim(string parameter, EnemyAnimType animType)
    {
        if (anim == null || string.IsNullOrEmpty(parameter)) return;

        if (animType == EnemyAnimType.Trigger)
            anim.SetTrigger(parameter);
        else
            anim.SetBool(parameter, true);
    }

    void UpdateUI()
    {
        if (nameText != null) nameText.text = enemyData.enemyName;
        if (hpText != null)
        {
            hpText.text = currentShield > 0
                ? $"{currentHp}+{currentShield} / {enemyData.Hp}"
                : $"{currentHp} / {enemyData.Hp}";
        }
        if (hpSlider != null)
        {
            hpSlider.maxValue = enemyData.Hp;
            hpSlider.value = currentHp;
        }

        if (shieldSlider != null)
        {
            shieldSlider.maxValue = enemyData.Hp;
            shieldSlider.value = currentShield;
        }
    }
}
