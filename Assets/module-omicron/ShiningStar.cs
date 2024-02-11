using UnityEngine;
using UnityEngine.Rendering;
using System.Collections.Generic;
using System.IO;
using System;

public class ShiningStar : MonoBehaviour
{
    public GameObject starPrefab; // 预制体用于表示星星
    public string csvFilePath; // CSV文件路径

    private List<StarData> starsData = new List<StarData>();
    private List<Constellation> constellations = new List<Constellation>(); 
    private List<LineRenderer> LineRenderers = new List<LineRenderer>();
    private bool constellationLineRendererEnable = true;

    void Start()
    {
        LoadDataFromCSV("Assets/module-omicron/athyg_31_reduced_m10.csv");
        CreateStars();
        DrawConstellations();
    }
    void Update()
    {
        UpdateConstellationLine();
    }

    void UpdateConstellationLine()
    {
        // 从第三个元素开始是星星的 hip 编号
        int k = 0;
        for (int i = 0; i < constellations.Count; i++)
        {
            // 找到对应的星星对象
            for (int j = 0; j < constellations[i].STAR_PAIRS.Count; j++)
            {
                k += 1;
                GameObject starObject_1 = constellations[i].STAR_PAIRS[j].Item1;

                GameObject starObject_2 = constellations[i].STAR_PAIRS[j].Item2;

                if (starObject_1 != null && starObject_1 != null)
                {
                    // 在星星之间创建连接线或其他表示星座的几何图形
                    LineRenderers[k].SetPosition(0, starObject_1.transform.position);
                    LineRenderers[k].SetPosition(1, starObject_2.transform.position);
                    //LineRenderers[k].(starObject_1.transform.position, starObject_2.transform.position);
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

            for (int i = 1; i < csvLines.Length; i++) // 从第二行开始读取，第一行通常是标题
            {
                string[] values = csvLines[i].Split(',');

                Debug.Log("Start spliting csv file");
                // 解析CSV数据并创建StarData对象
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
                    star.X0 = float.Parse(values[3]);
                    star.Y0 = float.Parse(values[4]);
                    star.Z0 = float.Parse(values[5]);
                    star.ABSMAG = float.Parse(values[6]);
                    star.MAG = float.Parse(values[7]);
                    star.VX = float.Parse(values[8]);
                    star.VY = float.Parse(values[9]);
                    star.VZ = float.Parse(values[10]);
                    star.SPECT = values[11];
                }

                // 检查并添加有效的星星数据
                if ((10 * 3.262 < star.DIST) && (star.DIST < 25 * 3.262) && !float.IsNaN(star.HIP) && !string.IsNullOrEmpty(star.SPECT) && !float.IsNaN(star.X0) && !float.IsNaN(star.Y0) && !float.IsNaN(star.Z0))
                {

                    starsData.Add(star);
                    Debug.Log(star);
                }
            }

            string[] constellationLines = File.ReadAllLines("Assets/module-omicron/constellationship.fab");

            Debug.Log("successful loading the constellation file");
            Constellation constellation = new Constellation();


            foreach (var constellationLine in constellationLines)
            {
                string[] constellationData = System.Text.RegularExpressions.Regex.Split(constellationLine, @"\s+");
                //string[] constellationData = constellationLine.Split(' ');


                // 解析星座数据
                Debug.Log("constellationData.Length: " + constellationData.Length);
                Debug.Log(constellationData[0]);
                Debug.Log(constellationData[1]);
                string constellationName = constellationData[0];
                int starCount = int.Parse(constellationData[1]);

                constellation.NAME = constellationName;

                constellation.PAIR_NUMBER = starCount;
                Debug.Log("successful parsing the constellation file");

                // 从第三个元素开始是星星的 hip 编号
                for (int i = 2; i < constellationData.Length-1; i += 2)
                {
                    Debug.Log("i: " + i + ", constellationData.Length: " + constellationData.Length);

                    //Debug.Log(constellationData.Length);
                    int hipNumber_1 = int.Parse(constellationData[i]);
                    //Debug.LogError("Index out of bounds while parsing constellation data.");
                    //Debug.Log(i + 1);
                    int hipNumber_2 = int.Parse(constellationData[i + 1]);
                    
                    //Debug.Log("successful load the hip");
                    // 找到对应的星星对象
                    GameObject starObject_1 = FindStarByHIP(hipNumber_1);

                    GameObject starObject_2 = FindStarByHIP(hipNumber_2);

                    //Debug.Log("successful find the star by hip");

                    Tuple<GameObject, GameObject> starpair = Tuple.Create(starObject_1, starObject_2);

                    constellation.STAR_PAIRS.Add(starpair);

                    //Debug.Log("successful add the star pair");

                    //if (starObject_1 != null && starObject_1 != null)
                    //{
                    // 在星星之间创建连接线或其他表示星座的几何图形
                    //DrawConstellationLine(starObject_1.transform.position, starObject_2.transform.position);
                    //}
                    if(i >= constellationData.Length - 2)
                    {
                        Debug.Log("Index out of bounds while parsing constellation data.");
                        break;  // 终止循环或采取其他适当的措施
                    }
                }
                Debug.Log("successful add the constellation");
                constellations.Add(constellation);
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
            // 根据StarData创建星星对象
            GameObject starObject = Instantiate(starPrefab, new Vector3(starData.X0, starData.Y0, starData.Z0), Quaternion.identity);
            starObject.name = $"Star_{starData.HIP}";

            // 根据亮度设置星星的大小
            float starSize = Mathf.Clamp(1.0f / starData.ABSMAG, 0.1f, 10.0f);
            starObject.transform.localScale = new Vector3(starSize, starSize, starSize);

            // 根据光谱类型设置星星的颜色
            Color starColor = GetColorBySpectralType(starData.SPECT);
            starObject.GetComponent<Renderer>().material.color = starColor;

            // 将星星对象放置在场景中
            starObject.transform.SetParent(transform);

            starObject.AddComponent<StarOrbit>().InitializeOrbit(starData, transform);
        }
    }

    Color GetColorBySpectralType(string spectralType)
    {
        // 根据光谱类型返回颜色
        // 这里简单示例，您可能需要创建更详细的颜色映射表
        switch (spectralType[0])
        {
            case 'O': return Color.blue;
            case 'B': return Color.cyan;
            case 'A': return Color.white;
            case 'F': return Color.yellow;
            case 'G': return Color.yellow;
            case 'K': return new Color(1.0f, 0.5f, 0.0f); // 橙色
            case 'M': return Color.red;
            default: return Color.gray;
        }
    }

    void DrawConstellations()
    {
        // 读取星座文件

            // 从第三个元素开始是星星的 hip 编号
            for (int i = 0; i < constellations.Count; i++)
            {
            // 找到对应的星星对象
            for (int j = 0; j < constellations[i].STAR_PAIRS.Count; j++)
            {
                GameObject starObject_1 = constellations[i].STAR_PAIRS[j].Item1;

                GameObject starObject_2 = constellations[i].STAR_PAIRS[j].Item2;

                if (starObject_1 != null && starObject_1 != null)
                {
                    // 在星星之间创建连接线或其他表示星座的几何图形
                    DrawConstellationLine(starObject_1.transform.position, starObject_2.transform.position);
                }
            }
            }
        
    }

    GameObject FindStarByHIP(int hipNumber)
    {
        // 根据 hip 编号查找星星对象
        foreach (var starData in starsData)
        {
            if (starData.HIP == hipNumber)
            {
                return GameObject.Find($"Star_{hipNumber}"); // 此处假设星星对象的名称类似于 "Star_12345"
            }
        }

        return null;
    }

    void DrawConstellationLine(Vector3 starPosition_1, Vector3 starPosition_2)
    {
        // 在星星位置创建连接线或其他表示星座的几何图形
        // 这可以使用 LineRenderer 组件或其他 Unity 组件来实现
        // 此处仅做示例，具体实现取决于您的需求
        //GameObject constellationLine = new GameObject("ConstellationLine");
        //constellationLine.transform.position = starPosition_1;
        //constellationLine.AddComponent<LineRenderer>();
        LineRenderer lineRenderer = GetComponent<LineRenderer>();
        LineRenderers.Add(lineRenderer);
        lineRenderer.positionCount = 2;
        lineRenderer.SetPosition(0, starPosition_1);
        lineRenderer.SetPosition(1, starPosition_2);
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
    private Transform center; // 物体的Transform，即星星绕着的中心点
    private LineRenderer constellationLineRenderer; // 用于绘制星座连接线的LineRenderer

    public void InitializeOrbit(StarData starData, Transform center)
    {
        this.starData = starData;
        this.center = center;
    }

    void Update()
    {
        // 计算每帧旋转的角度
        float rotationSpeed = Mathf.Sqrt(starData.VX * starData.VX + starData.VY * starData.VY + starData.VZ * starData.VZ);

        // 根据速度分量旋转星星
        transform.RotateAround(center.position, new Vector3(starData.VX, starData.VY, starData.VZ), rotationSpeed * Time.deltaTime);

        //UpdateConstellationLine();
    }

    /*void UpdateConstellationLine()
    {
        if (constellationLineRenderer.enabled)
        {
            // 设置连接线的起始点和终点
            constellationLineRenderer.SetPosition(0, center.position);
            constellationLineRenderer.SetPosition(1, transform.position);
        }
    }

    public void ToggleConstellationLine(bool showLine)
    {
        constellationLineRenderer.enabled = showLine;
    }*/
 }


