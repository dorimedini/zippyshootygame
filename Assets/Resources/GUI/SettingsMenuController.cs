using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class SettingsMenuController : MonoBehaviour
{
    public GameObject settingsContainer;
    public GameObject sliderInputPrefab, stringInputPrefab, toggleInputPrefab;

    Dictionary<string, UserDefinedConstants.RangeEntry<float>> floatVals = UserDefinedConstants.GetFloatEntries();
    Dictionary<string, UserDefinedConstants.Entry<string>> stringVals = UserDefinedConstants.GetStringEntries();
    Dictionary<string, UserDefinedConstants.Entry<bool>> boolVals = UserDefinedConstants.GetBoolEntries();

    List<SliderInputController> sliderInputs;
    List<TextInputController> textInputs;
    List<ToggleInputController> toggleInputs;

    // Start is called before the first frame update
    void Start()
    {
        sliderInputs = new List<SliderInputController>();
        textInputs = new List<TextInputController>();
        toggleInputs = new List<ToggleInputController>();

        foreach (var entry in boolVals.Values)
        {
            toggleInputs.Add(InstantiateToggleInput(entry).GetComponent<ToggleInputController>());
        }
        foreach (var entry in stringVals.Values)
        {
            textInputs.Add(InstantiateStringInput(entry).GetComponent<TextInputController>());
        }
        foreach (var entry in floatVals.Values)
        {
            if (!entry._midgame_ok)
                continue;
            sliderInputs.Add(InstantiateFloatInput(entry).GetComponent<SliderInputController>());
        }
    }

    public void OnApply()
    {
        foreach (SliderInputController slider in sliderInputs)
        {
            floatVals[slider.key]._val = slider.value;
        }
        foreach (TextInputController text in textInputs)
        {
            stringVals[text.key]._val = text.value;
        }
        foreach (ToggleInputController toggle in toggleInputs)
        {
            boolVals[toggle.key]._val = toggle.value;
        }
    }

    public void OnCancel()
    {
        foreach (SliderInputController slider in sliderInputs)
        {
            slider.value = floatVals[slider.key]._val;
        }
        foreach (TextInputController text in textInputs)
        {
            text.value = stringVals[text.key]._val;
        }
        foreach (ToggleInputController toggle in toggleInputs)
        {
            toggle.value = boolVals[toggle.key]._val;
        }
    }

    public void OnResetToDefaults()
    {
        foreach (SliderInputController slider in sliderInputs)
        {
            slider.value = floatVals[slider.key]._default_val;
        }
        foreach (TextInputController text in textInputs)
        {
            text.value = stringVals[text.key]._default_val;
        }
        foreach (ToggleInputController toggle in toggleInputs)
        {
            toggle.value = boolVals[toggle.key]._default_val;
        }
    }

    GameObject InstantiateFloatInput(UserDefinedConstants.RangeEntry<float> entry)
    {
        var obj = Instantiate(sliderInputPrefab, settingsContainer.transform);
        SliderInputController slider = obj.GetComponent<SliderInputController>();
        slider.key = entry._name;
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
        text.key = entry._name;
        text.value = entry;
        text.label = entry._label;
        return obj;
    }

    GameObject InstantiateToggleInput(UserDefinedConstants.Entry<bool> entry)
    {
        var obj = Instantiate(toggleInputPrefab, settingsContainer.transform);
        ToggleInputController toggle = obj.GetComponent<ToggleInputController>();
        toggle.key = entry._name;
        toggle.value = entry;
        toggle.label = entry._label;
        return obj;
    }
}
