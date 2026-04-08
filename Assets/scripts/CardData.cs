using UnityEngine;

[CreateAssetMenu(fileName = "New Card", menuName = "ScriptableObjects/CardData")]
public class CardData : ScriptableObject
{
    public string cardName;    // 카드 이름
    public int cost;           // 사용 비용 (에너지)
    public int damage;         // 적에게 줄 데미지
    public int shield;         // 내가 얻을 방어력
    public Sprite cardImage;   // 카드에 들어갈 그림
    [TextArea]
    public string description; // 카드 설명 (여러 줄 입력 가능)
}
