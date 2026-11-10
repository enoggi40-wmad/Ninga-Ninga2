using UnityEngine;
using UnityEngine.UI;

public class Player_HpBar : MonoBehaviour
{
    public Player_Health target;   // PlayerHealth 붙은 플레이어
    public Slider slider;         // HP Slider
    //public Camera cam;            // Main Camera (비워도 됨)

    void Start()
    {
        //if (!cam) cam = Camera.main;

        if (target)
        {
            target.onHpChanged.AddListener(OnHpChanged);
            OnHpChanged(target.currentHP, target.maxHP); // 시작값 반영
        }
    }

    void OnDestroy()
    {
        if (target)
            target.onHpChanged.RemoveListener(OnHpChanged);
    }

    void OnHpChanged(int cur, int max)
    {
        if (!slider) return;

        slider.maxValue = max;
        slider.value = cur;
    }

    void LateUpdate()
    {
        // HP바가 카메라를 항상 보게(월드스페이스에서 필수급)
        //if (!cam) return;
        //transform.rotation = Quaternion.LookRotation(transform.position - cam.transform.position);
    }
}
