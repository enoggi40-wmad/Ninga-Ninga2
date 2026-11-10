using UnityEngine;

public class Item_DoubleJump : MonoBehaviour
{
    void OnTriggerEnter(Collider other)
    {
        // 플레이어 태그 방식 (추천: Player 태그 붙이기)
        if (!other.CompareTag("Player"))
            return;

        // CharacterController는 Collider를 같이 갖고 있으니,
        // same object에 PlayerMovement가 있으면 GetComponent로 잡힘
        Player_Movement pm = other.GetComponent<Player_Movement>();
        if (pm == null)
            pm = other.GetComponentInParent<Player_Movement>();

        if (pm != null)
        {
            pm.EnableDoubleJump();
            Destroy(gameObject);
        }
    }
}
