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
        currentHp -= realDamage;
        currentHp = Mathf.Max(0, currentHp);
        UpdateUI();

        DamagePopupManager.ShowDamage(transform.position + Vector3.up * 0.6f, realDamage);
        PlayAnim(enemyData.hurtAnim);
        Debug.Log($"Enemy took damage. Input: {damage}, Defense: {enemyData.defense}, Final: {realDamage}");

        if (currentHp <= 0) Die();
    }

    public void TakeTurn(PlayerController player)
    {
        if (!isAlive) return;

        EnemyAttackData selectedAttack = ChooseAttack();
        int finalDamage = CalculateAttackDamage(selectedAttack);

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
        Debug.Log($"{enemyData.enemyName} defense action is not implemented yet.");
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
        if (hpText != null) hpText.text = $"{currentHp} / {enemyData.Hp}";
        if (hpSlider != null)
        {
            hpSlider.maxValue = enemyData.Hp;
            hpSlider.value = currentHp;
        }

        if (shieldSlider != null)
        {
            shieldSlider.maxValue = enemyData.Hp;
            shieldSlider.value = 0;
        }
    }
}
