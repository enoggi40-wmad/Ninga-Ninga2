using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class Player_Skill_Dash : MonoBehaviour
{
    [Header("Input")]
    public KeyCode skillKey = KeyCode.E;

    [Header("Charge")]
    public float chargeTime = 2.0f;
    public bool cancelIfKeyUp = true;

    [Header("Dash")]
    public float dashSpeed = 22f;
    public float dashTime = 0.4f;
    public float dashMaxDistance = 20f;

    [Header("Cooldown")]
    public float cooldown = 2.0f;

    [Header("Refs")]
    public Player_WeaponHolder weaponHolder;   // Player에 있는 WeaponHolder
    public Player_Movement movement;           // 너 기존 이동 스크립트
    public LayerMask obstacleMask = ~0;        // 벽/장애물 레이어

    [Header("Hit")]
    public int damage = 30;
    public float hitRadius = 1.2f;
    public LayerMask enemyMask;

    [Header("Dash Aim")]
    public Transform cam;      // 카메라 트랜스폼(없으면 Main Camera 사용)
    public bool dashToCameraForward = true;

    [Header("Gravity During Skill")]
    public float gravity = 20f;   // Player_Movement랑 같은 값 추천
    float dashYVel;

    [Header("Animation")]
    public Animator animator;
    public string chargeTrigger = "Charge";
    public string dashTrigger = "DashSlash";

    [Header("VFX")]
    public GameObject chargeVfx;   // 차지 중 켤 오브젝트
    public GameObject dashVfx;     // (선택) 대쉬 시작 순간 켤 오브젝트

    [Header("VFX End")]
    public GameObject endFlashVfxPrefab;   // 반짝 프리팹
    public Transform endFlashPoint;        // 위치(없으면 Player 기준)
    public float endFlashLife = 1.0f;      // 삭제 시간

    [Header("VFX Cancel")]
    public GameObject cancelVfxPrefab;   // 취소 이펙트 프리팹
    public Transform cancelVfxPoint;     // 위치(없으면 플레이어 기준)
    public float cancelVfxLife = 1.0f;   // 삭제 시간

    [Header("End Lag")]
    public float endLag = 0.2f;

    CharacterController cc;
    float lastUseTime = -999f;
    bool isDashing;

    int playerLayer;
    int enemyLayer;
    HashSet<Enemy_Health> hitSet = new HashSet<Enemy_Health>();

    void Awake()
    {
        cc = GetComponent<CharacterController>();
        if (!weaponHolder) weaponHolder = GetComponent<Player_WeaponHolder>();
        if (!movement) movement = GetComponent<Player_Movement>();
        if (!cam && Camera.main) cam = Camera.main.transform;

        if (!animator) animator = GetComponentInChildren<Animator>();

        playerLayer = LayerMask.NameToLayer("Player");
        enemyLayer = LayerMask.NameToLayer("Enemy");
    }

    void Update()
    {
        if (isDashing) return;

        // 검(근접)일 때만
        var w = weaponHolder ? weaponHolder.GetEquippedWeapon() : null;
        if (w == null || w.type != WeaponType.Melee) return;

        if (Input.GetKeyDown(skillKey) && Time.time >= lastUseTime + cooldown)
        {
            StartCoroutine(CoChargeThenDash());
        }
    }

    IEnumerator CoChargeThenDash()
    {
        isDashing = true;
        lastUseTime = Time.time;

        // 차지 중엔 이동 금지
        if (movement) movement.enabled = false;

        // VFX는 애니랑 상관없이 켜기(원하면)
        if (chargeVfx) chargeVfx.SetActive(true);

        // (선택) 차지 애니 트리거
        if (animator && !string.IsNullOrEmpty(chargeTrigger))
        {
            animator.ResetTrigger(chargeTrigger);
            animator.SetTrigger(chargeTrigger);
            Debug.Log("Charge trigger!");
        }

        // ===== 차지(2초) : 여기서는 중력 적용 =====
        float t = 0f;
        dashYVel = cc.isGrounded ? -2f : 0f; // 차지 중 중력용 초기화

        while (t < chargeTime)
        {
            FaceCameraYaw();

            if (cancelIfKeyUp && Input.GetKeyUp(skillKey))
            {
                // VFX 끄기
                if (chargeVfx) chargeVfx.SetActive(false);
                if (dashVfx) dashVfx.SetActive(false);

                // 애니 원복(중요)
                if (animator)
                {
                    if (!string.IsNullOrEmpty(chargeTrigger)) animator.ResetTrigger(chargeTrigger);
                    if (!string.IsNullOrEmpty(dashTrigger)) animator.ResetTrigger(dashTrigger);

                    // UpperBody 레이어라면 해당 레이어 인덱스 넣기(없으면 0)
                    int upper = animator.GetLayerIndex("UpperBody");
                    if (upper < 0) upper = 0;

                    animator.Play("Locomotion", 0, 0f);
                }

                if (cancelVfxPrefab)
                {
                    Vector3 pos = cancelVfxPoint ? cancelVfxPoint.position : (transform.position + Vector3.up * 1.0f);
                    Quaternion rot = cancelVfxPoint ? cancelVfxPoint.rotation : Quaternion.identity;

                    GameObject fx = Instantiate(cancelVfxPrefab, pos, rot);
                    Destroy(fx, cancelVfxLife);
                }

                // 대쉬 끝 - 잠깐 경직(End Lag)
                if (movement) movement.enabled = false;

                float lag = endLag;
                while (lag > 0f)
                {
                    //FaceCameraYaw();

                    lag -= Time.deltaTime;
                    yield return null;
                }

                // 이동 복구
                if (movement) movement.enabled = true;
                isDashing = false;

                yield break;
            }

            // 중력 적용(차지 중 낙하/바닥붙기)
            if (cc.isGrounded && dashYVel < 0f) dashYVel = -2f;
            dashYVel -= gravity * Time.deltaTime;
            cc.Move(Vector3.up * (dashYVel * Time.deltaTime));

            t += Time.deltaTime;
            yield return null;
        }

        // ===== 대쉬 시작 : 대쉬 VFX 켜기 =====
        if (dashVfx) dashVfx.SetActive(true);

        // (선택) 대쉬슬래시 애니 트리거
        if (animator && !string.IsNullOrEmpty(dashTrigger))
        {
            animator.ResetTrigger(dashTrigger);
            animator.SetTrigger(dashTrigger);
            Debug.Log("DashSlash trigger!");
        }

        hitSet.Clear();

        if (playerLayer != -1 && enemyLayer != -1)
            Physics.IgnoreLayerCollision(playerLayer, enemyLayer, true);

        // ===== 대쉬 방향(카메라 보는 방향 그대로) =====
        Vector3 dir;

        if (dashToCameraForward && cam)
        {
            dir = cam.forward;

            // 너무 과하게 위/아래로 쏘지 않게 제한(원하는 값으로 조절)
            dir.y = Mathf.Clamp(dir.y, -0.2f, 0.6f);

            if (dir.sqrMagnitude < 0.0001f) dir = transform.forward;
        }
        else
        {
            dir = transform.forward;
        }

        dir.Normalize();

        // 벽 앞에서 멈추게 최대거리 보정
        float allowedDist = dashMaxDistance;
        Vector3 rayOrigin = transform.position + Vector3.up * 1.0f;
        if (Physics.Raycast(rayOrigin, dir, out RaycastHit hit, dashMaxDistance, obstacleMask, QueryTriggerInteraction.Ignore))
            allowedDist = Mathf.Max(0f, hit.distance - 0.2f);

        float elapsed = 0f;
        float moved = 0f;

        // ===== 대쉬 : 여기서는 중력 미적용(시작 방향 고정) =====
        while (elapsed < dashTime && moved < allowedDist)
        {
            float step = dashSpeed * Time.deltaTime;
            step = Mathf.Min(step, allowedDist - moved);

            cc.Move(dir * step);
            DoHitCheck();

            moved += step;
            elapsed += Time.deltaTime;
            yield return null;
        }

        // 대쉬 끝 - 잠깐 경직(End Lag)
        if (movement) movement.enabled = false;

        float cancelLag = endLag;
        while (cancelLag > 0f)
        {
            // 중력만 살짝 적용
            if (!cc.isGrounded)
                cc.Move(Vector3.up * (-gravity * Time.deltaTime * Time.deltaTime));

            cancelLag -= Time.deltaTime;
            yield return null;
        }

        // 이동 복구
        if (movement) movement.enabled = true;

        if (playerLayer != -1 && enemyLayer != -1)
            Physics.IgnoreLayerCollision(playerLayer, enemyLayer, false);

        // 스킬 끝 반짝
        if (endFlashVfxPrefab)
        {
            Vector3 pos = endFlashPoint ? endFlashPoint.position : (transform.position + Vector3.up * 1.0f);
            Quaternion rot = endFlashPoint ? endFlashPoint.rotation : Quaternion.identity;

            GameObject fx = Instantiate(endFlashVfxPrefab, pos, rot);
            Destroy(fx, endFlashLife);
        }

        // VFX 정리
        if (chargeVfx) chargeVfx.SetActive(false);
        if (dashVfx) dashVfx.SetActive(false);

        isDashing = false;
    }

    void FaceCameraYaw()
    {
        if (!cam) return;
        Vector3 e = transform.eulerAngles;
        e.y = cam.eulerAngles.y;
        transform.eulerAngles = e;
    }

    void DoHitCheck()
    {
        // 앞쪽으로 살짝 오프셋된 구체 판정
        Vector3 center = transform.position + transform.forward * 0.8f;

        // 점프해도 바닥 적 맞게 높이 고정(너가 이미 해결했던 방식)
        center.y = transform.position.y + 1.0f;

        Collider[] cols = Physics.OverlapSphere(center, hitRadius, enemyMask, QueryTriggerInteraction.Ignore);
        for (int i = 0; i < cols.Length; i++)
        {
            var enemy = cols[i].GetComponentInParent<Enemy_Health>();
            if (!enemy) continue;

            // 중복 타격 방지(한 번의 대쉬에 한 번만)
            if (hitSet.Contains(enemy)) continue;
            hitSet.Add(enemy);

            enemy.TakeDamage(damage);
        }
    }
}
