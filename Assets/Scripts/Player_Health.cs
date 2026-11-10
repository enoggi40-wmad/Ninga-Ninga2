using UnityEngine;
using UnityEngine.Events;

public class Player_Health : MonoBehaviour
{
    public int maxHP = 100;
    public int currentHP;
    Animator animator;

    public UnityEvent<int, int> onHpChanged; // (current, max)

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.H)) TakeDamage(10);
        //if (Input.GetKeyDown(KeyCode.J)) Heal(10);
    }

    void Awake()
    {
        animator = GetComponentInChildren<Animator>();
        currentHP = maxHP;
        onHpChanged?.Invoke(currentHP, maxHP);
    }

    public void TakeDamage(int dmg)
    {
        currentHP = Mathf.Clamp(currentHP - dmg, 0, maxHP);
        onHpChanged?.Invoke(currentHP, maxHP);
       
        Debug.Log(currentHP);

        if (animator) animator.SetTrigger("GetHit");

        // 맞으면 은신 깨기
        var stealth = GetComponentInParent<Player_Skill_Stealth>();
        if (stealth) stealth.BreakStealth();

        if (currentHP <= 0)
        {
            Die();
        }
    }

    public void Heal(int amount)
    {
        currentHP = Mathf.Clamp(currentHP + amount, 0, maxHP);
        onHpChanged?.Invoke(currentHP, maxHP);
    }

    void Die()
    {
        Debug.Log("Player Dead");
        // 여기서 리스폰/게임오버 처리 연결
    }
}