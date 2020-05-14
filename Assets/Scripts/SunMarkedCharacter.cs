using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SunMarkedCharacter : MonoBehaviour
{
    public Transform sunMarker;

    private SunController sun;
    private MeshRenderer rend;

    // Start is called before the first frame update
    void Start()
    {
        // Grab the sun (for the radius)
        sun = GameObject.Find("Sun").GetComponent<SunController>();
        if (sun == null)
        {
            Debug.LogError("Sun not found");
            return;
        }

        // Set random color
        rend = sunMarker.GetComponentInChildren<MeshRenderer>();
        Material mat = new Material(rend.sharedMaterial);
        mat.color = Random.ColorHSV();
        rend.sharedMaterial = mat;

        // Deactivate renderer by default
        Hide();
    }

    // Update is called once per frame
    void Update()
    {
        // Set position to surface of sun, with up rotation toward player and forward direction towards player forward
        Vector3 playerDirection = transform.position.normalized;
        sunMarker.position = Vector3.Lerp(sunMarker.position, (0.5f + sun.Radius()) * playerDirection, 0.1f);
        sunMarker.rotation = Quaternion.LookRotation(transform.forward, playerDirection);
    }

    public void Show() { rend.enabled = true; }

    public void Hide() { rend.enabled = false; }
}
