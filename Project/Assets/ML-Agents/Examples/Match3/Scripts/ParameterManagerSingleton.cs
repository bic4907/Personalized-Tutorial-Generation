using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ParameterManagerSingleton
{

    public static ParameterManagerSingleton Instance;
    private Hashtable ParsedArgs;


    public static ParameterManagerSingleton GetInstance()
    {
        if (Instance == null)
        {
            Instance = new ParameterManagerSingleton();
            Instance.Initialize();
        }
        return Instance;
    }

    // Start is called before the first frame update
    void Initialize()
    {
        ParsedArgs = new Hashtable();
        ParseCommandlineArgs();
        RegisterDefaultValues();

        // Make Log folder if not exists
        if (!System.IO.Directory.Exists(ParsedArgs["logPath"].ToString())) {
            System.IO.Directory.CreateDirectory(ParsedArgs["logPath"].ToString());
        }

        Debug.Log(this);
    }

    private void RegisterDefaultValues()
    {
        // Check key existance in ParsedArgs
        // If not, add default value
        if (!ParsedArgs.ContainsKey("runId")) { ParsedArgs.Add("runId", "default-id"); }
        if (!ParsedArgs.ContainsKey("logPath")) { ParsedArgs.Add("logPath", Application.dataPath + "/ML-Agents/Examples/Match3/Logs/"); }
        // if (!ParsedArgs.ContainsKey("objective")) { ParsedArgs.Add("objective", "score"); }
    }

    private void ParseCommandlineArgs()
    {
        // Parse command line arguments and save it on a hashtable
        string[] args = System.Environment.GetCommandLineArgs();


        int idx = 0;
        while (idx < args.Length)
        {
            if (args[idx].Contains("--runId"))
            {
                ParsedArgs.Add("runId", args[++idx]);
            }
            else if (args[idx].Contains("--logPath"))
            {
                ParsedArgs.Add("logPath", args[++idx]);
            }
            else if (args[idx].Contains("--targetPlayer"))
            {
                ParsedArgs.Add("targetPlayer", args[++idx]);
            }
            else if (args[idx].Contains("--method"))
            {
                ParsedArgs.Add("method",  args[++idx]);
            }
            else if (args[idx].Contains("--mctsSimulation"))
            {
                ParsedArgs.Add("mctsSimulation",  args[++idx]);
            }
            else if (args[idx].Contains("--objective"))
            {
                ParsedArgs.Add("objective",  args[++idx]);
            }
            else if (args[idx].Contains("--targetEpisodeCount"))
            {
                ParsedArgs.Add("targetEpisodeCount", args[++idx]);
            }
            else if (args[idx].Contains("--simpleEffect"))
            {
                ParsedArgs.Add("simpleEffect", true);
            }
            else if (args[idx].Contains("--knowledgeAlmostRatio"))
            {
                ParsedArgs.Add("knowledgeAlmostRatio", args[++idx]);
            }
            else if (args[idx].Contains("--playerDepth"))
            {
                ParsedArgs.Add("playerDepth", args[++idx]);
            }
            else if (args[idx].Contains("--samplingNum"))
            {
                ParsedArgs.Add("samplingNum", args[++idx]);
            }
            else if (args[idx].Contains("--greedyActionRatio"))
            {
                ParsedArgs.Add("greedyActionRatio", args[++idx]);
            }
            else if (args[idx].Contains("--evolutionNum"))
            {
                ParsedArgs.Add("evolutionNum", args[++idx]);
            }

            idx++;
        }
   }

    public override string ToString()
    {
        string result = "[ParameterManagerSingleton]\n";
        foreach (DictionaryEntry entry in ParsedArgs)
        {
            result += entry.Key + " : " + entry.Value + "\n";
        }
        return result;
    }

    #nullable enable
    public object? GetParam(string key)
    {
        if(ParsedArgs.ContainsKey(key)) {
            return ParsedArgs[key].ToString();
        } else {
            return null;
        }
    }
    #nullable disable

    public bool HasParam(string key)
    {
        return ParsedArgs.ContainsKey(key);
    }

    public bool IsSimpleSpecialEffectMode()
    {
        return true;
        // return ParsedArgs.ContainsKey("simpleEffect");
    }

    // Update is called once per frame
    void Update()
    {

    }

}
