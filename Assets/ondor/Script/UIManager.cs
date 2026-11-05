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

    [Serializable] public class Entry { public string id; public int score, money; }
    [Serializable] class Data { public List<Entry> list = new(); }

    static List<Entry> Load() =>
        JsonUtility.FromJson<Data>(PlayerPrefs.GetString(Key, "{}")).list ?? new();

    public static bool Qualifies(int score)
    {
        var list = Load();
        return list.Count < Max || score > list[^1].score;
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
        return list.Count == 0 ? "등록된 랭킹이 없습니다." :
            string.Join("\n", list.Select((e, i) => $"{i + 1}위  {e.id}  -  {e.score}점 ({e.money}G)"));
    }
}

public class UIManager : MonoBehaviour
{
    public static UIManager I;
    public string gameScene = "Game";

    [Header("UI 연결 슬롯")]
    public TMP_Text hpText, score, money, bombs, stage, remain, time;
    public TMP_InputField nameInput;
    public Slider hpBar, bossBar;

    // 프리팹을 자유롭게 넣을 수 있도록 GameObject 타입으로 변경했습니다.
    public GameObject enemyHpPrefab;

    public Transform hpContainer;
    public Vector3 hpOffset = new(0, .8f, 0);
    public GameObject over, clear, rank, setting, nebulaFog;
    public Toggle bgmToggle, sfxToggle;

    readonly Dictionary<Enemy, Slider> bars = new();
    int lastT = -1;

    void Awake() => I = this;

    void Start()
    {
        if (setting) setting.SetActive(false);
        if (bgmToggle) { bgmToggle.isOn = SoundManager.BgmOn; bgmToggle.onValueChanged.AddListener(on => SoundManager.Set(true, on)); }
        if (sfxToggle) { sfxToggle.isOn = SoundManager.SfxOn; sfxToggle.onValueChanged.AddListener(on => SoundManager.Set(false, on)); }
    }

    public void RegisterEnemy(Enemy e)
    {
        if (e.boss || !enemyHpPrefab) return;

        // 프리팹 생성 후 내부의 Slider 컴포넌트를 알아서 찾아오도록 수정했습니다.
        var obj = Instantiate(enemyHpPrefab, hpContainer ? hpContainer : transform);
        var slider = obj.GetComponentInChildren<Slider>();

        if (slider) bars[e] = slider;
    }

    void Update()
    {
        var g = GameManager.I;
        if (!g) return;
        if (nebulaFog && g.player) nebulaFog.transform.position = Camera.main.WorldToScreenPoint(g.player.position);

        var dead = new List<Enemy>();
        foreach (var kvp in bars)
        {
            var e = kvp.Key;
            var bar = kvp.Value;
            if (!e) { if (bar) Destroy(bar.gameObject); dead.Add(e); continue; }
            bar.value = e.hp / e.maxHp;
            bar.transform.position = Camera.main.WorldToScreenPoint(e.transform.position + hpOffset);
        }
        foreach (var e in dead) bars.Remove(e);

        int t = (int)g.StageTime;
        if (t == lastT) return;
        lastT = t;
        if (time) time.text = $"{t / 60:00}:{t % 60:00}";
    }

    public void Refresh()
    {
        var g = GameManager.I;

        if (hpBar) hpBar.value = g.hp / g.maxHp;
        if (hpText) hpText.text = $"{Mathf.CeilToInt(g.hp / g.maxHp * 100)}%";

        if (score) score.text = $"Score : {g.Score}";
        if (money) money.text = $"Money : {g.Money}";

        if (bombs) bombs.text = g.bombs.ToString();
        if (stage) stage.text = $"STAGE {g.Stage}";
        if (remain) remain.text = g.Remain.ToString();

        if (bossBar)
        {
            bossBar.gameObject.SetActive(g.BossOn);
            bossBar.value = g.BossRatio;
        }

        if (nebulaFog) nebulaFog.SetActive(g.Stage >= 3);
        if (over) over.SetActive(g.Over);
        if (clear) clear.SetActive(g.Cleared && !g.Ranked);
        if (nameInput) nameInput.gameObject.SetActive(g.CanRank);
        if (rank)
        {
            rank.SetActive(g.Ranked);
            if (g.Ranked)
            {
                var txt = rank.GetComponentInChildren<TMP_Text>();
                if (txt) txt.text = Ranking.Text();
            }
        }
    }

    public void Submit() => GameManager.I.Submit(nameInput.text);

    public void StartGame()
    {
        RunData.Reset();
        SceneManager.LoadScene(gameScene);
    }

    public void SetSetting(bool open) { if (setting) setting.SetActive(open); }

    public void Quit()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}