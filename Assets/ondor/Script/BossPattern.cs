using System.Collections;
using UnityEngine;

public class BossPattern : MonoBehaviour
{
    public int bossStage = 1;
    public GameObject bulletPrefab;
    public float moveSpeed = 1.5f, fireRate = 1.2f;
    public AudioClip shootSfx;
    float angleOffset;

    void Start()
    {
        if (GameManager.I) bossStage = GameManager.I.Stage;
        StartCoroutine(PatternRoutine());
    }

    void Update()
    {
        if (!GameManager.I) return;
        float x = Mathf.Sin(Time.time * moveSpeed) * (GameManager.I.half.x - 2f);
        transform.position = new Vector3(x, GameManager.I.half.y - 2.5f, 0);
    }

    IEnumerator PatternRoutine()
    {
        while (true)
        {
            yield return new WaitForSeconds(fireRate);
            SoundManager.PlaySfx(shootSfx);

            if (bossStage == 1)
            {
                FireSpread(3, 20f);
            }
            else if (bossStage == 2)
            {
                FireSpread(4, 15f);
                yield return new WaitForSeconds(0.4f);
                FireCircle(10);
            }
            else
            {
                FireSpread(5, 12f);
                for (int i = 0; i < 12; i++)
                {
                    FireAngle(angleOffset);
                    angleOffset += 30f;
                    yield return new WaitForSeconds(0.08f);
                }
            }
        }
    }

    void FireSpread(int count, float gap)
    {
        if (!GameManager.I || !GameManager.I.player) return;
        Vector2 dir = (GameManager.I.player.position - transform.position).normalized;
        float baseAngle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg - 90f;
        float start = baseAngle - (count - 1) * gap * 0.5f;
        for (int i = 0; i < count; i++) FireAngle(start + i * gap);
    }

    void FireCircle(int count)
    {
        for (int i = 0; i < count; i++) FireAngle(i * (360f / count));
    }

    void FireAngle(float zAngle)
    {
        if (!bulletPrefab) return;
        Instantiate(bulletPrefab, transform.position, Quaternion.Euler(0, 0, zAngle));
    }
}
