using System.Collections;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.AI;

public class Enemy_Health : MonoBehaviour
{
    [Header("HP")]
    public int maxHP = 50;
    public int currentHP;

    [Header("Death")]
    public bool destroyOnDeath = false;
    public float deathDelay = 3f;          // 죽고 몇 초 후 사라질지
    public string deathTrigger = "Die";    // Animator 트리거 이름

    Animator animator;
    bool isDead = false;

    // 스포너가 주입
    EnemySpawner spawner;

    // (current, max)
    public UnityEvent<int, int> onHpChanged;

    [Header("Alert Allies (see victim)")]
    public float alertRadius = 10f;
    public LayerMask allyMask; // Enemy 레이어만 체크!
    public bool alertAlliesOnHit = true;

    void Awake()
    {
        animator = GetComponentInChildren<Animator>();
        ResetState();
    }

    public void OnSpawned(EnemySpawner owner)
    {
        spawner = owner;
        ResetState();

        var ai = GetComponent<Enemy_ChaseAttack3>();
        if (ai) ai.ResetAfterSpawn();
    }

    void ResetState()
    {
        isDead = false;
        currentHP = maxHP;
        onHpChanged?.Invoke(currentHP, maxHP);
    }

    public void TakeDamage(int dmg)
    {
        if (isDead) return;
        if (currentHP <= 0) return;

        // 내 AI
        var myAI = GetComponent<Enemy_ChaseAttack3>();

        // 공격자 위치: 기본은 "플레이어 타겟" (정확 전달하고 싶으면 오버로드로 확장 가능)
        Vector3 attackerPos = transform.position;
        if (myAI && myAI.target) attackerPos = myAI.target.position;

        // [핵심] "나(맞은 적)를 보고 있던" 주변 적들도 공격자 방향으로 고개 돌리기
        if (alertAlliesOnHit)
            AlertAlliesWhoSeeVictim(attackerPos);

        // 기존: 맞은 적도 반응(플레이어 방향)
        if (myAI) myAI.OnHitTurnToTarget();

        // HP 처리
        currentHP = Mathf.Clamp(currentHP - dmg, 0, maxHP);
        onHpChanged?.Invoke(currentHP, maxHP);
        if (animator) animator.SetTrigger("GetHit");
        if (currentHP <= 0)
            Die();   
    }

    void AlertAlliesWhoSeeVictim(Vector3 attackerPos)
    {
        // 반경 내 "적"만 찾기
        Collider[] hits = Physics.OverlapSphere(transform.position, alertRadius, allyMask, QueryTriggerInteraction.Ignore);

        for (int i = 0; i < hits.Length; i++)
        {
            var ai = hits[i].GetComponentInParent<Enemy_ChaseAttack3>();
            if (!ai) continue;

            // 나 자신 제외
            if (ai.gameObject == gameObject) continue;

            // 핵심: "저 적이 나(맞은 적)를 시야로 보고 있었는가?"
            if (!ai.CanSeeTransform(transform, alertRadius)) continue;

            // 그럼 공격자 방향으로 고개 돌리기
            ai.TurnToPoint(attackerPos);
        }
    }

    public void Heal(int amount)
    {
        if (currentHP <= 0) return;

        currentHP = Mathf.Clamp(currentHP + amount, 0, maxHP);
        onHpChanged?.Invoke(currentHP, maxHP);
    }

    void Die()
    {
        if (isDead) return;
        isDead = true;

        var chaseAttack = GetComponent<Enemy_ChaseAttack3>();
        if (chaseAttack) chaseAttack.StopAllOnDeath();

        // 죽는 순간 리스폰 예약(10초 카운트 시작)
        if (spawner != null)
            spawner.NotifyEnemyDied(gameObject);

        if (animator && !string.IsNullOrEmpty(deathTrigger))
            animator.SetTrigger(deathTrigger);

        StartCoroutine(CoDisappearAfterDelay());
    }

    IEnumerator CoDisappearAfterDelay()
    {
        yield return new WaitForSeconds(deathDelay);

        // Destroy 대신 풀로 반환
        if (spawner != null)
            spawner.ReturnToPool(gameObject);
        else
            gameObject.SetActive(false);
    }
}
