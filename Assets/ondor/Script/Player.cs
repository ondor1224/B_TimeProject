using UnityEngine;

public class Player : MonoBehaviour
{
    public float speed = 8, ramDmg = 3, ramCool = .3f;
    public Catalog catalog;
    public SpriteRenderer wing;
    float ramT;

    void Start() => catalog.Apply(wing);

    void Update()
    {
        Vector2 h = GameManager.I.half;
        Vector2 p = (Vector2)transform.position + new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical")).normalized * speed * Time.deltaTime;
        transform.position = new Vector2(Mathf.Clamp(p.x, -h.x, h.x), Mathf.Clamp(p.y, -h.y, h.y));
        if (Input.GetKeyDown(KeyCode.Space)) GameManager.I.Bomb();
    }

    void OnTriggerStay2D(Collider2D c)
    {
        var e = c.GetComponentInParent<Enemy>();
        if (!e || Time.time < ramT) return;
        ramT = Time.time + ramCool;
        e.TakeDamage(ramDmg);
    }
}
