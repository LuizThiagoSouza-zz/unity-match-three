using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Pool : MonoBehaviour
{
	[Header("Pool Properties:")]
	[SerializeField] private int initialQty;
	[SerializeField] private bool preload;
	[Header("Pool References:")]
	[SerializeField] private GameObject poolItemPrefab;
	private Stack<PoolItemMonobehaviour> inactive;

	private void Awake()
	{
		inactive = new Stack<PoolItemMonobehaviour>(initialQty);
		
		if (preload)
			Preload();
	}

	private void Preload()
	{
		PoolItemMonobehaviour poolItemToPreload;
		
		for (int i = 0; i < initialQty; i++)
		{
			poolItemToPreload = Instantiate(poolItemPrefab, transform, false).GetComponent<PoolItemMonobehaviour>();
			poolItemToPreload.myPool = this;
			poolItemToPreload.gameObject.SetActive(false);

			inactive.Push(poolItemToPreload);
		}
	}

	public PoolItemMonobehaviour GetPoolItem(Vector3 pos, Quaternion rot, Transform parent = null)
	{
		PoolItemMonobehaviour poolItemToReturn;
		
		if (inactive.Count == 0)
		{
			poolItemToReturn = Instantiate(poolItemPrefab, pos, rot).GetComponent<PoolItemMonobehaviour>();
			poolItemToReturn.myPool = this;
		}
		else
		{
			poolItemToReturn = inactive.Pop();

			if (poolItemToReturn == null)
				return GetPoolItem(pos, rot, parent);
		}

		poolItemToReturn.transform.SetParent(parent, false);
		poolItemToReturn.transform.localScale = Vector3.one;
		poolItemToReturn.transform.position = pos;
		poolItemToReturn.transform.rotation = rot;
		poolItemToReturn.gameObject.SetActive(true);
		poolItemToReturn.OnSpawn();

		return poolItemToReturn;
	}

	public void RemovePoolItem(PoolItemMonobehaviour poolItem)
	{
		if (poolItem == null) return;

		poolItem.transform.SetParent(transform);
		poolItem.OnDespawn();
		poolItem.gameObject.SetActive(false);
		inactive.Push(poolItem);
	}
}

public abstract class PoolItemMonobehaviour : MonoBehaviour
{
	public Pool myPool;

	public void Despawn()
	{
		if (myPool != null)
			myPool.RemovePoolItem(this);
	}

	public abstract void OnSpawn();
	public abstract void OnDespawn();
}