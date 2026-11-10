using UnityEngine;

public class Item_FloatRotate : MonoBehaviour
{
    [Header("Float")]
    public float floatHeight = 0.3f;   // 떠오르는 높이
    public float floatSpeed = 2f;      // 둥둥 속도

    [Header("Rotate")]
    public float rotateSpeed = 60f;    // 초당 회전 속도

    Vector3 startPos;

    void Start()
    {
        startPos = transform.position;
    }

    void Update()
    {
        // 1. 둥둥 떠오르기 (sin 파형)
        float yOffset = Mathf.Sin(Time.time * floatSpeed) * floatHeight;
        transform.position = startPos + Vector3.up * yOffset;

        // 2. 빙글빙글 회전
        transform.Rotate(Vector3.up * rotateSpeed * Time.deltaTime, Space.World);
    }
}
