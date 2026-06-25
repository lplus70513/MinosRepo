using System.IO;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

public class LeaderboardSetupTool : EditorWindow
{
    [MenuItem("Tools/排行榜系统/一键创建全部")]
    public static void SetupAll()
    {
        CreateLeaderboardEntryViewPrefab();
        CreateLeaderboardPanelPrefab();
        UpdateSettingsPanelPrefab();
        SetupWinLosePanelsInScene();

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log("[排行榜系统] 全部配置完成！");
        EditorUtility.DisplayDialog("排行榜系统", "配置完成！\n\n已完成:\n- LeaderboardEntryView.prefab\n- LeaderboardPanel.prefab\n- 设置界面中的排行榜按钮\n- 确认主菜单新游戏按钮", "确定");
    }

    [MenuItem("Tools/排行榜系统/3-创建 LeaderboardEntryView Prefab")]
    public static void CreateLeaderboardEntryViewPrefab()
    {
        string prefabPath = "Assets/Prefabs/LeaderboardEntryView.prefab";
        if (File.Exists(prefabPath))
        {
            Debug.Log("[排行榜系统] LeaderboardEntryView.prefab 已存在，跳过");
            return;
        }

        var root = new GameObject("LeaderboardEntryView", typeof(RectTransform));
        var rootRt = root.GetComponent<RectTransform>();
        rootRt.sizeDelta = new Vector2(700, 60);

        var layout = root.AddComponent<HorizontalLayoutGroup>();
        layout.childAlignment = TextAnchor.MiddleLeft;
        layout.childControlWidth = false;
        layout.childControlHeight = false;
        layout.childForceExpandWidth = false;
        layout.childForceExpandHeight = true;
        layout.spacing = 15;
        layout.padding = new RectOffset(10, 10, 5, 5);

        root.AddComponent<ContentSizeFitter>().horizontalFit = ContentSizeFitter.FitMode.PreferredSize;

        var bg = root.AddComponent<Image>();
        bg.color = new Color(0.1f, 0.1f, 0.15f, 0.8f);

        root.AddComponent<LeaderboardEntryView>();

        CreateTextChild(root.transform, "rankText", 50, 50, "1", 28, TextAlignmentOptions.Center);
        CreateTextChild(root.transform, "nameText", 120, 50, "玩家名", 24, TextAlignmentOptions.Left);
        CreateTextChild(root.transform, "scoreText", 100, 50, "0", 32, TextAlignmentOptions.Center);
        CreateTextChild(root.transform, "detailText", 380, 50, "第1层 | 击杀0 | 金币0 | 胜利 | 2024-01-01", 20, TextAlignmentOptions.Left);

        var prefab = PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
        DestroyImmediate(root);
        Debug.Log($"[排行榜系统] 已创建: {prefabPath}");
    }

    [MenuItem("Tools/排行榜系统/4-创建 LeaderboardPanel Prefab")]
    public static void CreateLeaderboardPanelPrefab()
    {
        string prefabPath = "Assets/Prefabs/LeaderboardPanel.prefab";
        if (File.Exists(prefabPath))
        {
            Debug.Log("[排行榜系统] LeaderboardPanel.prefab 已存在，尝试更新...");
        }

        var root = new GameObject("LeaderboardPanel", typeof(RectTransform));
        var rootRt = root.GetComponent<RectTransform>();
        rootRt.anchorMin = Vector2.zero;
        rootRt.anchorMax = Vector2.one;
        rootRt.sizeDelta = Vector2.zero;

        var bg = root.AddComponent<Image>();
        bg.color = new Color(0, 0, 0, 0.85f);

        root.AddComponent<LeaderboardPanelController>();

        CreateTextChild(root.transform, "emptyText", 400, 50, "暂无排行榜记录", 32, TextAlignmentOptions.Center,
            anchoredPos: Vector2.zero, anchorMin: new Vector2(0.5f, 0.5f), anchorMax: new Vector2(0.5f, 0.5f));

        var title = CreateTextChild(root.transform, "Title", 300, 50, "排行榜", 40, TextAlignmentOptions.Center,
            anchoredPos: new Vector2(0, -50), anchorMin: new Vector2(0.5f, 1), anchorMax: new Vector2(0.5f, 1));
        var titleRt = title.GetComponent<RectTransform>();
        titleRt.pivot = new Vector2(0.5f, 1);

        var scrollViewGo = new GameObject("ScrollView", typeof(RectTransform), typeof(ScrollRect), typeof(Image));
        scrollViewGo.transform.SetParent(root.transform, false);
        var scrollRt = scrollViewGo.GetComponent<RectTransform>();
        scrollRt.anchorMin = new Vector2(0.05f, 0.15f);
        scrollRt.anchorMax = new Vector2(0.95f, 0.85f);
        scrollRt.sizeDelta = Vector2.zero;
        scrollViewGo.GetComponent<Image>().color = new Color(0.05f, 0.05f, 0.08f, 0.6f);

        var viewport = new GameObject("Viewport", typeof(RectTransform), typeof(Image), typeof(Mask));
        viewport.transform.SetParent(scrollViewGo.transform, false);
        var viewportRt = viewport.GetComponent<RectTransform>();
        viewportRt.anchorMin = Vector2.zero;
        viewportRt.anchorMax = Vector2.one;
        viewportRt.sizeDelta = Vector2.zero;
        viewport.GetComponent<Image>().color = Color.clear;
        viewport.AddComponent<Mask>().showMaskGraphic = false;

        var content = new GameObject("Content", typeof(RectTransform), typeof(VerticalLayoutGroup), typeof(ContentSizeFitter));
        content.transform.SetParent(viewport.transform, false);
        var contentRt = content.GetComponent<RectTransform>();
        contentRt.anchorMin = new Vector2(0, 1);
        contentRt.anchorMax = new Vector2(1, 1);
        contentRt.pivot = new Vector2(0.5f, 1);
        contentRt.sizeDelta = new Vector2(0, 0);

        var vlg = content.GetComponent<VerticalLayoutGroup>();
        vlg.childAlignment = TextAnchor.UpperCenter;
        vlg.childControlWidth = true;
        vlg.childControlHeight = false;
        vlg.childForceExpandWidth = true;
        vlg.childForceExpandHeight = false;
        vlg.spacing = 5;
        vlg.padding = new RectOffset(5, 5, 10, 10);

        var csf = content.GetComponent<ContentSizeFitter>();
        csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        var scrollRect = scrollViewGo.GetComponent<ScrollRect>();
        scrollRect.content = contentRt;
        scrollRect.viewport = viewportRt;
        scrollRect.horizontal = false;
        scrollRect.vertical = true;
        scrollRect.movementType = ScrollRect.MovementType.Clamped;

        var closeBtnGo = new GameObject("closeButton", typeof(RectTransform), typeof(Image), typeof(Button));
        closeBtnGo.transform.SetParent(root.transform, false);
        var closeRt = closeBtnGo.GetComponent<RectTransform>();
        closeRt.anchorMin = new Vector2(1, 1);
        closeRt.anchorMax = new Vector2(1, 1);
        closeRt.pivot = new Vector2(1, 1);
        closeRt.anchoredPosition = new Vector2(-20, -20);
        closeRt.sizeDelta = new Vector2(40, 40);
        closeBtnGo.GetComponent<Image>().color = new Color(0.8f, 0.2f, 0.2f);
        var closeText = CreateTextChild(closeBtnGo.transform, "X", 40, 40, "X", 24, TextAlignmentOptions.Center);
        closeText.GetComponent<RectTransform>().anchorMin = Vector2.zero;
        closeText.GetComponent<RectTransform>().anchorMax = Vector2.one;
        closeText.GetComponent<RectTransform>().sizeDelta = Vector2.zero;

        var clearBtnGo = new GameObject("clearButton", typeof(RectTransform), typeof(Image), typeof(Button));
        clearBtnGo.transform.SetParent(root.transform, false);
        var clearRt = clearBtnGo.GetComponent<RectTransform>();
        clearRt.anchorMin = new Vector2(0.5f, 0);
        clearRt.anchorMax = new Vector2(0.5f, 0);
        clearRt.pivot = new Vector2(0.5f, 0);
        clearRt.anchoredPosition = new Vector2(0, 50);
        clearRt.sizeDelta = new Vector2(160, 40);
        clearBtnGo.GetComponent<Image>().color = new Color(0.5f, 0.1f, 0.1f, 0.8f);
        var clearLabel = CreateTextChild(clearBtnGo.transform, "ClearLabel", 160, 40, "清空排行榜", 20, TextAlignmentOptions.Center);
        clearLabel.GetComponent<RectTransform>().anchorMin = Vector2.zero;
        clearLabel.GetComponent<RectTransform>().anchorMax = Vector2.one;
        clearLabel.GetComponent<RectTransform>().sizeDelta = Vector2.zero;

        var panelCtrl = root.GetComponent<LeaderboardPanelController>();
        var ctrlSo = new SerializedObject(panelCtrl);
        ctrlSo.FindProperty("entryContainer").objectReferenceValue = content.transform;
        ctrlSo.FindProperty("closeButton").objectReferenceValue = closeBtnGo.GetComponent<Button>();
        ctrlSo.FindProperty("clearButton").objectReferenceValue = clearBtnGo.GetComponent<Button>();
        ctrlSo.FindProperty("emptyText").objectReferenceValue = root.transform.Find("emptyText")?.GetComponent<TMP_Text>();
        ctrlSo.ApplyModifiedProperties();

        var prefab = PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
        DestroyImmediate(root);

        var entryViewPrefabGo = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/LeaderboardEntryView.prefab");
        if (entryViewPrefabGo != null)
        {
            var entryComp = entryViewPrefabGo.GetComponent<LeaderboardEntryView>();
            var prefabSo = new SerializedObject(prefab.GetComponent<LeaderboardPanelController>());
            prefabSo.FindProperty("entryPrefab").objectReferenceValue = entryComp;
            prefabSo.ApplyModifiedProperties();
            EditorUtility.SetDirty(prefab);
        }

        Debug.Log($"[排行榜系统] 已创建: {prefabPath}");
    }

    [MenuItem("Tools/排行榜系统/5-更新设置界面 Prefab")]
    public static void UpdateSettingsPanelPrefab()
    {
        string prefabPath = "Assets/Prefabs/设置界面.prefab";
        string absolutePath = Path.Combine(Application.dataPath, "../", prefabPath);

        if (!File.Exists(absolutePath))
        {
            Debug.LogError($"[排行榜系统] 找不到 {prefabPath} (full: {absolutePath})");
            return;
        }

        var prefabContents = PrefabUtility.LoadPrefabContents(absolutePath);
        try
        {
            var settingManager = prefabContents.GetComponentInChildren<SettingManager>(true);
            if (settingManager == null)
            {
                Debug.LogError("[排行榜系统] 找不到 SettingManager 组件");
                return;
            }

            var so = new SerializedObject(settingManager);
            var leaderboardBtnProp = so.FindProperty("leaderboardButton");
            var leaderboardPanelProp = so.FindProperty("leaderboardSubPanel");

            if (leaderboardBtnProp != null && leaderboardBtnProp.objectReferenceValue != null
                && leaderboardPanelProp != null && leaderboardPanelProp.objectReferenceValue != null)
            {
                Debug.Log("[排行榜系统] 排行榜按钮和面板引用已存在，跳过");
                return;
            }

            Transform buttonsRoot = FindButtonsContainer(prefabContents.transform);
            if (buttonsRoot == null)
            {
                Debug.LogError("[排行榜系统] 找不到按钮容器");
                return;
            }

            var templateBtn = buttonsRoot.Find("设置") ?? buttonsRoot.Find("继续");
            if (templateBtn == null)
            {
                Debug.LogError("[排行榜系统] 找不到模板按钮");
                return;
            }

            if (leaderboardBtnProp != null && leaderboardBtnProp.objectReferenceValue == null)
            {
                var leaderboardBtnGo = Instantiate(templateBtn.gameObject, buttonsRoot);
                leaderboardBtnGo.name = "排行榜";
                leaderboardBtnGo.transform.SetSiblingIndex(templateBtn.GetSiblingIndex() + 1);

                var btnRect = leaderboardBtnGo.GetComponent<RectTransform>();
                if (btnRect != null)
                {
                    btnRect.anchoredPosition = new Vector2(-25, -175);
                }

                var btnText = leaderboardBtnGo.GetComponentInChildren<TMP_Text>();
                if (btnText != null) btnText.text = "排行榜";

                leaderboardBtnProp.objectReferenceValue = leaderboardBtnGo.GetComponent<Button>();
            }

            if (leaderboardPanelProp != null && leaderboardPanelProp.objectReferenceValue == null)
            {
                var panelPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/LeaderboardPanel.prefab");
                if (panelPrefab != null)
                {
                    var panelInstance = Instantiate(panelPrefab, prefabContents.transform);
                    panelInstance.name = "LeaderboardSubPanel";
                    panelInstance.SetActive(false);
                    leaderboardPanelProp.objectReferenceValue = panelInstance;
                }
                else
                {
                    Debug.LogWarning("[排行榜系统] LeaderboardPanel.prefab 未找到，创建空占位面板");
                    var panelGo = new GameObject("LeaderboardSubPanel", typeof(RectTransform));
                    panelGo.transform.SetParent(prefabContents.transform, false);
                    panelGo.SetActive(false);
                    var panelRt = panelGo.GetComponent<RectTransform>();
                    panelRt.anchorMin = new Vector2(0.5f, 0.5f);
                    panelRt.anchorMax = new Vector2(0.5f, 0.5f);
                    panelRt.anchoredPosition = Vector2.zero;
                    panelRt.sizeDelta = new Vector2(600, 400);
                    panelGo.AddComponent<CanvasRenderer>();
                    panelGo.AddComponent<Image>().color = new Color(0, 0, 0, 0.85f);
                    panelGo.AddComponent<LeaderboardPanelController>();
                    leaderboardPanelProp.objectReferenceValue = panelGo;
                }
            }

            so.ApplyModifiedProperties();
            PrefabUtility.SaveAsPrefabAsset(prefabContents, absolutePath);
            Debug.Log("[排行榜系统] 设置界面 Prefab 已更新");
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(prefabContents);
        }
    }

    [MenuItem("Tools/排行榜系统/6-确认胜负面板配置")]
    public static void SetupWinLosePanelsInScene()
    {
        Debug.Log("[排行榜系统] GameWinPanel 和 GameLosePanel 需要手动配置 UI 文本组件");
        Debug.Log("  - 在 GameWinPanel 下添加 TMP_Text: playerNameText, scoreText, statsText 和 Button: saveToLeaderboardButton");
        Debug.Log("  - 在 GameLosePanel 下添加 TMP_Text: playerNameText, scoreText, statsText 和 Button: saveToLeaderboardButton");
        Debug.Log("  - 将组件拖入对应 Controller 的序列化字段");

        Debug.Log("[排行榜系统] 主菜单\"新游戏\"按钮确认:");
        Debug.Log("  - 确认 1_MainMenu 场景中的\"新游戏\"按钮 onClick 绑定到 GameManager.Instance.GameStart()");
    }

    private static Transform FindButtonsContainer(Transform root)
    {
        var continueBtn = root.Find("继续");
        if (continueBtn != null) return continueBtn.parent;
        var settingsBtn = root.Find("设置");
        if (settingsBtn != null) return settingsBtn.parent;
        foreach (Transform child in root)
        {
            var result = FindButtonsContainer(child);
            if (result != null) return result;
        }
        return null;
    }

    private static GameObject CreateTextChild(Transform parent, string name, float w, float h, string text, float fontSize,
        TextAlignmentOptions alignment, Vector2? anchoredPos = null, Vector2? anchorMin = null, Vector2? anchorMax = null)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(TMP_Text));
        go.transform.SetParent(parent, false);
        var rt = go.GetComponent<RectTransform>();
        rt.sizeDelta = new Vector2(w, h);
        if (anchoredPos.HasValue) rt.anchoredPosition = anchoredPos.Value;
        if (anchorMin.HasValue) rt.anchorMin = anchorMin.Value;
        if (anchorMax.HasValue) rt.anchorMax = anchorMax.Value;

        var tmp = go.GetComponent<TMP_Text>();
        tmp.text = text;
        tmp.fontSize = fontSize;
        tmp.alignment = alignment;
        tmp.color = Color.white;

        TMP_FontAsset fontAsset = GetFontAsset();
        if (fontAsset != null) tmp.font = fontAsset;

        return go;
    }

    private static TMP_FontAsset GetFontAsset()
    {
        string[] guids = AssetDatabase.FindAssets("t:TMP_FontAsset");
        foreach (var guid in guids)
        {
            var path = AssetDatabase.GUIDToAssetPath(guid);
            if (path.Contains("LiberationSans") || path.Contains("SourceHan") || path.Contains("Noto"))
                return AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(path);
        }
        if (guids.Length > 0)
            return AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(AssetDatabase.GUIDToAssetPath(guids[0]));
        return null;
    }
}
