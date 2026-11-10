using UnityEngine;

public enum WeaponType
{
    ThrowProjectile,
    Melee
}

[CreateAssetMenu(menuName = "Game/Weapon Data", fileName = "WD_")]
public class WeaponData : ScriptableObject
{
    [Header("Info")]
    public string weaponName = "Weapon";
    public WeaponType type = WeaponType.ThrowProjectile;

    [Header("Model (Decoration)")]
    public GameObject weaponModelPrefab; // 손에 들고 있는 장식용 모델

    [Header("Combat")]
    public int damage = 10;
    public float cooldown = 0.2f;

    [Header("Animation")]
    public string animTrigger = "Throw";

    [Header("Throw (Projectile)")]
    public GameObject projectilePrefab;  // 실제 날아가는 Blade Projectile 프리팹
    public float aimMaxDistance = 200f;
    public LayerMask aimMask = ~0;

    [Header("Melee")]
    public float meleeRange = 1.0f;
    public float meleeRadius = 0.6f;
    public LayerMask meleeMask = ~0;
}
