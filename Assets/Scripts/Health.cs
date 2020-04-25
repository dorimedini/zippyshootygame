using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class Health : MonoBehaviour
{
    public HealthBar bar;
    public Image redOverlay;

    private float currentHealth;

    void Start()
    {
        currentHealth = UserDefinedConstants.maxHealth;
        bar.SetMaxHealth(UserDefinedConstants.maxHealth);
        bar.HealToMax();
    }

    void Update()
    {
        Color c = redOverlay.color;
        if (redOverlay.color.a > 0.01f)
        {
            redOverlay.color = new Color(c.r, c.g, c.b, Mathf.Max(0, c.a - Time.deltaTime));
        }
        else
        {
            redOverlay.color = new Color(c.r, c.g, c.b, 0);
        }
    }

    public void InflictDamage(int damage)
    {
        currentHealth -= damage;
        bar.TakeDamage(damage);
        redOverlay.color = new Color(redOverlay.color.r, redOverlay.color.g, redOverlay.color.b, 0.5f);
        if (currentHealth <= 0)
            Die();
    }

    void Die()
    {
        // TODO:
    }
}
