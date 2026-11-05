using UnityEngine;

public class Player : MonoBehaviour
{
    [Header("Settings")]
    public float thrust = 15f;
    public float maxSpeed = 8f;
    public float turnSpeed = 250f;
    public float drag = 1.5f;
    public float brakeDrag = 6f;

    public Vector2 Vel { get; private set; }

    float flashT;
    SpriteRenderer sr;

    void Awake()
    {
        sr = GetComponentInChildren<SpriteRenderer>();
    }

    void Update()
    {
        Move();
        ClampPosition();
        if (sr) sr.color = Time.time < flashT ? Color.red : Color.white;
    }

    void Move()
    {
        float turnInput = Input.GetAxisRaw("Horizontal");
        transform.Rotate(0, 0, -turnInput * turnSpeed * Time.deltaTime);

        float moveInput = Input.GetAxisRaw("Vertical");

        if (moveInput > 0)
        {
            Vel += (Vector2)transform.up * thrust * Time.deltaTime;
        }

        float currentDrag = (moveInput < 0) ? brakeDrag : drag;
        Vel = Vector2.Lerp(Vel, Vector2.zero, currentDrag * Time.deltaTime);

        Vel = Vector2.ClampMagnitude(Vel, maxSpeed);

        transform.position += (Vector3)Vel * Time.deltaTime;
    }

    void ClampPosition()
    {
        if (!GameManager.I) return;
        var half = GameManager.I.half;
        Vector3 pos = transform.position;
        pos.x = Mathf.Clamp(pos.x, -half.x + 0.5f, half.x - 0.5f);
        pos.y = Mathf.Clamp(pos.y, -half.y + 0.5f, half.y - 0.5f);
        transform.position = pos;
    }

    public void Flash()
    {
        flashT = Time.time + 0.1f;
    }

    public void Slow(float factor)
    {
        Vel *= factor;
    }

    // --- will(벽)에 닿았을 때 강제로 튕겨내는 기능 ---
    public void BounceFromWall(Vector2 pushDir)
    {
        // 1. 뚫고 들어가지 못하게 벽 바깥으로 살짝 강제 이동 (위치 보정)
        transform.position += (Vector3)pushDir * 0.1f;

        // 2. 벽 쪽으로 가던 관성을 튕겨 나오는 힘으로 바꿈 (당구공처럼)
        Vel = pushDir * (maxSpeed * 0.5f);
    }
}