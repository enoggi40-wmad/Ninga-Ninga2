using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.AI;

public class Player_Skill_Stealth : MonoBehaviour
{
    [Header("Input")]
    public KeyCode key = KeyCode.Q;

    [Header("Rule")]
    public int unarmedIndex = 2; // WeaponHolder에서 맨손 슬롯
    public float stealthAlpha = 0.25f;

    [Header("Refs")]
    public Player_WeaponHolder weaponHolder;

    [Header("Stealth Require")]
    public KeyCode stealthHoldKey = KeyCode.Z; // Z 누르고 있을 때만 은신 가능


    Renderer[] rends;
    int lastWeaponIndex = -999;
    bool stealthed;

    // 공개 프로퍼티 
    public bool IsStealthed => stealthed;

    [Header("VFX (Burst)")]
    public GameObject stealthOnBurstPrefab;   // 은신 ON 펑 프리팹
    public GameObject stealthOffBurstPrefab;  // 은신 OFF 펑 프리팹
    public Transform vfxAnchor;

    void Awake()
    {
        if (!weaponHolder) weaponHolder = GetComponent<Player_WeaponHolder>();
        rends = GetComponentsInChildren<Renderer>(true);

        // 시작은 무조건 정상(불투명)
        SetStealth(false);

        if (weaponHolder) lastWeaponIndex = weaponHolder.CurrentIndex;

        if (!vfxAnchor) vfxAnchor = transform;

    }

    void Update()
    {
        // weaponHolder가 있으면 현재 무기 인덱스를 가져오고, 없으면 -1로 하자
        int idx = weaponHolder ? weaponHolder.CurrentIndex : -1;

        // 무기 바뀌면 은신 자동 해제
        if (idx != lastWeaponIndex)
        {
            lastWeaponIndex = idx;
            if (stealthed && idx != unarmedIndex)
                SetStealth(false);
        }


        // 은신은 맨손상태일때만
        if (Input.GetKeyDown(key))
        {
            if (idx != unarmedIndex) return;
            //if (!Input.GetKey(stealthHoldKey)) return;
            SetStealth(!stealthed);
        }

        

    }

    void SetStealth(bool on)
    {
        bool was = stealthed;
        if (was == on) return;

        stealthed = on;

        SpawnBurstVFX(on);

        foreach (var r in rends)
        {
            if (!r) continue;
            foreach (var mat in r.materials)
            {
                if (!mat || mat.shader == null || mat.shader.name != "Standard") continue;

                if (on) SetFade(mat);
                else SetOpaque(mat);

                if (mat.HasProperty("_Color"))
                {
                    var c = mat.color;
                    c.a = on ? stealthAlpha : 1f;
                    mat.color = c;
                }
            }
        }
    }

    public void BreakStealth()
    {
        if (stealthed) SetStealth(false);
    }

    void SpawnBurstVFX(bool on)
    {
        GameObject prefab = on ? stealthOnBurstPrefab : stealthOffBurstPrefab;
        if (!prefab) return;

        Transform anchor = vfxAnchor ? vfxAnchor : transform;

        // 플레이어 위치에서 터뜨리기
        GameObject go = Instantiate(prefab, anchor.position, anchor.rotation);

        // 파티클 길이만큼 있다가 제거
        float life = 1.5f;
        var ps = go.GetComponentInChildren<ParticleSystem>();
        if (ps)
        {
            var main = ps.main;
            life = Mathf.Max(0.1f, main.duration + main.startLifetime.constantMax);
            ps.Play();
        }

        Destroy(go, life);
    }

    static void SetOpaque(Material m)
    {
        m.SetFloat("_Mode", 0f);
        m.SetInt("_SrcBlend", (int)BlendMode.One);
        m.SetInt("_DstBlend", (int)BlendMode.Zero);
        m.SetInt("_ZWrite", 1);
        m.DisableKeyword("_ALPHATEST_ON");
        m.DisableKeyword("_ALPHABLEND_ON");
        m.DisableKeyword("_ALPHAPREMULTIPLY_ON");
        m.renderQueue = -1;
    }

    static void SetFade(Material m)
    {
        m.SetFloat("_Mode", 2f); // Fade
        m.SetInt("_SrcBlend", (int)BlendMode.SrcAlpha);
        m.SetInt("_DstBlend", (int)BlendMode.OneMinusSrcAlpha);
        m.SetInt("_ZWrite", 0);
        m.DisableKeyword("_ALPHATEST_ON");
        m.EnableKeyword("_ALPHABLEND_ON");
        m.DisableKeyword("_ALPHAPREMULTIPLY_ON");
        m.renderQueue = 3000;
    }
}
