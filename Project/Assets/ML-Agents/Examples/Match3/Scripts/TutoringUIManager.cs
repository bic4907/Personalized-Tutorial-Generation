using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace Unity.MLAgentsExamples
{
    public class TutoringUIManager : MonoBehaviour
    {
        public TextMeshProUGUI MissionLabel;
        public GameObject UIPanel;

        // Start is called before the first frame update
        void Start()
        {
            
        }

        // Update is called once per frame
        void Update()
        {
            
        }


        public void SetNumber(int number)
        {
            MissionLabel.text = $"Learn the Match Skills with {number} Moves";
        }

        public void SetActive(bool active)
        {
            UIPanel.SetActive(active);
        }

    }
}