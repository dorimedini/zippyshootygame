using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Photon.Pun;

[RequireComponent(typeof(NetworkCharacter))]
public class Health : MonoBehaviourPun
{
    public HealthBar bar;
    public Image redOverlay;
    public GameObject playerRagdoll;

    private float currentHealth;
    private SpawnManager spawnMngr;

    void Start()
    {
        currentHealth = UserDefinedConstants.maxHealth;
        spawnMngr = GameObject.Find("_SCRIPTS").GetComponent<SpawnManager>();
        if (spawnMngr == null)
            Debug.LogError("No spawn manager found!");
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
        // All clients need to update health, but only the player owner should initiate death sequence
        if (currentHealth <= 0 && photonView.IsMine)
            DieAndRespawn();
    }

    void DieAndRespawn()
    {
        spawnMngr.KillAndRespawn(gameObject);
    }
}
