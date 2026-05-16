using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using TMPro;

// ✅ 외부의 NodeType과 충돌하지 않도록 명확하게 정의
public enum NodeType { Battle, HardBattle, Heal, Boss }

public class MapManager : MonoBehaviour
{
    // ✅ 1. HealItem을 클래스 안으로 이동 (중복 정의 에러 해결)
    [System.Serializable]
    public class HealItem
    {
        public string itemName;
        public int healAmount;
        public Sprite itemSprite;
    }

    [System.Serializable]
    public class MapLine
    {
        public List<Button> buttonsInLine;
    }

    [Header("UI Panels")]
    public GameObject mapPanel;
    public GameObject battleUI;
    public GameObject healPanel;

    [Header("Heal System UI")]
    public Image fruitImage;
    public TextMeshProUGUI healText;
    public List<HealItem> fruitList; // 이제 이 리스트는 위에서 정의한 클래스를 씁니다.
    private HealItem selectedFruit;

    [Header("Map Configuration")]
    public List<MapLine> mapLines;
    private int currentRound = 0;

    [Header("Managers & Player")]
    public BattleManager battleManager;
    public PlayerController player;

    void Start()
    {
        mapPanel.SetActive(true);
        battleUI.SetActive(false);
        if (healPanel) healPanel.SetActive(false);

        InitMap();
    }

    public void InitMap()
    {
        for (int i = 0; i < mapLines.Count; i++)
        {
            bool isRowActive = (i == currentRound);
            foreach (Button btn in mapLines[i].buttonsInLine)
            {
                btn.interactable = isRowActive;
            }
        }
    }

    // (NodeType 및 MapManager 클래스 상단 생략 - 제시하신 것과 동일)

    public void SelectNode(string nodeTypeStr)
    {
        if (currentRound < mapLines.Count)
        {
            foreach (Button btn in mapLines[currentRound].buttonsInLine)
            {
                btn.interactable = false;
            }
        }

        NodeType type = (NodeType)System.Enum.Parse(typeof(NodeType), nodeTypeStr);

        // 전투 관련 노드일 경우
        if (type == NodeType.Battle || type == NodeType.HardBattle || type == NodeType.Boss)
        {
            mapPanel.SetActive(false);
            battleUI.SetActive(true);
            // ✅ 수정: 문자열 타입 대신 현재 라운드 번호(0, 1, 2...)를 넘깁니다.
            battleManager.PrepareBattle(currentRound, type);
        }
        else if (type == NodeType.Heal)
        {
            SetupRandomHeal();
            healPanel.SetActive(true);
        }
    }

    void SetupRandomHeal()
    {
        if (fruitList == null || fruitList.Count == 0) return;

        int randomIndex = Random.Range(0, fruitList.Count);
        selectedFruit = fruitList[randomIndex];

        if (fruitImage != null) fruitImage.sprite = selectedFruit.itemSprite;
        if (healText != null)
            healText.text = $"{selectedFruit.itemName} 발견!\n회복량: {selectedFruit.healAmount}";
    }

    public void OnHealConfirm()
    {
        if (selectedFruit != null && player != null)
        {
            player.HP = Mathf.Min(player.HP + selectedFruit.healAmount, player.maxHP);
            player.SendMessage("UpdateHpUI", SendMessageOptions.DontRequireReceiver);
            if (GamePresentationManager.Instance != null)
                GamePresentationManager.Instance.PlayHealButton();

            healPanel.SetActive(false);
            FinishRound();
        }
    }

    public void FinishRound()
    {
        currentRound++;
        if (currentRound < mapLines.Count)
        {
            mapPanel.SetActive(true);
            battleUI.SetActive(false);
            UpdateMapButtons();
        }
        else
        {
            Debug.Log("게임 클리어!");
            if (GamePresentationManager.Instance != null)
                GamePresentationManager.Instance.ShowGameClear();
        }
    }

    void UpdateMapButtons()
    {
        for (int i = 0; i < mapLines.Count; i++)
        {
            foreach (Button btn in mapLines[i].buttonsInLine)
            {
                btn.interactable = (i == currentRound);
            }
        }
    }
}
