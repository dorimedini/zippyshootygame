using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SunrayCageController : MonoBehaviour
{
    public LineRenderer[] cageLines;

    private Vector3[] originalLocalPositions;
    private Transform target;
    private float timeElapsed, timeToCage, originalRadius;
    bool active;

    void Start()
    {
        RetractCageLines();
        active = false;
        originalLocalPositions = new Vector3[cageLines.Length];
        for (int i=0; i<cageLines.Length; ++i)
        {
            originalLocalPositions[i] = cageLines[i].transform.localPosition;
        }
        if (cageLines.Length == 0)
        {
            Debug.LogError("No line renderers in cageLines array");
            return;
        }
        originalRadius = originalLocalPositions[0].magnitude;
    }

    void Update()
    {
        if (!active)
            return;

        // Rotate towards target
        transform.rotation = Quaternion.LookRotation(target.position);

        // Close the bars inwards
        timeElapsed += Time.deltaTime;
        float newRadius = Mathf.Lerp(originalRadius, 0, timeElapsed / timeToCage);
        SetCageRadius(newRadius);

        // Stop caging when time's up
        if (timeElapsed > timeToCage)
        {
            active = false;
            RetractCageLines();
        }
    }

    public void CageForDuration(Transform target, float time)
    {
        ExtendCageLines();
        timeElapsed = 0;
        active = true;
        this.target = target;
        timeToCage = time;
    }

    void RetractCageLines() { SetCageLineLength(0); }
    void ExtendCageLines() { SetCageLineLength(UserDefinedConstants.sphereRadius + 10); }
    void SetCageLineLength(float length)
    {
        foreach (var line in cageLines)
        {
            line.SetPosition(1, new Vector3(0, 0, length));
        }
    }

    void SetCageRadius(float radius)
    {
        for (int i=0; i<cageLines.Length; ++i)
        {
            cageLines[i].transform.localPosition = (radius / originalRadius) * originalLocalPositions[i];
        }
    }
}
