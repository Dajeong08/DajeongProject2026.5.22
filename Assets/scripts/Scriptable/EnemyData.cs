using UnityEngine;
public enum EnemyAnimType { Trigger, Bool }

[System.Serializable]
public class EnemyAnimData
{
    public string animName;
    public string animParameter;
    public EnemyAnimType animType;
}

[System.Serializable]
public class EnemyAttackData
{
    public bool canUse;
    public int damage;
    public string animParameter;
    public EnemyAnimType animType;
}

[System.Serializable]
public class EnemyCriticalData
{
    public bool canUse;
    [Range(0, 100)] public int chancePercent = 5;
    public int damage;
}

[System.Serializable]
public class EnemyHealData
{
    public bool canUse;
    public int healAmount;
    [Range(0, 100)] public int hpThresholdPercent;
    public int cooldownTurns;
}

[System.Serializable]
public class EnemyDefenseData
{
    public bool canUse;
    public int shieldAmount;
    public int durationTurns = 1;
    [Range(0, 100)] public int hpThresholdPercent;
    public int cooldownTurns;
}

[CreateAssetMenu(fileName = "New Enemy", menuName = "ScriptableObjects/EnemyData")]
public class EnemyData : ScriptableObject
{
    [Header("Basic")]
    public string enemyName;
    public int Hp;
    public int Damage;
    public int defense;

    [Header("Animator")]
    public RuntimeAnimatorController animatorController;

    [Header("Direction")]
    public bool flipX;

    [Header("Attack Movement")]
    public bool useDashAttack;

    [Header("Actions")]
    public EnemyAttackData attack1 = new EnemyAttackData { canUse = true, damage = 200 };
    public EnemyAttackData attack2 = new EnemyAttackData();
    public EnemyCriticalData critical = new EnemyCriticalData();
    public EnemyHealData heal = new EnemyHealData();
    public EnemyDefenseData defenseAction = new EnemyDefenseData();

    [Header("Reaction Animations")]
    public EnemyAnimData hurtAnim = new EnemyAnimData();
    public EnemyAnimData deathAnim = new EnemyAnimData();
}
