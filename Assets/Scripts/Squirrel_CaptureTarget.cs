using UnityEngine;
using UnityEngine.UI;
using UnityEngine.AI;

public class Squirrel_CaptureTarget : MonoBehaviour
{
    [Header("World Gauge (Slider)")]
    public GameObject gaugeRoot;     // 게이지 캔버스(또는 루트)
    public Slider gaugeSlider;       // 복붙한 Slider

    [Header("Mission")]
    public SquirrelMissionManager mission;

    [Header("Follow")]
    public float followDistance = 1.8f;
    public float repathInterval = 0.25f;

    [Header("Guide (Look At Acorn)")]
    public Transform lookPivot;      // 머리/몸통 기준(없으면 자기 transform)
    public float guideLookLerp = 8f; // 고개 돌리는 속도

    public bool captured { get; private set; }

    NavMeshAgent agent;
    Transform followTarget;
    Transform guideTarget;

    float repathTimer;

    void Awake()
    {
        agent = GetComponent<NavMeshAgent>();

        SetGauge(false, 0f);
        if (gaugeSlider)
        {
            gaugeSlider.minValue = 0f;
            gaugeSlider.maxValue = 1f;
        }

        if (!lookPivot) lookPivot = transform;
        if (!mission) mission = FindObjectOfType<SquirrelMissionManager>();
    }

    void Update()
    {
        // 포획 후 따라오기
        if (captured && followTarget && agent)
            TickFollow();

        // 도토리 스폰 후: 도토리 방향을 "알려주기"(고개/몸 돌리기)
        if (guideTarget && lookPivot)
            TickGuideLook();
    }

    void TickFollow()
    {
        repathTimer -= Time.deltaTime;
        if (repathTimer > 0f) return;
        repathTimer = repathInterval;

        float dist = Vector3.Distance(transform.position, followTarget.position);

        if (dist > followDistance)
        {
            agent.stoppingDistance = followDistance;
            agent.isStopped = false;
            agent.SetDestination(followTarget.position); // MVP: 뭉쳐도 OK
        }
        else
        {
            agent.isStopped = true; // 너무 가까우면 멈춰서 떨림 방지
        }
    }

    void TickGuideLook()
    {
        Vector3 dir = guideTarget.position - lookPivot.position;
        dir.y = 0f;
        if (dir.sqrMagnitude < 0.0001f) return;

        Quaternion targetRot = Quaternion.LookRotation(dir.normalized);
        lookPivot.rotation = Quaternion.Slerp(lookPivot.rotation, targetRot, Time.deltaTime * guideLookLerp);
    }

    public void SetGauge(bool on, float value01)
    {
        if (gaugeRoot) gaugeRoot.SetActive(on);
        if (gaugeSlider) gaugeSlider.value = Mathf.Clamp01(value01);
    }

    // 플레이어가 다람쥐 포획
    public void Capture()
    {
        if (captured) return;
        captured = true;

        SetGauge(false, 0f);

        // 야생 이동/AI만 끄기 (NavMeshAgent는 끄면 안 됨!)
        var move = GetComponent<Squirrel_Movement>();
        if (move) move.enabled = false;

        if (!agent)
        {
            Debug.LogWarning($"{name}: NavMeshAgent가 없음");
            return;
        }
        if (!agent.enabled) agent.enabled = true;

        if (!mission) mission = FindObjectOfType<SquirrelMissionManager>();

        if (mission)
        {
            mission.RegisterCaptured(this);
            mission.OnCapturedOne();
            StartFollow(mission.player);
        }
    }

    void StartFollow(Transform player)
    {
        if (!player) return;
        followTarget = player;
        repathTimer = 0f;
    }

    // 미션 매니저가 도토리 스폰 후 호출
    public void SetGuideTarget(Transform t)
    {
        guideTarget = t;
    }

    public void ClearGuideTarget()
    {
        guideTarget = null;
    }

}
