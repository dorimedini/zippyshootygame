using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class LockingTargetImageBehaviour : MonoBehaviour
{
    public Image lockImage;
    public Sprite[] lockStateSquares;
    public RectTransform uiCanvas;
    public Camera playerCam;

    private bool active, locked;
    private float lockingFor;
    private Action onLock;
    private Transform target;

    // Update is called once per frame
    void Update()
    {
        if (!active)
        {
            return;
        }
        else // active
        {
            lockingFor = Mathf.Min(UserDefinedConstants.timeToLockOn, lockingFor + Time.deltaTime);
            if (!locked && Tools.NearlyEqual(lockingFor, UserDefinedConstants.timeToLockOn, 0.01f))
            {
                Locked();
            }
            UpdateImage();
        }
    }

    public void StartTargeting(Transform target, Action onLockAction)
    {
        if (onLockAction == null)
        {
            Debug.LogWarning("Nothing to do when lock is complete!");
        }
        lockingFor = 0;
        active = true;
        locked = false;
        this.target = target;
        onLock = onLockAction;
        lockImage.enabled = true;
    }

    public void StopTargeting()
    {
        locked = active = false;
        lockingFor = 0;
        lockImage.enabled = false;
    }

    void UpdateImage()
    {
        // Rotate and place the square so it overlays the target player
        Vector2 screenPoint = RectTransformUtility.WorldToScreenPoint(playerCam, target.position);
        lockImage.rectTransform.anchoredPosition = screenPoint - uiCanvas.sizeDelta / 2f;
        // Show different image for locked-on
        if (locked)
        {
            // TODO: Show locked-on square graphic (flashy red-white?)
            return;
        }
        // TODO: Show stages of lock image to give a feel of targetting. For now, just a descending run...
        int imageIdx = Math.Max(0, lockStateSquares.Length - 1 - (int)(lockStateSquares.Length * Mathf.Clamp01(lockingFor / UserDefinedConstants.timeToLockOn)));
        lockImage.sprite = lockStateSquares[imageIdx];
    }

    void Locked()
    {
        locked = true;
        onLock?.Invoke();
    }
}
