using System.Collections;
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

        bool reset = GUILayout.Button("Reset to defaults");
        if (reset)
        {
            UserDefinedConstants.LoadDefaultValues();
            return;
        }

        GUILayout.BeginHorizontal();
        UserDefinedConstants.chargeMode = GUILayout.Toggle(UserDefinedConstants.chargeMode, " Weapon charging");
        GUILayout.EndHorizontal();
        if (UserDefinedConstants.chargeMode)
        {
            UserDefinedConstants.minProjectileCharge = GetSetting("Min projectile speed: ", UserDefinedConstants.minProjectileCharge);
            UserDefinedConstants.maxChargeTime = GetSetting("Max weapon charge time: ", UserDefinedConstants.maxChargeTime);
        }

        UserDefinedConstants.spawnTime = GetSetting("Respawn time: ", UserDefinedConstants.spawnTime);
        UserDefinedConstants.maxHealth = GetSetting("Max health: ", UserDefinedConstants.maxHealth);
        UserDefinedConstants.jumpSpeed = GetSetting("Jump speed: ", UserDefinedConstants.jumpSpeed);
        UserDefinedConstants.lookSpeedX = GetSetting("Horizontal look speed: ", UserDefinedConstants.lookSpeedX);
        UserDefinedConstants.lookSpeedY = GetSetting("Vertical look speed: ", UserDefinedConstants.lookSpeedY);
        UserDefinedConstants.grappleSpeed = GetSetting("Grapple fly speed: ", UserDefinedConstants.grappleSpeed);
        UserDefinedConstants.sphereRadius = GetSetting("Sphere radius: ", UserDefinedConstants.sphereRadius);
        UserDefinedConstants.movementSpeed = GetSetting("Movement speed multiplier: ", UserDefinedConstants.movementSpeed);
        UserDefinedConstants.explosionLift = GetSetting("Explosion lift force: ", UserDefinedConstants.explosionLift);
        UserDefinedConstants.explosionForce = GetSetting("Explosion force: ", UserDefinedConstants.explosionForce);
        UserDefinedConstants.weaponCooldown = GetSetting("Weapon cooldown: ", UserDefinedConstants.weaponCooldown);
        UserDefinedConstants.explosionRadius = GetSetting("Explosion radius: ", UserDefinedConstants.explosionRadius);
        UserDefinedConstants.messageBoxUpTime = GetSetting("Messagebox uptime: ", UserDefinedConstants.messageBoxUpTime);
        UserDefinedConstants.projectileImpulse = GetSetting("Max projectile speed: ", UserDefinedConstants.projectileImpulse);
        UserDefinedConstants.gravityMultiplier = GetSetting("Gravity multiplier: ", UserDefinedConstants.gravityMultiplier);
        UserDefinedConstants.grappleRampupTime = GetSetting("Grapple ramp-up time: ", UserDefinedConstants.grappleRampupTime);
        UserDefinedConstants.maxGrappleDistanceRatio = GetSetting("Grapple distance / radius (ratio): ", UserDefinedConstants.maxGrappleDistanceRatio);
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
