using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class UserSettingsManager
{
    public static void ExposeDebugSettings()
    {
        UserDefinedConstants.jumpSpeed = GetSetting("Jump speed: ", UserDefinedConstants.jumpSpeed);
        UserDefinedConstants.lookSpeedX = GetSetting("Horizontal look speed: ", UserDefinedConstants.lookSpeedX);
        UserDefinedConstants.lookSpeedY = GetSetting("Vertical look speed: ", UserDefinedConstants.lookSpeedY);
        UserDefinedConstants.maxChargeTime = GetSetting("Max weapon charge time: ", UserDefinedConstants.maxChargeTime);
        UserDefinedConstants.weaponCooldown = GetSetting("Weapon cooldown: ", UserDefinedConstants.weaponCooldown);
        UserDefinedConstants.projectileImpulse = GetSetting("Max projectile speed: ", UserDefinedConstants.projectileImpulse);
        UserDefinedConstants.gravityMultiplier = GetSetting("Gravity multiplier: ", UserDefinedConstants.gravityMultiplier);
        UserDefinedConstants.launchForceMultiplier = GetSetting("Pillar launch force: ", UserDefinedConstants.launchForceMultiplier);
    }

    private static float GetSetting(string label, float defaultVal)
    {
        GUILayout.BeginHorizontal();
        GUILayout.Label(label);
        float res = float.Parse(GUILayout.TextField(FloatToStrWithDecimal(defaultVal)));
        GUILayout.EndHorizontal();
        return res;
    }

    private static string FloatToStrWithDecimal(float f)
    {
        string s = f.ToString();
        if (!s.Contains("."))
        {
            s += ".0";
        }
        return s;
    }
}
