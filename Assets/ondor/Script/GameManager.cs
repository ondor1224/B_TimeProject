using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class RunData
{
    public static int Stage = 1, Score, Money;
    public static string Wing = "", GameScene = "";
    public static string[] Slots = { "", "" };
    public static HashSet<string> Owned = new HashSet<string>();
 

    public static void Reset()
    {
        Stage = 1; Score = Money = 0; Wing = "";
        Slots = new[] { "", "" }; Owned.Clear();
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
    public string hubScene = "Hub";
    public float hp = 100, maxHp = 100, inv = .5f;
    public int bombs = 2, count = 5, moneyPerKill = 10;
    public float spawnDelay = .6f, respawnDelay = .5f, spawnGap = 2.5f, above = 2, follow = 1.5f, spacing = 1.5f;
    public Vector2 stretch = new Vector2(2.5f, 2);

    public SpriteRenderer bgRenderer;
    public Sprite[] bgSprites;

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
    float invT, lastX;
    float scoreTimer;
    Enemy boss;

    void Awake()
    {
        I = this;
        Stage = RunData.Stage;
        Score = RunData.Score;
        RunData.GameScene = SceneManager.GetActiveScene().name;
        Time.timeScale = 1;
        center = player.position;
        var c = Camera.main;
        half = new Vector2(c.orthographicSize * c.aspect, c.orthographicSize);

        if (Stage >= 1 && Stage <= 3) stages[Stage - 1].total = 20 + Stage * 5;
        if (bgRenderer && bgSprites != null && bgSprites.Length >= Stage)
            bgRenderer.sprite = bgSprites[Stage - 1];
    }

    void Start()
    {
        StartCoroutine(Wave());
        if (Stage >= 2 && obstaclePrefab) StartCoroutine(SpawnObstacles());
        Refresh();
    }

    void Update()
    {
        center = Vector2.Lerp(center, player.position, follow * Time.deltaTime);
        if (!Over && !Cleared) StageTime += Time.deltaTime;
        if (Over && Input.GetKeyDown(KeyCode.R)) Restart();
        if (Input.GetKeyDown(KeyCode.F3)) God = !God;
        if (Input.GetKeyDown(KeyCode.F4)) KillAll();
        if (Input.GetKeyDown(KeyCode.F5)) AddMoney(100);

        if (!Over && !Cleared)
        {
            scoreTimer += Time.deltaTime;
            if (scoreTimer >= 5f)
            {
                scoreTimer = 0f;
                Score += 10; // 5초마다 오를 점수 (원하는 수치로 변경하세요)
                if (UIManager.I) UIManager.I.Refresh();
            }
        }
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
        if (Stage < stages.Length)
        {
            RunData.Stage = Stage + 1;
            RunData.Score = Score;
            SceneManager.LoadScene(hubScene);
            return;
        }
        Time.timeScale = 0;
        Cleared = true;
        CanRank = Ranking.Qualifies(Score);
        Refresh();
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
        RunData.Reset();
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    public void Bomb()
    {
        if (bombs <= 0 || Over || Cleared) return;
        bombs--;
        ClearBullets();
        Refresh();
    }

    public void Damage(float d)
    {
        if (Over || God || Time.time < invT) return;
        invT = Time.time + inv;
        hp = Mathf.Max(0, hp - d);
        if (player && player.TryGetComponent<Player>(out var p)) p.Flash();
        if (hp == 0)
        {
            Over = true;
            Time.timeScale = 0;
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
        if (!Cleared || Ranked) return;
        if (CanRank) Ranking.Add(string.IsNullOrWhiteSpace(id) ? "PLAYER" : id, Score, Money);
        Ranked = true;
        Refresh();
    }

    public void Refresh()
    {
        if (UIManager.I) UIManager.I.Refresh();
    }
}