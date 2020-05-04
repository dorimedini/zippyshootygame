using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class TextInputController : MonoBehaviour
{
    public string defaultValue;
    public TextMeshProUGUI labelTMP;
    public TMP_InputField inputTMP;

    public string value
    {
        get { return inputTMP.text; }
        set { inputTMP.text = value; }
    }
    public string label
    {
        get { return labelTMP.text; }
        set { labelTMP.text = value; }
    }
}
