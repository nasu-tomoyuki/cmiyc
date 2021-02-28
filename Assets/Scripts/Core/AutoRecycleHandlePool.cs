using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;


namespace Logic.Core
{


	/// <summary>
	/// アクセスしない要素を自動的に回収するコンテナ
	/// 要素は削除せずに再利用する
	/// </summary>
	/// <typeparam name="T"></typeparam>
	class AutoRecycleHandlePool<T>
		where T : class
	{
		class Handler : IHandlePoolItem
		{
			public int Index { get; set; }
			public uint Handle { get; set; }

			T _o;
			uint _accessedAt;

			public Handler()
			{
			}

			public Handler(T o)
			{
				_o = o;
				_accessedAt = 0;
			}

			public void reset(T o)
			{
				_o = o;
			}

			public bool isExpired(uint now)
			{
				return now - _accessedAt > 3;
			}

			public T access(uint now)
			{
				_accessedAt = now;
				return _o;
			}
		}

		HandlePool<Handler> container_;

		uint now_ = 0;


		public AutoRecycleHandlePool(int capacity = 32)
		{
			container_ = new HandlePool<Handler>(capacity);
		}

		public T this[uint handle] => container_[handle]?.access(now_);

		public int Count { get => container_.Count; }
		public int Remains { get => container_.Remains; }

		public bool register(T item) => container_.register(new Handler(item));

		public uint alloc(int tag = 0)
		{
			var h = container_.alloc(tag);
			h.access(now_);
			return h.Handle;
		}

		public void clear() => container_.clear();

		public void update()
		{
			for (var i = container_.Actives.Count - 1; i >= 0; --i)
			{
				var item = container_.Actives[i];
				if (item.isExpired(now_))
				{
					container_.free(item.Handle);
				}
			}
			++now_;
		}

		public bool touch(uint handle)
		{
			var t = this[handle];
			return t != null;
		}
	}

}