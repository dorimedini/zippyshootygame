using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

public class LockingTargetImageBehaviour : MonoBehaviour
{
    public RectTransform uiCanvas;
    public Camera playerCam;
    public RectTransform targetingImageTransform;
    public Image[] lockStateSquares;

    public float borderAlpha, innerAlpha;

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
        UpdateImage();
    }

    public void StopTargeting()
    {
        locked = active = false;
        lockingFor = 0;
        foreach (Image image in lockStateSquares)
            image.gameObject.SetActive(false);
    }

    void UpdateImage()
    {
        // Rotate and place the square so it overlays the target player
        Vector2 screenPoint = RectTransformUtility.WorldToScreenPoint(playerCam, target.position);
        targetingImageTransform.anchoredPosition = screenPoint - uiCanvas.sizeDelta / 2f;
        // Show different image for locked-on
        if (locked)
        {
            // TODO: Show locked-on square graphic (flashy red-white?)
            return;
        }
        SetImageTargetState();
    }

    void SetImageTargetState()
    {
        int totalStates = 9;
        int state = Mathf.FloorToInt(Mathf.Clamp(totalStates * Mathf.Clamp01(lockingFor / UserDefinedConstants.timeToLockOn), 0.5f, (float)totalStates -0.5f));
        // Split into three phases; each phase fixes some border with alpha 75%, and each of the three states of a phase
        // also activate 50%-alpha squares going inward from the border.
        int activeBorderImageIdx;
        List<bool> activeHalfAlphaImages = Enumerable.Repeat(false, lockStateSquares.Length).ToList();

        // Border index
        activeBorderImageIdx = state <= 2 ? lockStateSquares.Length - 1 :
                            (state <= 5 ? lockStateSquares.Length - 3 : lockStateSquares.Length - 5);

        // First phase
        if (state <= 2)
        {
            activeHalfAlphaImages[lockStateSquares.Length - 7] = (state == 2);
            activeHalfAlphaImages[lockStateSquares.Length - 5] = (state != 0);
            activeHalfAlphaImages[lockStateSquares.Length - 3] = true;
        }

        // Second phase
        else if (state <= 5)
        {
            activeHalfAlphaImages[lockStateSquares.Length - 9] = (state == 5);
            activeHalfAlphaImages[lockStateSquares.Length - 7] = (state != 3);
            activeHalfAlphaImages[lockStateSquares.Length - 5] = true;
        }

        // Third phase
        else // 6 <= state <= 8
        {
            activeHalfAlphaImages[lockStateSquares.Length - 11] = (state == 8);
            activeHalfAlphaImages[lockStateSquares.Length - 9] = (state != 6);
            activeHalfAlphaImages[lockStateSquares.Length - 7] = true;
        }

        // Update game object states
        for (int i=0; i< lockStateSquares.Length; ++i)
        {
            if (i == activeBorderImageIdx)
                continue;
            lockStateSquares[i].gameObject.SetActive(activeHalfAlphaImages[i]);
            if (activeHalfAlphaImages[i])
                lockStateSquares[i].color = new Color(
                    lockStateSquares[i].color.r,
                    lockStateSquares[i].color.g,
                    lockStateSquares[i].color.b,
                    innerAlpha);
        }
        lockStateSquares[activeBorderImageIdx].gameObject.SetActive(true);
        lockStateSquares[activeBorderImageIdx].color = new Color(
            lockStateSquares[activeBorderImageIdx].color.r,
            lockStateSquares[activeBorderImageIdx].color.g,
            lockStateSquares[activeBorderImageIdx].color.b,
            borderAlpha);
    }

    void Locked()
    {
        locked = true;
        onLock?.Invoke();
    }
}
