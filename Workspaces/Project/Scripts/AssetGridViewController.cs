﻿using System;
using ListView;
using UnityEditor;
using UnityEngine;
using UnityEngine.VR.Utilities;

public class AssetGridViewController : ListViewController<AssetData, AssetGridItem>
{
	private const float kClipMargin = 0.005f; // Give the cubes a margin so that their sides don't get clipped

	private Material m_TextMaterial;

	private Transform m_GrabbedObject;

	private int m_NumPerRow;

	public float scaleFactor
	{
		get { return m_ScaleFactor; }
		set { m_ScaleFactor = value; }
	}
	[SerializeField]
	private float m_ScaleFactor = 0.75f;

	protected override int dataLength { get { return Mathf.CeilToInt((float)base.dataLength / m_NumPerRow); } }

	public AssetData[] listData { set { m_Data = value; } }

	public Func<string, bool> testFilter; 

	protected override void Setup()
	{
		base.Setup();
		var item = m_Templates[0].GetComponent<AssetGridItem>();
		item.GetMaterials(out m_TextMaterial);

		m_Data = new AssetData[0]; // Start with empty list to avoid null references
	}

	protected override void ComputeConditions()
	{
		base.ComputeConditions();

		m_NumPerRow = (int) (bounds.size.x / m_ItemSize.x);

		m_NumRows = Mathf.CeilToInt(bounds.size.z / m_ItemSize.z);

		m_StartPosition = (bounds.extents.z - m_ItemSize.z * 0.5f) * Vector3.forward + (bounds.extents.x - m_ItemSize.x * 0.5f) * Vector3.left;

		m_DataOffset = (int) (m_ScrollOffset / itemSize.z);
		if (m_ScrollOffset < 0)
			m_DataOffset --;

		m_ScrollReturn = float.MaxValue;

		// Snap back if list scrolled too far
		if (-m_DataOffset >= dataLength)
			m_ScrollReturn = (1 - dataLength) * itemSize.z;

		// Extend clip bounds slightly in Z for extra text
		var clipExtents = bounds.extents;
		clipExtents.z += kClipMargin;
		var parentMatrix = transform.worldToLocalMatrix;
		m_TextMaterial.SetMatrix("_ParentMatrix", parentMatrix);
		m_TextMaterial.SetVector("_ClipExtents", clipExtents);
	}

	protected override Vector3 GetObjectSize(GameObject g)
	{
		return base.GetObjectSize(g) * m_ScaleFactor;
	}

	protected override void UpdateItems()
	{
		int count = 0;
		for (int i = 0; i < m_Data.Length; i++)
		{
			if (!testFilter(m_Data[i].type))
			{
				CleanUpBeginning(m_Data[i]);
				continue;
			}
			//Debug.Log(m_Data[i].type);
			if (count / m_NumPerRow + m_DataOffset < -1)
			{
				CleanUpBeginning(m_Data[i]);
			}
			else if (count / m_NumPerRow + m_DataOffset > m_NumRows - 1)
			{
				CleanUpEnd(m_Data[i]);
			}
			else
			{
				UpdateVisibleItem(m_Data[i], count);
			}
			count++;
		}
	}

	protected override void UpdateItem(Transform t, int offset)
	{
		AssetGridItem item = t.GetComponent<AssetGridItem>();
		item.UpdateTransforms(m_ScaleFactor);
		item.Clip(bounds, transform.worldToLocalMatrix);

		var zOffset = m_ItemSize.z * (offset / m_NumPerRow) + m_ScrollOffset;
		var xOffset = m_ItemSize.x * (offset % m_NumPerRow);
		t.localPosition = m_StartPosition + zOffset * Vector3.back + xOffset * Vector3.right;
		t.localRotation = Quaternion.identity;
	}

	protected override AssetGridItem GetItem(AssetData listData)
	{
		var item = base.GetItem(listData);
		item.SwapMaterials(m_TextMaterial);
		return item;
	}

	private void OnDestroy()
	{
		U.Object.Destroy(m_TextMaterial);
	}
}