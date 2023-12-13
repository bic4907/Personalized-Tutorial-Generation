using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MessageBoxHandler : MonoBehaviour
{

    public List<GameObject> Pages = new List<GameObject>();
    public int CurrentPage = 0;
    // Start is called before the first frame update
    void Start()
    {
        ShowMessage();
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void NextMessage()
    {
        CurrentPage++;
        ShowMessage();
    }

    public void PrevMessage()
    {
        CurrentPage--;
        ShowMessage();
    }

    public void ShowMessage()
    {
        for (int i = 0; i < Pages.Count; i++)
        {
            Pages[i].SetActive(false);
        }
        Pages[CurrentPage].SetActive(true);
    }

}
