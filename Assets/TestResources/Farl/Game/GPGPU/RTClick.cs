﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[ExecuteInEditMode]
public class RTClick : MonoBehaviour {

	public enum OutputType
	{
		Position,
		Velocity
	}

	[System.Serializable]
	public class OutputMaterial
	{
		public Material material;
		public OutputType type;
		public string propertyName;
	}

	public Shader kernel;
	public Material kernelMaterial;

	public float waveDensity = 0.01f;
	public float waveDampingForce = 50.0f;
	[Range(0.0f, 1.0f)]
	public float waveSpeed = 0.99f;
	[Range(0.0f, 1.0f)]
	public float waveDamping = 0.99f;
	private RenderTexture positionTexture1;
	private RenderTexture positionTexture2;
	private RenderTexture velocityTexture1;
	private RenderTexture velocityTexture2;

	private List<Vector3> clickList = new List<Vector3> ();
	private List<Vector4> clickVecArray = new List<Vector4> ();

	[Header("Outpu")]
	public OutputMaterial[] outputMaterials;

	[Header("Render Texture")]
	public int rtWidth = 256;
	public int rtHeight = 256;
	public RenderTextureFormat rtFormat = RenderTextureFormat.ARGB32;
	public TextureWrapMode rtWrap = TextureWrapMode.Repeat;
	public FilterMode rtFilter = FilterMode.Bilinear;

	public void SetClickTexCoord(Vector3 texcoord)
	{
		clickList.Add (texcoord);
	}

	RenderTexture CreateTexture()
	{
		rtWidth = Mathf.Max (1, rtWidth);
		rtHeight = Mathf.Max (1, rtHeight);
		RenderTexture rt = new RenderTexture (rtWidth, rtHeight, 0, rtFormat);
		rt.hideFlags = HideFlags.DontSave;
		rt.filterMode = rtFilter;
		rt.wrapMode = rtWrap;
		return rt;
	}

	void _Init()
	{
		bool doInit = false;
		if (positionTexture1 == null) {
			positionTexture1 = CreateTexture();
			positionTexture2 = CreateTexture();
			doInit = true;
		}

		if (velocityTexture1 == null) {
			velocityTexture1 = CreateTexture();
			velocityTexture2 = CreateTexture();
			doInit = true;
		}

		if (kernelMaterial == null && kernel != null) {
			kernelMaterial = new Material (kernel);
			kernelMaterial.hideFlags = HideFlags.DontSave;

			kernelMaterial.SetTexture ("_PositionBuffer", positionTexture1);
			kernelMaterial.SetTexture ("_VelocityBuffer", velocityTexture1);

			// Output prepare
			if (outputMaterials != null) {
				foreach (OutputMaterial om in outputMaterials) {
					Texture tex = null;
					switch (om.type) {
					case OutputType.Velocity:
						tex = velocityTexture1;
						break;
					case OutputType.Position:
						tex = positionTexture1;
						break;
					}
					if (tex && om.material)
					{
						if (!string.IsNullOrEmpty (om.propertyName) && om.material.HasProperty (om.propertyName)) {
							om.material.SetTexture (om.propertyName, tex);
						}
						else
						{
							om.material.mainTexture = tex;
						}
					}		
				}
			}

			// Initialize

			if (kernelMaterial != null) {
				if (positionTexture1)
					Graphics.Blit (null, positionTexture1, kernelMaterial, 2);
				if (positionTexture2)
					Graphics.Blit (null, positionTexture2, kernelMaterial, 2);
				if (velocityTexture1)
					Graphics.Blit (null, velocityTexture1, kernelMaterial, 3);
				if (velocityTexture2)
					Graphics.Blit (null, velocityTexture2, kernelMaterial, 3);
			}

			doInit = true;
		}
	}

	void _CleanUp()
	{
		if (kernelMaterial)
			Object.DestroyImmediate (kernelMaterial);
		if (positionTexture1)
			Object.DestroyImmediate (positionTexture1);
		if (velocityTexture1)
			Object.DestroyImmediate (velocityTexture1);
		if (positionTexture2)
			Object.DestroyImmediate (positionTexture2);
		if (velocityTexture2)
			Object.DestroyImmediate (velocityTexture2);
	}

	void OnEnable () {
		_CleanUp ();
		_Init ();
	}

	void OnDisable()
	{
		_CleanUp ();
	}

	void UpdateInput()
	{
		
		bool click = false;
		Vector3 texcoord = Vector3.zero;
		if (kernelMaterial) {
			if (Input.GetMouseButton (0)) {
				Vector3 sPos = Input.mousePosition;
				Ray ray = Camera.main.ScreenPointToRay (sPos);
				RaycastHit hitInfo;
				if (Physics.Raycast (ray, out hitInfo, 100)) {
					texcoord = hitInfo.textureCoord;
					click = true;

					SetClickTexCoord (texcoord);
				}
			}
			// kernelMaterial.SetVector ("_Click", new Vector4 (texcoord.x, texcoord.y, texcoord.z, click? 1: 0));
		}

		int count = 6;
		clickVecArray.Clear ();
		while (count > 0) {
			if (clickList.Count > 0) {
				clickVecArray.Add(new Vector4(clickList[0].x, clickList[0].y, clickList[0].z, 1));
				clickList.RemoveAt (0);
			} else {
				clickVecArray.Add(new Vector4 (0, 0, 0, 0));
			}
			count--;
		}
		clickList.Clear ();

		kernelMaterial.SetVectorArray ("_ClickList", clickVecArray); 
	}
	
	// Update is called once per frame
	void Update () {
		_Init ();

		UpdateInput ();

		if (kernelMaterial) {
			// Swap buffer
			var tempVelocityTex = velocityTexture1;
			var tempPositionTex = positionTexture1;

			velocityTexture1 = velocityTexture2;
			velocityTexture2 = tempVelocityTex;

			positionTexture1 = positionTexture2;
			positionTexture2 = tempPositionTex;

			if (positionTexture1 && positionTexture2 && velocityTexture1 && velocityTexture2) {
				kernelMaterial.SetVector ("_WaveParam", new Vector4 (waveDensity, waveDampingForce, waveSpeed, waveDamping));

				kernelMaterial.SetTexture ("_PositionBuffer", positionTexture1);
				kernelMaterial.SetTexture ("_VelocityBuffer", velocityTexture1);
				Graphics.Blit (null, positionTexture2, kernelMaterial, 0);
				kernelMaterial.SetTexture ("_PositionBuffer", positionTexture2);
				kernelMaterial.SetTexture ("_VelocityBuffer", velocityTexture1);
				Graphics.Blit (null, velocityTexture2, kernelMaterial, 1);
			}
		}
	}
}
