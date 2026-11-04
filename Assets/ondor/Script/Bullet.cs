using UnityEngine;

public class Bullet : MonoBehaviour
{
    public float speed = 5, turn = 120, safeTime = 1.5f, life = 8, dmg = 10;
    float t;
    bool armed;
    SpriteRenderer sr;

    void Awake() => sr = GetComponentInChildren<SpriteRenderer>();

    void Update()
    {
        float dt = Time.deltaTime;
        t += dt;
        if (t > life)
        {
            Destroy(gameObject);
            return;
        }
        if (!armed && safeTime > 0 && t >= safeTime)
        {
            armed = true;
            if (sr) sr.color = Color.red;
        }
        float a = Vector2.SignedAngle(transform.up, GameManager.I.player.position - transform.position);
        transform.Rotate(0, 0, Mathf.Clamp(a, -turn * dt, turn * dt));
        transform.Translate(0, speed * dt, 0);
    }

    void OnTriggerEnter2D(Collider2D c)
    {
        var e = armed ? c.GetComponentInParent<Enemy>() : null;
        if (c.GetComponentInParent<Player>()) GameManager.I.Damage(dmg);
        else if (e) e.TakeDamage(dmg);
        else return;
        Destroy(gameObject);
    }
}
