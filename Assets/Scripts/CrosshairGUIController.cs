using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class CrosshairGUIController : MonoBehaviour
{
    public List<Image> chargeImages;
    public Image lockScope;

    // Start is called before the first frame update
    void Start()
    {
        // Disable all but the idle image
        for (int i = 1; i < chargeImages.Count; ++i)
            chargeImages[i].enabled = false;
        UpdateLockMode();
    }

    public void UpdateLockMode()
    {
        // Show the lock-on scope graphic while weapon mode is set to lock-on
        lockScope.enabled = UserDefinedConstants.weaponLockMode;
        if (lockScope.enabled)
        {
            lockScope.rectTransform.localScale = (new Vector3(1, 1, 1)) * UserDefinedConstants.lockScopeRadius;
        }
    }

    public void updateChargeState(float currentCharge, float maxCharge)
    {
        // Set exactly one of the images active, proportional to charge
        int activeImageIdx = (int)(chargeImages.Count * currentCharge / maxCharge);
        if (activeImageIdx >= chargeImages.Count)
            activeImageIdx = chargeImages.Count - 1;
        for (int i=0; i<chargeImages.Count; ++i)
        {
            chargeImages[i].enabled = (i == activeImageIdx);
        }
    }
}
