using UnityEngine;
using System.Collections.Generic;

public enum EnemyAnimType { Trigger, Bool }

[System.Serializable]
public class EnemyAnimData
{
    public string animName;        // 이 애니메이션 용도 (예: "강공격")
    public string animParameter;   // Animator 파라미터 이름
    public EnemyAnimType animType;
}

[CreateAssetMenu(fileName = "New Enemy", menuName = "ScriptableObjects/EnemyData")]
public class EnemyData : ScriptableObject
{
    [Header("기본 정보")]
    public string enemyName;
    public int Hp;
    public int Damage;
    public int defense;

    [Header("애니메이터")]
    public RuntimeAnimatorController animatorController; // ✅ 핵심!

    [Header("공격 애니메이션")]
    public List<EnemyAnimData> attackAnims;

    [Header("피격 애니메이션")]
    public List<EnemyAnimData> hurtAnims;

    [Header("방어 애니메이션")]
    public List<EnemyAnimData> defenseAnims;

    [Header("사망 애니메이션")]
    public List<EnemyAnimData> dieAnims;
}