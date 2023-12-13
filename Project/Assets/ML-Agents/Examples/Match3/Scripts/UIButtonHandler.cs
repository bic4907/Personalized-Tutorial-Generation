using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UIButtonHandler : MonoBehaviour
{
    public ButtonType buttonType = ButtonType.Okay;
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void OnClick()
    {
        switch (buttonType)
        {
            case ButtonType.Next:
                transform.parent.parent.gameObject.GetComponent<MessageBoxHandler>().NextMessage();
                break;
            case ButtonType.Previous:
                transform.parent.parent.gameObject.GetComponent<MessageBoxHandler>().PrevMessage();
                break;
            case ButtonType.Okay:
                // SetActive to false to the parent of parent game object
                transform.parent.parent.gameObject.SetActive(false);

                


                break;
            default:
                break;
        }

    }
}

public enum ButtonType
{
    Next,
    Previous,
    Okay,
}