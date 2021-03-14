using Unity.Mathematics;
using System;
using System.Collections.Generic;
using static Unity.Mathematics.math;


namespace Logic
{
	public enum UnitStateFlags
	{
		None = 0,
		HasTarget = 0x01 << 0,
		Walking = 0x01 << 10,

		Idling = 0x01 << 20,
		Taking = 0x01 << 21,
		Drunk = 0x01 << 22,
	}

	class Unit : Core.IHandlePoolItem
	{
		public int Index { get; set; }
		public uint Handle { get; set; }

		public Roles Role { get; private set; }
		public float2 Position { get; private set; }
		public int2 GridPosition { get; private set; }
		public int2 TargetPosition => _targetPosition;
		public float Radius { get; private set; }
		public float2 Front { get; private set; }       // 進行方向
		public float2 LookAt { get; private set; }      // 視線方向。ターゲットがいればターゲット、いなければ進行方向
		public float Rotation { get; private set; }
		public UnitStateFlags States { get; private set; }


		public class ViewDesc
		{
			public float Length { get; private set; }

			// 視野（degree）
			public float FoV { get; private set; }

			public ViewDesc()
			{
			}

			public void init(float length, float fov)
			{
				Length = length;
				FoV = fov;
			}
		}
		ViewDesc _view = new ViewDesc();
		public ViewDesc View => _view;

		float _speed;
		float _speedScale;
		uint _rentalMapHandle;
		uint _pathHandle;
		List<Pathfinding.Node> _wayPoints = new List<Pathfinding.Node>();
		int _wayPointIndex;

		Dictionary<Roles, float> _targetables = new Dictionary<Roles, float>();
		uint _target;
		int2 _targetPosition;

		int _idling;
		int _drunk;
		int _taking;


		Action _update;
		Action<Unit> _onCollide;


		public Unit()
		{
		}

		public void alloc(Roles role, int2 pos)
		{
			Role = role;
			GridPosition = pos;
			Position = pos;
			Radius = 0.5f;
			Front = float2(0.0f, 1.0f);
			LookAt = Front;
			Rotation = atan2(LookAt.y, LookAt.x);

			_speed = 1.0f;
			_speedScale = 1.0f;
			_rentalMapHandle = Core.HandleManager.INVALID;
			_pathHandle = Core.HandleManager.INVALID;
			clearWayPoints();

			_targetables.Clear();
			_target = Core.HandleManager.INVALID;

			_idling = 0;
			_drunk = 0;
			_taking = 0;

			switch (Role)
			{
				case Roles.Me:
					_speed = 3.0f;
					_update = updateMe;
					_onCollide = onCollideMe;
					// マップ内すべて
					_view.init(1000.0f, 360.0f);
					_targetables[Roles.Gem] = 0;
					break;
				case Roles.Gem:
					_speed = 0.0f;
					_update = updateGem;
					_onCollide = onCollideGem;
					_view.init(1000.0f, 360.0f);
					break;
				case Roles.Hunter:
					_speed = 1.2f;
					_update = updateHunter;
					_onCollide = onCollideHunter;
					_view.init(12.0f, 30.0f);
					_targetables[Roles.Me] = 0;
					_targetables[Roles.Sake] = -1000;
					break;
				case Roles.Sake:
					Radius = 0.2f;
					_update = updateSake;
					_onCollide = onCollideSake;
					break;
			}

			var fps = 50.0f;
			_speed /= fps;
		}

		public void release()
		{
		}

		public void update()
		{
			_update();
			actionMove();
			lookAt();

			{
				States = UnitStateFlags.None;
				var u = WorldManager.Instance.UnitManager.getUnit(_target);
				if (u != null)
				{
					States |= UnitStateFlags.HasTarget;
				}
				if (hasPath())
				{
					States |= UnitStateFlags.Walking;
				}

				if (_idling > 0)
				{
					--_idling;
					States |= UnitStateFlags.Idling;
				}
				if (_drunk > 0)
				{
					--_drunk;
					States |= UnitStateFlags.Drunk;
				}
				if (_taking > 0)
				{
					--_taking;
					States |= UnitStateFlags.Taking;
				}
			}
		}

		public void onCollide(Unit other)
		{
			_onCollide(other);
		}

		public static int2 randomPosition()
		{
			var size = WorldManager.Instance.PathfindingManager.Graph.Size;

			while (true)
			{
				var x = abs((int)WorldManager.Instance.Random.rand()) % size.x;
				var y = abs((int)WorldManager.Instance.Random.rand()) % size.y;
				var node = WorldManager.Instance.PathfindingManager.Graph.Map[y, x];
				if (node.Terrain == (uint)Pathfinding.TerrainRoles.Ground)
				{
					return int2(x, y);
				}
			}
		}

		void onCollideMe(Unit other)
		{
			var um = WorldManager.Instance.UnitManager;
			switch (other.Role)
			{
				case Roles.Gem:
					// 古い Gem を削除して新規追加
					um.release(other.Handle);
					um.alloc(Roles.Gem, randomPosition());
					_taking = 50;
					clearWayPoints();
					break;

				case Roles.Hunter:
					// 捕まったので世界をリセット
					um.requestReset();
					break;
			}
		}
		void onCollideHunter(Unit other)
		{
			var um = WorldManager.Instance.UnitManager;
			switch (other.Role)
			{
				case Roles.Sake:
					// ゲット
					um.release(other.Handle);
					_drunk = 150;
					clearWayPoints();
					break;
			}
		}
		void onCollideGem(Unit other)
		{
		}
		void onCollideSake(Unit other)
		{
			var um = WorldManager.Instance.UnitManager;
			switch (other.Role)
			{
				case Roles.Sake:
					// 同じマスに配置したら削除
					um.release(other.Handle);
					break;
			}
		}

		bool hasPath()
		{
			return _wayPoints.Count > 0;
		}

		/// <summary>
		/// 視野を使って検索する
		/// </summary>
		/// <param name="role"></param>
		/// <returns></returns>
		bool findByView(Roles role)
		{
			var um = WorldManager.Instance.UnitManager;

			var old = _target;
			var nearest = float.MaxValue;
			_target = Core.HandleManager.INVALID;
			foreach (var u in um.Actives)
			{
				if (u.Role != role)
				{
					continue;
				}
				var diff = u.Position - Position;
				var dist = length(diff);
				// ぶつかっていないなら視野判定を行う
				if (dist > Radius + u.Radius)
				{
					// 遠い
					if (dist > _view.Length)
					{
						continue;
					}
					var dir = normalize(diff);
					var theta = acos(dot(dir, LookAt));
					// FoV の外
					var fov = radians(_view.FoV * 0.5f);
					if (fov < theta)
					{
						continue;
					}
				}

				// 一番近いものを選ぶ
				if (dist > nearest)
				{
					continue;
				}
				nearest = dist;
				_target = u.Handle;
			}

			return old != _target;
		}

		/// <summary>
		/// ViewMap を使って検索
		/// </summary>
		/// <param name="role"></param>
		/// <returns></returns>
		bool findByMap(HeatMap map)
		{
			var um = WorldManager.Instance.UnitManager;

			var old = _target;
			var nearest = float.MaxValue;
			_target = Core.HandleManager.INVALID;
			foreach (var u in um.Actives)
			{
				if (!_targetables.TryGetValue(u.Role, out var offset))
				{
					continue;
				}
				var diff = u.Position - Position;
				var dist = length(diff);
				// ぶつかっていないなら視野判定を行う
				if (dist > Radius + u.Radius)
				{
					var v = map[u.GridPosition.y, u.GridPosition.x];
					if (v == 0)
					{
						continue;
					}
				}

				// 一番近いものを選ぶ
				dist += offset; // ロールによる補正
				if (dist > nearest)
				{
					continue;
				}
				nearest = dist;
				_target = u.Handle;
			}

			return old != _target;
		}

		uint getHazardCost(int2 p, int2 goal)
		{
			var map = WorldManager.Instance.RentalHeatMap[_rentalMapHandle];
			return (uint)map.ReadMap[p.y, p.x];
		}


		void updateMe()
		{
			if (_taking > 0)
			{
				_idling = 50;
				return;
			}
			if (_idling > 0)
			{
				return;
			}

			var wm = WorldManager.Instance;
			var pm = wm.PathfindingManager;
			var pf = pm.getFinder(_pathHandle);
			// パス検索をリクエスト
			if (pf == null)
			{
				_rentalMapHandle = wm.RentalHeatMap.alloc();
				var map = wm.RentalHeatMap[_rentalMapHandle];
				if (map == null)
				{
					return;
				}
				wm.HazardMap.copyTo(map);
				map.flip();

				var dir = Pathfinding.Manager.getDir((int2)Front);
				_pathHandle = pm.request(
					GridPosition,
					dir,
					Pathfinding.Manager.getTerrainFlag(Pathfinding.TerrainRoles.Ground),
					getHazardCost
				);
			}
			else
			{
				if (pf.isCompleted())
				{
					_pathHandle = Core.HandleManager.INVALID;
					var path = pf.getResult();
					if (path != null)
					{
						var hasPoint = false;
						foreach (var p in path)
						{
							if (p.Position.Equals(GridPosition))
							{
								hasPoint = true;
								break;
							}
						}
						if (!hasPoint)
						{
							return;
						}

						clearWayPoints();
						foreach (var p in path)
						{
							if (p.Position.Equals(GridPosition))
							{
								_wayPointIndex = _wayPoints.Count + 1;
							}
							_wayPoints.Add(p);
						}
						if (_wayPointIndex < 0 || _wayPointIndex >= _wayPoints.Count)
						{
							_wayPoints.Clear();
							_wayPointIndex = -1;
						}
						else
						{
							_targetPosition = _wayPoints[_wayPoints.Count - 1].Position;
						}
					}
				}
			}
		}

		void updateGem()
		{
		}

		void updateSake()
		{
		}


		void updateHunter()
		{
			// 泥酔
			if (_drunk > 0)
			{
				_idling = 100;
				return;
			}
			if (_idling > 0)
			{
				return;
			}

			var um = WorldManager.Instance.UnitManager;
			var hasTarget = !Core.HandleManager.isInvalid(_target);
			// 検索。ターゲットが変わった
			if (findByMap(WorldManager.Instance.HunterViewMap))
			{
				// 発見
				hasTarget = !Core.HandleManager.isInvalid(_target);
				if (hasTarget)
				{
					_pathHandle = Core.HandleManager.INVALID;
					clearWayPoints();
				}
			}
			{
				// ターゲットがいればターゲットの場所、なければ次のグリッドをターゲットとする
				var target = um.getUnit(_target);
				if (target != null)
				{
					_targetPosition = target.GridPosition;
					// ターゲットがあれば加速
					_speedScale = 3.0f;
				}
				else
				{
					_speedScale = 1.0f;
					if (_wayPointIndex + 1 < _wayPoints.Count)
					{
						_targetPosition = _wayPoints[_wayPointIndex + 1].Position;
					}
				}
			}

			// 少し進んだらパスを再検索
			if (hasTarget && _wayPointIndex >= 2)
			{
				clearWayPoints();
			}
			if (_wayPointIndex >= 0)
			{
				return;
			}

			var pm = WorldManager.Instance.PathfindingManager;
			var pf = pm.getFinder(_pathHandle);
			if (pf == null)
			{
				int2 targetPosition;
				var u = um.getUnit(_target);
				if (u != null)
				{
					targetPosition = u.GridPosition;
					_targetPosition = targetPosition;
				}
				else
				{
					targetPosition = randomPosition();
				}

				var dir = Pathfinding.Manager.getDir((int2)Front);
				_pathHandle = pm.request(
					GridPosition,
					targetPosition,
					dir,
					Pathfinding.Manager.getTerrainFlag(Pathfinding.TerrainRoles.Ground),
					null
					);
			}
			else
			{
				if (pf.isCompleted())
				{
					var path = pf.getResult();
					clearWayPoints();
					if (path != null)
					{
						_wayPointIndex = 0;
						foreach (var p in path)
						{
							if (p.Position.Equals(GridPosition))
							{
								_wayPointIndex = _wayPoints.Count + 1;
							}
							_wayPoints.Add(p);
						}
						if (_wayPointIndex >= _wayPoints.Count)
						{
							_wayPoints.Clear();
							_wayPointIndex = -1;
						}
					}
					_pathHandle = Core.HandleManager.INVALID;
				}
			}
		}

		void clearWayPoints()
		{
			_wayPoints.Clear();
			_wayPointIndex = -1;
		}

		float normalizeRad(float rad)
		{
			var pi2 = PI * 2.0f;
			rad = (rad + pi2) % pi2;
			if (rad <= PI)
			{
				return rad;
			}
			return rad - pi2;
		}

		void lookAt()
		{
			var dir = Front;
			// ターゲットがいればターゲットを向く
			var tu = WorldManager.Instance.UnitManager.getUnit(_target);
			if (tu != null)
			{
				dir = normalize(tu.Position - Position);
			}

			// ラジアン [0, pi]
			var theta = (1.0f - dot(LookAt, dir)) * 0.5f;

			var rotSpeed = math.PI * 0.4f / WorldManager.FPS;

			if (theta <= rotSpeed)
			{
				Rotation = atan2(dir.y, dir.x);
				LookAt = dir;
			}
			else
			{
				var nrm = cross(float3(LookAt, 0), float3(dir, 0));
				if (nrm.z < 0.0f)
				{
					Rotation -= rotSpeed;
				}
				else
				{
					Rotation += rotSpeed;
				}
				var q = Unity.Mathematics.quaternion.RotateZ(Rotation);
				var r = rotate(q, float3(1, 0, 0));
				LookAt = r.xy;
			}
		}

		void actionMove()
		{
			if (!hasPath())
			{
				return;
			}

			var next = _wayPoints[_wayPointIndex];
			var diff = next.Position - Position;
			var len = length(diff);
			if (len > 0.0001f)
			{
				var dir = diff / len;
				var d = (dot(dir, Front) + 1.0f) * 0.5f;
				Front = lerp(Front, dir, 0.05f);
				var step = dir * (_speed * _speedScale * d);
				Position += step;
			}

			if (len < 0.5f)
			{
				GridPosition = next.Position;
				++_wayPointIndex;
				if (_wayPointIndex == _wayPoints.Count)
				{
					clearWayPoints();
					return;
				}
			}
		}

		void actionFindPath()
		{
		}
	}

}