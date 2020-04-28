using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class HealthBar : MonoBehaviour
{
    public Image greenBar;
    public Image reductionBar;

    private float maxHealth;
    private float currentHealth, prevCurrentHealth;
    private float waitToReduce, initialWaitToReduce;

    void Start()
    {
        currentHealth = prevCurrentHealth = maxHealth;
        waitToReduce = 0;
        initialWaitToReduce = 0.75f;
    }

    void Update()
    {
        // When a player takes damage, immediately display the chunk of health lost with the reduction bar.
        // Then, after a delay, start shrinking the reduction bar.
        if (Tools.NearlyEqual(prevCurrentHealth, currentHealth, 0.01f))
        {
            prevCurrentHealth = currentHealth;
            reductionBar.fillAmount = greenBar.fillAmount;
        }
        else if (waitToReduce > 0.01f)
        {
            waitToReduce -= Time.deltaTime;
        }
        else
        {
            waitToReduce = 0;
            reductionBar.fillAmount = Mathf.Max(currentHealth / maxHealth, reductionBar.fillAmount - Time.deltaTime);
        }
    }

    public void SetMaxHealth(float mh) { maxHealth = mh; }

    public void HealToMax()
    {
        currentHealth = prevCurrentHealth = maxHealth;
        greenBar.fillAmount = reductionBar.fillAmount = 1;
    }

    // Update is called once per frame
    public void TakeDamage(float damage)
    {
        prevCurrentHealth = currentHealth;
        currentHealth = Mathf.Max(currentHealth - damage, 0);
        greenBar.fillAmount = currentHealth / maxHealth;
        reductionBar.fillAmount = Mathf.Min(reductionBar.fillAmount, prevCurrentHealth / maxHealth);
        waitToReduce = initialWaitToReduce;
    }
}
