using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class UserDefinedConstants
{
    public static string nickname;

    public static float spawnTime;
    public static float maxHealth;
    public static float jumpSpeed;
    public static float lookSpeedX;
    public static float lookSpeedY;
    public static float movementSpeed;
    public static float maxChargeTime;
    public static float explosionLift;
    public static float explosionForce;
    public static float weaponCooldown;
    public static float explosionRadius;
    public static float projectileImpulse;
    public static float gravityMultiplier;
    public static float maxGrappleDistance;
    public static float minProjectileCharge;
    public static float launchForceMultiplier;

    public static void LoadFromPlayerPrefs()
    {
        nickname = PlayerPrefs.GetString("nickname", "NOOBNOOB");
        spawnTime = PlayerPrefs.GetFloat("spawnTime", 5f);
        maxHealth = PlayerPrefs.GetFloat("maxHealth", 100f);
        jumpSpeed = PlayerPrefs.GetFloat("jumpSpeed", 8f);
        lookSpeedX = PlayerPrefs.GetFloat("lookSpeedX", 10f);
        lookSpeedY = PlayerPrefs.GetFloat("lookSpeedY", 5f);
        movementSpeed = PlayerPrefs.GetFloat("movementSpeed", 1f);
        maxChargeTime = PlayerPrefs.GetFloat("maxChargeTime", 1f);
        explosionLift = PlayerPrefs.GetFloat("explosionLift", 1f);
        explosionForce = PlayerPrefs.GetFloat("explosionForce", 25f);
        weaponCooldown = PlayerPrefs.GetFloat("weaponCooldown", 1f);
        explosionRadius = PlayerPrefs.GetFloat("explosionRadius", 15f);
        projectileImpulse = PlayerPrefs.GetFloat("projectileImpulse", 50f);
        gravityMultiplier = PlayerPrefs.GetFloat("gravityMultiplier", 1f);
        maxGrappleDistance = PlayerPrefs.GetFloat("maxGrappleDistance", 50f);
        minProjectileCharge = PlayerPrefs.GetFloat("minProjectileCharge", 0.3f);
        launchForceMultiplier = PlayerPrefs.GetFloat("launchForceMultiplier", 4f);
    }

    public static void SaveToPlayerPrefs()
    {
        PlayerPrefs.SetString("nickname", nickname);
        PlayerPrefs.SetFloat("spawnTime", spawnTime);
        PlayerPrefs.SetFloat("maxHealth", maxHealth);
        PlayerPrefs.SetFloat("jumpSpeed", jumpSpeed);
        PlayerPrefs.SetFloat("lookSpeedX", lookSpeedX);
        PlayerPrefs.SetFloat("lookSpeedY", lookSpeedY);
        PlayerPrefs.SetFloat("movementSpeed", movementSpeed);
        PlayerPrefs.SetFloat("maxChargeTime", maxChargeTime);
        PlayerPrefs.SetFloat("explosionLift", explosionLift);
        PlayerPrefs.SetFloat("explosionForce", explosionForce);
        PlayerPrefs.SetFloat("weaponCooldown", weaponCooldown);
        PlayerPrefs.SetFloat("explosionRadius", explosionRadius);
        PlayerPrefs.SetFloat("projectileImpulse", projectileImpulse);
        PlayerPrefs.SetFloat("gravityMultiplier", gravityMultiplier);
        PlayerPrefs.SetFloat("maxGrappleDistance", maxGrappleDistance);
        PlayerPrefs.SetFloat("minProjectileCharge", minProjectileCharge);
        PlayerPrefs.SetFloat("launchForceMultiplier", launchForceMultiplier);
    }
}
