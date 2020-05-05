using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MainMenuController : MonoBehaviour
{
    public SliderInputController radiusInput, EHNInput;
    public NetworkManager netMngr;

    void Start()
    {
        radiusInput.value = UserDefinedConstants.sphereRadius;
        EHNInput.value = UserDefinedConstants.EHN;
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
}
