using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class LeaderboardPanelController : MonoBehaviour
{
    [SerializeField] private Transform entryContainer;
    [SerializeField] private LeaderboardEntryView entryPrefab;
    [SerializeField] private Button closeButton;
    [SerializeField] private Button clearButton;
    [SerializeField] private TMP_Text emptyText;

    private List<LeaderboardEntryView> _spawnedViews = new();

    void Awake()
    {
        if (closeButton != null)
            closeButton.onClick.AddListener(OnClose);
        if (clearButton != null)
            clearButton.onClick.AddListener(OnClear);
    }

    void OnEnable()
    {
        Refresh();
    }

    private void Refresh()
    {
        ClearEntries();

        var entries = LeaderboardSystem.Instance?.Entries;
        if (entries == null || entries.Count == 0)
        {
            if (emptyText != null)
                emptyText.gameObject.SetActive(true);
            return;
        }

        if (emptyText != null)
            emptyText.gameObject.SetActive(false);

        for (int i = 0; i < entries.Count; i++)
        {
            var view = Instantiate(entryPrefab, entryContainer);
            view.Setup(i + 1, entries[i]);
            _spawnedViews.Add(view);
        }
    }

    private void ClearEntries()
    {
        foreach (var view in _spawnedViews)
        {
            if (view != null)
                Destroy(view.gameObject);
        }
        _spawnedViews.Clear();
    }

    private void OnClose()
    {
        gameObject.SetActive(false);
    }

    private void OnClear()
    {
        LeaderboardSystem.Instance?.ClearLeaderboard();
        Refresh();
    }
}
