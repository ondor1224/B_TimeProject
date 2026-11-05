using System.Collections;
using UnityEngine;

public class Enemy : MonoBehaviour
{
    public enum Kind { Scout, Fighter, Heavy, Interceptor, Bomber }
    public Kind kind; public bool boss;
    public int score = 100, shots = 1;
    public float hp = 30, speed = 4, fireDelay = 2, spread = 30;
    public Bullet bullet; public Sprite[] themeSprites;
    [HideInInspector] public float maxHp;

    // 왔다갔다(Hover) 및 거리 조절용 변수
    float nextFire, flashT, baseAngle, phase, hoverSpeed, hoverRange, currentDist, targetDist, distTimer;
    SpriteRenderer sr;

    void Awake()
    {
        var g = GameManager.I;
        sr = GetComponentInChildren<SpriteRenderer>();
        if (!boss)
        {
            (hp, speed, fireDelay, shots, spread) = kind switch
            {
                Kind.Scout => (20f, 6f, 2f, 1, 0f),
                Kind.Fighter => (40f, 4f, 2.5f, 3, 0f),
                Kind.Heavy => (80f, 2f, 3f, 5, 25f),
                Kind.Interceptor => (15f, 7f, 1.5f, 1, 0f),
                Kind.Bomber => (60f, 3f, 3f, 1, 0f),
                _ => (hp, speed, fireDelay, shots, spread)
            };
            int idx = Mathf.Clamp(g.Stage - 1, 0, themeSprites.Length - 1);
            if (sr && themeSprites.Length > 0 && themeSprites[idx]) sr.sprite = themeSprites[idx];

            // 생성 시 플레이어 기준 기본 방향 및 왔다갔다 할 폭/속도 설정
            baseAngle = Random.Range(0f, Mathf.PI * 2f);
            phase = Random.Range(0f, 100f);
            hoverSpeed = Random.Range(0.5f, 1.5f);
            hoverRange = Random.Range(0.5f, 1.2f); // 왔다갔다 하는 각도 범위

            currentDist = Random.Range(4f, 10f);
            targetDist = currentDist;
        }
        else
        {
            hp = 100 * g.Stage;
            speed = 1.5f + g.Stage * 0.5f;
            shots = 3 + g.Stage;
        }
        maxHp = hp;
        nextFire = Time.time + Random.value * fireDelay;
        if (UIManager.I) UIManager.I.RegisterEnemy(this);
    }

    void Update()
    {
        var g = GameManager.I;
        if (sr) sr.color = Time.time < flashT ? new Color(10, 10, 10, 1) : Color.white;
        Vector2 p = transform.position, goal;

        if (boss)
        {
            goal = new Vector2(Mathf.Sin(Time.time * speed * 0.5f) * (g.half.x - 2), g.half.y - 2.5f);
            if (g.player) LookAtPlayer(g.player.position, 4f);
        }
        else
        {
            if (g.player)
            {
                // 1. 거리 조절 (넓게~좁게)
                if (Time.time > distTimer)
                {
                    targetDist = Random.Range(4f, 10f);
                    distTimer = Time.time + Random.Range(2f, 4f);
                }
                currentDist = Mathf.Lerp(currentDist, targetDist, Time.deltaTime * 0.5f);

                // 2. 빙빙 도는 대신 특정 방향을 기준으로 좌우로 왔다갔다 (Sin 함수 활용)
                float currentAngle = baseAngle + Mathf.Sin(Time.time * hoverSpeed + phase) * hoverRange;
                goal = (Vector2)g.player.position + new Vector2(Mathf.Cos(currentAngle), Mathf.Sin(currentAngle)) * currentDist;

                // 3. 자연스럽게 플레이어를 바라보기
                LookAtPlayer(g.player.position, 6f);
            }
            else goal = g.center;

            if (kind == Kind.Interceptor && g.player)
            {
                var pd = p - (Vector2)g.player.position;
                if (pd.sqrMagnitude < 9f) goal += pd.normalized * 2.5f;
            }

            foreach (var o in Obstacle.list)
            {
                var d = p - (Vector2)o.transform.position;
                float r = o.radius + g.spacing;
                if (d.sqrMagnitude < r * r) goal += d.normalized * (r - d.magnitude) * 2f;
            }

            goal.x = Mathf.Clamp(goal.x, -g.half.x, g.half.x);
            goal.y = Mathf.Min(goal.y, g.half.y - 1);

            foreach (var e in g.list)
            {
                if (e == this) continue;
                var d = p - (Vector2)e.transform.position;
                if (d.sqrMagnitude < g.spacing * g.spacing)
                    goal += d.normalized * (g.spacing - d.magnitude) * 1.5f;
            }
        }

        transform.position = Vector2.MoveTowards(p, goal, speed * Time.deltaTime);
        if (Time.time < nextFire) return;
        nextFire = Time.time + fireDelay;
        StartCoroutine(Fire());
    }

    void LookAtPlayer(Vector2 targetPos, float turnSpeed)
    {
        Vector2 dir = targetPos - (Vector2)transform.position;
        float targetAngle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg - 90f;
        transform.rotation = Quaternion.Lerp(transform.rotation, Quaternion.Euler(0, 0, targetAngle), Time.deltaTime * turnSpeed);
    }

    IEnumerator Fire()
    {
        var pl = GameManager.I.player;
        if (boss)
        {
            bool p2 = hp <= maxHp * 0.5f;
            int curShots = p2 ? shots * 2 : shots;
            for (int i = 0; i < curShots; i++)
            {
                SpawnBullet(180 + (i - (curShots - 1) / 2f) * (p2 ? spread * 1.5f : spread), p2 ? 7 : 5, p2 ? 120 : 90);
                if (p2) yield return new WaitForSeconds(0.1f);
            }
        }
        else if (kind == Kind.Heavy) { for (int i = 0; i < shots; i++) SpawnBullet(transform.eulerAngles.z + 180 + (i - (shots - 1) / 2f) * spread, 5, 90); }
        else if (kind == Kind.Scout) SpawnBullet(transform.eulerAngles.z + 180, 5.5f, 140);
        else if (kind == Kind.Fighter) { for (int i = 0; i < 3; i++) { SpawnBullet(transform.eulerAngles.z + 180, 5.5f, 120); yield return new WaitForSeconds(.15f); } }
        else if (kind == Kind.Interceptor && pl) SpawnBullet(Aim((Vector2)pl.position - (Vector2)transform.position), 10, 0);
        else if (kind == Kind.Bomber && pl)
        {
            var pr = pl.GetComponent<Player>();
            Vector2 target = (Vector2)pl.position + (pr ? pr.Vel * .8f : Vector2.zero);
            SpawnBullet(Aim(target - (Vector2)transform.position), 4.5f, 40, .6f);
        }
    }

    float Aim(Vector2 d) => Mathf.Atan2(d.y, d.x) * Mathf.Rad2Deg - 90;
    void SpawnBullet(float ang, float spd, float trn, float arc = 0) => Instantiate(bullet, transform.position, Quaternion.Euler(0, 0, ang)).Setup(spd, trn, arc, false);

    public void TakeDamage(float d)
    {
        if (hp <= 0) return;
        hp -= d; flashT = Time.time + .1f;
        if (boss) { GameManager.I.BossRatio = Mathf.Max(hp, 0) / maxHp; GameManager.I.Refresh(); }
        if (hp > 0) return;
        GameManager.I.Kill(this); Destroy(gameObject);
    }
}