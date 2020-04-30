using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HookshotController : MonoBehaviour
{
    public GameObject segmentPrefab;

    List<HookshotSegment> segments;
    Transform grappleSourceTransform;
    Vector3 ropeTarget;

    public void Init(Transform source, Vector3 target)
    {
        segments = new List<HookshotSegment>();
        grappleSourceTransform = source;
        ropeTarget = target;
    }

    // When the hookshot is fired the hook location will move from firer's hand to the final target.
    public void UpdateTarget(Vector3 newTarget) { ropeTarget = newTarget; }

    void Update()
    {
        // Without scaling, each additional chain link segment adds roughly 1/12 units of length, in the (local) positive Y direction. Total segment chain length ~2.5 units.
        // To avoid redundant re-instantiation of many segments we only add/remove a segment when the chain is long/short enough.
        transform.position = ropeTarget;
        transform.LookAt(grappleSourceTransform.position);
        float distToTarget = (ropeTarget - grappleSourceTransform.position).magnitude - 0.25f; // Offset slightely so the links align nicely
        int totalLinks = (int)(12f * distToTarget);
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
