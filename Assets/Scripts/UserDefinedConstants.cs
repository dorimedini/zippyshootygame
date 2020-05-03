using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class UserDefinedConstants
{
    private class Entry<T>
    {
        public T _val;
        public T _default_val;
        public string _name;
        public bool _midgame_ok;
        public Entry(T dv, string n) : this(dv, n, true) { }
        public Entry(T dv, string n, bool mgok) { _midgame_ok = mgok; _val = _default_val = dv; _name = n; }
        public static implicit operator T(Entry<T> e) => e._val;
    }
    private class FloatEntry : Entry<float>
    {
        public float _min, _max;
        public FloatEntry(float dv, string n, float min, float max) : this(dv, n, true, min, max) { }
        public FloatEntry(float dv, string n, bool mgok, float min, float max) : base(dv, n, mgok) { _min = min; _max = max; }
    }

    private static Dictionary<string, FloatEntry> floatEntries = new Dictionary<string, FloatEntry>
    {
        {"sunDamage", new FloatEntry(50, "sunDamage", 0, 1000f)},
        {"spawnTime", new FloatEntry(5, "spawnTime", 0.05f, 5*60)},
        {"maxHealth", new FloatEntry(100, "maxHealth", 1, 500)},
        {"jumpSpeed", new FloatEntry(8, "jumpSpeed", 0.05f, 50)},
        {"lookSpeedX", new FloatEntry(10, "lookSpeedX", 1, 50)},
        {"lookSpeedY", new FloatEntry(5, "lookSpeedY", 1, 50)},
        {"grappleSpeed", new FloatEntry(50, "grappleSpeed", 1, 200)},
        {"sphereRadius", new FloatEntry(70, "sphereRadius", false, 20, 500)},
        {"movementSpeed", new FloatEntry(1, "movementSpeed", 0.05f, 15)},
        {"maxChargeTime", new FloatEntry(1, "maxChargeTime", 0.05f, 10)},
        {"explosionLift", new FloatEntry(0.2f, "explosionLift", 0, 10)},
        {"explosionForce", new FloatEntry(25, "explosionForce", 0, 200)},
        {"weaponCooldown", new FloatEntry(0.5f, "weaponCooldown", 0.01f, 5)},
        {"explosionRadius", new FloatEntry(15, "explosionRadius", 0, 50)},
        {"shotSoundVolume", new FloatEntry(0.15f, "shotSoundVolume", 0, 1)},
        {"messageBoxUpTime", new FloatEntry(5, "messageBoxUpTime", 0.5f, 10)},
        {"projectileImpulse", new FloatEntry(50, "projectileImpulse", 1, 200)},
        {"gravityMultiplier", new FloatEntry(1, "gravityMultiplier", 0.05f, 10)},
        {"grappleRampupTime", new FloatEntry(0.5f, "grappleRampupTime", 0.01f, 2)},
        {"maxGrappleDistanceRatio", new FloatEntry(1, "maxGrappleDistanceRatio", 0.01f, 3)},
        {"minProjectileCharge", new FloatEntry(0.3f, "minProjectileCharge", 0, 1)},
        {"projectileHitDamage", new FloatEntry(15, "projectileHitDamage", 0, 200)},
        {"launchForceMultiplier", new FloatEntry(4, "launchForceMultiplier", 1, 10)},
        {"explosionParalysisTime", new FloatEntry(1, "explosionParalysisTime", 0, 3)},
        {"localMovementOverrideWindow", new FloatEntry(0.7f, "localMovementOverrideWindow", 0.01f, 2)},
        {"localForceDampen", new FloatEntry(0.8f, "localForceDampen", 0, 1)},
    };
    private static Dictionary<string, Entry<string>> stringEntries = new Dictionary<string, Entry<string>>
    {
        {"nickname", new Entry<string>("NOOBNOOB", "nickname")}
    };
    private static Dictionary<string, Entry<bool>> boolEntries = new Dictionary<string, Entry<bool>>
    {
        {"chargeMode", new Entry<bool>(false, "chargeMode")}
    };

    public static string nickname { get { return stringEntries["nickname"]; } set { stringEntries["nickname"]._val = value; } }

    public static float sunDamage { get { return floatEntries["sunDamage"]; } set { floatEntries["sunDamage"]._val = value; } }
    public static float spawnTime { get { return floatEntries["spawnTime"]; } set { floatEntries["spawnTime"]._val = value; } }
    public static float maxHealth { get { return floatEntries["maxHealth"]; } set { floatEntries["maxHealth"]._val = value; } }
    public static float jumpSpeed { get { return floatEntries["jumpSpeed"]; } set { floatEntries["jumpSpeed"]._val = value; } }
    public static float lookSpeedX { get { return floatEntries["lookSpeedX"]; } set { floatEntries["lookSpeedX"]._val = value; } }
    public static float lookSpeedY { get { return floatEntries["lookSpeedY"]; } set { floatEntries["lookSpeedY"]._val = value; } }
    public static float grappleSpeed { get { return floatEntries["grappleSpeed"]; } set { floatEntries["grappleSpeed"]._val = value; } }
    public static float sphereRadius { get { return floatEntries["sphereRadius"]; } set { floatEntries["sphereRadius"]._val = value; } }
    public static float movementSpeed { get { return floatEntries["movementSpeed"]; } set { floatEntries["movementSpeed"]._val = value; } }
    public static float maxChargeTime { get { return floatEntries["maxChargeTime"]; } set { floatEntries["maxChargeTime"]._val = value; } }
    public static float explosionLift { get { return floatEntries["explosionLift"]; } set { floatEntries["explosionLift"]._val = value; } }
    public static float explosionForce { get { return floatEntries["explosionForce"]; } set { floatEntries["explosionForce"]._val = value; } }
    public static float weaponCooldown { get { return floatEntries["weaponCooldown"]; } set { floatEntries["weaponCooldown"]._val = value; } }
    public static float explosionRadius { get { return floatEntries["explosionRadius"]; } set { floatEntries["explosionRadius"]._val = value; } }
    public static float shotSoundVolume { get { return floatEntries["shotSoundVolume"]; } set { floatEntries["shotSoundVolume"]._val = value; } }
    public static float messageBoxUpTime { get { return floatEntries["messageBoxUpTime"]; } set { floatEntries["messageBoxUpTime"]._val = value; } }
    public static float projectileImpulse { get { return floatEntries["projectileImpulse"]; } set { floatEntries["projectileImpulse"]._val = value; } }
    public static float gravityMultiplier { get { return floatEntries["gravityMultiplier"]; } set { floatEntries["gravityMultiplier"]._val = value; } }
    public static float grappleRampupTime { get { return floatEntries["grappleRampupTime"]; } set { floatEntries["grappleRampupTime"]._val = value; } }
    public static float maxGrappleDistanceRatio { get { return floatEntries["maxGrappleDistanceRatio"]; } set { floatEntries["maxGrappleDistanceRatio"]._val = value; } }
    public static float minProjectileCharge { get { return floatEntries["minProjectileCharge"]; } set { floatEntries["minProjectileCharge"]._val = value; } }
    public static float projectileHitDamage { get { return floatEntries["projectileHitDamage"]; } set { floatEntries["projectileHitDamage"]._val = value; } }
    public static float launchForceMultiplier { get { return floatEntries["launchForceMultiplier"]; } set { floatEntries["launchForceMultiplier"]._val = value; } }
    public static float explosionParalysisTime { get { return floatEntries["explosionParalysisTime"]; } set { floatEntries["explosionParalysisTime"]._val = value; } }

    // Explosions override location updates from remote players. movementOverrideWindow controls for how long, and localForceDampen controls
    // which percentage of the "real" force (applied remotely, updated later) should be applied locally.
    public static float localMovementOverrideWindow { get { return floatEntries["localMovementOverrideWindow"]; } set { floatEntries["localMovementOverrideWindow"]._val = value; } }
    public static float localForceDampen { get { return floatEntries["localForceDampen"]; } set { floatEntries["localForceDampen"]._val = value; } }

    public static bool chargeMode { get { return boolEntries["chargeMode"]; } set { boolEntries["chargeMode"]._val = value; } }

    public static void LoadFromPlayerPrefs(bool midGame)
    {
        LoadAux(true, midGame);
    }

    public static void LoadDefaultValues(bool midGame)
    {
        LoadAux(false, midGame);
    }

    static void LoadAux(bool fromPrefs, bool midGame)
    {
        foreach (var floatEntry in floatEntries.Values)
        {
            if (!floatEntry._midgame_ok && midGame)
                continue;
            string name = floatEntry._name;
            float def = floatEntry._default_val;
            floatEntries[name]._val = Mathf.Clamp(fromPrefs ? PlayerPrefs.GetFloat(name, def) : def, floatEntry._min, floatEntry._max);
        }
        foreach (var stringEntry in stringEntries.Values)
        {
            if (!stringEntry._midgame_ok && midGame)
                continue;
            string name = stringEntry._name;
            string def = stringEntry._default_val;
            stringEntries[name]._val = fromPrefs ? PlayerPrefs.GetString(name, def) : def;
        }
        foreach (var boolEntry in boolEntries.Values)
        {
            if (!boolEntry._midgame_ok && midGame)
                continue;
            string name = boolEntry._name;
            bool def = boolEntry._default_val;
            boolEntries[name]._val = fromPrefs ? (PlayerPrefs.GetInt(name, def ? 1 : 0) == 1) : def;
        }
    }

    public static void SaveToPlayerPrefs()
    {
        foreach (var floatEntry in floatEntries.Values)
            PlayerPrefs.SetFloat(floatEntry._name, floatEntry);
        foreach (var stringEntry in stringEntries.Values)
            PlayerPrefs.SetString(stringEntry._name, stringEntry);
        foreach (var boolEntry in boolEntries.Values)
            PlayerPrefs.SetInt(boolEntry._name, boolEntry ? 1 : 0);
    }
}
