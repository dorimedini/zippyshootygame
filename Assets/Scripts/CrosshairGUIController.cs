using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class CrosshairGUIController : MonoBehaviour
{
    private Image[] images;

    // Start is called before the first frame update
    void Start()
    {
        images = GetComponentsInChildren<Image>();
        // Disable all but the idle image
        for (int i = 1; i < images.Length; ++i)
            images[i].enabled = false;
    }

    public void updateChargeState(float currentCharge, float maxCharge)
    {
        // Set exactly one of the images active, proportional to charge
        int activeImageIdx = (int)(images.Length * currentCharge / maxCharge);
        if (activeImageIdx >= images.Length)
            activeImageIdx = images.Length - 1;
        for (int i=0; i<images.Length; ++i)
        {
            images[i].enabled = (i == activeImageIdx);
        }
    }
}
