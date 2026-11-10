using UnityEngine;
using UnityEngine.AI;

public class Enemy_ChaseAttack2 : MonoBehaviour
{
    [Header("Target")]
    public Transform target;                 // 비워두면 Player 태그 자동

    [Header("Chase")]
    public float detectRange = 10f;
    public float loseRange = 14f;
    public float stopRange = 1.8f;          // 이 거리면 공격 시도

    NavMeshAgent agent;
    bool chasing;
    bool dead;

    [Header("Player Stealth")]
    public float revealRange = 2.5f;              // 은신 중에도 이 거리면 들킴
    public Player_Skill_Stealth stealth;          // Player에 붙은 은신 스크립트

    [Header("Vision (FOV)")]
    [Range(1f, 180f)]
    public float viewAngle = 120f;                // 부채꼴 시야각(전체 각도)
    public Transform eye;                         // 눈 위치(없으면 자동)
    public float eyeHeight = 1.6f;                // eye 없을 때 높이
    public LayerMask obstacleMask;                // 벽/장애물 마스크 (시야 가림 체크용)

    [Header("Attack")]
    public float attackCooldown = 1.2f;
    public int damage = 10;
    public float hitRadius = 1.2f;
    public LayerMask targetMask;            // Player 레이어/마스크
    public Transform attackPoint;           // 공격 판정 중심(없으면 자기 앞)
    public float attackWindup = 0.2f;       // 휘두르기 시작 후 맞는 타이밍
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

        // =========================
        // [0] 시야 기반 감지 판정 (은신 포함)
        // =========================
        bool canSee = CanSeeTarget(dist);

        // 추적 시작/유지/종료를 "canSee" 기반으로 바꿈
        if (!chasing)
        {
            if (canSee && dist <= detectRange) chasing = true;
        }
        else
        {
            // 추적 중인데 너무 멀어지면 끄기
            if (dist >= loseRange)
            {
                chasing = false;
                SafeResetPath();
            }
            // loseRange 안이라도 완전히 시야에서 놓치면 끄고 싶으면 아래 주석 해제
            else if (!canSee)
            {
                chasing = false;
                SafeResetPath();
            }
        }

        if (!chasing) { UpdateAnim(); return; }

        // =========================
        // [1] 공격 거리면 멈추고 공격
        // =========================
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

        // =========================
        // [2] 추적 이동
        // =========================
        if (agent && agent.isOnNavMesh)
            agent.SetDestination(target.position);

        UpdateAnim();
    }

    // -------------------------
    // 핵심: 부채꼴 시야 + 레이캐스트 가시선 + 은신 처리
    // -------------------------
    bool CanSeeTarget(float dist)
    {
        if (!target) return false;

        if (dist > loseRange) return false;

        if (stealth && stealth.IsStealthed && dist > revealRange)
            return false;

        // 0) 기준점(눈)과 타겟점(가슴/머리) 설정
        Vector3 eyePos = eye ? eye.position : (transform.position + Vector3.up * eyeHeight);
        Vector3 targetPos = target.position + Vector3.up * 1.0f;

        // 1) 3D 방향 벡터로 FOV 체크
        Vector3 dir = targetPos - eyePos;
        float len = dir.magnitude;
        if (len <= 0.001f) return true;
        dir /= len;

        float half = viewAngle * 0.5f;
        float ang = Vector3.Angle(transform.forward, dir);   // 3D 원뿔 각도
        if (ang > half) return false;

        // 2) 가시선(벽 가림) 체크
        if (Physics.Raycast(eyePos, dir, len, obstacleMask, QueryTriggerInteraction.Ignore))
            return false;

        return true;
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

        Vector3 center;
        if (attackPoint) center = attackPoint.position;
        else center = transform.position + transform.forward * 1.0f + Vector3.up * 1.0f;

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

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow; Gizmos.DrawWireSphere(transform.position, detectRange);
        Gizmos.color = Color.cyan; Gizmos.DrawWireSphere(transform.position, loseRange);
        Gizmos.color = Color.red; Gizmos.DrawWireSphere(transform.position, stopRange);
        Gizmos.color = Color.gray; Gizmos.DrawWireSphere(transform.position, revealRange);

        // 시야 부채꼴(대충 선 2개로 표시)
        Vector3 origin = eye ? eye.position : (transform.position + Vector3.up * eyeHeight);
        Vector3 forward = transform.forward;
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