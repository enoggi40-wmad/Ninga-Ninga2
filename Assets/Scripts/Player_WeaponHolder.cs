using UnityEngine;

public class Player_WeaponHolder : MonoBehaviour
{
    [Header("Slots")]
    [SerializeField] WeaponData[] slots = new WeaponData[3];
    [SerializeField] int currentIndex = 0;
    public int CurrentIndex => currentIndex;

    [Header("Refs")]
    [SerializeField] Transform weaponSocket;
    [SerializeField] Transform throwPoint;
    [SerializeField] Transform meleePoint;
    [SerializeField] Camera cam;
    [SerializeField] Animator animator;

    GameObject currentModelInstance;
    float lastAttackTime;

    [Header("Blink (T)")]
    [SerializeField] Player_Skill_DaggerTeleport daggerTeleportSkill;
    bool blinkQueued;

    [Header("Hand Blink VFX")]
    [SerializeField] string handBlinkVFXRootName = "HandBlinkVFXRoot";
    GameObject handBlinkVFXRoot;

    void Awake()
    {
        if (!cam) cam = Camera.main;
        if (!animator) animator = GetComponentInChildren<Animator>();
        if (!daggerTeleportSkill) daggerTeleportSkill = GetComponentInParent<Player_Skill_DaggerTeleport>();
    }

    void Start()
    {
        Equip(currentIndex);
    }

    void Update()
    {
        HandleWeaponSwapInput();
        HandleAttackInput();
        HandleBlinkPrepareInput();
    }

    // =========================
    // Input
    // =========================
    void HandleWeaponSwapInput()
    {
        if (Input.GetKeyDown(KeyCode.Alpha1)) Equip(0);
        if (Input.GetKeyDown(KeyCode.Alpha2)) Equip(1);
        if (Input.GetKeyDown(KeyCode.Alpha3)) Equip(2);
    }

    void HandleAttackInput()
    {
        if (Input.GetMouseButtonDown(0))
            TryAttackInput();
    }

    void HandleBlinkPrepareInput()
    {
        if (!Input.GetKeyDown(KeyCode.T)) return;
        if (!daggerTeleportSkill) return;

        blinkQueued = daggerTeleportSkill.TogglePrepareBlink();
        SetHandBlinkVFX(blinkQueued);
    }

    // =========================
    // Weapon / Equip
    // =========================
    WeaponData GetCurrentWeapon()
    {
        if (slots == null || currentIndex < 0 || currentIndex >= slots.Length) return null;
        return slots[currentIndex];
    }

    public WeaponData GetEquippedWeapon() => GetCurrentWeapon();

    public void Equip(int index)
    {
        if (slots == null || index < 0 || index >= slots.Length) return;
        currentIndex = index;

        ReplaceHandModel(GetCurrentWeapon());

        // 장착 바뀌면 예약/표시 초기화
        ForceClearBlinkQueue();

        // 새 모델에 대한 VFX 루트 캐시
        CacheHandBlinkVFXRoot();
        SetHandBlinkVFX(false);
    }

    void ReplaceHandModel(WeaponData w)
    {
        if (currentModelInstance) Destroy(currentModelInstance);

        if (w != null && w.weaponModelPrefab != null && weaponSocket != null)
        {
            currentModelInstance = Instantiate(w.weaponModelPrefab, weaponSocket);
            currentModelInstance.transform.localPosition = Vector3.zero;
            currentModelInstance.transform.localRotation = Quaternion.identity;
        }
        else
        {
            currentModelInstance = null;
        }
    }

    // =========================
    // Attack flow
    // =========================
    void TryAttackInput()
    {
        WeaponData w = GetCurrentWeapon();
        if (w == null) return;

        if (Time.time < lastAttackTime + w.cooldown) return;
        lastAttackTime = Time.time;

        // 모션 트리거 (실제 공격은 애니 이벤트에서 Anim_AttackNow로 호출)
        if (animator && !string.IsNullOrEmpty(w.animTrigger))
        {
            animator.ResetTrigger(w.animTrigger);
            animator.SetTrigger(w.animTrigger);
        }
    }

    // Animation Event
    public void Anim_AttackNow()
    {
        PerformAttackForCurrent();
    }

    void PerformAttackForCurrent()
    {
        WeaponData w = GetCurrentWeapon();
        if (w == null) return;

        if (w.type == WeaponType.ThrowProjectile) ThrowProjectile(w);
        else if (w.type == WeaponType.Melee) MeleeAttack(w);
    }

    // =========================
    // Throw / Melee
    // =========================
    void ThrowProjectile(WeaponData w)
    {
        if (!w.projectilePrefab || !throwPoint || !cam) return;

        Vector3 dir = GetAimDirection(w);
        GameObject obj = Instantiate(w.projectilePrefab, throwPoint.position, Quaternion.identity);

        var proj = obj.GetComponent<Projectile_Dagger>();
        if (proj)
        {
            proj.owner = transform;

            // 이번 투척을 텔포용으로 만들지 결정
            if (daggerTeleportSkill != null)
                daggerTeleportSkill.OnDaggerSpawned(proj);

            // 던지는 순간 예약 소비(손 VFX 끄기)
            blinkQueued = false;
            SetHandBlinkVFX(false);

            proj.Fire(dir);
            return;
        }

        // fallback
        Rigidbody rb = obj.GetComponent<Rigidbody>();
        if (rb) rb.velocity = dir * 25f;
    }

    Vector3 GetAimDirection(WeaponData w)
    {
        Ray ray = cam.ScreenPointToRay(Input.mousePosition);

        if (Physics.Raycast(ray, out RaycastHit hit, w.aimMaxDistance, w.aimMask, QueryTriggerInteraction.Ignore))
            return (hit.point - throwPoint.position).normalized;

        Vector3 far = ray.origin + ray.direction * w.aimMaxDistance;
        return (far - throwPoint.position).normalized;
    }

    void MeleeAttack(WeaponData w)
    {
        Transform p = meleePoint ? meleePoint : throwPoint;
        if (!p) return;

        Vector3 center = p.position + transform.forward * w.meleeRange;
        center.y = transform.position.y + 1.0f;

        Collider[] hits = Physics.OverlapSphere(center, w.meleeRadius, w.meleeMask, QueryTriggerInteraction.Ignore);
        for (int i = 0; i < hits.Length; i++)
        {
            Enemy_Health enemy = hits[i].GetComponentInParent<Enemy_Health>();
            if (enemy) enemy.TakeDamage(w.damage);
        }
    }

    // =========================
    // Hand Blink VFX
    // =========================
    void CacheHandBlinkVFXRoot()
    {
        handBlinkVFXRoot = currentModelInstance
            ? currentModelInstance.transform.Find(handBlinkVFXRootName)?.gameObject
            : null;
    }

    void SetHandBlinkVFX(bool on)
    {
        if (handBlinkVFXRoot) handBlinkVFXRoot.SetActive(on);
    }

    public void ForceClearBlinkQueue()
    {
        blinkQueued = false;
        SetHandBlinkVFX(false);
    }

    // =========================
    // Gizmos
    // =========================
    void OnDrawGizmosSelected()
    {
        WeaponData w = null;
        if (slots != null && currentIndex >= 0 && currentIndex < slots.Length) w = slots[currentIndex];
        if (w == null || w.type != WeaponType.Melee) return;

        Transform p = meleePoint ? meleePoint : throwPoint;
        if (!p) return;

        Vector3 center = p.position + transform.forward * w.meleeRange;
        Gizmos.DrawWireSphere(center, w.meleeRadius);
    }
}