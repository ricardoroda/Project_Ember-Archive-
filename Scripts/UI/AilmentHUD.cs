using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

public class AilmentHUD : MonoBehaviour
{
    [Header("References")]
    public PlayerStats stats;
    [Tooltip("Parent RectTransform with a LayoutGroup (Grid/Horizontal)")]
    public RectTransform ailmentPanel;
    [Tooltip("Prefab with an Image (icon) and a TextMeshProUGUI (percent)")]
    public GameObject ailmentCellPrefab;
    [Tooltip("Sprites order: 0=Fire(Burn),1=Water,2=Air,3=Earth,4=Arcane")]
    public Sprite[] ailmentIcons = new Sprite[5];

    // internal
    Dictionary<int, GameObject> cells = new Dictionary<int, GameObject>();
    int[] prevStacks = new int[5];

    void OnEnable()
    {
        TrySubscribeToStats();
    }

    void OnDisable()
    {
        UnsubscribeFromStats();
        ClearAllCells();
    }

    void Start()
    {
        if (stats == null) stats = FindFirstObjectByType<PlayerStats>();

        for (int i = 0; i < prevStacks.Length; i++) prevStacks[i] = -1;

        TrySubscribeToStats();

        // initial read so pre-existing stacks show immediately
        RefreshAll();
    }

    void Update()
    {
        if (stats == null) return;

        for (int i = 0; i < 5; i++)
        {
            int stacks = stats.ailmentCurrentStacks[i];
            if (prevStacks[i] == stacks) continue;
            prevStacks[i] = stacks;
            ApplyAilmentIndex(i, stacks);
        }
    }

    public void ForceRefresh()
    {
        for (int i = 0; i < prevStacks.Length; i++) prevStacks[i] = -1;
        RefreshAll();
    }

    // full refresh of indices 0..4
    private void RefreshAll()
    {
        if (stats == null) return;
        for (int i = 0; i < 5; i++)
        {
            int stacks = stats.ailmentCurrentStacks[i];
            prevStacks[i] = -1; // force update below
            ApplyAilmentIndex(i, stacks);
        }
    }

    // create/update/destroy
    private void ApplyAilmentIndex(int i, int stacks)
    {
        bool show;
        int percent;

        if (i == 0)
        {
            // Burn
            percent = Mathf.Clamp(stacks, 0, 100);
            show = percent > 0;
        }
        else
        {
            // Other ailments: 1 stack = 10%
            percent = Mathf.Clamp(stacks * 10, 0, 100);
            show = stacks > 0;
        }

        if (show)
        {
            if (!cells.ContainsKey(i))
            {
                if (ailmentCellPrefab == null || ailmentPanel == null) return;

                var go = Instantiate(ailmentCellPrefab, ailmentPanel, false);

                // assign icon
                var img = go.GetComponentInChildren<Image>();
                if (img != null && ailmentIcons != null && i < ailmentIcons.Length && ailmentIcons[i] != null)
                    img.sprite = ailmentIcons[i];

                // assign text
                var tmp = go.GetComponentInChildren<TextMeshProUGUI>();
                if (tmp != null) tmp.text = $"{percent}%";

                cells[i] = go;

                // ensure final correct ordering after change
                ReorderCellsByIndex();
            }
            else
            {
                var tmp = cells[i].GetComponentInChildren<TextMeshProUGUI>();
                if (tmp != null) tmp.text = $"{percent}%";
            }
        }
        else
        {
            if (cells.ContainsKey(i))
            {
                Destroy(cells[i]);
                cells.Remove(i);
                ReorderCellsByIndex();
            }
        }
    }

    // Make sure children sibling order matches ascending ailment index
    private void ReorderCellsByIndex()
    {
        if (ailmentPanel == null) return;
        int sibling = 0;
        for (int idx = 0; idx < 5; idx++)
        {
            if (cells.ContainsKey(idx) && cells[idx] != null)
            {
                int target = Mathf.Clamp(sibling, 0, Mathf.Max(0, ailmentPanel.childCount - 1));
                cells[idx].transform.SetSiblingIndex(target);
                sibling++;
            }
        }
    }

    private void ClearAllCells()
    {
        foreach (var kv in cells)
            if (kv.Value != null) Destroy(kv.Value);
        cells.Clear();

        for (int i = 0; i < prevStacks.Length; i++) prevStacks[i] = -1;
    }

    //Events subscription to refresh when stats change
    private void TrySubscribeToStats()
    {
        if (stats == null) return;

        UnsubscribeFromStats();

        // CharacterStats events (PlayerStats inherits them)
        stats.OnAilmentDamageTakenChange += RefreshAll;
        stats.OnCurrentMoveSpeedChange += RefreshAll;
        stats.OnAbilitySpeedChange += RefreshAll;
        stats.OnDamageTakenChange += RefreshAll;
        stats.OnCleanseAilments += RefreshAll;
        // PlayerStats specific event
        stats.OnStatsChanged += RefreshAll;
    }

    private void UnsubscribeFromStats()
    {
        if (stats == null) return;

        stats.OnAilmentDamageTakenChange -= RefreshAll;
        stats.OnCurrentMoveSpeedChange -= RefreshAll;
        stats.OnAbilitySpeedChange -= RefreshAll;
        stats.OnDamageTakenChange -= RefreshAll;
        stats.OnCleanseAilments -= RefreshAll;
        stats.OnStatsChanged -= RefreshAll;
    }
}
