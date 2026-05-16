using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System.Collections.Generic;

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

    private Animator anim;
    private bool isDead;

    void Awake()
    {
        anim = GetComponent<Animator>();
        currentEnergy = maxEnergy;
        UpdateEnergyUI();
        UpdateHpUI();
    }

    // 방어력 추가 (지속 시간 포함)
    public void AddShield(int amount, int duration)
    {
        activeShields.Add(new ShieldBuff { amount = amount, remainingTurns = duration });
        UpdateHpUI();
        Debug.Log($"방어력 획득: {amount} (지속: {duration}턴)");
    }

    // 턴마다 방어력 지속시간 감소 및 애니메이션 해제 판단
    public void TickShieldTurns()
    {
        for (int i = activeShields.Count - 1; i >= 0; i--)
        {
            activeShields[i].remainingTurns--;
            if (activeShields[i].remainingTurns <= 0)
            {
                activeShields.RemoveAt(i);
            }
        }

        // 모든 방어력이 사라지면 애니메이션 해제
        if (activeShields.Count == 0) EndDefenseAnimation();

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

            // ✅ 이 부분을 true로 고정하거나 아래 줄처럼 항상 켜두게 합니다.
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
}
