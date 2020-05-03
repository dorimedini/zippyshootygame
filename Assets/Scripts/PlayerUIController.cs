using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerUIController : MonoBehaviour
{
    public HealthBar healthBar;

    // Start is called before the first frame update
    void Start()
    {
        healthBar.SetMaxHealth(UserDefinedConstants.maxHealth);
        healthBar.HealToMax();
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
