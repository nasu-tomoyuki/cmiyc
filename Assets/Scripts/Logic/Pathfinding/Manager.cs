using System;
using System.Collections.Generic;
using Unity.Mathematics;
using static Unity.Mathematics.math;


namespace Logic.Pathfinding
{
	/// <summary>
	/// 地形の種類
	/// </summary>
	enum TerrainRoles
	{
		None,
		Ground,
		Wall,
	}


	class Manager
	{
		public const uint BASE_COST = 10u;
		public IGraph Graph { get; private set; }

		List<uint> _releaseCandidates = new List<uint>();
		Core.HandlePool<Finder> _astars;

		/// <summary>
		/// 地形からフラグに変換する
		/// </summary>
		/// <param name="role"></param>
		/// <returns></returns>
		public static uint getTerrainFlag(TerrainRoles role)
		{
			return (uint)(0x01 << (int)role);
		}

		/// <summary>
		/// フラグが地形を含むか
		/// </summary>
		/// <param name="roleFlags"></param>
		/// <param name="role"></param>
		/// <returns></returns>
		public static bool hasTerrainRole(uint roleFlags, TerrainRoles role)
		{
			return (roleFlags & getTerrainFlag(role)) != 0;
		}

		public enum Dir
		{
			All = Right | Up | Left | Down,
			Right = 0x01 << 0,
			Up = 0x01 << 1,
			Left = 0x01 << 2,
			Down = 0x01 << 3,
		}

		public static Dir getDir(int2 dir)
		{
			if (dir.x != 0)
			{
				if (dir.x > 0)
				{
					return Dir.Right;
				}
				return Dir.Left;
			}

			if (dir.y > 0)
			{
				return Dir.Up;
			}
			return Dir.Down;
		}


		public Manager()
		{
		}

		public void setup(int countAstar)
		{
			Graph = new GridGraph();

			_astars = new Core.HandlePool<Finder>(countAstar);
			for (var i = 0; i < countAstar; ++i)
			{
				var f = new Finder();
				f.setup(Graph);
				_astars.register(f);
			}
		}

		public void loadMap(string map)
		{
			var dic = new Dictionary<int, TerrainRoles>() {
				{ '.', TerrainRoles.None },
				{ ' ', TerrainRoles.Ground },
				{ '#', TerrainRoles.Wall },
			};

			var size = int2(0, 0);
			var x = 0;
			foreach (var c in map)
			{
				if (c == '\n')
				{
					x = 0;
					size.x = max(size.x, x);
					++size.y;
					continue;
				}
				var tag = (uint)dic[c];
				var pos = int2(x, size.y);
				var node = new Pathfinding.Node(pos, tag);
				Graph.addNode(node);
				++x;
			}

			// ノードを登録したのでマップを構築する
			Graph.build();
		}

		/// <summary>
		/// A* を使った経路探索要求
		/// </summary>
		/// <param name="start"></param>
		/// <param name="end"></param>
		/// <param name="terrains"></param>
		/// <returns>ハンドル</returns>
		public uint request(int2 start, int2 end, Dir dir, uint terrains, Func<int2, int2, uint> getCost)
		{
			var finder = _astars.alloc();
			if (finder == null)
			{
				return Core.HandleManager.INVALID;
			}

			var s = Graph.find(start);
			var e = Graph.find(end);
			finder.request(s, e, dir, terrains, getCost);
			return finder.Handle;
		}

		public uint request(int2 start, Dir dir, uint terrains, Func<int2, int2, uint> getCost)
		{
			var finder = _astars.alloc();
			if (finder == null)
			{
				return Core.HandleManager.INVALID;
			}

			var s = Graph.find(start);
			finder.request(s, dir, terrains, getCost);
			return finder.Handle;
		}

		public IFinder getFinder(uint handle)
		{
			var item = _astars.getItem(handle);
			return item;
		}

		public void update()
		{
			foreach (var f in _astars.Actives)
			{
				f.update();
				// IDLE に戻ったら削除
				if (f.Status == IFinder.Statuses.Idle)
				{
					_releaseCandidates.Add(f.Handle);
				}
			}

			foreach (var h in _releaseCandidates)
			{
				_astars.free(h);
			}
			_releaseCandidates.Clear();
		}
	}

}