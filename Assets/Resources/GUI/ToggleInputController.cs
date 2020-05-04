using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class ToggleInputController : MonoBehaviour
{
    public string defaultValue;
    public TextMeshProUGUI labelTMP;
    public Toggle toggle;

    public bool value
    {
        get { return toggle.isOn; }
        set { toggle.isOn = value; }
    }
    public string label
    {
        get { return labelTMP.text; }
        set { labelTMP.text = value; }
    }
}
