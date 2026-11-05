using UnityEngine;

public class RepeatBG : MonoBehaviour
{
    [SerializeField][Range(1f, 200f)] float speed = 3f;

    float repeatDistance;
    Vector2 startPos;
    float newPos;

    void Start()
    {
        startPos = transform.position;
        repeatDistance = GetComponentInChildren<SpriteRenderer>().bounds.size.y;
    }

    void Update()
    {
        newPos = Mathf.Repeat(Time.time * speed, repeatDistance);
        transform.position = startPos + Vector2.down * newPos;
    }
}