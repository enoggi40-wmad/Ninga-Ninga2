using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class EnemySpawner : MonoBehaviour
{
    [Header("Enemy Prefabs (Random)")]
    public GameObject[] enemyPrefabs;

    [Header("Limit")]
    public int maxAlive = 10;

    [Header("Radius Spawn (center = this)")]
    public float radius = 10f;
    public float minRadius = 0f;
    public bool randomYRotation = true;

    [Header("NavMesh")]
    public float navMeshSampleDistance = 2f;
    public int tryCount = 12;

    [Header("Respawn")]
    public float respawnDelay = 10f;

    [Header("Pool")]
    public int prewarmPerPrefab = 3; // 시작할 때 미리 만들어둘 개수(프리팹마다)

    [Header("Start Spawn")]
    public int initialSpawn = 10;   // 시작하자마자 몇 마리 뽑을지

    // (Dictionary)풀: 프리팹 인덱스별로 Queue로 관리
    readonly Dictionary<int, Queue<GameObject>> pool = new Dictionary<int, Queue<GameObject>>();

    // (HashSet) 살아있는(활성) 적만 추적
    readonly HashSet<GameObject> alive = new HashSet<GameObject>();

    // 죽어서 리스폰 기다리는 “예약 수”
    int reservedRespawns = 0;

    // 초기 적 10체 씬에 추가
    void Start()
    {
        int n = Mathf.Clamp(initialSpawn, 0, maxAlive);
        for (int i = 0; i < n; i++)
            SpawnOneRandom();
    }

    void Awake()
    {
        // 풀 초기화 + 프리워밍
        if (enemyPrefabs == null) enemyPrefabs = new GameObject[0];

        for (int i = 0; i < enemyPrefabs.Length; i++)
        {
            pool[i] = new Queue<GameObject>();

            for (int k = 0; k < prewarmPerPrefab; k++)
            {
                var obj = CreateNew(i);
                // 비활성으로 풀에 넣기
                ReturnToPool(obj); 
            }
        }
    }

    // Enemy_Health가 “죽는 순간” 호출
    public void NotifyEnemyDied(GameObject enemy)
    {
        // 죽는 순간부터 10초 카운트 시작
        if (alive.Contains(enemy))
            alive.Remove(enemy);

        reservedRespawns++;
        StartCoroutine(CoRespawnAfterDelay(respawnDelay));
    }

    IEnumerator CoRespawnAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);

        // 이미 꽉 찼으면 예약만 해제(스폰 안 함)
        if (alive.Count >= maxAlive)
        {
            reservedRespawns = Mathf.Max(0, reservedRespawns - 1);
            yield break;
        }

        reservedRespawns = Mathf.Max(0, reservedRespawns - 1);
        SpawnOneRandom();
    }

    public void ReturnToPool(GameObject enemy)
    {
        if (enemy == null) return;

        // 풀 토큰 확인
        var token = enemy.GetComponent<EnemyPoolToken>();
        if (token == null)
        {
            // 토큰이 없으면 그냥 꺼버리기(안전장치)
            enemy.SetActive(false);
            return;
        }

        // 활성 리스트에서 제거
        alive.Remove(enemy);

        // 상태 리셋(필요한 컴포넌트만 가볍게)
        enemy.SetActive(false);

        // 풀에 반환
        if (!pool.ContainsKey(token.prefabIndex))
            pool[token.prefabIndex] = new Queue<GameObject>();

        pool[token.prefabIndex].Enqueue(enemy);
    }

    void SpawnOneRandom()
    {
        if (enemyPrefabs == null || enemyPrefabs.Length == 0) return;
        if (alive.Count + reservedRespawns >= maxAlive) return;

        int prefabIndex = Random.Range(0, enemyPrefabs.Length);

        if (!TryGetRandomPointOnNavMesh(transform.position, radius, minRadius, out Vector3 pos))
            return;

        Quaternion rot = randomYRotation ?
            Quaternion.Euler(0f, Random.Range(0f, 360f), 0f)  : Quaternion.identity;


        // 적 가져오기 (오브젝트 풀링 핵심)
        GameObject enemy = GetFromPool(prefabIndex);
        enemy.transform.SetPositionAndRotation(pos, rot);
        enemy.SetActive(true);

        // NavMeshAgent 안정화
        var agent = enemy.GetComponent<NavMeshAgent>();
        if (agent != null) agent.Warp(pos);

        // Enemy_Health가 스포너를 알도록 연결 + HP 리셋
        var hp = enemy.GetComponent<Enemy_Health>();
        if (hp != null) hp.OnSpawned(this);

        alive.Add(enemy);
    }

    GameObject GetFromPool(int prefabIndex)
    {
        if (!pool.ContainsKey(prefabIndex))
            pool[prefabIndex] = new Queue<GameObject>();

        if (pool[prefabIndex].Count > 0)
            return pool[prefabIndex].Dequeue();

        // 풀에 없으면 새로 생성
        return CreateNew(prefabIndex);
    }

    GameObject CreateNew(int prefabIndex)
    {
        var prefab = enemyPrefabs[prefabIndex];
        var obj = Instantiate(prefab, transform.position, Quaternion.identity);

        // 어떤 프리팹에서 왔는지 토큰 저장
        var token = obj.GetComponent<EnemyPoolToken>();
        if (token == null) token = obj.AddComponent<EnemyPoolToken>();
        token.prefabIndex = prefabIndex;

        // Enemy_Health가 필요 기능을 갖추도록(없으면 경고 느낌으로라도)
        return obj;
    }

    bool TryGetRandomPointOnNavMesh(Vector3 center, float maxR, float minR, out Vector3 result)
    {
        for (int i = 0; i < tryCount; i++)
        {
            Vector2 v = Random.insideUnitCircle.normalized * Random.Range(minR, maxR);
            Vector3 candidate = center + new Vector3(v.x, 0f, v.y);

            if (NavMesh.SamplePosition(candidate, out NavMeshHit hit, navMeshSampleDistance, NavMesh.AllAreas))
            {
                result = hit.position;
                return true;
            }
        }

        result = default;
        return false;
    }
}

// 풀에 들어갈 때 “내가 어떤 프리팹 인덱스인지” 기억하는 꼬리표
public class EnemyPoolToken : MonoBehaviour
{
    public int prefabIndex;
}
