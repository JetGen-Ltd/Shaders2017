﻿using UnityEngine;
using UnityEngine.Rendering;
using System.Collections.Generic;

// See _ReadMe.txt for an overview
[ExecuteInEditMode]
public class TargetCommandBufferBlur : MonoBehaviour
{
	public Shader m_BlurShader;
	public Renderer[] targetRenderers;
	public Material maskMaterial;

    static private CameraEvent cameraEvent = CameraEvent.BeforeImageEffects;

    private Material m_Material;


	private Camera m_Cam;

	// We'll want to add a command buffer on any camera that renders us,
	// so have a dictionary of them.
	private Dictionary<Camera,CommandBuffer> m_Cameras = new Dictionary<Camera,CommandBuffer>();

	// Remove command buffers from all cameras we added into
	private void Cleanup()
	{
		foreach (var cam in m_Cameras)
		{
			if (cam.Key)
			{
				cam.Key.RemoveCommandBuffer (cameraEvent, cam.Value);
			}
		}
		m_Cameras.Clear();
		Object.DestroyImmediate (m_Material);
	}

	public void OnEnable()
	{
		Cleanup();
	}

	public void OnDisable()
	{
		Cleanup();
	}

	// Whenever any camera will render us, add a command buffer to do the work on it
	public void OnWillRenderObject()
	{
		var act = gameObject.activeInHierarchy && enabled;
		if (!act)
		{
			Cleanup();
			return;
		}
		
		var cam = Camera.current;
		if (!cam)
			return;

		CommandBuffer buf = null;
		// Did we already add the command buffer on this camera? Nothing to do then.
		if (m_Cameras.ContainsKey(cam))
			return;

		if (!m_Material)
		{
			m_Material = new Material(m_BlurShader);
			m_Material.hideFlags = HideFlags.HideAndDontSave;
		}

		buf = new CommandBuffer();
		buf.name = "Grab screen and blur";
		m_Cameras[cam] = buf;

		// copy screen into temporary RT
		int screenCopyID = Shader.PropertyToID("_ScreenCopyTexture");
        int maskID = Shader.PropertyToID("_MaskRT");
        int maskID2 = Shader.PropertyToID("_MaskRT2");

        buf.GetTemporaryRT (screenCopyID, -1, -1, 0, FilterMode.Bilinear);
        buf.GetTemporaryRT(maskID, -1, -1, 32, FilterMode.Bilinear, RenderTextureFormat.ARGBHalf);
        buf.GetTemporaryRT(maskID2, -1, -1, 0, FilterMode.Bilinear);

        buf.Blit(BuiltinRenderTextureType.CurrentActive, screenCopyID);
        buf.Blit(BuiltinRenderTextureType.CurrentActive, maskID);

        // get two smaller RTs
        int blurredID = Shader.PropertyToID("_Temp1");
		int blurredID2 = Shader.PropertyToID("_Temp2");
		buf.GetTemporaryRT (blurredID, -2, -2, 0, FilterMode.Bilinear);
		buf.GetTemporaryRT (blurredID2, -2, -2, 0, FilterMode.Bilinear);
		
		// downsample screen copy into smaller RT, release screen RT
		buf.Blit (screenCopyID, blurredID);
		buf.ReleaseTemporaryRT (screenCopyID); 
		
		// horizontal blur
		buf.SetGlobalVector("offsets", new Vector4(2,0,0,0));
		buf.Blit (blurredID, blurredID2, m_Material);
		// vertical blur
		buf.SetGlobalVector("offsets", new Vector4(0,2,0,0));
		buf.Blit (blurredID2, blurredID, m_Material);
		// horizontal blur
		buf.SetGlobalVector("offsets", new Vector4(4,0,0,0));
		buf.Blit (blurredID, blurredID2, m_Material);
		// vertical blur
		buf.SetGlobalVector("offsets", new Vector4(0,4,0,0));
		buf.Blit (blurredID2, blurredID, m_Material);

		buf.SetGlobalTexture("_GrabBlurTexture", blurredID);

        buf.ReleaseTemporaryRT(blurredID2);

        // Draw mask

        buf.SetRenderTarget (maskID);

		// clear render texture before drawing to it each frame!!
		buf.ClearRenderTarget(false, true, new Color(0, 0, 0, 0));

		foreach (Renderer r in targetRenderers) {
			buf.DrawRenderer (r, maskMaterial);
		}
		buf.SetGlobalVector("offsets", new Vector4(4,0,0,0));
		buf.Blit (maskID, maskID2, m_Material);
		buf.SetGlobalVector("offsets", new Vector4(0,4,0,0));
		buf.Blit (maskID2, maskID, m_Material);
		buf.SetGlobalTexture("_MaskTex", maskID);

        buf.ReleaseTemporaryRT(maskID2);

        cam.AddCommandBuffer (cameraEvent, buf);
	}	
}
