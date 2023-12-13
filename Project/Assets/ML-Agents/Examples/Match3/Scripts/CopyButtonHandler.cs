using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class CopyButtonHandler : MonoBehaviour
{
    private TextMeshProUGUI m_ButtonText;
    public float displayTime = 1f;
    public TMP_InputField CopyTarget;
    // Start is called before the first frame update
    void Start()
    {
        // Get child TextMeshProUGUI component
        m_ButtonText = GetComponentInChildren<TextMeshProUGUI>();       

    }

    // Update is called once per frame
    void Update()
    {
        
    }

    private System.Collections.IEnumerator ShowCopiedText()
    {
        m_ButtonText.text = "Copied!";
        yield return new WaitForSeconds(displayTime);
        m_ButtonText.text = "Copy";
    }
    public void OnClick()
    {
        // Copy the text from the input field and set it to the clipboard
        GUIUtility.systemCopyBuffer = CopyTarget.text;
        StartCoroutine(ShowCopiedText());
    }
}
