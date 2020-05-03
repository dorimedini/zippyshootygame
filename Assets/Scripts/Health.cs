using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Photon.Pun;

[RequireComponent(typeof(NetworkCharacter))]
public class Health : MonoBehaviourPun
{
    public PlayerUIController ui;
    public GameObject playerRagdoll;

    private float currentHealth;
    private SpawnManager spawnMngr;

    void Start()
    {
        currentHealth = UserDefinedConstants.maxHealth;
        spawnMngr = GameObject.Find("_SCRIPTS").GetComponent<SpawnManager>();
        if (spawnMngr == null)
            Debug.LogError("No spawn manager found!");
    }

    public void InflictDamage(float damage)
    {
        currentHealth -= damage;
        // UI element effects should only be done if the local player is the player hit
        if (photonView.IsMine)
        {
            InflictDamageUI(damage);
        }
        // All clients need to update health, but only the player owner should initiate death sequence
        if (currentHealth <= 0 && photonView.IsMine)
            DieAndRespawn();
    }

    void DieAndRespawn()
    {
        spawnMngr.KillAndRespawn(gameObject);
    }

    void InflictDamageUI(float damage)
    {
        ui.healthBar.TakeDamage(damage);
    }
}
