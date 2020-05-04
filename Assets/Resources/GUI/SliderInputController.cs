using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class SliderInputController : MonoBehaviour
{
    public float defaultVal;
    public Slider slider;
    public TMP_InputField valueText;
    public TextMeshProUGUI labelTMP;

    public float value
    {
        get { return slider.value; }
        set { slider.value = value; }
    }

    public float min
    {
        get { return slider.minValue; }
        set { slider.minValue = value; }
    }

    public float max
    {
        get { return slider.maxValue; }
        set { slider.maxValue = value; }
    }

    public string label
    {
        get { return labelTMP.text; }
        set { labelTMP.text = value; }
    }

    // Called when slider value changes
    public void OnUpdate()
    {
        valueText.text = string.Format("{0:f2}", value);
    }

    // Called when text value of slider changes
    public void OnTextUpdate()
    {
        slider.value = float.Parse(valueText.text); // Should invoke OnUpdate()
    }
}
