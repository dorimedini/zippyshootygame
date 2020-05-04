using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SettingsMenuController : MonoBehaviour
{
    public GameObject settingsContainer;
    public GameObject sliderInputPrefab, stringInputPrefab, toggleInputPrefab;

    List<SliderInputController> sliderInputs;
    List<TextInputController> textInputs;
    List<ToggleInputController> toggleInputs;

    // Start is called before the first frame update
    void Start()
    {
        sliderInputs = new List<SliderInputController>();
        textInputs = new List<TextInputController>();
        toggleInputs = new List<ToggleInputController>();
        var floatVals = UserDefinedConstants.GetFloatEntries();
        var stringVals = UserDefinedConstants.GetStringEntries();
        var boolVals = UserDefinedConstants.GetBoolEntries();
        Debug.Log(string.Format("Building settings area, got {0} toggles, {1} texts and {2} floats (sliders)", boolVals.Count, stringVals.Count, floatVals.Count));

        foreach (var entry in boolVals.Values)
        {
            toggleInputs.Add(InstantiateToggleInput(entry).GetComponent<ToggleInputController>());
        }
        foreach (var entry in stringVals.Values)
        {
            Debug.Log("Instantiating string input with entry " + entry._name);
            textInputs.Add(InstantiateStringInput(entry).GetComponent<TextInputController>());
        }
        foreach (var entry in floatVals.Values)
        {
            sliderInputs.Add(InstantiateFloatInput(entry).GetComponent<SliderInputController>());
        }
    }

    public void OnApply()
    {
        // TODO: Read all settings values to user constants
    }

    public void OnCancel()
    {
        // TODO: Set UI values on all input fields to be the previous values
    }

    public void OnResetToDefaults()
    {
        // TODO: Reset all UI values to default
    }

    GameObject InstantiateFloatInput(UserDefinedConstants.FloatEntry entry)
    {
        var obj = Instantiate(sliderInputPrefab, settingsContainer.transform);
        SliderInputController slider = obj.GetComponent<SliderInputController>();
        slider.min = entry._min;
        slider.max = entry._max;
        slider.value = entry;
        slider.label = entry._label;
        return obj;
    }

    GameObject InstantiateStringInput(UserDefinedConstants.Entry<string> entry)
    {
        var obj = Instantiate(stringInputPrefab, settingsContainer.transform);
        TextInputController text = obj.GetComponent<TextInputController>();
        text.value = entry;
        text.label = entry._label;
        return obj;
    }

    GameObject InstantiateToggleInput(UserDefinedConstants.Entry<bool> entry)
    {
        var obj = Instantiate(toggleInputPrefab, settingsContainer.transform);
        ToggleInputController toggle = obj.GetComponent<ToggleInputController>();
        toggle.value = entry;
        toggle.label = entry._label;
        return obj;
    }
}
