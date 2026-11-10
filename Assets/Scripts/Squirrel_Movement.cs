using UnityEngine;
using UnityEngine.AI;

public class Squirrel_Movement : MonoBehaviour
{
    public float wanderRadius = 8f;
    public float minWait = 0.5f;
    public float maxWait = 2.0f;

    public Animator animator; // 있으면 연결
    public string speedParam = "Speed";

    NavMeshAgent agent;
    float waitTimer;
    float waitTime;

    void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        waitTime = Random.Range(minWait, maxWait);
        waitTimer = waitTime;
    }

    void Start()
    {
        SetNewDestination();
    }

    void Update()
    {
        // 애니메이션: 이동 속도 기반으로 Walk/Idle 전환
        if (animator)
            animator.SetFloat(speedParam, agent.velocity.magnitude);

        // 목적지 도착(혹은 거의 도착)
        if (!agent.pathPending && agent.remainingDistance <= agent.stoppingDistance)
        {
            waitTimer -= Time.deltaTime;

            if (waitTimer <= 0f)
            {
                waitTime = Random.Range(minWait, maxWait);
                waitTimer = waitTime;
                SetNewDestination();
            }
        }
    }

    void SetNewDestination()
    {
        Vector3 randomPoint = transform.position + Random.insideUnitSphere * wanderRadius;

        // NavMesh 위의 유효한 지점으로 스냅
        if (NavMesh.SamplePosition(randomPoint, out NavMeshHit hit, wanderRadius, NavMesh.AllAreas))
        {
            agent.SetDestination(hit.position);
        }
    }
}
