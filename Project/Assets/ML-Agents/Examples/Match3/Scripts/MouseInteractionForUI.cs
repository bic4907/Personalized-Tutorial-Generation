using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using Unity.MLAgentsExamples;
using Unity.MLAgents.Integrations.Match3;

public class CurrentlyUserLearnedBlock
{
    public bool Bomb { get; set; }
    public bool Horizontal { get; set; }
    public bool Vertical { get; set; }
    public bool Cross { get; set; }
    public bool Rainbow { get; set; }
    public bool Rocket { get; set; }
    public CurrentlyUserLearnedBlock()
    {
        Bomb = false;
        Horizontal = false;
        Vertical = false;
        Cross = false;
        Rainbow = false;
        Rocket = false;
    }
}
public class MouseInteractionForUI : MonoBehaviour
{
    public Camera mainCamera;
    public CurrentlyUserLearnedBlock currentlyUserLearnedBlock;
    RaycastHit hit;
    Ray ray;
    void Start()
    {
        currentlyUserLearnedBlock = new CurrentlyUserLearnedBlock();
    }
    void Update()
    {
        WaitForMouseInput();
    }
    public void WaitForMouseInput()
    {
        // Vector3 mousePos = Input.mousePosition;
        // mousePos = mainCamera.ScreenToWorldPoint(mousePos);
        // Debug.DrawRay(transform.position, mousePos -  transform.position, Color.blue);
            if (Input.GetMouseButtonDown(0))
            {
                ray = mainCamera.ScreenPointToRay(Input.mousePosition);
                
                if (Physics.Raycast(ray,out hit))
                {
                    if(hit.transform.name == "DoneButton")
                    {
                        this.gameObject.SetActive(false);
                    }
                    
                    if(hit.transform.name == "BombButton")
                    {
                        hit.transform.GetComponent<Renderer>().material.color = Color.blue;
                        currentlyUserLearnedBlock.Bomb = true;
                    }
                    
                    if(hit.transform.name == "CandyButton")
                    {
                        hit.transform.GetComponent<Renderer>().material.color = Color.blue;
                        currentlyUserLearnedBlock.Rainbow = true;
                    }
                    
                    if(hit.transform.name == "CrossButton")
                    {
                        hit.transform.GetComponent<Renderer>().material.color = Color.blue;
                        currentlyUserLearnedBlock.Cross = true;
                    }
                    
                    if(hit.transform.name == "HorizontalButton")
                    {
                        hit.transform.GetComponent<Renderer>().material.color = Color.blue;
                        currentlyUserLearnedBlock.Horizontal = true;
                    }
                    
                    if(hit.transform.name == "VerticalButton")
                    {
                        hit.transform.GetComponent<Renderer>().material.color = Color.blue;
                        currentlyUserLearnedBlock.Vertical = true;
                    }
                    
                    if(hit.transform.name == "RocketButton")
                    {
                        hit.transform.GetComponent<Renderer>().material.color = Color.blue;
                        currentlyUserLearnedBlock.Rocket = true;
                    }
                }
            }
    }
}
