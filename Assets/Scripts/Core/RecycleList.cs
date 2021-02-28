using System;
using System.Collections;
using System.Collections.Generic;


namespace Logic.Core
{
	/// <summary>
	/// 開放しないリスト
	/// 予めプールへためておき、プールから確保する。開放すればまたプールへ戻す。
	/// </summary>
	/// <typeparam name="T"></typeparam>
	class RecycleList<T> : IEnumerable
		where T : class
	{
		Queue<T> _frees;
		List<T> _actives;
		List<T> _pool;



		public RecycleList()
		{
			_frees = new Queue<T>();
			_actives = new List<T>();
			_pool = new List<T>();
		}

		public RecycleList(int capacity)
		{
			_frees = new Queue<T>(capacity);
			_actives = new List<T>(capacity);
			_pool = new List<T>(capacity);
		}

		public T this[int index]
		{
			get => _actives[index];
		}

		public int Count { get => _actives.Count; }
		public int Capacity { get => _actives.Capacity; }
		public int Remains { get => _frees.Count; }


		/// <summary>
		/// 要素をプールへ登録する
		/// </summary>
		/// <param name="item"></param>
		/// <returns></returns>
		public bool register(T item)
		{
			_pool.Add(item);
			_frees.Enqueue(item);
			return true;
		}

		/// <summary>
		/// プールから確保する
		/// </summary>
		/// <returns></returns>
		public T add()
		{
			if (_frees.Count == 0)
			{
				return null;
			}
			var item = _frees.Dequeue();
			_actives.Add(item);
			return item;
		}

		/// <summary>
		/// すべて削除する (プールへ戻す)
		/// </summary>
		public void clear()
		{
			_frees.Clear();
			_actives.Clear();
			foreach (var i in _pool)
			{
				_frees.Enqueue(i);
			}
		}

		public bool contains(T item) => _actives.Contains(item);
		public bool exists(Predicate<T> match) => _actives.Exists(match);

		public T find(Predicate<T> match) => _actives.Find(match);
		public List<T> findAll(Predicate<T> match) => _actives.FindAll(match);
		public int findIndex(Predicate<T> match) => _actives.FindIndex(match);


		public IEnumerator<T> GetEnumerator() => _actives.GetEnumerator();
		IEnumerator IEnumerable.GetEnumerator() => _actives.GetEnumerator();

		public int indexOf(T item) => _actives.IndexOf(item);
		public bool remove(T item)
		{
			var index = indexOf(item);
			if (index < 0)
			{
				return false;
			}
			removeAt(index);
			return true;
		}
		public void removeAt(int index)
		{
			var item = _actives[index];
			_frees.Enqueue(item);
			_actives.RemoveAt(index);
		}

		public void reverse() => _actives.Reverse();
		public void sort(Comparison<T> comparison) => _actives.Sort(comparison);
		public void sort() => _actives.Sort();
		public void sort(IComparer<T> comparer) => _actives.Sort(comparer);
	}

}