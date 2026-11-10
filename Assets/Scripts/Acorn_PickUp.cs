using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Acorn_PickUp : MonoBehaviour
{
    public SquirrelMissionManager mission;
    public string playerTag = "Player";

    void Awake()
    {
        if (!mission) mission = FindObjectOfType<SquirrelMissionManager>();
    }

    void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag(playerTag)) return;
        if (!mission) return;

        if (mission) mission.ObtainAcorn();
        gameObject.SetActive(false);
    }
}
