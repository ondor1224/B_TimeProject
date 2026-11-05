using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class RunData
{
    public static int Stage = 1, Score, Money, AtkLv, SpdLv, HpLv;
    public static string Wing = "", GameScene = "Game";
    public static string[] Slots = { "", "" };
    public static HashSet<string> Owned = new HashSet<string>();
    public static Dictionary<string, int> SkillLv = new Dictionary<string, int>();

    public static int GetSkillLv(string s) => (!string.IsNullOrEmpty(s) && SkillLv.TryGetValue(s, out int lv)) ? lv : 0;

    public static void Reset()
    {
        Stage = 1; Score = Money = AtkLv = SpdLv = HpLv = 0;
        Wing = ""; Slots = new[] { "", "" };
        Owned.Clear(); SkillLv.Clear();
    }
}

public class GameManager : MonoBehaviour
{
    [System.Serializable]
    public class StageData
    {
        public Enemy[] enemies;
        public Enemy boss;
        public int total = 10;
    }

    public static GameManager I;
    public Transform player;
    public StageData[] stages;
    public Obstacle obstaclePrefab;
    public float obsDelay = 2.5f;
    public string hubScene = "Hub", overScene = "GameOver", clearScene = "Clear";
    public float hp = 100, maxHp = 100, inv = .5f;
    public int bombs = 2, count = 5, moneyPerKill = 10;
    public float spawnDelay = .6f, respawnDelay = .5f, spawnGap = 2.5f, above = 2, follow = 1.5f, spacing = 1.5f;
    public Vector2 stretch = new Vector2(2.5f, 2);

    public SpriteRenderer bgRenderer;
    public Sprite[] bgSprites;
    public AudioClip[] stageBgms;
    public AudioClip bossBgm, hitSfx, bombSfx, skillSfx;
    public GameObject blackHoleVisualPrefab;

    [HideInInspector] public Vector2 half, center;
    [HideInInspector] public float StageTime, BossRatio;
    [HideInInspector] public int Score, Stage = 1;
    [HideInInspector] public bool Over, Cleared, Ranked, CanRank, BossOn, God;
    [HideInInspector] public List<Enemy> list = new List<Enemy>();
    public int Money { get => RunData.Money; set => RunData.Money = value; }
    public int Total => St.total;
    public int Remain => Total - killed;
    StageData St => stages[Stage - 1];
    int killed, spawned;
    float invT, lastX, scoreTimer;
    float[] skillCool = new float[2];
    Enemy boss;
    public Enemy CurrentBoss => boss;
    SkillController skillCtrl;

    void Awake()
    {
        I = this;
        Stage = Mathf.Clamp(RunData.Stage, 1, stages.Length);
        Score = RunData.Score;
        RunData.GameScene = SceneManager.GetActiveScene().name;
        Time.timeScale = 1;
        maxHp += RunData.HpLv * 30;
        hp = maxHp;
        center = player.position;
        var c = Camera.main;
        half = new Vector2(c.orthographicSize * c.aspect, c.orthographicSize);

        if (Stage >= 1 && Stage <= 3) stages[Stage - 1].total = 20 + Stage * 5;
        if (bgRenderer && bgSprites != null && bgSprites.Length >= Stage)
            bgRenderer.sprite = bgSprites[Stage - 1];

        skillCtrl = gameObject.GetComponent<SkillController>();
        if (!skillCtrl) skillCtrl = gameObject.AddComponent<SkillController>();
    }

    void Start()
    {
        if (stageBgms != null && stageBgms.Length >= Stage)
            SoundManager.PlayBgm(stageBgms[Stage - 1]);
        StartCoroutine(Wave());
        if (Stage >= 2 && obstaclePrefab) StartCoroutine(SpawnObstacles());
        Refresh();
    }

    void Update()
    {
        if (player) center = Vector2.Lerp(center, player.position, follow * Time.deltaTime);
        if (!Over && !Cleared) StageTime += Time.deltaTime;
        if (Input.GetKeyDown(KeyCode.Q)) UseSkill(0);
        if (Input.GetKeyDown(KeyCode.E)) UseSkill(1);
        if (Input.GetKeyDown(KeyCode.F3)) God = !God;
        if (Input.GetKeyDown(KeyCode.F4)) KillAll();
        if (Input.GetKeyDown(KeyCode.F5)) AddMoney(100);

        if (!Over && !Cleared)
        {
            scoreTimer += Time.deltaTime;
            if (scoreTimer >= 5f)
            {
                scoreTimer = 0f;
                Score += 10;
                Refresh();
            }
        }
    }

    public void UseSkill(int slot)
    {
        string s = RunData.Slots[slot];
        int lv = Mathf.Clamp(RunData.GetSkillLv(s), 1, 3);
        if (Time.time < skillCool[slot] || string.IsNullOrEmpty(s)) return;
        SoundManager.PlaySfx(skillSfx);
        skillCool[slot] = Time.time + skillCtrl.Activate(s, lv);
        Refresh();
    }

    IEnumerator Wave()
    {
        for (int i = 0; i < count; i++)
        {
            Spawn();
            yield return new WaitForSeconds(spawnDelay);
        }
    }

    IEnumerator SpawnObstacles()
    {
        while (!Over && !Cleared)
        {
            yield return new WaitForSeconds(obsDelay);
            Instantiate(obstaclePrefab, new Vector2(Random.Range(-half.x + 1, half.x - 1), half.y + 2), Quaternion.identity);
        }
    }

    void Spawn()
    {
        if (list.Count >= count || spawned >= Total) return;
        Vector2 p = default;
        for (int i = 0; i < 20; i++)
        {
            p = new Vector2(Random.Range(-half.x + 1, half.x - 1), half.y + 1.5f);
            if (Mathf.Abs(p.x - lastX) > 1.5f && !list.Exists(e => Vector2.Distance(p, e.transform.position) < spawnGap)) break;
        }
        lastX = p.x;
        spawned++;
        var es = St.enemies;
        int maxKind = Mathf.Min(es.Length, Stage == 1 ? 3 : Stage == 2 ? 4 : 5);
        list.Add(Instantiate(es[Random.Range(0, maxKind)], p, Quaternion.identity));
    }

    void SpawnBoss()
    {
        if (bossBgm) SoundManager.PlayBgm(bossBgm);
        boss = Instantiate(St.boss, new Vector2(0, half.y - 2.5f), Quaternion.identity);
        BossOn = true;
        BossRatio = 1;
        Refresh();
    }

    public void Kill(Enemy e)
    {
        if (e != boss && !list.Remove(e)) return;
        Score += e.score;
        if (e == boss) { Clear(); return; }
        killed++;
        Invoke(killed == Total ? nameof(SpawnBoss) : nameof(Spawn), respawnDelay);
        Refresh();
    }

    void Clear()
    {
        AddMoney(killed * moneyPerKill);
        Wipe();
        RunData.Score = Score;
        if (Stage < stages.Length)
        {
            RunData.Stage = Stage + 1;
            SceneManager.LoadScene(hubScene);
            return;
        }
        Cleared = true;
        CanRank = Ranking.Qualifies(Score);
        SceneManager.LoadScene(clearScene);
    }

    void Wipe()
    {
        StopAllCoroutines();
        CancelInvoke();
        foreach (var e in list) if (e) Destroy(e.gameObject);
        list.Clear();
        if (boss) Destroy(boss.gameObject);
        foreach (var o in Obstacle.list.ToArray()) if (o) Destroy(o.gameObject);
        BossOn = false;
        ClearBullets();
    }

    static void ClearBullets()
    {
        foreach (var b in FindObjectsByType<Bullet>(FindObjectsSortMode.None)) Destroy(b.gameObject);
    }

    void KillAll()
    {
        foreach (var e in list.ToArray()) e.TakeDamage(9999);
        if (boss) boss.TakeDamage(9999);
    }

    public void Restart()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    public void Bomb()
    {
        if (bombs <= 0 || Over || Cleared) return;
        bombs--;
        SoundManager.PlaySfx(bombSfx);
        ClearBullets();
        Refresh();
    }

    public void Damage(float d)
    {
        if (Over || God || Time.time < invT) return;
        if (skillCtrl && skillCtrl.IsReflecting) return;
        invT = Time.time + inv;
        hp = Mathf.Max(0, hp - d);
        SoundManager.PlaySfx(hitSfx);
        if (player && player.TryGetComponent<Player>(out var p)) p.Flash();
        if (hp == 0)
        {
            Over = true;
            RunData.Stage = Stage;
            RunData.Score = Score;
            SceneManager.LoadScene(overScene);
            return;
        }
        Refresh();
    }

    public void AddMoney(int n)
    {
        Money += n;
        Refresh();
    }

    public void Submit(string id)
    {
        if (Ranked) return;
        if (Ranking.Qualifies(RunData.Score)) Ranking.Add(string.IsNullOrWhiteSpace(id) ? "PLAYER" : id, RunData.Score, RunData.Money);
        Ranked = true;
        Refresh();
    }

    public void Refresh()
    {
        if (UIManager.I) UIManager.I.Refresh();
    }
}
