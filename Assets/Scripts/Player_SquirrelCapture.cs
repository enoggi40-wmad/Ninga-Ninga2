using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Player_SquirrelCapture : MonoBehaviour
{
    [Header("Capture")]
    public float holdTime = 1.0f;     // E 누르고 있어야 하는 시간
    public float captureRange = 2.0f; // 너무 멀면 취소
    public KeyCode key = KeyCode.R;

    [Header("Optional")]
    public Transform eye;             // 시야 기준(없으면 transform)
    public LayerMask squirrelMask;    // 다람쥐 레이어 지정하면 더 정확

    float holdTimer;
    Squirrel_CaptureTarget currentTarget;
    Squirrel_CaptureTarget lastTarget;

    [Header("Stealth")]
    SquirrelMissionManager mission;
    Player_Skill_Stealth stealth;
    bool brokeStealthThisHold;

    void Awake()
    {
        if (!eye) eye = transform;
        mission = FindFirstObjectByType<SquirrelMissionManager>();

        stealth = GetComponent<Player_Skill_Stealth>();
    }

    void Update()
    {
        currentTarget = FindBestTarget();

        // 타겟이 바뀌면 이전 타겟 게이지 끄고 진행 초기화
        if (lastTarget && lastTarget != currentTarget)
        {
            lastTarget.SetGauge(false, 0f);
            holdTimer = 0f;
            brokeStealthThisHold = false;
        }
        lastTarget = currentTarget;

        if (!currentTarget || currentTarget.captured)
        {
            holdTimer = 0f;
            brokeStealthThisHold = false;
            return;
        }

        float dist = Vector3.Distance(transform.position, currentTarget.transform.position);
        if (dist > captureRange)
        {
            currentTarget.SetGauge(false, 0f);
            holdTimer = 0f;
            brokeStealthThisHold = false;
            return;
        }

        // 범위 안이면 게이지는 보여주되,
        // 키를 안 누르면 0으로 표시
        if (!Input.GetKey(key))
        {
            currentTarget.SetGauge(true, 0f);
            holdTimer = 0f;
            brokeStealthThisHold = false;
            return;
        }

        // 은신상태였을 떄 포획시 은신이 풀림
        if (!brokeStealthThisHold)
        {
            if (stealth) stealth.BreakStealth();
            brokeStealthThisHold = true;
        }

        // 길게 누르는 중
        holdTimer += Time.deltaTime;
        float value01 = Mathf.Clamp01(holdTimer / holdTime);
        currentTarget.SetGauge(true, value01);

        if (holdTimer >= holdTime)
        {
            currentTarget.Capture();
            //if (mission) mission.OnCapturedOne();
            holdTimer = 0f;
            brokeStealthThisHold = false;
        }
    }

    Squirrel_CaptureTarget FindBestTarget()
    {
        // 아주 단순한 버전: 근처 콜라이더를 훑어서 가장 가까운 대상을 찾음
        // LayerMask 값이 있으면 hits에 들어갈건 squirrelMask 
        int mask = (squirrelMask.value == 0) ? ~0 : squirrelMask.value;
        Collider[] hits = Physics.OverlapSphere(transform.position, captureRange, mask);

        float best = float.MaxValue;
        Squirrel_CaptureTarget bestTarget = null;

        for (int i = 0; i < hits.Length; i++)
        {
            var t = hits[i].GetComponentInParent<Squirrel_CaptureTarget>();
            if (!t || t.captured) continue;

            float d = Vector3.Distance(transform.position, t.transform.position);
            if (d < best)
            {
                best = d;
                bestTarget = t;
            }
        }

        return bestTarget;
    }
}
