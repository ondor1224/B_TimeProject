using UnityEngine;

public class Will : MonoBehaviour
{
    Collider2D col;

    void Awake()
    {
        col = GetComponent<Collider2D>();
    }

    // 닿는 순간뿐만 아니라 머물러 있는 동안 계속해서 밖으로 밀어냅니다!
    void OnTriggerStay2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            Player p = other.GetComponent<Player>();
            if (p)
            {
                // 벽 표면에서 플레이어를 바깥으로 밀어내는 방향 계산
                Vector2 closestPoint = col.ClosestPoint(p.transform.position);
                Vector2 pushDir = ((Vector2)p.transform.position - closestPoint).normalized;

                // 플레이어가 너무 빨라서 벽 중심과 완전히 겹쳤을 때의 방어코드
                if (pushDir == Vector2.zero)
                {
                    pushDir = -p.Vel.normalized; // 오던 길로 반송
                    if (pushDir == Vector2.zero) pushDir = Vector2.up;
                }

                // 플레이어 튕겨내기 호출
                p.BounceFromWall(pushDir);
            }
        }
    }
}