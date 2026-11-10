using UnityEngine;

public class Projectile_Dagger : MonoBehaviour
{
    public float speed = 25f;
    public int damage = 10;
    public float lifeTime = 2.5f;

    public Transform owner;

    Rigidbody rb;

    [Header("Blink Mode")]
    public bool blinkMode;          // T 예약된 경우 true로 발사됨

    bool landed;
    Vector3 landedPos;
    Vector3 landedNormal = Vector3.up;
    public Vector3 TeleportNormal => landedNormal;

    public bool CanTeleport => landed;
    public Vector3 TeleportPos => landedPos;

    Collider col;

    [Header("Stick Rule")]
    public LayerMask stickMask;

    [Header("Blink VFX")]
    public GameObject blinkVFXRoot;      // 자식 VFX 루트(Loop/Trail/VFXGraph 등)
    

    void Awake()
    {
        rb = GetComponent<Rigidbody>();

        col = GetComponent<Collider>();

        SetBlinkVFX(false);
    }

    public void Fire(Vector3 dir)
    {
        dir.Normalize();
        rb.velocity = dir * speed;

        // 날아가는 방향으로 단검 회전(모델 축이 다르면 수정)
        if (dir.sqrMagnitude > 0.0001f)
            transform.rotation = Quaternion.LookRotation(-dir);

        SetBlinkVFX(blinkMode);

        Destroy(gameObject, lifeTime);
    }


    void OnTriggerEnter(Collider other)
    {
        // 1) 내 소유자(플레이어)면 무시
        if (owner != null && (other.transform == owner || other.transform.IsChildOf(owner)))
            return;

        // 2_1) 순간이동 단검: 적은 무시, 벽/바닥(트리거 아닌 것)에만 박힘
        if (blinkMode)
        {
            if (!other.isTrigger)
            {
                // Wall/Ground 레이어에만 박히기
                if (((1 << other.gameObject.layer) & stickMask) != 0)
                    Stick(other);
            }
            return;
        }

        // 2_2) 평소 단검: 적 데미지
        var enemy = other.GetComponentInParent<Enemy_Health>();
        if (enemy != null)
        {
            enemy.TakeDamage(damage);
            Destroy(gameObject);
            return;
        }

        // 3) 트리거가 아닌 벽/바닥 등에 닿으면
        if (!other.isTrigger)
        {
            // 일반 단검은 즉시 사라짐
            if (!blinkMode)
                Destroy(gameObject);

            // blinkMode는 위에서 Stick하고 return하므로 여기까지 보통 안 옴
        }
    }

    // 벽/바닥에 박히기
    void Stick(Collider other)
    {
        if (landed) return;

        landed = true;
        landedPos = transform.position;

        // 표면 바깥 방향 추정(가까운 점 기준)
        Vector3 cp = other.ClosestPoint(transform.position);
        Vector3 n = (transform.position - cp);
        if (n.sqrMagnitude < 0.0001f) n = -transform.forward; // fallback
        landedNormal = n.normalized;

        rb.velocity = Vector3.zero;
        rb.isKinematic = true;

        if (col) col.enabled = false;

        // 움직이는 플랫폼이면 같이 움직이게 붙이기 (원치 않으면 이 줄 삭제 가능)
        transform.SetParent(other.transform, true);
    }

    void SetBlinkVFX(bool on)
    {
        if (blinkVFXRoot) blinkVFXRoot.SetActive(on);
    }

}

