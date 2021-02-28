using System.Collections.Generic;
using Unity.Mathematics;
using static Unity.Mathematics.math;


namespace Logic
{

	class UnitManager
	{
		Core.HandlePool<Unit> _units;
		List<uint> _releaseCandidates = new List<uint>();

		int _hunterAt;
		int _hunterCount;
		bool _isResetRequested;


		public List<Unit> Actives { get => _units.Actives; }



		public UnitManager()
		{
			_units = new Core.HandlePool<Unit>(128);
		}

		public void setup()
		{
			_isResetRequested = false;
			for (var i = 0; i < 100; ++i)
			{
				var unit = new Unit();
				_units.register(unit);
			}
		}

		public void start()
		{
			reset();
		}

		public void requestReset()
		{
			_isResetRequested = true;
		}

		void reset()
		{
			foreach (var u in Actives)
			{
				release(u.Handle);
			}

			alloc(Roles.Hunter, Unit.randomPosition());
			_hunterAt = WorldManager.Instance.Time.Now;
			_hunterCount = 3;

			alloc(Roles.Me, Unit.randomPosition());

			// 宝石を適当に作成
			alloc(Roles.Gem, Unit.randomPosition());
			alloc(Roles.Gem, Unit.randomPosition());
			alloc(Roles.Gem, Unit.randomPosition());
			alloc(Roles.Gem, Unit.randomPosition());

			_isResetRequested = false;
		}

		void releaseCandidates()
		{
			foreach (var h in _releaseCandidates)
			{
				var u = _units.getItem(h);
				if (u == null)
				{
					continue;
				}
				u.release();

				_units.free(h);
			}
			_releaseCandidates.Clear();
		}

		public void update()
		{
			if (_isResetRequested)
			{
				reset();
			}

			// 削除
			releaseCandidates();

			var spent = WorldManager.Instance.Time.Now - _hunterAt;
			if (_hunterCount > 0 && spent >= (Time.Second * 30))
			{
				alloc(Roles.Hunter, Unit.randomPosition());
				_hunterAt = WorldManager.Instance.Time.Now;
				--_hunterCount;
			}

			// 更新
			var actives = _units.Actives;
			foreach (var u in actives)
			{
				u.update();
			}

			// 衝突判定
			for (var i = 0; i < actives.Count; ++i)
			{
				var ui = actives[i];
				for (var j = i + 1; j < actives.Count; ++j)
				{
					var uj = actives[j];
					var diff = uj.Position - ui.Position;
					var dist = length(diff);
					if (dist <= ui.Radius + uj.Radius)
					{
						ui.onCollide(uj);
						uj.onCollide(ui);
					}
				}
			}
		}

		public Unit alloc(Roles role, int2 pos)
		{
			if (pos.x < 0 || pos.y < 0)
			{
				return null;
			}
			var graph = WorldManager.Instance.PathfindingManager.Graph;
			var s = graph.Size;
			if (pos.x >= s.x || pos.y >= s.y)
			{
				return null;
			}
			if (graph.Map[pos.y, pos.x].Terrain != (uint)Pathfinding.TerrainRoles.Ground)
			{
				return null;
			}

			var u = _units.alloc();
			if (u == null)
			{
				_units.register(new Unit());
				u = _units.alloc();
				if (u == null)
				{
					return null;
				}
			}
			u.alloc(role, pos);

			WorldManager.Instance.WatcherManager.register(u);

			return u;
		}

		public void release(uint handle)
		{
			_releaseCandidates.Add(handle);
		}

		public Unit getUnit(uint handle)
		{
			return _units.getItem(handle);
		}

		public void createSake(int2 pos)
		{
			alloc(Roles.Sake, pos);
		}
	}

}