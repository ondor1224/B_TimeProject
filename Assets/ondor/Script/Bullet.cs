using UnityEngine;

public class Bullet : MonoBehaviour
{
    public float speed = 5, turn = 120, safeTime = 1.5f, life = 8, dmg = 10, arcScale = 0;
    public bool isPlayer;
    float t; bool armed; SpriteRenderer sr; Vector3 baseScale;

    void Awake() { sr = GetComponentInChildren<SpriteRenderer>(); baseScale = transform.localScale; }

    public void Setup(float spd, float trn, float arc = 0, bool isPl = false)
    {
        speed = spd; turn = trn; arcScale = arc; isPlayer = isPl;
    }

    void Update()
    {
        float dt = Time.deltaTime; t += dt;
        if (t > life) { Destroy(gameObject); return; }
        if (!armed && safeTime > 0 && t >= safeTime) { armed = true; if (sr) sr.color = Color.red; }
        if (turn > 0 && GameManager.I.player)
        {
            float a = Vector2.SignedAngle(transform.up, GameManager.I.player.position - transform.position);
            transform.Rotate(0, 0, Mathf.Clamp(a, -turn * dt, turn * dt));
        }
        if (arcScale > 0) transform.localScale = baseScale * (1 + Mathf.Sin(Mathf.Min(t / 1.5f, 1) * Mathf.PI) * arcScale);
        transform.Translate(0, speed * dt, 0);
    }

    void OnTriggerEnter2D(Collider2D c)
    {
        var e = c.GetComponentInParent<Enemy>();
        var p = c.GetComponentInParent<Player>();

        if (isPlayer && e) e.TakeDamage(dmg);
        else if (!isPlayer && p && armed) GameManager.I.Damage(dmg);
        else return;
        Destroy(gameObject);
    }
}