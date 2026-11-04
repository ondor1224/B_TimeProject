using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class Hub : MonoBehaviour
{
    public Catalog catalog;
    public Button slotPrefab;
    public Transform wingList, skillList;
    public TMP_Text money, stage;
    public SpriteRenderer previewWing;
    int slot;
    readonly List<(Catalog.Item it, TMP_Text label)> slots = new List<(Catalog.Item, TMP_Text)>();

    void Start()
    {
        Time.timeScale = 1;
        foreach (var it in catalog.items)
        {
            var b = Instantiate(slotPrefab, it.kind == Catalog.Kind.Wing ? wingList : skillList);
            b.transform.GetChild(0).GetComponent<Image>().sprite = it.sprite;
            b.onClick.AddListener(() => Click(it));
            slots.Add((it, b.GetComponentInChildren<TMP_Text>()));
        }
        Refresh();
    }

    public void SelectSlot(int i)
    {
        slot = i;
        Refresh();
    }

    void Click(Catalog.Item it)
    {
        if (!RunData.Owned.Contains(it.id))
        {
            if (RunData.Money < it.price) return;
            RunData.Money -= it.price;
            RunData.Owned.Add(it.id);
        }
        else if (it.kind == Catalog.Kind.Wing) RunData.Wing = RunData.Wing == it.id ? "" : it.id;
        else
        {
            bool same = RunData.Slots[slot] == it.id;
            for (int i = 0; i < RunData.Slots.Length; i++) if (RunData.Slots[i] == it.id) RunData.Slots[i] = "";
            if (!same) RunData.Slots[slot] = it.id;
        }
        Refresh();
    }

    public void Next()
    {
        if (RunData.GameScene != "") SceneManager.LoadScene(RunData.GameScene);
    }

    void Refresh()
    {
        money.text = RunData.Money.ToString();
        stage.text = $"STAGE {RunData.Stage}";
        foreach (var (it, label) in slots)
        {
            bool own = RunData.Owned.Contains(it.id);
            bool eq = RunData.Wing == it.id || System.Array.IndexOf(RunData.Slots, it.id) >= 0;
            label.text = eq ? "장착중" : own ? "보유" : it.price.ToString();
        }
        catalog.Apply(previewWing);
    }
}
