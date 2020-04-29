using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class MessagesController : MonoBehaviour
{
    public Image background;
    public Text textComponent;
    public CanvasGroup canvasGroup;

    // Message box will start to fade away after several seconds without updates
    float keepFor;

    void Start()
    {
        canvasGroup.alpha = 0;
    }

    void Update()
    {
        // Fade the message box out after a cooldown
        keepFor = Mathf.Max(0, keepFor - Time.deltaTime);
        float currentAlpha = canvasGroup.alpha;
        if (Tools.NearlyEqual(keepFor, 0, 0.01f))
        {
            if (currentAlpha < 0.01)
            {
                canvasGroup.alpha = 0;
            }
            else
            {
                canvasGroup.alpha = Mathf.Lerp(currentAlpha, 0, 0.05f);
            }
        }
    }

    void WakeUpMessageBox()
    {
        canvasGroup.alpha = 1;
        keepFor = UserDefinedConstants.messageBoxUpTime;
    }

    public void AppendMessage(string msg, bool noNewline=false)
    {
        textComponent.text += (noNewline ? "" : "\n") + msg;
        WakeUpMessageBox();
    }

    public void ReplaceLine(string msg)
    {
        var lines = textComponent.text.Split('\n');
        textComponent.text = string.Join("\n", lines.Take(lines.Length - 1)) + "\n" + msg;
        WakeUpMessageBox();
    }
}
