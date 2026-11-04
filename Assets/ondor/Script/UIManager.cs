using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public static class Ranking
{
    const int Max = 5;
    const string Key = "rank";

    [Serializable]
    public class Entry
    {
        public string id;
        public int score, money;
    }

    [Serializable]
    class Data
    {
        public List<Entry> list = new List<Entry>();
    }

    static List<Entry> Load() =>
        JsonUtility.FromJson<Data>(PlayerPrefs.GetString(Key, "{}")).list ?? new List<Entry>();

    public static bool Qualifies(int score)
    {
        var list = Load();
        return list.Count < Max || score > list[list.Count - 1].score;
    }

    public static void Add(string id, int score, int money)
    {
        var list = Load();
        list.Add(new Entry { id = id, score = score, money = money });
        list = list.OrderByDescending(e => e.score).ThenByDescending(e => e.money).Take(Max).ToList();
        PlayerPrefs.SetString(Key, JsonUtility.ToJson(new Data { list = list }));
        PlayerPrefs.Save();
    }

    public static string Text()
    {
        var list = Load();
        if (list.Count == 0) return "등록된 랭킹이 없습니다.";
        return string.Join("\n", list.Select((e, i) => $"{i + 1}위  {e.id}  -  {e.score}점 ({e.money}G)"));
    }
}

public class UIManager : MonoBehaviour
{
    public static UIManager I;
    public string gameScene = "Game";
    public TMP_Text hpText, score, money, bombs, stage, remain, time;
    public TMP_InputField nameInput;
    public Slider hpBar, bossBar;
    public GameObject over, clear, rank, setting;
    public Toggle bgmToggle, sfxToggle;
    int lastT = -1;

    void Awake() => I = this;

    void Start()
    {
        if (bgmToggle) { bgmToggle.isOn = SoundManager.BgmOn; bgmToggle.onValueChanged.AddListener(on => SoundManager.Set(true, on)); }
        if (sfxToggle) { sfxToggle.isOn = SoundManager.SfxOn; sfxToggle.onValueChanged.AddListener(on => SoundManager.Set(false, on)); }
    }

    void Update()
    {
        if (!GameManager.I) return;
        int t = (int)GameManager.I.StageTime;
        if (t == lastT) return;
        lastT = t;
        time.text = $"{t / 60:00}:{t % 60:00}";
    }

    public void Refresh()
    {
        var g = GameManager.I;
        hpBar.value = g.hp / g.maxHp;
        hpText.text = $"{Mathf.CeilToInt(g.hp / g.maxHp * 100)}%";
        score.text = g.Score.ToString();
        money.text = g.Money.ToString();
        bombs.text = g.bombs.ToString();
        stage.text = $"STAGE {g.Stage}";
        remain.text = g.Remain.ToString();
        bossBar.gameObject.SetActive(g.BossOn);
        bossBar.value = g.BossRatio;
        over.SetActive(g.Over);
        clear.SetActive(g.Cleared && !g.Ranked);
        nameInput.gameObject.SetActive(g.CanRank);
        rank.SetActive(g.Ranked);
        if (g.Ranked) rank.GetComponentInChildren<TMP_Text>().text = Ranking.Text();
    }

    public void Submit() => GameManager.I.Submit(nameInput.text);

    public void StartGame()
    {
        RunData.Reset();
        SceneManager.LoadScene(gameScene);
    }

    public void SetSetting(bool open) => setting.SetActive(open);

    public void Quit()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}