using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Acorn_DoorY_Open : MonoBehaviour
{
    public SquirrelMissionManager mission;
    public Transform doorPivot;          // 문 회전축(없으면 자기 자신)
    public float openAngle = -90f;
    public float speed = 180f;

    bool opened;
    Quaternion closedRot;
    Quaternion openRot;

    void Awake()
    {
        if (!mission) mission = FindFirstObjectByType<SquirrelMissionManager>();
        if (!doorPivot) doorPivot = transform;

        closedRot = doorPivot.rotation;
        openRot = closedRot * Quaternion.Euler(0f, openAngle, 0f);
    }

    void Update()
    {
        if (!opened)
        {
            if (mission && mission.hasAcorn) opened = true;
            else return;
        }

        doorPivot.rotation = Quaternion.RotateTowards(doorPivot.rotation, openRot, speed * Time.deltaTime);
    }
}
