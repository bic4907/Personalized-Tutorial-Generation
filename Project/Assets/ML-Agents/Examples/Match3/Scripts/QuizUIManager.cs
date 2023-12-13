using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace Unity.MLAgentsExamples
{

    public class QuizUIManager : MonoBehaviour
    {
        public RawImage HorizontalImage;
        public RawImage VerticalImage;
        public RawImage CrossImage;
        public RawImage RocketImage;
        public RawImage BombImage;
        public RawImage RainbowImage;
        public int QuizNumber = 0;
        public TextMeshProUGUI QuizNumberText;
        public GameObject UIPanel;

        public Dictionary<PieceType, RawImage> ImageDict = new Dictionary<PieceType, RawImage>();

        // Start is called before the first frame update
        void Start()
        {
            ImageDict.Add(PieceType.HorizontalPiece, HorizontalImage);
            ImageDict.Add(PieceType.VerticalPiece, VerticalImage);
            ImageDict.Add(PieceType.CrossPiece, CrossImage);
            ImageDict.Add(PieceType.RocketPiece, RocketImage);
            ImageDict.Add(PieceType.BombPiece, BombImage);
            ImageDict.Add(PieceType.RainbowPiece, RainbowImage);

        }

        // Update is called once per frame
        void Update()
        {
            
        }

        public void SetNumber(int number)
        {
            QuizNumber = number;
            QuizNumberText.text = $"Q{QuizNumber}.";
        }

        public void SetPieceType(PieceType pieceType)
        {
            // Disable all images to setActive
            foreach (KeyValuePair<PieceType, RawImage> entry in ImageDict)
            {
                entry.Value.gameObject.SetActive(false);
            }
            ImageDict[pieceType].gameObject.SetActive(true);
        }

        public void SetActive(bool active)
        {
            UIPanel.SetActive(active);
        }

    }
}