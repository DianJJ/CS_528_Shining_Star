﻿/**************************************************************************************************
 * StereoscopicCamera.cs
 * 
 * Generates a stereoscopic camera from a main camera/
 *-------------------------------------------------------------------------------------------------
 * Copyright 2018-2022   		Electronic Visualization Laboratory, University of Illinois at Chicago
 * Authors:										
 *  Arthur Nishimoto		anishimoto42@gmail.com
 *-------------------------------------------------------------------------------------------------
 * Copyright (c) 2018-2022, Electronic Visualization Laboratory, University of Illinois at Chicago
 * All rights reserved.
 * Redistribution and use in source and binary forms, with or without modification, are permitted 
 * provided that the following conditions are met:
 * 
 * Redistributions of source code must retain the above copyright notice, this list of conditions 
 * and the following disclaimer. Redistributions in binary form must reproduce the above copyright 
 * notice, this list of conditions and the following disclaimer in the documentation and/or other 
 * materials provided with the distribution. 
 * 
 * THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS "AS IS" AND ANY EXPRESS OR 
 * IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO THE IMPLIED WARRANTIES OF MERCHANTABILITY AND 
 * FITNESS FOR A PARTICULAR PURPOSE ARE DISCLAIMED. IN NO EVENT SHALL THE COPYRIGHT OWNER OR 
 * CONTRIBUTORS BE LIABLE FOR ANY DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR CONSEQUENTIAL 
 * DAMAGES (INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE  GOODS OR SERVICES; LOSS OF 
 * USE, DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER CAUSED AND ON ANY THEORY OF LIABILITY, 
 * WHETHER IN CONTRACT, STRICT LIABILITY, OR TORT (INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN 
 * ANY WAY OUT OF THE USE OF THIS SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.
 *************************************************************************************************/

using UnityEngine;
using System.Collections;

public class StereoscopicCamera : MonoBehaviour {

    [SerializeField]
    Material stereoscopicMaterial = null;

    [SerializeField]
    bool generateCameras = false;

    public enum StereoscopicMode { Left, Right, Interleaved, Checkerboard, SideBySide };
    [SerializeField]
    StereoscopicMode stereoMode = StereoscopicMode.Interleaved;

    [SerializeField]
    Vector2 outputResolution = new Vector2(1366, 768);

    [SerializeField]
    float eyeSeparation = 0.065f;

    GameObject leftEye;
    GameObject rightEye;

    RenderTexture leftTexture;
    RenderTexture rightTexture;

    [SerializeField]
    Vector2 textureScale = Vector2.one;

    [SerializeField]
    Vector2 textureOffset;

    [SerializeField]
    bool invertEyes;

    [SerializeField]
    bool regenerate;

    [SerializeField]
    bool update;

    bool camerasGenerated;

    // Use this for initialization
    void Start () {
        if (!update)
        {
            ForceSetupStereoCameras();
        }
    }
	
    public void ForceSetupStereoCameras()
    {
        if (generateCameras && transform.parent.GetComponent<StereoscopicCamera>() == null)
        {
            SetupStereoCameras();

            if (transform.parent.name == "rightEye")
            {
                Destroy(gameObject);
            }

            GetComponent<Camera>().targetTexture = null;
        }
        else
        {
            Destroy(GetComponent<StereoscopicCamera>());
        }
        update = true;
    }

    bool RebuildTextures()
    {
        // Check if eye objects exist first, if not, may still be initializing (in conjunction with
        // GeneralizedPerspectiveProjection, in this case wait and try again next Update.
        if (leftEye && rightEye)
        {
            if(leftTexture)
            {
                Destroy(leftTexture);
            }
            if (rightTexture)
            {
                Destroy(rightTexture);
            }

            // Setup stereo materials and render textures
            leftTexture = new RenderTexture((int)outputResolution.x, (int)outputResolution.y, 24);
            rightTexture = new RenderTexture((int)outputResolution.x, (int)outputResolution.y, 24);

            leftEye.GetComponent<Camera>().targetTexture = leftTexture;
            rightEye.GetComponent<Camera>().targetTexture = rightTexture;

            stereoscopicMaterial.SetFloat("_RenderWidth", outputResolution.x);
            stereoscopicMaterial.SetFloat("_RenderHeight", outputResolution.y);

            stereoscopicMaterial.SetTexture("_LeftTex", leftTexture);
            stereoscopicMaterial.SetTexture("_RightTex", rightTexture);

            return true;
        }
        return false;
    }

    // Update is called once per frame
    void Update() {
        if (regenerate)
        {
            if (RebuildTextures())
            {
                regenerate = false;
            }
        }

        // In general, these should never be null, unless generated at runtime
        // and may be null for a frame or two
        if (leftEye && rightEye)
        {
            leftEye.transform.localPosition = Vector3.left * eyeSeparation / 2.0f;
            rightEye.transform.localPosition = Vector3.right * eyeSeparation / 2.0f;

            if (GetComponent<GeneralizedPerspectiveProjection>())
            {
                leftEye.GetComponent<GeneralizedPerspectiveProjection>().SetEyeOffset(leftEye.transform.localPosition);
                rightEye.GetComponent<GeneralizedPerspectiveProjection>().SetEyeOffset(rightEye.transform.localPosition);
            }

            stereoscopicMaterial.SetTextureOffset("_LeftTex", textureOffset);
            stereoscopicMaterial.SetTextureOffset("_RightTex", textureOffset);
            stereoscopicMaterial.SetTextureScale("_LeftTex", textureScale);
            stereoscopicMaterial.SetTextureScale("_RightTex", textureScale);
            stereoscopicMaterial.SetTexture("_LeftTex", leftTexture);
            stereoscopicMaterial.SetTexture("_RightTex", rightTexture);

            stereoscopicMaterial.SetFloat("_InvertEyes", invertEyes ? 1.0f : 0.0f);

            if (update)
            {
                if (stereoMode == StereoscopicMode.SideBySide)
                {
                    if (invertEyes)
                    {
                        rightEye.GetComponent<Camera>().rect = new Rect(0, 0, 0.5f, 1);
                        leftEye.GetComponent<Camera>().rect = new Rect(0.5f, 0, 0.5f, 1);
                    }
                    else
                    {
                        leftEye.GetComponent<Camera>().rect = new Rect(0, 0, 0.5f, 1);
                        rightEye.GetComponent<Camera>().rect = new Rect(0.5f, 0, 0.5f, 1);
                    }
                }
                else
                {
                    leftEye.GetComponent<Camera>().rect = new Rect(0, 0, 1, 1);
                    rightEye.GetComponent<Camera>().rect = new Rect(0, 0, 1, 1);
                }

                SetStereoMode(stereoMode);
                update = false;
            }
        }

        if (GetComponent<GeneralizedPerspectiveProjection>().enabled == false)
        {
            // Re-enable script for one update loop to prevent jittering
            GetComponent<GeneralizedPerspectiveProjection>().enabled = true;
        }
    }

    void SetupStereoCameras()
    {
        // Create the gameobjects for the eyes
        leftEye = Instantiate(gameObject, transform) as GameObject;
        leftEye.name = "leftEye";

        // Cloning the leftEye instead of the source since cloning
        // the source now will add another left eye
        rightEye = Instantiate(leftEye, transform) as GameObject;
        rightEye.name = "rightEye";

        // If this object has a GeneralizedPerspectiveProjection
        // Set head position to eyes
        if (GetComponent<GeneralizedPerspectiveProjection>())
        {
            // Disable head offset since eyes since we're calculating that above to include eye separation
            leftEye.GetComponent<GeneralizedPerspectiveProjection>().DisablePosition();
            rightEye.GetComponent<GeneralizedPerspectiveProjection>().DisablePosition();

            // Disable script for one update loop to prevent jittering
            GetComponent<GeneralizedPerspectiveProjection>().enabled = false;
        }

        // Set the eye separation
        leftEye.transform.localPosition = Vector3.left * eyeSeparation / 2.0f;
        rightEye.transform.localPosition = Vector3.right * eyeSeparation / 2.0f;

        // Cleanup unnecessary duplicated components
        Destroy(leftEye.GetComponent<AudioListener>());
        Destroy(rightEye.GetComponent<AudioListener>());

        // Setup stereo materials and render textures
        leftTexture = new RenderTexture((int)outputResolution.x, (int)outputResolution.y, 24);
        rightTexture = new RenderTexture((int)outputResolution.x, (int)outputResolution.y, 24);

        stereoscopicMaterial.SetTextureOffset("_LeftTex", textureOffset);
        stereoscopicMaterial.SetTextureOffset("_RightTex", textureOffset);
        stereoscopicMaterial.SetTextureScale("_LeftTex", textureScale);
        stereoscopicMaterial.SetTextureScale("_RightTex", textureScale);
        stereoscopicMaterial.SetTexture("_LeftTex", leftTexture);
        stereoscopicMaterial.SetTexture("_RightTex", rightTexture);

        stereoscopicMaterial.SetFloat("_RenderWidth", outputResolution.x);
        stereoscopicMaterial.SetFloat("_RenderHeight", outputResolution.y);

        leftEye.GetComponent<Camera>().targetTexture = leftTexture;
        rightEye.GetComponent<Camera>().targetTexture = rightTexture;

        // In case where mocap tracking is done on Main Camera disable tracking on parent object
        // to allow child eye cameras to calculate proper rotation
        CAVE2MocapUpdater cameraTracking = GetComponent<CAVE2MocapUpdater>();
        if (cameraTracking != null)
        {
            cameraTracking.enabled = false;
        }

        camerasGenerated = true;
    }

    void OnRenderImage(RenderTexture src, RenderTexture dest)
    {
        // Copy the source Render Texture to the destination,
        // applying the material along the way.
        Graphics.Blit(src, dest, stereoscopicMaterial);
    }

    public void SetStereoMode(int value)
    {
        SetStereoMode((StereoscopicMode)value);
    }

    public void SetStereoMode(StereoscopicMode value)
    {
        stereoMode = value;

        switch (value)
        {
            case (StereoscopicMode.Interleaved):
                stereoscopicMaterial.SetFloat("_StereoMode", (int)StereoscopicMode.Interleaved);
                break;
            case (StereoscopicMode.Checkerboard):
                stereoscopicMaterial.SetFloat("_StereoMode", (int)StereoscopicMode.Checkerboard);
                break;
            case (StereoscopicMode.SideBySide):
                stereoscopicMaterial.SetFloat("_StereoMode", (int)StereoscopicMode.SideBySide);
                break;
            case (StereoscopicMode.Right):
                stereoscopicMaterial.SetFloat("_StereoMode", (int)StereoscopicMode.Right);
                break;
            default:
                stereoscopicMaterial.SetFloat("_StereoMode", (int)StereoscopicMode.Left);
                break;
        }
    }

    public StereoscopicMode GetStereoMode()
    {
        return stereoMode;
    }

    public bool IsStereoInverted()
    {
        return invertEyes;
    }

    public void SetStereoInverted(bool value)
    {
        invertEyes = value;
    }

    public void InvertStereo()
    {
        invertEyes = !invertEyes;
    }

    public void UpdateScreenScale_x(string value)
    {
        float.TryParse(value, out textureScale.x);
    }

    public void UpdateScreenScale_y(string value)
    {
        float.TryParse(value, out textureScale.y);
    }

    public void UpdateScreenOffset_x(string value)
    {
        float.TryParse(value, out textureOffset.x);
    }

    public void UpdateScreenOffset_y(string value)
    {
        float.TryParse(value, out textureOffset.y);
    }

    public Vector2 GetScreenScale()
    {
        return textureScale;
    }

    public Vector2 GetScreenOffset()
    {
        return textureOffset;
    }

    public void EnableStereo(bool value)
    {
        generateCameras = value;
    }

    public void SetScreenScale(Vector2 value)
    {
        textureScale = value;
    }

    public void SetScreenOffset(Vector2 value)
    {
        textureOffset = value;
    }

    public Vector2 GetStereoResolution()
    {
        return outputResolution;
    }

    public void SetStereoResolution(Vector2 value, bool rebuild = true)
    {
        outputResolution = value;
        if(rebuild)
            RebuildTextures();
    }

    public void UpdateStereoResolution_x(string value)
    {
        float.TryParse(value, out outputResolution.x);
        RebuildTextures();
    }

    public void UpdateStereoResolution_y(string value)
    {
        float.TryParse(value, out outputResolution.y);
        RebuildTextures();
    }

    public float GetEyeSeparation()
    {
        return eyeSeparation;
    }

    public void UpdateEyeSeparation(string value)
    {
        float.TryParse(value, out eyeSeparation);
    }

    public void SetEyeSeparation(float value)
    {
        eyeSeparation = value;
    }

    public bool IsCamerasGenerated()
    {
        return camerasGenerated;
    }
}
