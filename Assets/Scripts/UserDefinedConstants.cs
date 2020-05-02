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
    public static float grappleSpeed;
    public static float sphereRadius;
    public static float movementSpeed;
    public static float maxChargeTime;
    public static float explosionLift;
    public static float explosionForce;
    public static float weaponCooldown;
    public static float explosionRadius;
    public static float shotSoundVolume;
    public static float messageBoxUpTime;
    public static float projectileImpulse;
    public static float gravityMultiplier;
    public static float grappleRampupTime;
    public static float maxGrappleDistanceRatio;
    public static float minProjectileCharge;
    public static float projectileHitDamage;
    public static float launchForceMultiplier;
    public static float explosionParalysisTime;

    // Explosions override location updates from remote players. movementOverrideWindow controls for how long, and localForceDampen controls
    // which percentage of the "real" force (applied remotely, updated later) should be applied locally.
    public static float localMovementOverrideWindow;
    public static float localForceDampen;

    public static bool chargeMode;

    public static void LoadFromPlayerPrefs()
    {
        LoadAux(true);
    }

    public static void LoadDefaultValues()
    {
        LoadAux(false);
    }

    static void LoadAux(bool fromPrefs)
    {
        nickname = fromPrefs ? PlayerPrefs.GetString("nickname", "NOOBNOOB") : "NOOBNOOB";
        spawnTime = fromPrefs ? PlayerPrefs.GetFloat("spawnTime", 5f) : 5f;
        maxHealth = fromPrefs ? PlayerPrefs.GetFloat("maxHealth", 100f) : 100f;
        jumpSpeed = fromPrefs ? PlayerPrefs.GetFloat("jumpSpeed", 8f) : 8f;
        lookSpeedX = fromPrefs ? PlayerPrefs.GetFloat("lookSpeedX", 10f) : 10f;
        lookSpeedY = fromPrefs ? PlayerPrefs.GetFloat("lookSpeedY", 5f) : 5f;
        grappleSpeed = fromPrefs ? PlayerPrefs.GetFloat("grappleSpeed", 50f) : 50f;
        sphereRadius = fromPrefs ? PlayerPrefs.GetFloat("sphereRadius", 70f) : 70f;
        movementSpeed = fromPrefs ? PlayerPrefs.GetFloat("movementSpeed", 1f) : 1f;
        maxChargeTime = fromPrefs ? PlayerPrefs.GetFloat("maxChargeTime", 1f) : 1f;
        explosionLift = fromPrefs ? PlayerPrefs.GetFloat("explosionLift", 0.2f) : 0.2f;
        explosionForce = fromPrefs ? PlayerPrefs.GetFloat("explosionForce", 25f) : 25f;
        weaponCooldown = fromPrefs ? PlayerPrefs.GetFloat("weaponCooldown", 0.5f) : 0.5f;
        explosionRadius = fromPrefs ? PlayerPrefs.GetFloat("explosionRadius", 15f) : 15f;
        shotSoundVolume = fromPrefs ? PlayerPrefs.GetFloat("shotSoundVolume", 0.15f) : 0.15f;
        messageBoxUpTime = fromPrefs ? PlayerPrefs.GetFloat("messageBoxUpTime", 5f) : 5f;
        projectileImpulse = fromPrefs ? PlayerPrefs.GetFloat("projectileImpulse", 50f) : 50f;
        gravityMultiplier = fromPrefs ? PlayerPrefs.GetFloat("gravityMultiplier", 1f) : 1f;
        grappleRampupTime = fromPrefs ? PlayerPrefs.GetFloat("grappleRampupTime", 0.5f) : 0.5f;
        maxGrappleDistanceRatio = fromPrefs ? PlayerPrefs.GetFloat("maxGrappleDistanceRatio", 1f) : 1f;
        minProjectileCharge = fromPrefs ? PlayerPrefs.GetFloat("minProjectileCharge", 0.3f) : 0.3f;
        projectileHitDamage = fromPrefs ? PlayerPrefs.GetFloat("projectileHitDamage", 15f) : 15f;
        launchForceMultiplier = fromPrefs ? PlayerPrefs.GetFloat("launchForceMultiplier", 4f) : 4f;
        explosionParalysisTime = fromPrefs ? PlayerPrefs.GetFloat("explosionParalysisTime", 1f) : 1f;
        localMovementOverrideWindow = fromPrefs ? PlayerPrefs.GetFloat("localMovementOverrideWindow", 0.7f) : 0.7f;
        localForceDampen = fromPrefs ? PlayerPrefs.GetFloat("localForceDampen", 0.8f) : 0.8f;
        chargeMode = fromPrefs ? PlayerPrefs.GetInt("chargeMode", 0) == 1 : false;
    }

    public static void SaveToPlayerPrefs()
    {
        PlayerPrefs.SetString("nickname", nickname);
        PlayerPrefs.SetFloat("spawnTime", spawnTime);
        PlayerPrefs.SetFloat("maxHealth", maxHealth);
        PlayerPrefs.SetFloat("jumpSpeed", jumpSpeed);
        PlayerPrefs.SetFloat("lookSpeedX", lookSpeedX);
        PlayerPrefs.SetFloat("lookSpeedY", lookSpeedY);
        PlayerPrefs.SetFloat("grappleSpeed", grappleSpeed);
        PlayerPrefs.SetFloat("sphereRadius", sphereRadius);
        PlayerPrefs.SetFloat("movementSpeed", movementSpeed);
        PlayerPrefs.SetFloat("maxChargeTime", maxChargeTime);
        PlayerPrefs.SetFloat("explosionLift", explosionLift);
        PlayerPrefs.SetFloat("explosionForce", explosionForce);
        PlayerPrefs.SetFloat("weaponCooldown", weaponCooldown);
        PlayerPrefs.SetFloat("explosionRadius", explosionRadius);
        PlayerPrefs.SetFloat("shotSoundVolume", shotSoundVolume);
        PlayerPrefs.SetFloat("messageBoxUpTime", messageBoxUpTime);
        PlayerPrefs.SetFloat("projectileImpulse", projectileImpulse);
        PlayerPrefs.SetFloat("gravityMultiplier", gravityMultiplier);
        PlayerPrefs.SetFloat("grappleRampupTime", grappleRampupTime);
        PlayerPrefs.SetFloat("maxGrappleDistanceRatio", maxGrappleDistanceRatio);
        PlayerPrefs.SetFloat("minProjectileCharge", minProjectileCharge);
        PlayerPrefs.SetFloat("projectileHitDamage", projectileHitDamage);
        PlayerPrefs.SetFloat("launchForceMultiplier", launchForceMultiplier);
        PlayerPrefs.SetFloat("explosionParalysisTime", explosionParalysisTime);
        PlayerPrefs.SetFloat("localMovementOverrideWindow", localMovementOverrideWindow);
        PlayerPrefs.SetFloat("localForceDampen", localForceDampen);
        PlayerPrefs.SetInt("chargeMode", chargeMode ? 1 : 0);
    }
}
