using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using System.Collections.Generic;

public class EnemyController : MonoBehaviour
{
    [Header("적 데이터")]
    public EnemyData enemyData;

    [Header("UI 연결")]
    public TextMeshProUGUI nameText;
    public TextMeshProUGUI hpText;
    public Slider hpSlider;

    [Header("돌진 설정")]
    public float dashDistance = 150f;  // 얼마나 앞으로 올지
    public float dashSpeed = 10f;      // 속도

    private int currentHp;
    private Animator anim;
    private Vector3 originalPos;      // 원래 위치 저장

    public void Init()
    {
        if (enemyData == null) return;

        anim = GetComponent<Animator>();
        if (enemyData.animatorController != null)
            anim.runtimeAnimatorController = enemyData.animatorController;

        currentHp = enemyData.Hp;
        originalPos = transform.position;  // ✅ 원래 위치 저장
        UpdateUI();
        Debug.Log($"Init 호출됨! HP: {enemyData.Hp}");
    }

    public void TakeDamage(int damage)
    {
        int realDamage = Mathf.Max(0, damage - enemyData.defense);
        currentHp -= realDamage;
        currentHp = Mathf.Max(0, currentHp);
        UpdateUI();

        PlayRandomAnim(enemyData.hurtAnims);
        Debug.Log($"들어온 데미지: {damage}, 방어력: {enemyData.defense}, 실제 데미지: {realDamage}");

        if (currentHp <= 0) Die();
    }

    // ✅ 돌진 후 공격
    public void AttackPlayer(PlayerController player)
    {
        StartCoroutine(DashAttack(player));
    }

    IEnumerator DashAttack(PlayerController player)
    {
        // 1. 빠르게 돌진
        Vector3 dashTarget = originalPos + Vector3.left * dashDistance;

        float elapsed = 0f;
        while (elapsed < 0.08f)
        {
            transform.position = Vector3.Lerp(transform.position, dashTarget, elapsed / 0.08f);
            elapsed += Time.deltaTime;
            yield return null;
        }
        transform.position = dashTarget;

        // --- 데미지 계산 로직 추가 ---
        int finalDamage;

        // 5% 확률로 크리티컬 터짐 (0~100 사이 랜덤 값이 5보다 작으면 실행)
        if (Random.Range(0, 100) < 5)
        {
            finalDamage = 777;
            Debug.Log("<color=red>★ 크리티컬 발생! ★</color> 데미지: 777");
        }
        else
        {
            // 기본 공격: 200 ~ 500 사이 랜덤 (Max값은 포함되지 않으므로 501 설정)
            finalDamage = Random.Range(200, 501);
            Debug.Log($"적 공격! 데미지: {finalDamage}");
        }
        // ---------------------------

        // 2. 공격 애니메이션 + 계산된 데미지 입히기
        PlayRandomAnim(enemyData.attackAnims);
        player.TakeDamage(finalDamage);

        yield return new WaitForSeconds(0.1f);

        // 3. 빠르게 복귀
        elapsed = 0f;
        while (elapsed < 0.12f)
        {
            transform.position = Vector3.Lerp(transform.position, originalPos, elapsed / 0.12f);
            elapsed += Time.deltaTime;
            yield return null;
        }
        transform.position = originalPos;
    }

    public void Defend()
    {
        PlayRandomAnim(enemyData.defenseAnims);
    }

    void Die()
    {
        PlayRandomAnim(enemyData.dieAnims);
        StartCoroutine(DieAfterAnim());
        
    }

    IEnumerator DieAfterAnim()
    {
        yield return new WaitForSeconds(1.0f);
        gameObject.SetActive(false);

        FindObjectOfType<BattleManager>().CheckBattleEnd();
    }

    void PlayRandomAnim(List<EnemyAnimData> animList)
    {
        if (anim == null || animList == null || animList.Count == 0) return;
        int randomIndex = Random.Range(0, animList.Count);
        PlayAnim(animList[randomIndex].animParameter, animList[randomIndex].animType);
    }

    void PlayAnim(string parameter, EnemyAnimType animType)
    {
        if (string.IsNullOrEmpty(parameter)) return;
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
    }
}