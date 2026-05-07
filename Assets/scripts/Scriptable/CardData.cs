using UnityEngine;

public enum AnimType { Trigger, Bool } // 애니메이션 방식 정의

[CreateAssetMenu(fileName = "New Card", menuName = "ScriptableObjects/CardData")]
public class CardData : ScriptableObject
{
    public string cardName;
    public int cost;
    public int damage;
    public int shield;
    public int shieldDuration; // 방어력 지속 턴수 (예: 3)
    public Sprite cardImage;
    [TextArea]
    public string description;

    [Header("애니메이션 설정")]
    public string animationParameter; // 파라미터 이름 (예: "Slash", "isGuarding")
    public AnimType animType;         // Trigger인지 Bool인지 선택
}