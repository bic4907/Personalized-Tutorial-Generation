using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Match3TileSelector : MonoBehaviour
{
    public GameObject emptyTile;
    public GameObject[] tileTypes = new GameObject[0];
    public Material[] materialTypes = new Material[0];
    public GameObject explosionPrefab;
    public GameObject glowPrefab;
    private Dictionary<int, MeshRenderer> tileDict = new Dictionary<int, MeshRenderer>();
    int corutineControlFlag = 0;
    bool isCorutineRunning = false;
    int glowTileOnlyOnce = 0;

    // Start is called before the first frame update
    void Awake()
    {
        for (int i = 0; i < tileTypes.Length; i++)
        {
            tileDict.Add(i, tileTypes[i].GetComponent<MeshRenderer>());
        }

        SetActiveTile(0, 0);
    }

    public void AllTilesOff()
    {
        foreach (var item in tileTypes)
        {
            item.SetActive(false);
        }
    }

    public void SetActiveTile(int typeIndex, int matIndex, bool isHumanControlled = false)
    {
        if (matIndex == -1)
        {
            AllTilesOff();
            emptyTile.SetActive(true);
            corutineControlFlag += 1;
            glowTileOnlyOnce += 1;
            
        }
        else
        {
            emptyTile.SetActive(false);
            for (int i = 0; i < tileTypes.Length; i++)
            {
                if (i == typeIndex)
                {
                    tileTypes[i].SetActive(true);
                    if (i != 6)
                    {
                        tileDict[i].sharedMaterial = materialTypes[matIndex];
                    }
                    if(corutineControlFlag >= 1 && isHumanControlled && !isCorutineRunning)
                    {
                        isCorutineRunning = true;
                        StartCoroutine(ScaleTile(tileDict[i].transform.localScale, i));
                    }
                }
                else
                {
                    tileTypes[i].SetActive(false);
                }
            }
        }
    }
    public void InstantiateTiles(GameObject explosionPrefab, GameObject glowPrefab)
    {
        var tmp = Instantiate(explosionPrefab, this.transform.position, Quaternion.identity);
        tmp.transform.SetParent(this.transform);
        this.explosionPrefab = tmp;

        tmp = Instantiate(glowPrefab, this.transform.position, Quaternion.identity);
        tmp.transform.SetParent(this.transform);
        this.glowPrefab = tmp;
        
        this.explosionPrefab.SetActive(false);
        this.glowPrefab.SetActive(false);
    }
    public void ExplodeTile()
    {
        explosionPrefab.SetActive(false);
        explosionPrefab.SetActive(true);
    }
    public void GlowTile()
    {
        glowPrefab.SetActive(false);
        glowPrefab.SetActive(true);
    }
    public void StopGlow()
    {
        glowPrefab.SetActive(false);
    }
    //corutine to scale the tile
    public IEnumerator ScaleTile(Vector3 scale, int i)
    {
        float time = 0;
        tileTypes[i].transform.localScale = transform.localScale * 0.1f;
        while (time < 3)
        {
            time += Time.deltaTime;
            tileTypes[i].transform.localScale = Vector3.Lerp(tileTypes[i].transform.localScale, scale, time / 6);
            yield return null;
        }
        isCorutineRunning = false;
        corutineControlFlag = 0;
    }
}
