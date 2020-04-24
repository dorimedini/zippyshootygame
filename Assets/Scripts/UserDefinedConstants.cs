using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class UserDefinedConstants
{
    public static string nickname;

    public static float jumpSpeed;
    public static float lookSpeedX;
    public static float lookSpeedY;
    public static float maxChargeTime;
    public static float weaponCooldown;
    public static float projectileImpulse;
    public static float gravityMultiplier;
    public static float launchForceMultiplier;

    public static void LoadFromPlayerPrefs()
    {
        nickname = PlayerPrefs.GetString("nickname", "NOOBNOOB");
        jumpSpeed = PlayerPrefs.GetFloat("jumpSpeed", 8f);
        lookSpeedX = PlayerPrefs.GetFloat("lookSpeedX", 10f);
        lookSpeedY = PlayerPrefs.GetFloat("lookSpeedY", 5f);
        maxChargeTime = PlayerPrefs.GetFloat("maxChargeTime", 1f);
        weaponCooldown = PlayerPrefs.GetFloat("weaponCooldown", 1f);
        projectileImpulse = PlayerPrefs.GetFloat("projectileImpulse", 50f);
        gravityMultiplier = PlayerPrefs.GetFloat("gravityMultiplier", 1f);
        launchForceMultiplier = PlayerPrefs.GetFloat("launchForceMultiplier", 4f);
    }

    public static void SaveToPlayerPrefs()
    {
        PlayerPrefs.SetString("nickname", nickname);
        PlayerPrefs.SetFloat("jumpSpeed", jumpSpeed);
        PlayerPrefs.SetFloat("lookSpeedX", lookSpeedX);
        PlayerPrefs.SetFloat("lookSpeedY", lookSpeedY);
        PlayerPrefs.SetFloat("maxChargeTime", maxChargeTime);
        PlayerPrefs.SetFloat("weaponCooldown", weaponCooldown);
        PlayerPrefs.SetFloat("projectileImpulse", projectileImpulse);
        PlayerPrefs.SetFloat("gravityMultiplier", gravityMultiplier);
        PlayerPrefs.SetFloat("launchForceMultiplier", launchForceMultiplier);
    }
}
