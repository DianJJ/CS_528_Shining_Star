﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;

public class ScreenConfigCalc : MonoBehaviour {

    ArrayList displayObjects;
    [SerializeField] bool regenerateDisplayWall;
    float regenerationRestoreRotationDelay = 1;
    float regenerationRestoreRotationTimer;
    Vector3 originalOrientation;

    [Header("Display Parameters")]
    [SerializeField] GameObject display = null;
    [SerializeField] float displayWidthIncBorders = 1027; // mm
    [SerializeField] float displayHeightIncBorders = 581; // mm

    [SerializeField] float borderTop = 4; // mm
    [SerializeField] float borderBottom = 2; // mm
    [SerializeField] float borderLeft = 4; // mm
    [SerializeField] float borderRight = 2; // mm

    [Header("Wall Parameters")]
    [SerializeField] float frontDisplayToTrackingOrigin = 3240; // mm
    [SerializeField] float displayToFloor = 293; // mm

    [SerializeField] int nDisplayColumns = 18;
    [SerializeField] int centerColumnOffset = -9;
    [SerializeField] int displaysPerColumn = 4;

    [Header("Flat Wall Mode")]
    [SerializeField] bool flatWall = false;
    [SerializeField] float wallOffset = 0; // mm
    [SerializeField] float displayRotation = 0; // mm

    [SerializeField] Transform trackingOrigin = null;

    float angle;

    float originX;
    float originY;
    float originZ;

    [SerializeField] float angleOffset = 0.01f;

    [Header("Rendering")]
    [SerializeField]
    bool simulateDisplays = false;
    bool last_simulateDisplaysState = false;

    public ScreenMaskGeneration generateScreenMask;

    public enum StereoscopicView { Mono, Left, Right };
    [SerializeField]
    StereoscopicView stereoView = StereoscopicView.Mono;

    [SerializeField]
    Material floorMaterial = null;

    [SerializeField]
    CAVE2ScreenMaskRenderer.RenderMode floorRenderMode = CAVE2ScreenMaskRenderer.RenderMode.Background;
    CAVE2ScreenMaskRenderer.RenderMode last_floorRenderMode;

    public enum ConfigOutput { None, All, getReal3D };
    public enum NodeArrangement { Sequential, Even, Odd };
    public enum ScreenMaskGeneration { None, GameObjectOnly, GameObjectAndAsset };

    [Header("Display Config Generator")]
    
    [SerializeField] NodeArrangement nodeArrangement = NodeArrangement.Sequential;

    [SerializeField] string nodeName = "lyra";
    public ConfigOutput outputConfig = ConfigOutput.None;

    GameObject screenMask;

    // Mesh Generation
    List<Vector3> CAVE2ScreenMaskVerticies;
    List<Vector2> CAVE2ScreenMaskUV;
    List<int> CAVE2ScreenMaskTriangles;

    Vector3 initPosition;
    Quaternion initRotation;
    void Start()
    {
        displayObjects = new ArrayList();
        CAVE2ScreenMaskVerticies = new List<Vector3>();
        CAVE2ScreenMaskUV = new List<Vector2>();
        CAVE2ScreenMaskTriangles = new List<int>();

        if (outputConfig != ConfigOutput.None)
        {
            initPosition = transform.position;
            initRotation = transform.rotation;
            transform.position = Vector3.zero;
            transform.rotation = Quaternion.identity;
        }
        GenerateCAVE2();
        if (outputConfig != ConfigOutput.None)
        {
            transform.position = initPosition;
            transform.rotation = initRotation;
        }

        last_floorRenderMode = floorRenderMode;
        last_simulateDisplaysState = simulateDisplays;
    } 

    void GenerateCAVE2() {
        if( transform.Find("Floor") )
        {
            transform.Find("Floor").transform.localScale = new Vector3(frontDisplayToTrackingOrigin, 0, frontDisplayToTrackingOrigin) / 500.0f;
        }

        angle = 2.0f * Mathf.Atan(displayWidthIncBorders / 2.0f / frontDisplayToTrackingOrigin);
        
        float displayPixelWidth = displayWidthIncBorders - borderLeft - borderRight;
        float displayPixelHeight = displayHeightIncBorders - borderTop - borderBottom;

        
        float dispBottomToOrigin = (displayPixelHeight / 2.0f) + borderBottom;
        float dispTopToOrigin = (displayPixelHeight / 2.0f) + borderTop;

        
        int currentNode = 1;
        if(nodeArrangement == NodeArrangement.Even)
        {
            currentNode = 2;
        }

        ArrayList getReal3DConfig = new ArrayList();

        // Mesh Origin (Floor)
        CAVE2ScreenMaskVerticies.Add(Vector3.zero);
        CAVE2ScreenMaskUV.Add(Vector2.zero);

        // Mesh Origin (Ceiling)
        CAVE2ScreenMaskVerticies.Add(Vector3.zero); // Set later
        CAVE2ScreenMaskUV.Add(Vector2.zero);

        Vector3[] entranceVert = new Vector3[4];
        int lastCeilingVertIndex = 0;
        for (int i = centerColumnOffset; i < nDisplayColumns + centerColumnOffset; i++)
        {
            float ang = angle * i + angleOffset;
            float originX = frontDisplayToTrackingOrigin * (Mathf.Sin(ang));
            float originY = frontDisplayToTrackingOrigin * (Mathf.Cos(ang));
            float h = ang * 360.0f / (2.0f * Mathf.PI);

            if (flatWall)
            {
                originX = (wallOffset - displayWidthIncBorders * i) * Mathf.Cos(angleOffset) + frontDisplayToTrackingOrigin * Mathf.Sin(angleOffset);
                originY = (wallOffset - displayWidthIncBorders * i) * Mathf.Sin(angleOffset) + frontDisplayToTrackingOrigin * Mathf.Cos(angleOffset);
                h = displayRotation;
            }

            // Convert to CAVE2 coordinate system
            originX *= -1;
            originX /= 1000.0f;
            originY /= 1000.0f;

            string nodeNameLabel = nodeName;
            if (currentNode < 10)
                nodeNameLabel += "-0";
            else
                nodeNameLabel += "-";

            for (int j = 0; j < displaysPerColumn; j++)
            {
                // Display center
                float originZ = displayToFloor + (displaysPerColumn - j - 1) * dispBottomToOrigin + (displaysPerColumn - j) * dispTopToOrigin;
                originZ /= 1000.0f;

                GameObject g = Instantiate(display, new Vector3(-originX, originZ, originY), Quaternion.Euler(0, h, 0)) as GameObject;
                g.transform.parent = transform;
                g.transform.localPosition = new Vector3(-originX, originZ, originY);
                g.transform.localRotation = Quaternion.Euler(0, h, 0);
                g.transform.Find("Borders").localScale = new Vector3(displayWidthIncBorders / 1000.0f, displayHeightIncBorders / 1000.0f, 0.05f);

                g.SendMessage("SetCAVE2DisplayEnabled", simulateDisplays, SendMessageOptions.DontRequireReceiver);
                g.SendMessage("SetRowID", j, SendMessageOptions.DontRequireReceiver);
                g.SendMessage("SetColumnID", (i - centerColumnOffset), SendMessageOptions.DontRequireReceiver);

                g.name = "Display " + nodeNameLabel + currentNode + " " + j;

                displayObjects.Add(g);

                // Convert from world space to relative to tracking orientation
                Vector3 Px_UpperLeft = trackingOrigin.InverseTransformPoint(g.transform.Find("Borders/PixelSpace/Px-UpperLeft").position);
                Vector3 Px_LowerLeft = trackingOrigin.InverseTransformPoint(g.transform.Find("Borders/PixelSpace/Px-LowerLeft").position);
                Vector3 Px_LowerRight = trackingOrigin.InverseTransformPoint(g.transform.Find("Borders/PixelSpace/Px-LowerRight").position);
                Vector3 Px_UpperRight = trackingOrigin.InverseTransformPoint(g.transform.Find("Borders/PixelSpace/Px-UpperRight").position);

                Px_UpperLeft *= 1000.0f;
                Px_LowerLeft *= 1000.0f;
                Px_LowerRight *= 1000.0f;
                Px_UpperRight *= 1000.0f;

                GenerateGetReal3DScreenConfig(nodeNameLabel + currentNode, j, Px_UpperLeft, Px_LowerLeft, Px_LowerRight, ref getReal3DConfig);

                // Mesh generation
                Vector3 v = Vector3.zero;
                Vector2 uv = Vector3.zero;
                if (j == 0) // Top screen
                {
                    if (i == centerColumnOffset) // Left-most column, top display
                    {
                        CAVE2ScreenMaskVerticies[1] = new Vector3(0, Px_UpperLeft.y, 0) / 1000.0f;
                        entranceVert[2] = Px_UpperLeft;
                        entranceVert[3] = new Vector3(Px_UpperLeft.x, 0, Px_UpperLeft.z);
                    }
                    else if (j == 0 && i == nDisplayColumns + centerColumnOffset - 1) // Right-most column, top display
                    {
                        entranceVert[1] = Px_UpperRight;
                        entranceVert[0] = new Vector3(Px_UpperRight.x, 0, Px_UpperRight.z);
                    }

                    // Ceiling
                    v = new Vector3(Px_UpperLeft.x, Px_UpperLeft.y, Px_UpperLeft.z) / 1000.0f;
                    uv = new Vector2(Px_UpperLeft.x, Px_UpperLeft.z) / 1000.0f;
                    CAVE2ScreenMaskVerticies.Add(v);
                    CAVE2ScreenMaskUV.Add(uv);

                    if (CAVE2ScreenMaskVerticies.Count >= 6)
                    {
                        // Floor triangle
                        CAVE2ScreenMaskTriangles.Add(1); // Origin
                        CAVE2ScreenMaskTriangles.Add(CAVE2ScreenMaskVerticies.Count - 1); // Right top
                        CAVE2ScreenMaskTriangles.Add(CAVE2ScreenMaskVerticies.Count - 4); // Left top
                    }

                    // Last column
                    if (i == nDisplayColumns + centerColumnOffset - 1)
                    {
                        lastCeilingVertIndex = CAVE2ScreenMaskVerticies.Count - 1;
                    }

                }
                else if ( j == displaysPerColumn - 1 ) // Bottom screen
                {
                    // Floor
                    v = new Vector3(Px_LowerLeft.x, 0, Px_LowerLeft.z) / 1000.0f;
                    uv = new Vector2(Px_LowerLeft.x, Px_LowerLeft.z) / 1000.0f;
                    CAVE2ScreenMaskVerticies.Add(v);
                    CAVE2ScreenMaskUV.Add(uv);

                    // Base of screen
                    v = new Vector3(Px_LowerLeft.x, Px_LowerLeft.y, Px_LowerLeft.z) / 1000.0f;
                    uv = new Vector2(Px_LowerLeft.x, Px_LowerLeft.y) / 1000.0f;
                    CAVE2ScreenMaskVerticies.Add(v);
                    CAVE2ScreenMaskUV.Add(uv);

                    if (CAVE2ScreenMaskVerticies.Count >= 6)
                    {
                        // Floor triangle
                        CAVE2ScreenMaskTriangles.Add(0); // Origin
                        CAVE2ScreenMaskTriangles.Add(CAVE2ScreenMaskVerticies.Count - 5); // Left floor
                        CAVE2ScreenMaskTriangles.Add(CAVE2ScreenMaskVerticies.Count - 2); // Right floor

                        // Floor to bottom of display
                        CAVE2ScreenMaskTriangles.Add(CAVE2ScreenMaskVerticies.Count - 5); // Left floor
                        CAVE2ScreenMaskTriangles.Add(CAVE2ScreenMaskVerticies.Count - 4); // Left screen base
                        CAVE2ScreenMaskTriangles.Add(CAVE2ScreenMaskVerticies.Count - 1); // Right screen base

                        CAVE2ScreenMaskTriangles.Add(CAVE2ScreenMaskVerticies.Count - 5); // Left floor
                        CAVE2ScreenMaskTriangles.Add(CAVE2ScreenMaskVerticies.Count - 1); // Right screen base
                        CAVE2ScreenMaskTriangles.Add(CAVE2ScreenMaskVerticies.Count - 2); // Right floor
                    }

                    // Last column
                    if (i == nDisplayColumns + centerColumnOffset - 1)
                    {
                        // Floor
                        v = new Vector3(Px_LowerRight.x, 0, Px_LowerRight.z) / 1000.0f;
                        uv = new Vector2(Px_LowerRight.x, Px_LowerRight.z) / 1000.0f;
                        CAVE2ScreenMaskVerticies.Add(v);
                        CAVE2ScreenMaskUV.Add(uv);

                        // Base of screen
                        v = new Vector3(Px_LowerRight.x, Px_LowerRight.y, Px_LowerRight.z) / 1000.0f;
                        uv = new Vector2(Px_LowerRight.x, Px_LowerRight.y) / 1000.0f;
                        CAVE2ScreenMaskVerticies.Add(v);
                        CAVE2ScreenMaskUV.Add(uv);

                        // Floor triangle
                        CAVE2ScreenMaskTriangles.Add(0);
                        CAVE2ScreenMaskTriangles.Add(CAVE2ScreenMaskVerticies.Count - 4); // Left floor
                        CAVE2ScreenMaskTriangles.Add(CAVE2ScreenMaskVerticies.Count - 2); // Right floor

                        // Floor to bottom of display
                        CAVE2ScreenMaskTriangles.Add(CAVE2ScreenMaskVerticies.Count - 4); // Left floor
                        CAVE2ScreenMaskTriangles.Add(CAVE2ScreenMaskVerticies.Count - 3); // Left screen base
                        CAVE2ScreenMaskTriangles.Add(CAVE2ScreenMaskVerticies.Count - 1); // Right screen base

                        CAVE2ScreenMaskTriangles.Add(CAVE2ScreenMaskVerticies.Count - 4); // Left floor
                        CAVE2ScreenMaskTriangles.Add(CAVE2ScreenMaskVerticies.Count - 1); // Right screen base
                        CAVE2ScreenMaskTriangles.Add(CAVE2ScreenMaskVerticies.Count - 2); // Right floor
                    }
                }
            }
            if (nodeArrangement == NodeArrangement.Sequential)
                currentNode += 1;
            else
                currentNode += 2;
        }

        if (generateScreenMask != ScreenMaskGeneration.None)
        {
            // Entrance
            Vector3 v2 = new Vector3(entranceVert[0].x, entranceVert[0].y, entranceVert[0].z) / 1000.0f;
            Vector2 uv2 = new Vector2(entranceVert[0].x, entranceVert[0].z) / 1000.0f;
            CAVE2ScreenMaskVerticies.Add(v2);
            CAVE2ScreenMaskUV.Add(uv2);

            v2 = new Vector3(entranceVert[1].x, entranceVert[1].y, entranceVert[1].z) / 1000.0f;
            uv2 = new Vector2(entranceVert[1].x, entranceVert[1].z) / 1000.0f;
            CAVE2ScreenMaskVerticies.Add(v2);
            CAVE2ScreenMaskUV.Add(uv2);

            v2 = new Vector3(entranceVert[2].x, entranceVert[2].y, entranceVert[2].z) / 1000.0f;
            uv2 = new Vector2(entranceVert[2].x, entranceVert[2].z) / 1000.0f;
            CAVE2ScreenMaskVerticies.Add(v2);
            CAVE2ScreenMaskUV.Add(uv2);

            v2 = new Vector3(entranceVert[3].x, entranceVert[3].y, entranceVert[3].z) / 1000.0f;
            uv2 = new Vector2(entranceVert[3].x, entranceVert[3].z) / 1000.0f;
            CAVE2ScreenMaskVerticies.Add(v2);
            CAVE2ScreenMaskUV.Add(uv2);

            CAVE2ScreenMaskTriangles.Add(CAVE2ScreenMaskVerticies.Count - 4); // Entrance right floor
            CAVE2ScreenMaskTriangles.Add(CAVE2ScreenMaskVerticies.Count - 3); // Entrance right top
            CAVE2ScreenMaskTriangles.Add(CAVE2ScreenMaskVerticies.Count - 2); // Entrance left top

            CAVE2ScreenMaskTriangles.Add(CAVE2ScreenMaskVerticies.Count - 4); // Entrance right floor
            CAVE2ScreenMaskTriangles.Add(CAVE2ScreenMaskVerticies.Count - 2); // Entrance left top
            CAVE2ScreenMaskTriangles.Add(CAVE2ScreenMaskVerticies.Count - 1); // Entrance left floor

            // Entrance floor
            CAVE2ScreenMaskTriangles.Add(0); // Origin
            CAVE2ScreenMaskTriangles.Add(CAVE2ScreenMaskVerticies.Count - 4); // Entrance right floor
            CAVE2ScreenMaskTriangles.Add(CAVE2ScreenMaskVerticies.Count - 1); // Entrance left floor

            // Entrance ceiling
            CAVE2ScreenMaskTriangles.Add(1); // Origin
            CAVE2ScreenMaskTriangles.Add(CAVE2ScreenMaskVerticies.Count - 2); // Entrance left top
            CAVE2ScreenMaskTriangles.Add(CAVE2ScreenMaskVerticies.Count - 3); // Entrance right top

            // Last column ceiling
            CAVE2ScreenMaskTriangles.Add(1); // Origin (Ceiling)
            CAVE2ScreenMaskTriangles.Add(CAVE2ScreenMaskVerticies.Count - 3); // Entrance right top
            CAVE2ScreenMaskTriangles.Add(lastCeilingVertIndex);

            screenMask = new GameObject("CAVE2 Screen Mask");
            screenMask.transform.parent = transform;
            screenMask.transform.localPosition = Vector3.zero;
            screenMask.transform.localRotation = Quaternion.identity;

            if (screenMask.GetComponent<MeshFilter>() == null)
                screenMask.AddComponent<MeshFilter>();
            if (screenMask.GetComponent<MeshRenderer>() == null)
                screenMask.AddComponent<MeshRenderer>();

            if (floorMaterial)
            {
                Mesh mesh = screenMask.GetComponent<MeshFilter>().mesh;
                MeshRenderer meshRenderer = screenMask.GetComponent<MeshRenderer>();

                mesh.Clear();
                meshRenderer.material = floorMaterial;

                mesh.vertices = CAVE2ScreenMaskVerticies.ToArray();
                mesh.uv = CAVE2ScreenMaskUV.ToArray();
                mesh.triangles = CAVE2ScreenMaskTriangles.ToArray();

                UpdateScreenMask();
#if UNITY_EDITOR
                if (generateScreenMask == ScreenMaskGeneration.GameObjectAndAsset)
                {
                    if (!Directory.Exists(Application.dataPath + "/Resources/"))
                    {
                        UnityEditor.AssetDatabase.CreateFolder("Assets", "Resources");
                    }
                    UnityEditor.AssetDatabase.Refresh();
                    if (!Directory.Exists(Application.dataPath + "/Resources/CAVE2/"))
                        UnityEditor.AssetDatabase.CreateFolder("Assets/Resources", "CAVE2");

                    UnityEditor.AssetDatabase.CreateAsset(mesh, "Assets/Resources/CAVE2/CAVE2ScreenMask.asset");
                    UnityEditor.AssetDatabase.SaveAssets();
                    UnityEditor.AssetDatabase.Refresh();

                    UnityEditor.PrefabUtility.SaveAsPrefabAsset(screenMask, "Assets/Resources/CAVE2/CAVE2ScreenMask.prefab");
                }
#endif
            }
        }
        if (outputConfig == ConfigOutput.All || outputConfig == ConfigOutput.getReal3D)
        {
            string[] getReal3DConfigArray = (string[])getReal3DConfig.ToArray(typeof(string));
            Debug.Log(Application.dataPath + "/getReal3D_" + nodeName + "_screenconfig.xml");
            System.IO.File.WriteAllLines(Application.dataPath + "/getReal3D_" + nodeName + "_screenconfig.xml", getReal3DConfigArray);
        }
    }
	
    void GenerateGetReal3DScreenConfig(string nodeNameLabel, int column, Vector3 Px_UpperLeft, Vector3 Px_LowerLeft, Vector3 Px_LowerRight, ref ArrayList outputString)
    {
        outputString.Add("<screen name = \"" + nodeNameLabel + "_" + column + "\" units = \"mm\">");
        outputString.Add("     <ul x=\"" + Px_UpperLeft.x + "\" y=\"" + Px_UpperLeft.y + "\" z=\"" + -Px_UpperLeft.z + "\"/>");
        outputString.Add("     <ll x=\"" + Px_LowerLeft.x + "\" y=\"" + Px_LowerLeft.y + "\" z=\"" + -Px_LowerLeft.z + "\"/>");
        outputString.Add("     <lr x=\"" + Px_LowerRight.x + "\" y=\"" + Px_LowerRight.y + "\" z=\"" + -Px_LowerRight.z + "\"/>");
        outputString.Add("</screen>");
    }

	// Update is called once per frame
	void Update () {
		if(regenerateDisplayWall)
        {
            originalOrientation = gameObject.transform.parent.eulerAngles;

            foreach( GameObject g in displayObjects )
            {
                if(g.GetComponent<CAVE2Display>())
                {
                    // Remove virtual camera before destroying display
                    g.GetComponent<CAVE2Display>().Cleanup();
                }
                Destroy(g);
            }
            Destroy(screenMask);
            displayObjects.Clear();
            CAVE2ScreenMaskVerticies.Clear();
            CAVE2ScreenMaskUV.Clear();
            CAVE2ScreenMaskTriangles.Clear();

            gameObject.transform.parent.eulerAngles = Vector3.zero;

            GenerateCAVE2();
            UpdateScreenMask();
            regenerateDisplayWall = false;

            
            regenerationRestoreRotationTimer = regenerationRestoreRotationDelay;
        }
        else if(regenerationRestoreRotationTimer > 0)
        {
            regenerationRestoreRotationTimer -= Time.deltaTime;
            if(regenerationRestoreRotationTimer <= 0)
            {
                gameObject.transform.parent.eulerAngles = originalOrientation;
            }
        }

        if (last_floorRenderMode != floorRenderMode)
        {
            UpdateScreenMask();
            last_floorRenderMode = floorRenderMode;
        }

        if(last_simulateDisplaysState != simulateDisplays)
        {
            foreach (GameObject g in displayObjects)
            {
                g.GetComponent<CAVE2Display>().enabled = simulateDisplays;
                if (!simulateDisplays)
                    g.GetComponent<CAVE2Display>().RemoveDisplayTexture();
                else
                    g.GetComponent<CAVE2Display>().CreateDisplayTexture();
            }

            last_simulateDisplaysState = simulateDisplays;
        }
    }

    void UpdateScreenMask()
    {
        if (screenMask)
        {
            switch (floorRenderMode)
            {
                case (CAVE2ScreenMaskRenderer.RenderMode.Background):
                    screenMask.GetComponent<Renderer>().sharedMaterial.SetFloat("_ZTest", 2);
                    screenMask.GetComponent<Renderer>().sharedMaterial.SetFloat("_Cull", 2);
                    break;
                case (CAVE2ScreenMaskRenderer.RenderMode.None):
                    screenMask.GetComponent<Renderer>().sharedMaterial.SetFloat("_ZTest", 1);
                    screenMask.GetComponent<Renderer>().sharedMaterial.SetFloat("_Cull", 0);
                    break;
                case (CAVE2ScreenMaskRenderer.RenderMode.Overlay):
                    screenMask.GetComponent<Renderer>().sharedMaterial.SetFloat("_ZTest", 0);
                    screenMask.GetComponent<Renderer>().sharedMaterial.SetFloat("_Cull", 0);
                    break;
            }
        }
    }

    public void RegenerateDisplayWall()
    {
        regenerateDisplayWall = true;
    }

    public StereoscopicView GetStereoscopicView()
    {
        return stereoView;
    }
}
