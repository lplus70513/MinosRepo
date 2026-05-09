using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class DeckViewer : MonoBehaviour
{
    [Header("UI 面板引用")]
    public GameObject viewerPanel; // 整个查看面板的 GameObject

    [Header("文本显示引用")]
    public TextMeshProUGUI drawPileText;   // 抽牌堆数量文本
    public TextMeshProUGUI discardPileText; // 弃牌堆数量文本
    public TextMeshProUGUI handText;       // 手牌数量文本

    // 可以在这里添加更复杂的 UI，比如卡牌列表的 Content 父物体
    // public Transform cardListContent; 

    private void Start()
    {
        // 初始状态隐藏面板
        if (viewerPanel != null) viewerPanel.SetActive(false);
    }

    // 绑定到按钮：打开查看器
    public void OpenViewer()
    {
        if (CardSystem.Instance == null) return;

        UpdateData();
        if (viewerPanel != null) viewerPanel.SetActive(true);

        // 可选：暂停游戏时间
        // Time.timeScale = 0; 
    }

    // 绑定到按钮：关闭查看器
    public void CloseViewer()
    {
        if (viewerPanel != null) viewerPanel.SetActive(false);

        // 可选：恢复游戏时间
        // Time.timeScale = 1;
    }

    // 更新 UI 数据
    private void UpdateData()
    {
        if (CardSystem.Instance == null) return;

        // 获取数据副本
        var drawPile = CardSystem.Instance.GetDrawPileCopy();
        var discardPile = CardSystem.Instance.GetDiscardPileCopy();
        var hand = CardSystem.Instance.GetHandCopy();

        // 更新文本
        if (drawPileText != null) drawPileText.text = $"抽牌堆: {drawPile.Count} 张";
        if (discardPileText != null) discardPileText.text = $"弃牌堆: {discardPile.Count} 张";
        if (handText != null) handText.text = $"手牌: {hand.Count} 张";

        // [进阶] 如果你想显示具体的卡牌列表：
        // DisplayCardList(drawPile); 
    }

    // [进阶] 显示具体卡牌列表的示例逻辑
    /*
    private void DisplayCardList(List<Card> cards)
    {
        // 1. 清空旧的 UI 元素
        // foreach (Transform child in cardListContent) Destroy(child.gameObject);

        // 2. 遍历数据生成新 UI
        // foreach (var card in cards)
        // {
        //     GameObject newCardUI = Instantiate(cardPrefab, cardListContent);
        //     newCardUI.GetComponent<CardView>().Setup(card.Data); // 假设你有预制体和 Setup 方法
        // }
    }
    */
}