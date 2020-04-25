using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Health : MonoBehaviour
{
    public static int maxHealth = 100;
    public HealthBar bar;

    private int currentHealth;

    void Start()
    {
        currentHealth = maxHealth;
        bar.SetMaxHealth(maxHealth);
        bar.HealToMax();
    }

    public void InflictDamage(int damage)
    {
        currentHealth -= damage;
        bar.TakeDamage(damage);
        if (currentHealth <= 0)
            Die();
    }

    void Die()
    {
        // TODO:
    }
}
