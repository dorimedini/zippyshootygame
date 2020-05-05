using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MainMenuController : MonoBehaviour
{
    public SliderInputController radiusInput, EHNInput;
    public NetworkManager netMngr;

    void Start()
    {
        ReadSettings();
    }

    void OnEnabled()
    {
        ReadSettings();
    }

    public void OnSingleplayer()
    {
        ApplySettings();
        netMngr.StartSingleplayer();
        gameObject.SetActive(false);
    }

    public void OnMultiplayer()
    {
        ApplySettings();
        netMngr.StartMultiplayer();
        gameObject.SetActive(false);
    }

    void ApplySettings()
    {
        UserDefinedConstants.sphereRadius = radiusInput.value;
        UserDefinedConstants.EHN = (int)EHNInput.value;
    }

    void ReadSettings()
    {
        radiusInput.value = UserDefinedConstants.sphereRadius;
        EHNInput.value = UserDefinedConstants.EHN;
    }

    public void ResetToDefaults()
    {
        radiusInput.value = UserDefinedConstants.GetFloatEntries()["sphereRadius"]._default_val;
        EHNInput.value = UserDefinedConstants.GetIntEntries()["EHN"]._default_val;
        ApplySettings();
    }
}
