using System.Collections.Generic;


namespace Logic.Core
{
	interface IHandlePoolItem
	{
		int Index { get; set; }
		uint Handle { get; set; }
	}

	/// <summary>
	/// IRecycleContainerItem を管理する
	/// アイテムは事前にプールへ確保しておき、そこから確保する。不要になったらプールへ戻して再利用する。
	/// 確保したアイテムはハンドルで行うことにより、リサイクルされたアイテムへのアクセスを回避する。
	/// </summary>
	/// <typeparam name="T"></typeparam>
	class HandlePool<T> where T : class, IHandlePoolItem
	{
		HandleManager handleManager_ = new HandleManager();
		Queue<T> _frees;
		List<T> _actives;
		List<T> _pool;

		public List<T> Actives { get => _actives; }
		public int Count => _actives.Count;
		public int Remains => _frees.Count;


		public HandlePool(int capacity)
		{
			_frees = new Queue<T>(capacity);
			_actives = new List<T>(capacity);
			_pool = new List<T>(capacity);
		}

		public bool register(T item)
		{
			var index = _pool.Count;
			item.Index = index;
			item.Handle = HandleManager.INVALID;

			_pool.Add(item);
			_frees.Enqueue(item);
			return true;
		}

		public T alloc(int tag = 0)
		{
			if (_frees.Count == 0)
			{
				return null;
			}
			var item = _frees.Dequeue();
			item.Handle = handleManager_.issue(item.Index, tag);
			_actives.Add(item);
			return item;
		}

		public void free(uint handle)
		{
			var item = getItem(handle);
			if (item == null)
			{
				return;
			}
			item.Handle = HandleManager.INVALID;
			_frees.Enqueue(item);
			_actives.Remove(item);
		}

		public void clear()
		{
			_actives.Clear();
			_frees.Clear();
			foreach (var item in _pool)
			{
				item.Handle = HandleManager.INVALID;
				_frees.Enqueue(item);
			}

		}

		public T this[uint handle] => getItem(handle);

		public T getItem(uint handle)
		{
			var index = HandleManager.getIndex(handle);
			if (index < 0 || index >= _pool.Count)
			{
				return null;
			}
			var t = _pool[index];
			if (t.Handle != handle)
			{
				return null;
			}
			return t;
		}
	}

}