using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Mathematics;
using static Unity.Mathematics.math;


namespace Logic
{
	class HunterWatcher : IWatcher
	{
		List<uint> _units = new List<uint>();
		IEnumerator _updater;
		IEnumerator _viewWriter;
		IEnumerator _aroundWriter;

		enum States
		{
			Init,
			Collecting,
			WriteAround,
			WriteView,
			WriteGem,
			Merge,
			Apply,
		}
		States _state;
		States _nextState;


		struct Hunter
		{
			public int2 Position { get; }
			public float ViewLength { get; }
			public float FoV { get; }
			public float2 LookAt { get; }
			public UnitStateFlags States { get; }

			public Hunter(Unit u)
			{
				Position = u.GridPosition;
				ViewLength = u.View.Length;
				FoV = u.View.FoV;
				LookAt = u.LookAt;
				States = u.States;
			}
		}
		List<Hunter> _hunters = new List<Hunter>();
		List<Hunter> _gems = new List<Hunter>();

		uint _aroundMap;


		class Triangle
		{
			HeatMap _heatMap;
			int _heat;
			int _initHeat;
			int _heatStep;
			int2 _a;
			bool _isHit;
			bool _isCancelAfterHit;

			Action<int, int> _plot;
			Action<int, int> _plotBC;

			Pathfinding.IGraph _graph;

			public Triangle()
			{
				_plot = plot;
				_plotBC = plotBC;
				_graph = WorldManager.Instance.PathfindingManager.Graph;
			}

			public void setup(HeatMap map, int heat, int heatStep, bool isCancelAfterHit)
			{
				_heatMap = map;
				_initHeat = _heat = heat;
				_heatStep = heatStep;
				_isCancelAfterHit = isCancelAfterHit;
			}

			public void draw(int2 a, int2 b, int2 c)
			{
				a *= 2;
				b *= 2;
				c *= 2;
				_a = a;
				Geometry.line(b.x, b.y, c.x, c.y, _plotBC);
			}

			void plotBC(int x, int y)
			{
				_isHit = false;
				_heat = _initHeat;
				Geometry.line(_a.x, _a.y, x, y, _plot);
			}

			void plot(int x, int y)
			{
				if (_isHit && _isCancelAfterHit)
				{
					return;
				}

				var ix = x / 2;
				var iy = y / 2;
				// 画面外
				if ((uint)ix >= _graph.Size.x || (uint)iy >= _graph.Size.y)
				{
					_isHit = true;
					return;
				}

				var node = _graph.Map[iy, ix];
				if (node.Terrain != (uint)Pathfinding.TerrainRoles.Ground)
				{
					_isHit = true;
					if (_isCancelAfterHit)
					{
						return;
					}
				}

				if (_isHit)
				{
					_heat /= 2;
				}
				_heatMap.write(ix, iy, _heat);
				_heat = max(0, _heat - _heatStep);
			}
		}
		Triangle _triangle = new Triangle();


		public HunterWatcher()
		{
			_state = States.Init;

			_updater = updateImpl();

			_viewWriter = writeView();
			_aroundWriter = writeAround();

			_aroundMap = Core.HandleManager.INVALID;
		}

		public void register(Unit u)
		{
			if (u.Role != Roles.Hunter && u.Role != Roles.Gem)
			{
				return;
			}
			_units.Add(u.Handle);
		}

		public void update()
		{
			var map = WorldManager.Instance.RentalHeatMap;
			map.touch(_aroundMap);

			_updater.MoveNext();
		}

		IEnumerator updateImpl()
		{
			while (true)
			{
				var map = WorldManager.Instance.RentalHeatMap;
				var am = map[_aroundMap];

				switch (_state)
				{
					case States.Init:
						{
							_nextState = States.Collecting;
							if (am == null)
							{
								_aroundMap = WorldManager.Instance.RentalHeatMap.alloc();
							}
							am = map[_aroundMap];
							if (am == null)
							{
								break;
							}

							_hunters.Clear();
							_gems.Clear();

							am.flip(0);
							_state = _nextState;
						}
						break;
					case States.Collecting:
						{
							_nextState = States.WriteAround;
							for (var i = _units.Count - 1; i >= 0; --i)
							{
								var h = _units[i];
								var u = WorldManager.Instance.UnitManager.getUnit(h);
								if (u == null)
								{
									_units.RemoveAt(i);
									continue;
								}
								var hunter = new Hunter(u);

								if (u.Role == Roles.Hunter)
								{
									_hunters.Add(hunter);
								}
								if (u.Role == Roles.Gem)
								{
									_gems.Add(hunter);
								}
							}
							_state = _nextState;
						}
						break;
					case States.WriteAround:
						{
							_nextState = States.WriteView;
							_aroundWriter.MoveNext();
						}
						break;
					case States.WriteView:
						{
							_nextState = States.WriteGem;
							_viewWriter.MoveNext();
						}
						break;
					case States.WriteGem:
						{
							_nextState = States.Merge;
							var om = WorldManager.Instance.ObjectMap;
							foreach (var h in _gems)
							{
								om.add(h.Position.x, h.Position.y, WorldManager.GEM_HAZARD);
							}
							_state = _nextState;
						}
						break;
					case States.Merge:
						{
							_nextState = States.Apply;
							var hvm = WorldManager.Instance.HunterViewMap;
							var om = WorldManager.Instance.ObjectMap;
							am.flip();
							hvm.flip();
							om.flip();

							var hm = WorldManager.Instance.HazardMap;
							for (var y = 0; y < hm.Size.y; ++y)
							{
								for (var x = 0; x < hm.Size.x; ++x)
								{
									var c = WorldManager.BASE_HAZARD + am[y, x] + hvm[y, x] + om[y, x];
									hm.write(x, y, c);
								}
							}
							_state = _nextState;
						}
						break;
					case States.Apply:
						{
							_nextState = States.Init;
							// WorldManager.Instance.HunterViewMap.flip();
							// WorldManager.Instance.ObjectMap.flip();
							WorldManager.Instance.HazardMap.flip();
							_state = _nextState;
						}
						break;
				}
				yield return null;
			}
		}

		IEnumerator writeView()
		{
			while (true)
			{
				var vm = WorldManager.Instance.HunterViewMap;
				_triangle.setup(vm, WorldManager.HUNTER_HAZARD, WorldManager.HAZARD_STEP, true);

				var half = float2(0.5f, 0.5f);
				foreach (var h in _hunters)
				{
					if ((h.States & UnitStateFlags.Idling) != 0)
					{
						continue;
					}
					var q = Unity.Mathematics.quaternion.Euler(0, 0, radians(h.FoV * 0.5f));
					var p = float3(h.LookAt * (h.ViewLength + 0.5f), 0);
					var a = int2(h.Position + half);
					var b = a + int2(rotate(q, p).xy + half);
					var c = a + int2(rotate(conjugate(q), p).xy + half);

					var tb = rotate(q, p);
					var tc = rotate(conjugate(q), p);
					_triangle.draw(a, b, c);

					yield return null;
				}
				_state = _nextState;
				yield return null;
			}
		}

		IEnumerator writeAround()
		{
			while (true)
			{
				var map = WorldManager.Instance.RentalHeatMap;
				var am = map[_aroundMap];
				_triangle.setup(am, WorldManager.HUNTER_HAZARD / 10, WorldManager.HAZARD_STEP / 10, false);

				var half = float2(0.5f, 0.5f);
				foreach (var hu in _hunters)
				{
					var a = int2(hu.Position + half);
					if ((hu.States & UnitStateFlags.Idling) == 0)
					{
						var l = (int)(hu.ViewLength + 1.0f);
						var w = int2(l, 0);
						var h = int2(0, l);
						var b = a + w + h;
						var c = a + w - h;
						var d = a - w - h;
						var e = a - w + h;

						_triangle.draw(a, b, c);
						_triangle.draw(a, c, d);
						_triangle.draw(a, d, e);
						_triangle.draw(a, e, b);
					}

					// 自分のいる位置は特に危険度を上げる
					am.add(a.x, a.y, WorldManager.HUNTER_HAZARD);

					yield return null;
				}
				_state = _nextState;
				yield return null;
			}
		}

		public bool isRunning()
		{
			return true;
		}
	}
}