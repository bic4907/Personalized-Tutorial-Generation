using System.Collections;
using UnityEngine;
using UnityEngine.Networking;
using System.Threading;
using System;

public class AssetLoader : MonoBehaviour 
{
    byte[] results;

    public byte[] LoadAssetBundle(string path, Action<string, byte[]> onLoaded)
    {
        // Start the coroutine using the static method

        StartCoroutine(GetAssetBundle(path, onLoaded));


        return results;
    }

    private IEnumerator GetAssetBundle(string path, Action<string, byte[]> onLoaded)
    {
        Debug.Log("Fecthing:" + path);

        UnityWebRequest www = UnityWebRequest.Get(path);
        yield return www.SendWebRequest();

        if (www.result != UnityWebRequest.Result.Success) {
            Debug.Log("Error Fecthing:" + www.error);

        }
        else {
            // Show results as text
            Debug.Log("Good Fecthing:" + path);
 
            // Or retrieve results as binary data
            results = www.downloadHandler.data;

            onLoaded?.Invoke(path, results);
        }
    }
}