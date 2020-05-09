using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;
using UnityEngine.UIElements;

public class TargetableCharacter : MonoBehaviourPun
{
    public AudioSource targetedSound, lockedSound;

    private string thisUserId;
    private HashSet<string> targetingEnemyIds;
    private HashSet<string> lockedEnemyIds;

    public Transform centerTransform { get { return GetComponent<NetworkCharacter>().characterCenter; } }

    private enum TargetState { IDLE, TARGET, LOCK }

    void Start()
    {
        thisUserId = photonView.Owner.UserId;
        targetingEnemyIds = new HashSet<string>();
        lockedEnemyIds = new HashSet<string>();
    }

    public void BroadcastBecameTargeted(string enemyUserId)
    {
        photonView.RPC("BecameTargeted", NetworkCharacter.GetPlayerByUserID(thisUserId), enemyUserId);
    }

    public void BroadcastBecameLockedOn(string enemyUserId)
    {
        photonView.RPC("BecameLockedOn", NetworkCharacter.GetPlayerByUserID(thisUserId), enemyUserId);
    }

    public void BroadcastBecameUntargeted(string enemyUserId)
    {
        photonView.RPC("BecameUntargeted", NetworkCharacter.GetPlayerByUserID(thisUserId), enemyUserId);
    }

    [PunRPC]
    public void BecameTargeted(string enemyUserId)
    {
        Debug.Log("Became targeted");
        targetingEnemyIds.Add(enemyUserId);
        if (lockedEnemyIds.Contains(enemyUserId))
        {
            lockedEnemyIds.Remove(enemyUserId);
            Debug.LogWarning("User " + enemyUserId + " started targeting user " + thisUserId + ", but was already locked on");
        }
        UpdateTargetedFeedbackState();
    }

    [PunRPC]
    public void BecameLockedOn(string enemyUserId)
    {
        Debug.Log("Became locked");
        lockedEnemyIds.Add(enemyUserId);
        targetingEnemyIds.Remove(enemyUserId);
        UpdateTargetedFeedbackState();
    }

    [PunRPC]
    public void BecameUntargeted(string enemyUserId)
    {
        // FIXME: This is fired when shooting player shoots the missile. The lock-on danger sound should continue as long as the projectile hasn't been destroyed!
        Debug.Log("Became untargeted");
        targetingEnemyIds.Remove(enemyUserId);
        lockedEnemyIds.Remove(enemyUserId);
        UpdateTargetedFeedbackState();
    }

    void UpdateTargetedFeedbackState()
    {
        switch (CurrentState())
        {
            case TargetState.IDLE:
                // We may need to stop sounds.
                // We also may need to activate warning sounds now that the locked player called BroadcastUntargeted while some other player is still locking.
                if (lockedSound.isPlaying)
                    lockedSound.Stop();
                if (targetedSound.isPlaying)
                    targetedSound.Stop();
                // TODO: Shut off respective exclamation mark graphics
                break;
            case TargetState.TARGET:
                Debug.Log("switch state TARGET");
                if (lockedSound.isPlaying)
                    lockedSound.Stop();
                if (!targetedSound.isPlaying)
                    targetedSound.Play();
                // TODO: Show a flashing yellow exclamation mark somewhere
                break;
            case TargetState.LOCK:
                Debug.Log("switch state LOCK");
                if (targetedSound.isPlaying)
                    targetedSound.Stop();
                if (!lockedSound.isPlaying)
                    lockedSound.Play();
                // TODO: Show a flashing red exclamation mark somewhere
                break;
        }
    }

    TargetState CurrentState()
    {
        if (lockedEnemyIds.Count > 0)
            return TargetState.LOCK;
        if (targetingEnemyIds.Count > 0)
            return TargetState.TARGET;
        return TargetState.IDLE;
    }
}
