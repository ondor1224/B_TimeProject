using System.Collections.Generic;
using UnityEngine;

public class SkillController : MonoBehaviour
{
    float timeStopEnd, reflectEnd, slowEnd, bhEnd;
    float reflectDmg, reflectRadius;
    float slowRadius, slowFactor;
    float bhRadius, bhPull;
    Vector3 bhPos;
    GameObject bhVisual;
    Dictionary<Bullet, Vector3> prevPos = new Dictionary<Bullet, Vector3>();

    public bool IsReflecting => Time.time < reflectEnd;

    public float Activate(string skill, int lv)
    {
        var gm = GameManager.I;
        if (!gm || !gm.player) return 1f;

        switch (skill)
        {
            case "Heal":
                gm.hp = Mathf.Min(gm.maxHp, gm.hp + 20f + lv * 20f);
                gm.Refresh();
                return 12f - lv * 2f;

            case "Homing":
                TriggerHoming(lv);
                return 12f - lv * 2f;

            case "TimeStop":
                timeStopEnd = Time.time + (1.5f + lv * 1.5f);
                return 14f - lv * 2f;

            case "Reflect":
                reflectEnd = Time.time + (0.15f + lv * 0.25f);
                reflectDmg = 10f + lv * 15f;
                reflectRadius = 1.2f + lv * 0.4f;
                return 7.5f - lv * 1.5f;

            case "BlackHole":
                bhPos = gm.player.position + gm.player.up * 2.5f;
                bhEnd = Time.time + (1.5f + lv * 1.5f);
                bhRadius = 2.0f + lv * 1.5f;
                bhPull = 4f + lv * 3f;
                if (bhVisual) Destroy(bhVisual);
                if (gm.blackHoleVisualPrefab)
                    bhVisual = Instantiate(gm.blackHoleVisualPrefab, bhPos, Quaternion.identity);
                return 14f - lv * 2f;

            case "SlowField":
                slowEnd = Time.time + (2f + lv * 2f);
                slowRadius = 2.5f + lv * 1.5f;
                slowFactor = 1f - (0.35f + lv * 0.17f);
                return 12f - lv * 1.5f;
        }
        return 5f;
    }

    void LateUpdate()
    {
        var gm = GameManager.I;
        if (!gm || !gm.player) return;

        bool timeStopped = Time.time < timeStopEnd;
        bool reflecting = Time.time < reflectEnd;
        bool slowing = Time.time < slowEnd;
        bool bhActive = Time.time < bhEnd;

        if (!bhActive && bhVisual) Destroy(bhVisual);

        var bullets = FindObjectsByType<Bullet>(FindObjectsSortMode.None);

        foreach (var b in bullets)
        {
            if (!b) continue;
            Vector3 cur = b.transform.position;

            if (reflecting && Vector2.Distance(cur, gm.player.position) <= reflectRadius)
            {
                ConvertBulletToAlly(b, GetLookTarget(), 16f, reflectDmg);
                continue;
            }

            if (bhActive)
            {
                float d = Vector2.Distance(cur, bhPos);
                if (d <= 0.45f) { Destroy(b.gameObject); continue; }
                if (d <= bhRadius)
                {
                    b.transform.position = Vector3.MoveTowards(cur, bhPos, bhPull * Time.deltaTime);
                    cur = b.transform.position;
                }
            }

            if (timeStopped)
            {
                if (b.enabled) b.enabled = false;
                if (b.TryGetComponent<Rigidbody2D>(out var rb)) rb.linearVelocity = Vector2.zero;
                prevPos[b] = cur;
                continue;
            }
            else if (!b.enabled)
            {
                b.enabled = true;
            }

            if (slowing && prevPos.TryGetValue(b, out Vector3 last))
            {
                if (Vector2.Distance(cur, gm.player.position) <= slowRadius)
                {
                    cur = Vector3.Lerp(last, cur, slowFactor);
                    b.transform.position = cur;
                }
            }

            prevPos[b] = cur;
        }
    }

    void TriggerHoming(int lv)
    {
        Enemy target = GetLookTarget();
        float spd = 8f + lv * 4f;
        float dmg = 10f + lv * 10f;
        foreach (var b in FindObjectsByType<Bullet>(FindObjectsSortMode.None))
        {
            if (b) ConvertBulletToAlly(b, target, spd, dmg);
        }
    }

    Enemy GetLookTarget()
    {
        var gm = GameManager.I;
        if (!gm || !gm.player) return null;
        Enemy best = gm.CurrentBoss;
        float bestDot = -2f;
        Vector2 pPos = gm.player.position;
        Vector2 pUp = gm.player.up;

        foreach (var e in gm.list)
        {
            if (!e) continue;
            Vector2 dir = ((Vector2)e.transform.position - pPos).normalized;
            float dot = Vector2.Dot(pUp, dir);
            if (dot > bestDot) { bestDot = dot; best = e; }
        }
        return best;
    }

    void ConvertBulletToAlly(Bullet b, Enemy target, float speed, float dmg)
    {
        GameObject go = b.gameObject;
        Destroy(b);
        if (go.TryGetComponent<Collider2D>(out var col)) col.enabled = false;
        var hb = go.AddComponent<AllyHomingMissile>();
        hb.Init(target, speed, dmg);
    }
}

public class AllyHomingMissile : MonoBehaviour
{
    Enemy target;
    float speed, dmg;
    Vector3 moveDir;

    public void Init(Enemy t, float s, float d)
    {
        target = t;
        speed = s;
        dmg = d;
        moveDir = GameManager.I && GameManager.I.player ? GameManager.I.player.up : Vector3.up;
        Destroy(gameObject, 4f);
    }

    void Update()
    {
        if (!target && GameManager.I)
        {
            if (GameManager.I.CurrentBoss) target = GameManager.I.CurrentBoss;
            else if (GameManager.I.list.Count > 0) target = GameManager.I.list[0];
        }

        if (target)
        {
            moveDir = (target.transform.position - transform.position).normalized;
            float angle = Mathf.Atan2(moveDir.y, moveDir.x) * Mathf.Rad2Deg - 90f;
            transform.rotation = Quaternion.Euler(0, 0, angle);

            if (Vector2.Distance(transform.position, target.transform.position) < 0.6f)
            {
                target.TakeDamage((int)dmg);
                Destroy(gameObject);
                return;
            }
        }
        transform.position += moveDir * (speed * Time.deltaTime);
    }
}
