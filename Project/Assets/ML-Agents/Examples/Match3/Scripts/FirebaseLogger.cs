using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Text;
using Newtonsoft.Json;
using UnityEngine;
using UnityEngine.Networking;



namespace Unity.MLAgentsExamples
{

    public class FirebaseLogger: MonoBehaviour
    {
        // Start is called before the first frame update
        private const string FirebaseUrl = "https://[Firebase-Project-Name].firebaseio.com/0824/{playerId}/";
        private const string LearningSessionUrl = "Learning.json";
        private const string QuizSessionUrl = "Quiz.json";
        private const string DoneUrl = "Done.json";

        private string m_UUID;

        private string m_ExternalIPAddress;

        void Start()
        {
            StartCoroutine(FetchExternalIPAddress());

        }


        public void Post(FirebaseLog.LearningLog log)
        {
            StartCoroutine(SendPostRequest(this.GetLearningSessionURL(), log.ToDict()));
        }

        public void Post(FirebaseLog.QuizLog log)
        {
            StartCoroutine(SendPostRequest(this.GetQuizSessionURL(), log.ToDict()));
        }

        public void PostDoneSignal()
        {
            Dictionary<string, object> jsonBody = new Dictionary<string, object>();
            jsonBody.Add("Done", true);
            StartCoroutine(SendPostRequest(this.GetDoneURL(), jsonBody));
        }

        public string GetRootURL()
        {
            // Return the replaced URL {playerId} to m_UUID
            return FirebaseUrl.Replace("{playerId}", m_UUID);
        }

        public string GetLearningSessionURL()
        {
            return GetRootURL() + LearningSessionUrl;
        }

        public string GetQuizSessionURL()
        {
            return GetRootURL() + QuizSessionUrl;
        }

        public string GetDoneURL()
        {
            return GetRootURL() + DoneUrl;
        }

        private IEnumerator SendPostRequest(string url, Dictionary<string, object> jsonBody)
        {
            jsonBody.Add("IPAddress", m_ExternalIPAddress);

            string jsonString = JsonConvert.SerializeObject(jsonBody);

            using (UnityWebRequest request = UnityWebRequest.Post(url, ""))
            {

                byte[] bodyRaw = Encoding.UTF8.GetBytes(jsonString);
                request.uploadHandler = new UploadHandlerRaw(bodyRaw);
                request.downloadHandler = new DownloadHandlerBuffer();
                request.SetRequestHeader("Content-Type", "application/json");
                request.SetRequestHeader("Access-Control-Allow-Origin", "*");

                yield return request.SendWebRequest();

                if (request.result == UnityWebRequest.Result.Success)
                {
                    Debug.Log(request.downloadHandler.text);
                }
                else
                {
                    Debug.LogError("Error sending POST request: " + request.responseCode);
                }
            }


        }


        private IEnumerator FetchExternalIPAddress()
        {

            using (UnityWebRequest request = UnityWebRequest.Get("https://api.ip.pe.kr/"))
            {
                request.SetRequestHeader("Access-Control-Allow-Origin", "*");
                request.SetRequestHeader("Access-Control-Allow-Credentials", "true");
                request.SetRequestHeader("Access-Control-Allow-Headers", "Accept, X-Access-Token, X-Application-Name, X-Request-Sent-Time");
                request.SetRequestHeader("Access-Control-Allow-Methods", "GET, POST, OPTIONS");

                yield return request.SendWebRequest();

                if (request.result == UnityWebRequest.Result.Success)
                {
                    m_ExternalIPAddress = request.downloadHandler.text;
                    Debug.Log("External IP Address: " + m_ExternalIPAddress);
                }
                else
                {
                    Debug.LogError("Error fetching external IP address: " + request.error);
                }
            }
        }

        public void SetUUID(string uuid)
        {
            m_UUID = uuid;
        }

    }


    public class FirebaseLog
    {
        public class LearningLog
        {
            public int EpisodeCount;
            public int EpisodeStepCount;
            public int TotalStepCount;
            public string Time;
            public int HintAction;
            public int PlayerAction;
            public string InstanceUUID;
            public float DecisionTime;
            public bool HintShown;
            public (int CellType, int SpecialType)[,] Board;
            public string[] MatchEvent;
            public SkillKnowledge SkillKnowledge;
            public int LastPCGTime;

            public Dictionary<string, object> ToDict()
            {
                // Reigster all local variables
                Dictionary<string, object> dict = new Dictionary<string, object>();
                dict.Add("EpisodeCount", EpisodeCount);
                dict.Add("EpisodeStepCount", EpisodeStepCount);
                dict.Add("TotalStepCount", TotalStepCount);
                dict.Add("Time", Time);
                dict.Add("InstanceUUID", InstanceUUID);
                dict.Add("DecisionTime", DecisionTime);
                dict.Add("HintAction", HintAction);
                dict.Add("HintShown", HintShown);
                dict.Add("PlayerAction", PlayerAction);
                dict.Add("CurrentMatches", SkillKnowledge.CurrentMatchCounts);
                dict.Add("CurrentLearned", SkillKnowledge.ManualCheck);
                dict.Add("Board", Board);
                dict.Add("MatchEvent", MatchEvent);
                dict.Add("PCGTime", LastPCGTime);
                dict.Add("SeenMatches", SkillKnowledge.SeenMatches);
                dict.Add("SeenDestroy", SkillKnowledge.SeenDestroys);

                return dict;
            }
        }

        public class QuizLog
        {
            public int QuestionNumber;
            public string QuizFile;
            public int PlayerAction;
            public float DecisionTime;
            public string Time;

            public Dictionary<string, object> ToDict()
            {
                // Reigster all local variables
                Dictionary<string, object> dict = new Dictionary<string, object>();
                dict.Add("QuestionNumber", QuestionNumber);
                dict.Add("QuizFile", QuizFile);
                dict.Add("DecisionTime", DecisionTime);
                dict.Add("PlayerAction", PlayerAction);
                dict.Add("Time", Time);

                return dict;
            }
        }


    }


}
