﻿using System;
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
    public Image lockedImage1;
    public Image lockedImage2;
    public Image[] lockStateSquares;

    public AudioSource targetingSound, lockSound;

    public float borderAlpha, innerAlpha;

    private bool active, locked;
    private float lockingFor;
    private Action onLock;
    private Transform target;
    private bool lockImageAlphaIncreasing;
    private int targetingStage;

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

    void OnDestroy()
    {
        ResetAlpha();
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
        targetingStage = -1;
        UpdateImage();
    }

    public void StopTargeting()
    {
        locked = active = false;
        lockingFor = 0;
        DisableTargetingImages();
    }

    public void DisableTargetingImages()
    {
        foreach (Image image in lockStateSquares)
            image.gameObject.SetActive(false);
        lockedImage1.gameObject.SetActive(false);
        lockedImage2.gameObject.SetActive(false);
    }

    void UpdateImage()
    {
        // Show different image for locked-on
        PlaceSquareOnTarget();
        if (locked)
        {
            UpdateImageLockedState();
        }
        else
        {
            UpdateImageTargetState();
        }
    }

    void PlaceSquareOnTarget()
    {
        // Rotate and place the square so it overlays the target player
        Vector2 screenPoint = RectTransformUtility.WorldToScreenPoint(playerCam, target.position);
        targetingImageTransform.anchoredPosition = screenPoint - uiCanvas.sizeDelta / 2f;
    }

    void UpdateImageTargetState()
    {
        int totalStates = 9;
        int newTargetingStage = Mathf.FloorToInt(Mathf.Clamp(totalStates * Mathf.Clamp01(lockingFor / UserDefinedConstants.timeToLockOn), 0.5f, (float)totalStates -0.5f));

        // Maybe there's nothing to do
        if (targetingStage == newTargetingStage)
            return;
        targetingStage = newTargetingStage;

        // Split into three phases; each phase fixes some border with alpha 75%, and each of the three states of a phase
        // also activate 50%-alpha squares going inward from the border.
        int activeBorderImageIdx;
        List<bool> activeHalfAlphaImages = Enumerable.Repeat(false, lockStateSquares.Length).ToList();

        // Border index
        activeBorderImageIdx = targetingStage <= 2 ? lockStateSquares.Length - 1 :
                            (targetingStage <= 5 ? lockStateSquares.Length - 3 : lockStateSquares.Length - 5);

        // This was easier to read once, until I found out I could do the same logic in less lines :)
        for (int i=0; i<totalStates/3; ++i)
        {
            if (targetingStage <= 3 * i + 2)
            {
                activeHalfAlphaImages[lockStateSquares.Length - 7 - 2 * i] = (targetingStage == 3 * i + 2);
                activeHalfAlphaImages[lockStateSquares.Length - 5 - 2 * i] = (targetingStage != 3 * i);
                activeHalfAlphaImages[lockStateSquares.Length - 3 - 2 * i] = true;
                break;
            }
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

        // If we passed a phase but haven't reached the last phase yet, play a targeting sound.
        // Don't play the first targetting sound, otherwise single-click firing will beep all the time
        // TODO: In stages not divisible by 3, from stage 1 onwards, maybe play a much quieter sound...? BEEP tick tick BEEP tick tick... etc
        if (targetingStage != 0 && targetingStage % 3 == 0 && targetingStage != totalStates)
            targetingSound.Play();
    }

    void UpdateImageLockedState()
    {
        // Lerp alpha on upper image.
        // It'll stop around alpha=0.5f due to direction toggle after lerp
        float flashSpeed = 4;
        float newAlpha = lockImageAlphaIncreasing ?
            Mathf.Lerp(0, 1f, lockedImage2.color.a + flashSpeed * Time.deltaTime) :
            Mathf.Lerp(0, 1f, lockedImage2.color.a - flashSpeed * Time.deltaTime);
        lockedImage2.color = new Color(lockedImage2.color.r, lockedImage2.color.g, lockedImage2.color.b, newAlpha);
        // Toggle lerp direction
        if ((lockImageAlphaIncreasing && newAlpha >= 0.5f)
            || (!lockImageAlphaIncreasing && Tools.NearlyEqual(newAlpha, 0, 0.01f)))
        {
            lockImageAlphaIncreasing = !lockImageAlphaIncreasing;
        }
    }

    void Locked()
    {
        locked = true;
        DisableTargetingImages();
        lockedImage1.gameObject.SetActive(true);
        lockedImage2.gameObject.SetActive(true);
        ResetAlpha();
        lockSound.Play();
        onLock?.Invoke();
    }

    void ResetAlpha()
    {
        lockImageAlphaIncreasing = false;
        lockedImage2.color = new Color(lockedImage2.color.r, lockedImage2.color.g, lockedImage2.color.b, 0.5f);
    }
}
