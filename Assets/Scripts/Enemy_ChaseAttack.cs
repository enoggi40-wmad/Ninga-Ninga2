using UnityEngine;
using UnityEngine.AI;

public class Enemy_ChaseAttack : MonoBehaviour
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
    // 공격모션
    public string attackTrigger = "Slash";
    // 걷기,달리기 모션
    public string speedParam = "Speed";
    public float speedDamp = 0.1f; // 부드럽게(선택)

    

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
        // [0] 은신 처리
        // 플레이어가 은신 중이면 revealRange 밖에서는 인식/추적 불가
        // =========================
        if (stealth && stealth.IsStealthed && dist > revealRange)
        {
            chasing = false;
            SafeResetPath();
            UpdateAnim();
            return;
        }

        // =========================
        // [1] 추적 시작/종료
        // =========================
        if (!chasing && dist <= detectRange) chasing = true;
        else if (chasing && dist >= loseRange)
        {
            chasing = false;
            SafeResetPath();
        }

        if (!chasing) { UpdateAnim(); return; }

        // =========================
        // [2] 공격 거리면 멈추고 공격 시도
        // =========================
        if (dist <= stopRange)
        {
            SafeResetPath();

            // 타겟 바라보기(수평)
            Vector3 look = target.position - transform.position;
            look.y = 0f;
            if (look.sqrMagnitude > 0.0001f)
                transform.rotation = Quaternion.LookRotation(look.normalized);

            TryAttack();
            UpdateAnim();
            return;
        }

        // =========================
        // [3] 추적 이동
        // =========================
        if (agent && agent.isOnNavMesh)
            agent.SetDestination(target.position);

        UpdateAnim();
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
            // 플레이어 체력 스크립트 이름
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

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow; Gizmos.DrawWireSphere(transform.position, detectRange);
        Gizmos.color = Color.cyan; Gizmos.DrawWireSphere(transform.position, loseRange);
        Gizmos.color = Color.red; Gizmos.DrawWireSphere(transform.position, stopRange);

        Vector3 center = attackPoint
            ? attackPoint.position
            : (transform.position + transform.forward * 1.0f + Vector3.up * 1.0f);

        Gizmos.color = Color.magenta;
        Gizmos.DrawWireSphere(center, hitRadius);

        Gizmos.color = Color.gray;
        Gizmos.DrawWireSphere(transform.position, revealRange);
    }

    public void ResetAfterSpawn(Transform newTarget = null)
    {
        // 스크립트 다시 켜기
        dead = false;
        chasing = false;
        enabled = true;

        // 타겟 다시 연결(필요하면)
        if (newTarget != null) target = newTarget;
        if (!target)
        {
            var p = GameObject.FindGameObjectWithTag("Player");
            if (p) target = p.transform;
        }
        if (target) stealth = target.GetComponent<Player_Skill_Stealth>();

        // NavMeshAgent 다시 켜고 정상 상태로
        if (!agent) agent = GetComponent<NavMeshAgent>();
        if (agent)
        {
            agent.enabled = true;     // 핵심
            agent.isStopped = false;  // 핵심
            if (agent.isOnNavMesh) agent.ResetPath();
        }

        // 공격 쿨다운도 초기화(원하면)
        lastAttackTime = -999f;
    }
}
