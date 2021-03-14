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


		/// <summary>
		/// 書き込むユニットの情報を一時保存する
		/// </summary>
		struct UnitData
		{
			public int2 Position { get; }
			public float ViewLength { get; }
			public float FoV { get; }
			public float2 LookAt { get; }
			public UnitStateFlags States { get; }

			public UnitData(Unit u)
			{
				Position = u.GridPosition;
				ViewLength = u.View.Length;
				FoV = u.View.FoV;
				LookAt = u.LookAt;
				States = u.States;
			}
		}
		List<UnitData> _hunters = new List<UnitData>();
		List<UnitData> _gems = new List<UnitData>();

		uint _aroundMap;
		uint _objectMap;


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
					onHit();
					return;
				}

				var node = _graph.Map[iy, ix];
				if (node.Terrain != (uint)Pathfinding.TerrainRoles.Ground)
				{
					onHit();
					if (_isCancelAfterHit)
					{
						return;
					}
				}

				_heatMap.write(ix, iy, _heat);
				_heat = max(0, _heat - _heatStep);
			}

			void onHit() {
				if (_isHit)
				{
					return;
				}
				_isHit = true;
				// 障害物にぶつかったら危険度を適当に下げる
				_heat /= 4;
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
			_objectMap = Core.HandleManager.INVALID;
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
			// 作業用マップを開放されないようにタッチする
			var map = WorldManager.Instance.RentalHeatMap;
			map.touch(_aroundMap);
			map.touch(_objectMap);

			_updater.MoveNext();
		}

		IEnumerator updateImpl()
		{
			while (true)
			{
				var map = WorldManager.Instance.RentalHeatMap;
				var am = map[_aroundMap];
				var om = map[_objectMap];

				switch (_state)
				{
					// 初期設定
					case States.Init:
						{
							_nextState = States.Collecting;
							// 作業用のマップを借りる
							if (am == null)
							{
								_aroundMap = WorldManager.Instance.RentalHeatMap.alloc();
							}
							if (om == null)
							{
								_objectMap = WorldManager.Instance.RentalHeatMap.alloc();
							}
							am = map[_aroundMap];
							om = map[_objectMap];
							// 借りることができなかったらリトライ
							if (am == null || om == null)
							{
								break;
							}

							_hunters.Clear();
							_gems.Clear();

							am.flip();
							om.flip();
							_state = _nextState;
						}
						break;
					// 対象のユニットを収集
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

								if (u.Role == Roles.Hunter)
								{
									_hunters.Add(new UnitData(u));
								}
								if (u.Role == Roles.Gem)
								{
									_gems.Add(new UnitData(u));
								}
							}
							_state = _nextState;
						}
						break;
					// ハンターの周囲の危険度を書く around map
					case States.WriteAround:
						{
							_nextState = States.WriteView;
							_aroundWriter.MoveNext();
						}
						break;
					// ハンターの視野の危険度を書く hunter view map
					case States.WriteView:
						{
							_nextState = States.WriteGem;
							_viewWriter.MoveNext();
						}
						break;
					// 宝石の位置に危険度を書き込む object map
					case States.WriteGem:
						{
							_nextState = States.Merge;
							foreach (var h in _gems)
							{
								om.add(h.Position.x, h.Position.y, WorldManager.GEM_HAZARD);
							}
							_state = _nextState;
						}
						break;
					// 書き込んだマップを合成する hazard map
					// around map + hunter view map + object map = hazard map
					case States.Merge:
						{
							_nextState = States.Apply;
							var hvm = WorldManager.Instance.HunterViewMap;
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
					// 合成結果の hazard map を反映する
					case States.Apply:
						{
							_nextState = States.Init;
							WorldManager.Instance.HazardMap.flip();
							_state = _nextState;
						}
						break;
				}
				yield return null;
			}
		}

		/// <summary>
		/// ハンターの視野の危険度を書く
		/// </summary>
		/// <returns></returns>
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
					var b = int2(h.Position + rotate(q, p).xy + half);
					var c = int2(h.Position + rotate(conjugate(q), p).xy + half);

					_triangle.draw(a, b, c);

					yield return null;
				}
				_state = _nextState;
				yield return null;
			}
		}

		/// <summary>
		/// ハンターの周囲に危険度を書き込む
		/// </summary>
		/// <returns></returns>
		IEnumerator writeAround()
		{
			while (true)
			{
				var map = WorldManager.Instance.RentalHeatMap;
				var am = map[_aroundMap];
				// 危険度は、視野内よりも適当に下げる
				_triangle.setup(am, WorldManager.HUNTER_HAZARD / 4, WorldManager.HAZARD_STEP / 4, false);

				var half = float2(0.5f, 0.5f);
				foreach (var hu in _hunters)
				{
					var a = int2(hu.Position + half);
					if ((hu.States & UnitStateFlags.Idling) == 0)
					{
						var l = (int)(hu.ViewLength + 1.0f);	// ぴったりではなく少し広げる
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