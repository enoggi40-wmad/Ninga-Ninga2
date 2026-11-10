using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Player_Skill_DaggerTeleport : MonoBehaviour
{
    [Header("Input")]
    public KeyCode prepareKey = KeyCode.T;   // 다음 단검 1회 텔포용 예약
    public KeyCode teleportKey = KeyCode.E;  // 텔포 실행

    [Header("Teleport")]
    public float safeUp = 0.2f;
    public float blockCheckRadius = 0.2f;
    public LayerMask blockMask = ~0;

    [Header("Cooldown")]
    public float cooldown = 2f; // 원하면 쿨타임 넣기 (ex 5)
    float cdTimer;
    float cdLogTimer;

    [Header("Move")]
    public CharacterController cc; // 쓰면 넣기

    [Header("Teleport VFX (Burst Prefab)")]
    public GameObject teleportStartVFXPrefab; // 텔포 시작 위치 반짝
    public GameObject teleportEndVFXPrefab;   // 텔포 도착 위치 반짝
    public float vfxLifeTime = 2f;            // 자동 삭제 시간(대충)

    bool nextBlink;
    Projectile_Dagger blinkDagger;

    void Awake()
    {
        if (!cc) cc = GetComponent<CharacterController>();
    }

    void Update()
    {
        cdTimer -= Time.deltaTime;

        if (cdTimer > 0f)
        {
            cdLogTimer -= Time.deltaTime;
            if (cdLogTimer <= 0f)
            {
                Debug.Log($"[Blink] Cooldown remaining: {cdTimer:F1}s");
                cdLogTimer = 1f; // 1초마다
            }
        }

        if (Input.GetKeyDown(teleportKey))
            TryTeleport();
    }

    public bool TogglePrepareBlink()
    {
        if (cdTimer > 0f) return nextBlink; // 쿨이면 변화 없음
        nextBlink = !nextBlink;
        return nextBlink;
    }

    void SpawnVFX(GameObject prefab, Vector3 pos, Quaternion rot)
    {
        if (!prefab) return;

        GameObject go = Instantiate(prefab, pos, rot);

        // 파티클 길이 기반으로 더 정확히 제거(없으면 vfxLifeTime 사용)
        float life = vfxLifeTime;
        var ps = go.GetComponentInChildren<ParticleSystem>();
        if (ps)
        {
            var main = ps.main;
            life = Mathf.Max(0.1f, main.duration + main.startLifetime.constantMax);
            ps.Play();
        }

        Destroy(go, life);
    }

    // 무기 홀더가 단검 생성할 때 호출해줘야 함 __ 매개변수가 Projectile_Dagger형
    public void OnDaggerSpawned(Projectile_Dagger proj)
    {
        if (!proj) return;
        //Debug.Log($"[Blink] nextBlink={nextBlink}");

        if (nextBlink)
        {
            proj.blinkMode = true;
            blinkDagger = proj;
            nextBlink = false;
            //Debug.Log("[Blink] registered blink dagger!");
        }
        else
        {
            proj.blinkMode = false;
        }
    }

    void TryTeleport()
    {
        if (cdTimer > 0f) return;
        if (!blinkDagger) return;
        if (!blinkDagger.CanTeleport) return;


        float outOffset = -2f; // 캐릭터 반경보다 조금 크게(0.4~1.0 조절)
        Vector3 target = blinkDagger.TeleportPos
                       + blinkDagger.TeleportNormal * outOffset
                       + Vector3.up * safeUp;

        
        // 벽에 너무 딱붙으면 텔레포트 불가
        if (Physics.CheckSphere(target, blockCheckRadius, blockMask, QueryTriggerInteraction.Ignore))
        {
            Debug.Log("[Blink] blocked: target occupied");
            return;
        }

        //텔포 시작전 vfx
        SpawnVFX(teleportStartVFXPrefab, transform.position, Quaternion.identity);

        //텔포 구현
        if (cc) cc.enabled = false;
        transform.position = target;
        if (cc) cc.enabled = true;

        // 텔포 도착 vfx
        SpawnVFX(teleportEndVFXPrefab, transform.position, Quaternion.identity);

        // 텔포 단검 Destroy
        Destroy(blinkDagger.gameObject);
        blinkDagger = null;

        // 텔포 성공하면 예약 상태 완전 종료
        nextBlink = false; 

        var holder = GetComponentInChildren<Player_WeaponHolder>();
        if (holder) holder.ForceClearBlinkQueue();
        

        cdTimer = cooldown;
    }
}
