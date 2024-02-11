using UnityEngine;
using UnityEngine.Rendering;
using System.Collections.Generic;
using System.IO;
using System;

public class ShiningStar : MonoBehaviour
{
    public GameObject starPrefab; // Prefab used to represent stars.
    public string csvFilePath; // CSV file path.

    private List<StarData> starsData = new List<StarData>();
    private List<Constellation> constellations = new List<Constellation>(); 
    private List<LineRenderer> LineRenderers = new List<LineRenderer>();
    private bool constellationLineRendererEnable = true;

    void Start()
    {
        LoadDataFromCSV("Assets/module-omicron/athyg_31_reduced_m10.csv");
        CreateStars();
        LoadConstellations("Assets/module-omicron/constellationship.fab");
        DrawConstellations();
    }
    void Update()
    {
        UpdateConstellationLine();
    }

    void UpdateConstellationLine()
    {
        // The HIP number of stars starts from the third element.
        int k = 0;
        
        for (int i = 0; i < constellations.Count; i++)
        {
            // Find the corresponding star objects.
            for (int j = 0; j < constellations[i].STAR_PAIRS.Count; j++)
            {
               
                GameObject starObject_1 = constellations[i].STAR_PAIRS[j].Item1;

                GameObject starObject_2 = constellations[i].STAR_PAIRS[j].Item2;

                if (starObject_1 != null && starObject_2 != null)
                {
                     k += 1;
                    Debug.Log("updating constellation line");
                    Debug.Log("LineRenderers length:"+LineRenderers.Count);
                    Debug.Log("K:" + k);
                    // Create connecting lines or other geometric shapes to represent constellations between stars.
                    //LineRenderers[k].material = new Material(Shader.Find("Standard"));
                    //LineRenderers[k].material.color = Color.white;
                    if (k < LineRenderers.Count)
                    {
                        Debug.Log("LineRenderers postions1:" + starObject_1.transform.position);
                        Debug.Log("LineRenderers postions2:" + starObject_2.transform.position);
                        LineRenderers[k].SetPosition(0, starObject_1.transform.position);
                        LineRenderers[k].SetPosition(1, starObject_2.transform.position);
                        //LineRenderers[k].(starObject_1.transform.position, starObject_2.transform.position);
                    }

                }
            }
        }
    }

    //public void ToggleConstellationLine(bool showLine)
    //{
    //    constellationLineRenderer.enabled = showLine;
    //}

    void LoadDataFromCSV(string csvFilePath)
    {
        try
        {
            Debug.Log("Start loading csv file");
            string[] csvLines = File.ReadAllLines(csvFilePath);

            for (int i = 1; i < csvLines.Length; i++) // Read from the second line; the first line is typically the header.
            {
                string[] values = csvLines[i].Split(',');

                Debug.Log("Start spliting csv file");
                // Parse the CSV data and create StarData objects.
                StarData star = new StarData();
                Debug.Log("Start loading ID");
                star.ID = int.Parse(values[0]);

                if (!string.IsNullOrEmpty(values[1]) && !string.IsNullOrEmpty(values[3]) && !string.IsNullOrEmpty(values[3]) && !string.IsNullOrEmpty(values[4]) && !string.IsNullOrEmpty(values[5])
                    && !string.IsNullOrEmpty(values[6]) && !string.IsNullOrEmpty(values[7])
                    && !string.IsNullOrEmpty(values[8]) && !string.IsNullOrEmpty(values[9]) && !string.IsNullOrEmpty(values[10]) && !string.IsNullOrEmpty(values[11]
                    ))
                {
                    star.HIP = int.Parse(values[1]);
                    star.DIST = float.Parse(values[2]);
                    star.X0 = float.Parse(values[3])*5;
                    star.Y0 = float.Parse(values[4])*5;
                    star.Z0 = float.Parse(values[5])*5;
                    star.ABSMAG = float.Parse(values[6]);
                    star.MAG = float.Parse(values[7]);
                    star.VX = float.Parse(values[8]) * 1.02269E-6f;
                    star.VY = float.Parse(values[9]) * 1.02269E-6f;
                    star.VZ = float.Parse(values[10]) * 1.02269E-6f;
                    star.SPECT = values[11];
                }

                // Check and add valid star data.
                if ((10.326 < star.DIST) && (star.DIST < 30 * 3.262) && !float.IsNaN(star.HIP) && !string.IsNullOrEmpty(star.SPECT) && !float.IsNaN(star.X0) && !float.IsNaN(star.Y0) && !float.IsNaN(star.Z0))
                {

                    starsData.Add(star);
                    Debug.Log(star);
                }
            }


        }
        catch (Exception e)
        {
            Debug.LogError("Error loading data from CSV: " + e.Message);
        }
    }

    void CreateStars()
    {
        foreach (var starData in starsData)
        {
            // Create star objects based on StarData.
            GameObject starObject = Instantiate(starPrefab, new Vector3(starData.X0, starData.Y0, starData.Z0), Quaternion.identity);
            starObject.name = $"Star_{starData.HIP}";

            // Set star size based on brightness.
            float starSize = Mathf.Clamp(1.0f / starData.ABSMAG, 0.1f, 10.0f);
            starObject.transform.localScale = new Vector3(starSize, starSize, starSize);

            // Set star color based on spectral type.
            Color starColor = GetColorBySpectralType(starData.SPECT);
            starObject.GetComponent<Renderer>().material.color = starColor;

            //Place the star objects in the scene.
            starObject.transform.SetParent(transform);

            starObject.AddComponent<StarOrbit>().InitializeOrbit(starData, transform);
        }
    }

    void LoadConstellations(string fabFilePath)
    {
        string[] constellationLines = File.ReadAllLines(fabFilePath);

        Debug.Log("successful loading the constellation file");



        foreach (var constellationLine in constellationLines)
        {
            string[] constellationData = System.Text.RegularExpressions.Regex.Split(constellationLine, @"\s+");
            //string[] constellationData = constellationLine.Split(' ');
            Constellation constellation = new Constellation();

            // Parse constellation data.
            //Debug.Log("constellationData.Length: " + constellationData.Length);
            //Debug.Log(constellationData[0]);
            //Debug.Log(constellationData[1]);
            string constellationName = constellationData[0];
            int starCount = int.Parse(constellationData[1]);

            constellation.NAME = constellationName;

            constellation.PAIR_NUMBER = starCount;
            Debug.Log("successful parsing the constellation file");

            //The HIP (Hipparcos) number of stars begins from the third element.
            for (int i = 2; i < constellationData.Length - 1; i += 2)
            {
                Debug.Log("i: " + i + ", constellationData.Length: " + constellationData.Length);

                //Debug.Log(constellationData.Length);
                int hipNumber_1 = int.Parse(constellationData[i]);
                //Debug.LogError("Index out of bounds while parsing constellation data.");
                //Debug.Log(i + 1);
                int hipNumber_2 = int.Parse(constellationData[i + 1]);

                //Debug.Log("successful load the hip");
                //Find the corresponding star objects.
                Debug.Log("hipNumber_1" + hipNumber_1);
                Debug.Log("hipNumber_2" + hipNumber_2);
                GameObject starObject_1 = FindStarByHIP(hipNumber_1);

                GameObject starObject_2 = FindStarByHIP(hipNumber_2);

                //Debug.Log("successful find the star by hip");
                if (starObject_1 != null && starObject_1 != null)
                {
                    Debug.Log("successful add the star pair");
                    Tuple<GameObject, GameObject> starpair = Tuple.Create(starObject_1, starObject_2);

                    constellation.STAR_PAIRS.Add(starpair);
                }



                //if (starObject_1 != null && starObject_1 != null)
                //{
                //Create connecting lines or other geometric shapes representing constellations between stars.
                //DrawConstellationLine(starObject_1.transform.position, starObject_2.transform.position);
                //}
                //if (i >= constellationData.Length - 2)
                //{
                //Debug.Log("Index out of bounds while parsing constellation data.");
                //  break;  // 
                //}
            }
            Debug.Log("pair length:"+ constellation.STAR_PAIRS.Count);
            Debug.Log("successful add the constellation");
            if (constellation.STAR_PAIRS.Count == starCount)
            {
                constellations.Add(constellation);
            }
            
        }
    }

    Color GetColorBySpectralType(string spectralType)
    {
        // According to the spectral type, return the color.
        switch (spectralType[0])
        {
            case 'O': return Color.blue;
            case 'B': return Color.cyan;
            case 'A': return Color.white;
            case 'F': return Color.yellow;
            case 'G': return Color.yellow;
            case 'K': return new Color(1.0f, 0.5f, 0.0f); 
            case 'M': return Color.red;
            default: return Color.gray;
        }
    }

    void DrawConstellations()
    {
        // Read the constellation file.

        // The HIP number of the stars starts from the third element.
        for (int i = 0; i < constellations.Count; i++)
            {
            // You're finding the corresponding star objects based on the HIP numbers in the constellation data.
            for (int j = 0; j < constellations[i].STAR_PAIRS.Count; j++)
            {
                GameObject starObject_1 = constellations[i].STAR_PAIRS[j].Item1;

                GameObject starObject_2 = constellations[i].STAR_PAIRS[j].Item2;

                if (starObject_1 != null && starObject_2 != null)
                {
                    // You are attempting to draw lines or other geometric shapes between stars to represent constellations.
                    Debug.Log("successfull draw lines");
                    Debug.Log("star1 position: "+starObject_1.transform.position+ "star2 position: "+starObject_2.transform.position);
                    DrawConstellationLine(starObject_1.transform.position, starObject_2.transform.position);
                }
            }
            }
        
    }

    GameObject FindStarByHIP(int hipNumber)
    {
        //  find the corresponding GameObject in the scene using the HIP (Henry Draper Catalog) number.
        foreach (var starData in starsData)
        {
            if (starData.HIP == hipNumber)
            {
                return GameObject.Find($"Star_{hipNumber}"); 
            }
        }

        return null;
    }

    void DrawConstellationLine(Vector3 starPosition_1, Vector3 starPosition_2)
    {
        // draw lines or geometric shapes between stars to represent constellations. 
        //GameObject constellationLine = new GameObject("ConstellationLine");
        //constellationLine.transform.position = starPosition_1;
        //constellationLine.AddComponent<LineRenderer>();
        Debug.Log("successfull draw lines");
        //LineRenderer lineRenderer = gameObject.AddComponent<LineRenderer>();
        //Debug.Log(lineRenderer);
        //LineRenderer lineRenderer = GetComponent<LineRenderer>();
        GameObject constellationLine = new GameObject("ConstellationLine");
        LineRenderer lineRenderer = constellationLine.AddComponent<LineRenderer>();
        lineRenderer.material = new Material(Shader.Find("Standard"));
        lineRenderer.material.color = Color.white;


        //LineRenderer lineRenderer = gameObject.AddComponent<LineRenderer>();
        //lineRenderer.material = new Material(Shader.Find("Sprites/Default"));
        //lineRenderer.material.color = Color.white;

        if (lineRenderer != null)
        {
            Debug.Log("line renderer is not null");
            
            lineRenderer.positionCount = 2;
            lineRenderer.SetPosition(0, starPosition_1);
            lineRenderer.SetPosition(1, starPosition_2);

            LineRenderers.Add(lineRenderer);
        }

    }

}

// 星星数据类
public class StarData
{
    public int ID;
    public int HIP;
    public float DIST;
    public float X0, Y0, Z0;
    public float ABSMAG, MAG;
    public float VX, VY, VZ;
    public string SPECT;
}


public class Constellation
{
    public string NAME;
    public int PAIR_NUMBER;
    public List<Tuple<GameObject, GameObject>> STAR_PAIRS = new List<Tuple<GameObject, GameObject>>();
}

public class StarOrbit : MonoBehaviour
{
    private StarData starData;
    private Transform center; // The object's Transform, which is the center around which the star rotates.
    private LineRenderer constellationLineRenderer; // LineRenderer used for drawing constellation connecting lines.

    public void InitializeOrbit(StarData starData, Transform center)
    {
        this.starData = starData;
        this.center = center;
    }

    void Update()
    {
        // Calculate the angle of rotation per frame.
        float rotationSpeed = Mathf.Sqrt(starData.VX * starData.VX + starData.VY * starData.VY + starData.VZ * starData.VZ);

        // Rotate the star based on velocity components.
        transform.RotateAround(center.position, new Vector3(starData.VX, starData.VY, starData.VZ), rotationSpeed * Time.deltaTime);

        //UpdateConstellationLine();
    }

    /*void UpdateConstellationLine()
    {
        if (constellationLineRenderer.enabled)
        {
            //
            constellationLineRenderer.SetPosition(0, center.position);
            constellationLineRenderer.SetPosition(1, transform.position);
        }
    }

    public void ToggleConstellationLine(bool showLine)
    {
        constellationLineRenderer.enabled = showLine;
    }*/
 }


