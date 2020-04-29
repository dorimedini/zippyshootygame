﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UserSettingsManager
{
    Vector2 scroll;

    public UserSettingsManager()
    {
        scroll = Vector2.zero;
    }

    public void ExposeDebugSettings()
    {
        scroll = GUILayout.BeginScrollView(scroll);
        UserDefinedConstants.spawnTime = GetSetting("Respawn time: ", UserDefinedConstants.spawnTime);
        UserDefinedConstants.maxHealth = GetSetting("Max health: ", UserDefinedConstants.maxHealth);
        UserDefinedConstants.jumpSpeed = GetSetting("Jump speed: ", UserDefinedConstants.jumpSpeed);
        UserDefinedConstants.lookSpeedX = GetSetting("Horizontal look speed: ", UserDefinedConstants.lookSpeedX);
        UserDefinedConstants.lookSpeedY = GetSetting("Vertical look speed: ", UserDefinedConstants.lookSpeedY);
        UserDefinedConstants.grappleSpeed = GetSetting("Grapple fly speed: ", UserDefinedConstants.grappleSpeed);
        UserDefinedConstants.movementSpeed = GetSetting("Movement speed multiplier: ", UserDefinedConstants.movementSpeed);
        UserDefinedConstants.maxChargeTime = GetSetting("Max weapon charge time: ", UserDefinedConstants.maxChargeTime);
        UserDefinedConstants.explosionLift = GetSetting("Explosion lift force: ", UserDefinedConstants.explosionLift);
        UserDefinedConstants.explosionForce = GetSetting("Explosion force: ", UserDefinedConstants.explosionForce);
        UserDefinedConstants.weaponCooldown = GetSetting("Weapon cooldown: ", UserDefinedConstants.weaponCooldown);
        UserDefinedConstants.explosionRadius = GetSetting("Explosion radius: ", UserDefinedConstants.explosionRadius);
        UserDefinedConstants.projectileImpulse = GetSetting("Max projectile speed: ", UserDefinedConstants.projectileImpulse);
        UserDefinedConstants.gravityMultiplier = GetSetting("Gravity multiplier: ", UserDefinedConstants.gravityMultiplier);
        UserDefinedConstants.grappleRampupTime = GetSetting("Grapple ramp-up time: ", UserDefinedConstants.grappleRampupTime);
        UserDefinedConstants.maxGrappleDistance = GetSetting("Grapple distance: ", UserDefinedConstants.maxGrappleDistance);
        UserDefinedConstants.minProjectileCharge = GetSetting("Min projectile speed: ", UserDefinedConstants.minProjectileCharge);
        UserDefinedConstants.projectileHitDamage = GetSetting("Projectile hit damage: ", UserDefinedConstants.projectileHitDamage);
        UserDefinedConstants.launchForceMultiplier = GetSetting("Pillar launch force: ", UserDefinedConstants.launchForceMultiplier);
        GUILayout.EndScrollView();
    }

    private float GetSetting(string label, float defaultVal)
    {
        GUILayout.BeginHorizontal();
        GUILayout.Label(label);
        float res = float.Parse(GUILayout.TextField(FloatToStrWithDecimal(defaultVal)));
        GUILayout.EndHorizontal();
        return res;
    }

    private string FloatToStrWithDecimal(float f)
    {
        string s = f.ToString();
        if (!s.Contains("."))
        {
            s += ".0";
        }
        return s;
    }
}
