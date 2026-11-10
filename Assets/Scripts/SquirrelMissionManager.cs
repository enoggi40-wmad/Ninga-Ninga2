using System.Collections.Generic;
using UnityEngine;

public class SquirrelMissionManager : MonoBehaviour
{
    [Header("Mission")]
    public int goal = 5;
    public int capturedCount { get; private set; }

    [Header("Refs")]
    public Transform player; // 플레이어 트랜스폼 연결

    [Header("Acorn Spawn")]
    public GameObject acornObject;      // 씬에 미리 배치된 도토리(처음엔 꺼져있음)
    public Transform acornTarget;       // 안내 타겟(보통 도토리 Transform)

    public bool acornRevealed { get; private set; }
    public bool hasAcorn { get; private set; }

    // 포획된 다람쥐의 리스트
    readonly List<Squirrel_CaptureTarget> captured = new();

    public GameObject exitTriggerObject;

    void Awake()
    {
        // 안전장치: 인스펙터에서 안 넣었으면 acornObject로부터 잡아오기
        if (!acornTarget && acornObject) acornTarget = acornObject.transform;

        // 시작은 숨김
        if (acornObject) acornObject.SetActive(false);
    }

    // 잡힌 다람쥐 등록
    public void RegisterCaptured(Squirrel_CaptureTarget s)
    {
        if (!s) return;
        if (captured.Contains(s)) return;
        captured.Add(s);
    }

    public void OnCapturedOne()
    {
        if (hasAcorn) return;

        capturedCount++;
        Debug.Log($"Squirrels: {capturedCount}/{goal}");

        if (!acornRevealed && capturedCount >= goal)
        {
            RevealAcorn();
            StartGuidingToAcorn();
        }
    }

    void RevealAcorn()
    {
        if (!acornObject || !acornTarget)
        {
            Debug.LogWarning("acornObject/acornTarget이 비어있음! (씬에 숨겨둔 도토리를 연결해줘)");
            return;
        }

        acornObject.SetActive(true);
        acornRevealed = true;

        Debug.Log("도토리가 수풀에서 나타났다!");
    }

    void StartGuidingToAcorn()
    {
        if (!acornTarget) return;

        captured.RemoveAll(s => !s || !s.gameObject.activeInHierarchy);

        foreach (var s in captured)
            s.SetGuideTarget(acornTarget);
    }

    public void ObtainAcorn()
    {
        if (hasAcorn) return;
        hasAcorn = true;

        // 탈출트리거 활성화
        if (exitTriggerObject) exitTriggerObject.SetActive(true);

        // 도토리 먹으면 안내 종료(원하면 계속 안내해도 됨)
        foreach (var s in captured)
        {
            if (!s) continue;
            s.SetGuideTarget(player);
            Debug.Log($"[ObtainAcorn] set {s.name} guideTarget -> {(player ? player.name : "NULL")}");
        }

        Debug.Log("도토리 획득! 이제 탈출 지점으로 가면 클리어 ");
    }
}
