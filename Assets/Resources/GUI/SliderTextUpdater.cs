using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

[RequireComponent(typeof(TextMesh))]
public class SliderTextUpdater : MonoBehaviour
{
    public Slider slider;
    public TextMeshProUGUI text;

    void Start() { UpdateValue(); }

    public void UpdateValue()
    {
        text.text = string.Format("{0:f2}", slider.value);
    }
}
