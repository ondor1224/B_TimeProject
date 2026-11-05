using UnityEngine;
using System.Collections.Generic;

public class Obstacle : MonoBehaviour
{
    public static List<Obstacle> list = new();
    public float speed = 3, radius = 1.5f, slowDur = 2;
    public Sprite[] themeSprites;
    SpriteRenderer sr;

    void Awake()
    {
        sr = GetComponentInChildren<SpriteRenderer>();
        var g = GameManager.I;
        if (g && sr && themeSprites.Length > 0)
        {
            int idx = Mathf.Clamp(g.Stage - 1, 0, themeSprites.Length - 1);
            if (themeSprites[idx]) sr.sprite = themeSprites[idx];
        }
    }

    void OnEnable() => list.Add(this);
    void OnDisable() => list.Remove(this);

    void Update()
    {
        transform.Translate(0, -speed * Time.deltaTime, 0, Space.World);
        if (transform.position.y < -GameManager.I.half.y - 2) Destroy(gameObject);
    }

    void OnTriggerEnter2D(Collider2D c)
    {
        var p = c.GetComponentInParent<Player>();
        if (!p) return;
        GameManager.I.Damage(1);
        p.Slow(slowDur);
    }
}