using UnityEngine;

public class Enemy : MonoBehaviour
{
    public bool boss;
    public int score = 100, shots = 1;
    public float hp = 30, speed = 4, fireDelay = 2, spread = 30;
    public Bullet bullet;
    float maxHp, nextFire;
    Vector2 offset;

    void Awake()
    {
        var g = GameManager.I;
        maxHp = hp;
        nextFire = Time.time + Random.value * fireDelay;
        offset = new Vector2(Random.Range(-g.stretch.x, g.stretch.x), g.above + Random.Range(0, g.stretch.y));
    }

    void Update()
    {
        var g = GameManager.I;
        Vector2 p = transform.position, goal;
        if (boss) goal = new Vector2(Mathf.Sin(Time.time) * (g.half.x - 2), g.half.y - 2.5f);
        else
        {
            goal = g.center + offset;
            goal.x = Mathf.Clamp(goal.x, -g.half.x, g.half.x);
            goal.y = Mathf.Min(goal.y, g.half.y - 1);
            foreach (var e in g.list)
            {
                var d = p - (Vector2)e.transform.position;
                if (d.sqrMagnitude < g.spacing * g.spacing) goal += d.normalized * (g.spacing - d.magnitude);
            }
        }
        transform.position = Vector2.MoveTowards(p, goal, speed * Time.deltaTime);
        if (Time.time < nextFire) return;
        nextFire = Time.time + fireDelay;
        for (int i = 0; i < shots; i++)
            Instantiate(bullet, p, Quaternion.Euler(0, 0, 180 + (i - (shots - 1) / 2f) * spread));
    }

    public void TakeDamage(float d)
    {
        if (hp <= 0) return;
        hp -= d;
        if (boss)
        {
            GameManager.I.BossRatio = Mathf.Max(hp, 0) / maxHp;
            GameManager.I.Refresh();
        }
        if (hp > 0) return;
        GameManager.I.Kill(this);
        Destroy(gameObject);
    }
}
