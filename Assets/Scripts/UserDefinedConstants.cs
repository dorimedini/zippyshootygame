using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class UserDefinedConstants
{
    public class Entry<T>
    {
        protected T __val;
        public T _val { get { return __val; } set { setval(value); } }
        public T _default_val;
        public string _name, _label;
        public bool _midgame_ok;
        public Entry(T dv, string n, string l) : this(dv, n, l, true) { }
        public Entry(T dv, string n, string l, bool mgok) { _midgame_ok = mgok; __val = _val = _default_val = dv; _name = n; _label = l; }
        virtual public void setval(T val) { __val = val; }
        public static implicit operator T(Entry<T> e) => e._val;
    }
    public class RangeEntry<T> : Entry<T> where T : IComparable
    {
        public T _min, _max;
        public RangeEntry(T dv, string n, string l, T min, T max) : this(dv, n, l, true, min, max) { }
        public RangeEntry(T dv, string n, string l, bool mgok, T min, T max) : base(dv, n, l, mgok) { _min = min; _max = max; }
        override public void setval(T val)
        {
            if (val.CompareTo(_min) == -1)
                __val = _min;
            else if (val.CompareTo(_max) == 1)
                __val = _max;
            else
                __val = val;
        }
    }

    private static Dictionary<string, RangeEntry<float>> floatEntries = new Dictionary<string, RangeEntry<float>>
    {
        {"sunDamage", new RangeEntry<float>(50, "sunDamage", "Sun damage", 0, 1000f)},
        {"spawnTime", new RangeEntry<float>(5, "spawnTime", "Spawn time", 0.05f, 5*60)},
        {"maxHealth", new RangeEntry<float>(100, "maxHealth", "Max health", 1, 500)},
        {"jumpSpeed", new RangeEntry<float>(8, "jumpSpeed", "Jump speed", 0.05f, 50)},
        {"lookSpeedX", new RangeEntry<float>(10, "lookSpeedX", "Horizontal look speed", 1, 50)},
        {"lookSpeedY", new RangeEntry<float>(5, "lookSpeedY", "Vertical look speed", 1, 50)},
        {"grappleSpeed", new RangeEntry<float>(50, "grappleSpeed", "Grapple speed", 1, 200)},
        {"sphereRadius", new RangeEntry<float>(70, "sphereRadius", "Sphere radius", false, 20, 500)},
        {"movementSpeed", new RangeEntry<float>(1, "movementSpeed", "Movement speed", 0.05f, 15)},
        {"maxChargeTime", new RangeEntry<float>(1, "maxChargeTime", "Max weapon charge time", 0.05f, 10)},
        {"explosionLift", new RangeEntry<float>(0.2f, "explosionLift", "Explosion lift force", 0, 10)},
        {"explosionForce", new RangeEntry<float>(25, "explosionForce", "Explosion force", 0, 200)},
        {"weaponCooldown", new RangeEntry<float>(0.5f, "weaponCooldown", "Weapon cooldown", 0.01f, 5)},
        {"explosionRadius", new RangeEntry<float>(15, "explosionRadius", "Explosion radius", 0, 50)},
        {"shotSoundVolume", new RangeEntry<float>(0.15f, "shotSoundVolume", "Shot sound volume", 0, 1)},
        {"messageBoxUpTime", new RangeEntry<float>(5, "messageBoxUpTime", "Messagebox uptime", 0.5f, 10)},
        {"projectileImpulse", new RangeEntry<float>(50, "projectileImpulse", "Projectile initial speed", 1, 200)},
        {"gravityMultiplier", new RangeEntry<float>(1, "gravityMultiplier", "Gravity multiplier", 0.05f, 10)},
        {"grappleRampupTime", new RangeEntry<float>(0.5f, "grappleRampupTime", "Grapple rampup time", 0.01f, 2)},
        {"maxGrappleDistanceRatio", new RangeEntry<float>(1, "maxGrappleDistanceRatio", "Grapple distance / radius ratio", 0.01f, 3)},
        {"minProjectileCharge", new RangeEntry<float>(0.3f, "minProjectileCharge", "Min weapon charge", 0, 1)},
        {"projectileHitDamage", new RangeEntry<float>(15, "projectileHitDamage", "Projectile hit damage", 0, 200)},
        {"launchForceMultiplier", new RangeEntry<float>(4, "launchForceMultiplier", "Pillar launch force multiplier", 1, 10)},
        {"explosionParalysisTime", new RangeEntry<float>(1, "explosionParalysisTime", "Explosion paralysis time", 0, 3)},
        {"localMovementOverrideWindow", new RangeEntry<float>(0.7f, "localMovementOverrideWindow", "Local-movement-override time window", 0.01f, 2)},
        {"localForceDampen", new RangeEntry<float>(0.8f, "localForceDampen", "Local-force dampen", 0, 1)},
    };
    private static Dictionary<string, Entry<string>> stringEntries = new Dictionary<string, Entry<string>>
    {
        {"nickname", new Entry<string>("NOOBNOOB", "nickname", "Nickname")}
    };
    private static Dictionary<string, Entry<bool>> boolEntries = new Dictionary<string, Entry<bool>>
    {
        {"chargeMode", new Entry<bool>(false, "chargeMode", "Charge mode")},
        {"weaponLockMode", new Entry<bool>(false, "weaponLockMode", "Weapon lock mode")} // TODO: these modes are mutually exclusive, make a dropdown thing
    };
    private static Dictionary<string, RangeEntry<int>> intEntries = new Dictionary<string, RangeEntry<int>>
    {
        {"EHN", new RangeEntry<int>(3, "EHN", "Exponential hexagon number", false, 1, 4)}
    };

    public static int EHN { get { return intEntries["EHN"]; } set { intEntries["EHN"]._val = value; } }

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
    public static bool weaponLockMode { get { return boolEntries["weaponLockMode"]; } set { boolEntries["weaponLockMode"]._val = value; } }

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

    public static ref Dictionary<string, RangeEntry<float>> GetFloatEntries() { return ref floatEntries; }
    public static ref Dictionary<string, Entry<string>> GetStringEntries() { return ref stringEntries; }
    public static ref Dictionary<string, Entry<bool>> GetBoolEntries() { return ref boolEntries; }
    public static ref Dictionary<string, RangeEntry<int>> GetIntEntries() { return ref intEntries; }
}
