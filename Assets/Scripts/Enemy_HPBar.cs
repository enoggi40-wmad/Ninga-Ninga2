using UnityEngine;
using UnityEngine.UI;

public class Enemy_HPBar : MonoBehaviour
{
    public Enemy_Health target;   // Enemy_Health 붙은 적
    public Slider slider;         // HP Slider
    

    void Start()
    {
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

        slider.minValue = 0;
        slider.maxValue = max;
        slider.value = cur;
    }
}
