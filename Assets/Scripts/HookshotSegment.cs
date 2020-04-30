using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HookshotSegment : MonoBehaviour
{
    public GameObject[] chainLinks;
    public GameObject hook;
    public Transform chainStart, chainEnd, hookEnd;
    public Material chainMat;

    int activeLinks;

    void Start()
    {
        if (TotalLinks() != chainLinks.Length)
        {
            Debug.LogError(string.Format("Should be {0} links, only {1} exist on instance", TotalLinks(), chainLinks.Length));
        }
        foreach (GameObject link in chainLinks)
        {
            link.GetComponent<MeshRenderer>().material = chainMat;
        }
        activeLinks = 0;
        foreach(var link in chainLinks)
        {
            if (link.activeSelf)
            {
                ++activeLinks;
            }
        }
    }

    // If this is an intermediate segment we need to turn off the hook
    public void SetIntermediateSegment(bool isIntermediate) { hook.SetActive(!isIntermediate); }
    public bool IsIntermediateSegment() { return hook.activeSelf; }

    // Sets how many chain links will be visible, counting from the hook backwards.
    public void SetNumOfActiveLinks(int num)
    {
        if (num > chainLinks.Length)
        {
            Debug.LogError(string.Format("Attempting to activate {0} links but only {1} exist", num, chainLinks.Length));
            return;
        }
        for (int i=0; i<chainLinks.Length; ++i)
        {
            SetLinkActive(i, i < num);
        }
    }
    public void ActivateAllLinks() { SetNumOfActiveLinks(chainLinks.Length); }

    void SetLinkActive(int idx, bool active)
    {
        if (idx >= chainLinks.Length)
        {
            Debug.LogError(string.Format("Requested to activate link {0} but only {1} exist", idx, chainLinks.Length));
            return;
        }
        // Check if there's nothing to do
        if (active == chainLinks[idx].activeSelf)
            return;
        chainLinks[idx].SetActive(active);
        // Now we know active state CHANGED so we always change the active links count
        activeLinks += active ? 1 : -1;
    }

    public int TotalActiveLinks() { return activeLinks; }
    public static int TotalLinks() { return 30; }
}
