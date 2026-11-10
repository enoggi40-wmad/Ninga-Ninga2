using UnityEngine;
using UnityEngine.AI;

public class Enemy_ChaseAttack3 : MonoBehaviour
{
    [Header("Target")]
    public Transform target;

    [Header("Chase")]
    public float detectRange = 10f;
    public float loseRange = 14f;
    public float stopRange = 1.8f;

    NavMeshAgent agent;
    bool chasing;
    bool dead;

    [Header("Player Stealth")]
    public float revealRange = 2.5f;
    public Player_Skill_Stealth stealth;

    [Header("Vision (FOV)")]
    [Range(1f, 180f)]
    public float viewAngle = 120f;          // yaw
    [Range(1f, 180f)]
    public float verticalAngle = 175f;      // pitch (위/아래)
    public Transform eye;
    public float eyeHeight = 1.6f;
    public LayerMask obstacleMask;

    [Header("Attack")]
    public float attackCooldown = 1.2f;
    public int damage = 10;
    public float hitRadius = 1.2f;
    public LayerMask targetMask;
    public Transform attackPoint;
    public float attackWindup = 0.2f;
    float lastAttackTime = -999f;

    [Header("Animation")]
    public Animator animator;
    public string attackTrigger = "Slash";
    public string speedParam = "Speed";
    public float speedDamp = 0.1f;

    void Awake()
    {
        agent = GetComponent<NavMeshAgent>();

        if (!target)
        {
            var p = GameObject.FindGameObjectWithTag("Player");
            if (p) target = p.transform;
        }

        if (!animator) animator = GetComponentInChildren<Animator>();

        if (target)
            stealth = target.GetComponent<Player_Skill_Stealth>();
    }

    void Update()
    {
        if (dead) return;
        if (!target) return;

        float dist = Vector3.Distance(transform.position, target.position);

        bool canSeeForStart = CanSeeTarget(detectRange);
        bool canSeeForKeep = CanSeeTarget(loseRange);

        if (!chasing)
        {
            if (canSeeForStart) chasing = true;
        }
        else
        {
            if (dist >= loseRange || !canSeeForKeep)
            {
                chasing = false;
                SafeResetPath();
            }
        }

        if (!chasing) { UpdateAnim(); return; }

        if (dist <= stopRange)
        {
            SafeResetPath();

            Vector3 look = target.position - transform.position;
            look.y = 0f;
            if (look.sqrMagnitude > 0.0001f)
                transform.rotation = Quaternion.LookRotation(look.normalized);

            TryAttack();
            UpdateAnim();
            return;
        }

        if (agent && agent.isOnNavMesh)
            agent.SetDestination(target.position);

        UpdateAnim();
    }

    Vector3 GetEyePos()
    {
        return eye ? eye.position : (transform.position + Vector3.up * eyeHeight);
    }

    Vector3 GetFwd()
    {
        return eye ? eye.forward : transform.forward;
    }

    Vector3 GetTargetAimPos()
    {
        if (!target) return transform.position;

        // CharacterController 우선
        var cc = target.GetComponent<CharacterController>();
        if (cc)
            return target.position + Vector3.up * (cc.height * 0.6f);

        // CapsuleCollider도 고려
        var cap = target.GetComponent<CapsuleCollider>();
        if (cap)
            return target.position + Vector3.up * (cap.height * 0.6f);

        // 기본값
        return target.position + Vector3.up * 1.0f;
    }

    bool CanSeePoint(Vector3 pointPos, float range, bool applyStealth)
    {
        if (dead) return false;

        Vector3 eyePos = GetEyePos();

        Vector3 to = pointPos - eyePos;
        float len = to.magnitude;

        if (len > range) return false;
        if (len <= 0.001f) return true;

        if (applyStealth && stealth && stealth.IsStealthed && len > revealRange)
            return false;

        Vector3 dir = to / len;

        Vector3 fwd = GetFwd();

        // yaw (수평)
        Vector3 fwdFlat = new Vector3(fwd.x, 0f, fwd.z);
        Vector3 dirFlat = new Vector3(dir.x, 0f, dir.z);

        if (fwdFlat.sqrMagnitude < 0.0001f) return false;
        fwdFlat.Normalize();

        // dirFlat 임계값을 낮춰서, 대부분의 경우 yaw가 적용되게 함
        const float dirFlatEps = 0.000001f;

        if (dirFlat.sqrMagnitude >= dirFlatEps)
        {
            dirFlat.Normalize();
            float yaw = Vector3.Angle(fwdFlat, dirFlat);
            if (yaw > viewAngle * 0.5f) return false;
        }
        else
        {
            // 거의 정수리/발밑이면 yaw는 무의미하니 스킵
            // (pitch만으로 판단)
        }

        // pitch (수직): 위/아래 모두 허용
        float pitch = Mathf.Abs(Mathf.Asin(Mathf.Clamp(dir.y, -1f, 1f)) * Mathf.Rad2Deg);
        if (pitch > verticalAngle * 0.5f) return false;

        if (Physics.Raycast(eyePos, dir, len, obstacleMask, QueryTriggerInteraction.Ignore))
            return false;

        return true;
    }

    bool CanSeeTarget(float range)
    {
        if (!target) return false;
        return CanSeePoint(GetTargetAimPos(), range, applyStealth: true);
    }

    public bool CanSeeTransform(Transform t, float range)
    {
        if (!t) return false;

        // t가 플레이어가 아니라면 높이 추정이 어려우니 기본 오프셋만 사용
        Vector3 pos = t.position + Vector3.up * 1.0f;
        return CanSeePoint(pos, range, applyStealth: false);
    }

    void TryAttack()
    {
        if (Time.time < lastAttackTime + attackCooldown) return;
        lastAttackTime = Time.time;

        if (animator && !string.IsNullOrEmpty(attackTrigger))
        {
            animator.ResetTrigger(attackTrigger);
            animator.SetTrigger(attackTrigger);
        }
    }

    public void AnimEvent_AttackHit()
    {
        DoHit();
    }

    void DoHit()
    {
        if (dead) return;

        Vector3 center = attackPoint
            ? attackPoint.position
            : (transform.position + transform.forward * 1.0f + Vector3.up * 1.0f);

        Collider[] cols = Physics.OverlapSphere(center, hitRadius, targetMask, QueryTriggerInteraction.Ignore);
        for (int i = 0; i < cols.Length; i++)
        {
            var hp = cols[i].GetComponentInParent<Player_Health>();
            if (hp) hp.TakeDamage(damage);
        }
    }

    void UpdateAnim()
    {
        if (!animator || !agent) return;

        Vector3 v = agent.velocity;
        v.y = 0f;

        float speed01 = Mathf.Clamp01(v.magnitude / Mathf.Max(0.01f, agent.speed));
        animator.SetFloat(speedParam, speed01, speedDamp, Time.deltaTime);
    }

    void SafeResetPath()
    {
        if (!agent || !agent.isOnNavMesh) return;
        agent.ResetPath();
    }

    public void StopAllOnDeath()
    {
        dead = true;
        chasing = false;

        SafeResetPath();

        if (agent && agent.isOnNavMesh)
            agent.isStopped = true;

        if (agent) agent.enabled = false;
        enabled = false;
    }

    public void ResetAfterSpawn(Transform newTarget = null)
    {
        dead = false;
        chasing = false;
        enabled = true;

        if (newTarget != null) target = newTarget;
        if (!target)
        {
            var p = GameObject.FindGameObjectWithTag("Player");
            if (p) target = p.transform;
        }
        if (target) stealth = target.GetComponent<Player_Skill_Stealth>();

        if (!agent) agent = GetComponent<NavMeshAgent>();
        if (agent)
        {
            agent.enabled = true;
            agent.isStopped = false;
            if (agent.isOnNavMesh) agent.ResetPath();
        }

        lastAttackTime = -999f;
    }

    public void OnHitTurnToTarget()
    {
        if (dead || !target) return;

        Vector3 look = target.position - transform.position;
        look.y = 0f;
        if (look.sqrMagnitude > 0.0001f)
            transform.rotation = Quaternion.LookRotation(look.normalized);

        SafeResetPath();
    }

    public void TurnToPoint(Vector3 worldPos)
    {
        if (dead) return;

        Vector3 look = worldPos - transform.position;
        look.y = 0f;
        if (look.sqrMagnitude > 0.0001f)
            transform.rotation = Quaternion.LookRotation(look.normalized);

        SafeResetPath();
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow; Gizmos.DrawWireSphere(transform.position, detectRange);
        Gizmos.color = Color.cyan; Gizmos.DrawWireSphere(transform.position, loseRange);
        Gizmos.color = Color.red; Gizmos.DrawWireSphere(transform.position, stopRange);
        Gizmos.color = Color.gray; Gizmos.DrawWireSphere(transform.position, revealRange);

        Vector3 origin = GetEyePos();
        Vector3 forward = GetFwd();
        forward.y = 0f;
        if (forward.sqrMagnitude < 0.0001f) forward = transform.forward;
        forward.Normalize();

        float half = viewAngle * 0.5f;

        Quaternion leftRot = Quaternion.AngleAxis(-half, Vector3.up);
        Quaternion rightRot = Quaternion.AngleAxis(half, Vector3.up);

        Gizmos.color = Color.green;
        Gizmos.DrawLine(origin, origin + (leftRot * forward) * detectRange);
        Gizmos.DrawLine(origin, origin + (rightRot * forward) * detectRange);

        Vector3 center = attackPoint
            ? attackPoint.position
            : (transform.position + transform.forward * 1.0f + Vector3.up * 1.0f);

        Gizmos.color = Color.magenta;
        Gizmos.DrawWireSphere(center, hitRadius);
    }
}