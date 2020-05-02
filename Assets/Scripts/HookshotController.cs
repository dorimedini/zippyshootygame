using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HookshotController : MonoBehaviour
{
    public GameObject segmentPrefab;
    public AudioSource fireHookshotSound, grapplingSound;
    public GameObject audioSourceObject;    // For positioning

    List<HookshotSegment> segments;
    Transform grappleSourceTransform;
    Vector3 hookTarget;
    float timeToTarget, currentTimeToTarget;
    bool hookHitTarget;

    public void Init(Transform source, Vector3 target)
    {
        segments = new List<HookshotSegment>();
        grappleSourceTransform = source;
        hookTarget = target;
        timeToTarget = UserDefinedConstants.grappleRampupTime / 2;
        currentTimeToTarget = timeToTarget;
        hookHitTarget = false;
        StartPlayingSounds();
    }

    // When the hookshot is fired the hook location will move from firer's hand to the final target.
    public void UpdateTarget(Vector3 newTarget) {
        if (hookTarget != newTarget)
        {
            currentTimeToTarget = timeToTarget;
            hookHitTarget = false;
            StartPlayingSounds();
        }
        hookTarget = newTarget;
    }

    void Update()
    {
        // Without scaling, each additional chain link segment adds roughly 1/12 units of length, in the (local) positive Y direction. Total segment chain length ~2.5 units.
        // To avoid redundant re-instantiation of many segments we only add/remove a segment when the chain is long/short enough.
        // The actual shot length may be less than the distance if the hook is still travelling towards target, so lerp them.
        // timeToTargetRatio starts at 1, so we're lerping from end to start
        currentTimeToTarget = Mathf.Max(0, currentTimeToTarget - Time.deltaTime);
        float timeToTargetRatio = currentTimeToTarget / timeToTarget;
        transform.position = Vector3.Lerp(hookTarget, grappleSourceTransform.position, timeToTargetRatio);
        transform.LookAt(grappleSourceTransform.position);
        audioSourceObject.transform.position = grappleSourceTransform.position;
        float distToTarget = (hookTarget - grappleSourceTransform.position).magnitude;
        float distToCurrentTarget = Mathf.Lerp(distToTarget, 0.5f, timeToTargetRatio);
        int totalLinks = (int)(12f * distToCurrentTarget);
        int totalSegments = (int)Mathf.Ceil(totalLinks / HookshotSegment.TotalLinks()) + 1;
        int linksInFirstSegment = totalLinks % HookshotSegment.TotalLinks();
        // Add / remove segments if need be
        while (totalSegments > segments.Count)
        {
            if (segments.Count > 0)
                segments[0].ActivateAllLinks();
            GameObject newSegment = Instantiate(segmentPrefab, transform);
            newSegment.transform.localPosition = new Vector3(0, 0, 2.5f * (float)(segments.Count));
            segments.Insert(0, newSegment.GetComponent<HookshotSegment>());
            segments[0].SetIntermediateSegment(true);   // Hook off by default
        }
        while (totalSegments < segments.Count)
        {
            Destroy(segments[0].gameObject);
            segments.RemoveAt(0);
        }
        // Set number of links on first segment. Also the place we turn on the hook model on the last segment
        if (segments.Count > 0)
        {
            segments[0].SetNumOfActiveLinks(linksInFirstSegment);
            segments[segments.Count - 1].SetIntermediateSegment(false);
        }

        // If we just hit the target play hit sound
        if (!hookHitTarget && Tools.NearlyEqual(currentTimeToTarget, 0, 0.01f))
        {
            hookHitTarget = true;
            PlayHookHitSound();
        }
    }

    void StartPlayingSounds()
    {
        // We want to play the crossbow-fire and the whoosh sounds simultaniously, then soon after the crossbow hit.
        if (grapplingSound.isPlaying)
            grapplingSound.Stop();
        fireHookshotSound.Play();
        grapplingSound.PlayDelayed(timeToTarget - 0.01f);
    }

    void PlayHookHitSound()
    {
        AudioSource.PlayClipAtPoint(fireHookshotSound.clip, hookTarget);
    }

    public static GameObject DrawHookshot(GameObject hookshotPrefab, Transform grappleHand, Vector3 to)
    {
        // Root trasnform location is hook location
        var hookshot = Instantiate(hookshotPrefab, to, Quaternion.identity);
        HookshotController hc = hookshot.GetComponent<HookshotController>();
        if (hc == null)
            Debug.LogError("No HookshotController on Hookshot prefab!");
        hc.Init(grappleHand, to);
        return hookshot;
    }
}
