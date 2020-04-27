using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class CrosshairGUIController : MonoBehaviour
{
    public List<Image> images;

    // Start is called before the first frame update
    void Start()
    {
        // Disable all but the idle image
        for (int i = 1; i < images.Count; ++i)
            images[i].enabled = false;
    }

    public void updateChargeState(float currentCharge, float maxCharge)
    {
        // Set exactly one of the images active, proportional to charge
        int activeImageIdx = (int)(images.Count * currentCharge / maxCharge);
        if (activeImageIdx >= images.Count)
            activeImageIdx = images.Count - 1;
        for (int i=0; i<images.Count; ++i)
        {
            images[i].enabled = (i == activeImageIdx);
        }
    }
}
