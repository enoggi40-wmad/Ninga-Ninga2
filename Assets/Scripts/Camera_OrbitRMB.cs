using UnityEngine;

public class Camera_OrbitRMB : MonoBehaviour
{
    [Header("Refs")]
    public Transform target;      // Player(또는 따라갈 대상)
    public Transform cam;         // Main Camera Transform

    [Header("Distance")]
    public float distance = 2f;
    public float height = 5f;

    [Header("Rotation")]
    public float sensitivity = 3f;
    public float minPitch = -35f;
    public float maxPitch = 70f;

    [Header("Collision")]
    public LayerMask wallMask = ~0;      // 벽/건물 레이어만 넣기 추천
    public float cameraRadius = 0.25f;   // 카메라 구 반지름
    public float wallBuffer = 0.15f;     // 벽에서 살짝 띄우기
    public float smooth = 12f;           // 당기는 속도

    float yaw;
    float pitch;
    float currentDist;

    void Start()
    {
        // 시작 각도(현재 트랜스폼 기준)
        Vector3 e = transform.eulerAngles;
        yaw = e.y;
        pitch = e.x;

        currentDist = distance;

        // 거리 세팅
        if (cam)
            cam.localPosition = new Vector3(0f, 0f, -distance);
    }

    void LateUpdate()
    {
        if (!target || !cam) return;

        // 타겟 위치로 피벗 이동
        transform.position = target.position + Vector3.up * height;

        // RMB 누르는 동안만 회전
        if (Input.GetMouseButton(1))
        {
            float mx = Input.GetAxis("Mouse X");
            float my = Input.GetAxis("Mouse Y");

            yaw += mx * sensitivity;
            pitch -= my * sensitivity;

            pitch = Mathf.Clamp(pitch, minPitch, maxPitch);
        }

        transform.rotation = Quaternion.Euler(pitch, yaw, 0f);

        // ===== [ADD] 벽 있으면 거리 줄이기(SphereCast) =====
        Vector3 pivot = transform.position;
        Vector3 desiredWorldPos = pivot - transform.forward * distance; // 카메라가 가고 싶은 월드 위치
        Vector3 dir = (desiredWorldPos - pivot).normalized;
        float desiredDist = distance;

        if (Physics.SphereCast(pivot, cameraRadius, dir, out RaycastHit hit, distance, wallMask, QueryTriggerInteraction.Ignore))
        {
            desiredDist = Mathf.Max(0.1f, hit.distance - wallBuffer);
        }

        // 부드럽게 거리 변화
        currentDist = Mathf.Lerp(currentDist, desiredDist, 1f - Mathf.Exp(-smooth * Time.deltaTime));


        // 카메라를 뒤로 유지(피벗 기준)
        cam.localPosition = new Vector3(0f, 0f, -currentDist);
        cam.LookAt(transform.position);
    }
}
