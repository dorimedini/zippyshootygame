using Photon.Pun;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PowerupableCharacter : MonoBehaviour
{
    private Dictionary<string, int> registeredPowerups;
    private HashSet<string> deniedPowerups;
    private HashSet<string> activePowerups;
    private Dictionary<string, float> timeUntilActive;
    private Dictionary<string, float> remainingDuration;
    private Dictionary<int, int> activePowersCounter;

    void Start()
    {
        deniedPowerups = new HashSet<string>();
        activePowerups = new HashSet<string>();
        registeredPowerups = new Dictionary<string, int>();
        timeUntilActive = new Dictionary<string, float>();
        remainingDuration = new Dictionary<string, float>();
        activePowersCounter = new Dictionary<int, int>();
    }

    void Update()
    {
        // Cleanup the relevant powerups after iterating over their container
        List<string> powerupIdsToRemove = new List<string>();

        // Handle all powerups we know about
        foreach (var kvp in registeredPowerups)
        {
            string powerupId = kvp.Key;
            int powerupIdx = kvp.Value;
            // Should this powerup be denied?
            if (deniedPowerups.Contains(powerupId))
            {
                // We only have a denied powerup if we broadcasted it's pickup but then another player reported he got there first.
                // That means, we can just remove the key from the dictionaries and there's no way the player will try to pick it up again later.
                if (timeUntilActive.ContainsKey(powerupId) || activePowerups.Contains(powerupId))
                {
                    ShowDeniedIndication();
                    timeUntilActive.Remove(powerupId);
                    if (activePowerups.Contains(powerupId))
                    {
                        // This powerup was denied while the player had the power active (bummer)
                        Debug.LogWarning("Player " + PhotonNetwork.LocalPlayer.UserId + " got a real deny for powerup " + powerupId);
                        activePowerups.Remove(powerupId);
                        DecrementPower(powerupIdx);
                    }
                }
                deniedPowerups.Remove(powerupId);
                powerupIdsToRemove.Add(powerupId);
            }
            // Are we still in the warmup time before power is granted?
            else if (timeUntilActive.ContainsKey(powerupId))
            {
                timeUntilActive[powerupId] = Mathf.Max(0, timeUntilActive[powerupId] - Time.deltaTime);
                if (Tools.NearlyEqual(timeUntilActive[powerupId], 0, 0.01f))
                {
                    timeUntilActive.Remove(powerupId);
                    activePowerups.Add(powerupId);
                    remainingDuration[powerupId] = UserDefinedConstants.powerupDuration;
                    IncrementPower(powerupIdx);
                }
            }
            // The powerup is active. Handle duration
            else
            {
                remainingDuration[powerupId] = Mathf.Max(0, remainingDuration[powerupId] - Time.deltaTime);
                if (Tools.NearlyEqual(remainingDuration[powerupId], 0, 0.01f))
                {
                    // Time's up.
                    activePowerups.Remove(powerupId);
                    powerupIdsToRemove.Add(powerupId);
                    DecrementPower(powerupIdx);
                }
            }
        }

        // Out of registeredPowerups iteration, can now edit contents
        foreach (string powerupId in powerupIdsToRemove)
            registeredPowerups.Remove(powerupId);
    }

    public void GrantPower(string powerupId, int powerupIdx)
    {
        registeredPowerups[powerupId] = powerupIdx;
        timeUntilActive[powerupId] = UserDefinedConstants.powerupPrewarm;
        // TODO: Maybe PowerupController should have a static function translating from powerup index to type of powerup?
        // TODO: Play an empowering sound, maybe some empowering graphics?
    }

    public void DenyPower(string powerupId)
    {
        // Can only get here if local player broadcasted a pickup of this powerup.
        if (!registeredPowerups.ContainsKey(powerupId))
        {
            Debug.LogError("Local player was denied powerup " + powerupId + ", but the powerup isn't locally registered");
            return;
        }
        deniedPowerups.Add(powerupId);
    }

    void IncrementPower(int powerupIdx)
    {
        if (!activePowersCounter.ContainsKey(powerupIdx) || activePowersCounter[powerupIdx] == 0)
        {
            activePowersCounter[powerupIdx] = 1;
            Empower(powerupIdx);
        }
        else
        {
            activePowersCounter[powerupIdx] += 1;
        }
    }

    void DecrementPower(int powerupIdx)
    {
        if (!activePowersCounter.ContainsKey(powerupIdx))
        {
            Debug.LogError("No counter for powerup with index " + powerupIdx + " is active on local player! Can't depower");
            return;
        }
        else if (activePowersCounter[powerupIdx] <= 0)
        {
            Debug.LogError("Counter for powerup with index " + powerupIdx + " is at " + activePowersCounter[powerupIdx] + " on local player! Can't depower");
            return;
        }
        else
        {
            activePowersCounter[powerupIdx] -= 1;
            if (activePowersCounter[powerupIdx] == 0)
            {
                Depower(powerupIdx);
            }
        }
    }

    void Empower(int powerupIdx)
    {
        Debug.Log("Empowering (id " + powerupIdx + ")");
        // TODO: Give player power
    }

    void Depower(int powerupIdx)
    {
        Debug.Log("Depowering (id " + powerupIdx + ")");
        // TODO: Revoke player power
    }

    void ShowDeniedIndication()
    {
        // TODO: Another player actually beat us to it. Show indication
    }
}
