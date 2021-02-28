using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Mathematics;
using static Unity.Mathematics.math;


namespace Logic.Pathfinding
{

	/// <summary>
	/// A* を使ったパス検索
	/// 参考: https://yttm-work.jp/algorithm/algorithm_0015.html
	/// </summary>
	class Finder : IFinder, Core.IHandlePoolItem
	{
		public IFinder.Statuses Status
		{
			get { return _status; }
			private set { lifetime_ = 3; _status = value; }
		}

		public int Index { get; set; }
		public uint Handle { get; set; }

		enum Methods
		{
			AStar,
			Dijkstra,
		}

		Methods _method;
		IFinder.Statuses _status;
		IEnumerator _updater;
		IGraph _graph;
		Node _start;
		Node _end;
		Manager.Dir _dir;
		uint _terrains;
		Func<uint, uint, StepCost, bool> _less;
		Func<uint, uint, StepCost, bool> _lessTotalCost;
		Func<uint, uint, StepCost, bool> _lessAdditionalCost;
		int lifetime_;

		Func<int2, int2, uint> _calcHeuristicCost;
		Func<int2, int2, uint> _calcHeuristicCostMD;
		Func<int2, int2, uint> _calcHeuristicCostEmpty;

		Func<StepCost, uint> _getGoalCostAsHeuristicCost;
		Func<StepCost, uint> _getGoalCostAsAdditionalCost;
		Func<StepCost, uint> _getGoalCost;


		System.Comparison<StepCost> _sorter;
		List<StepCost> _open = new List<StepCost>();
		List<StepCost> _closed = new List<StepCost>();
		Core.RecycleList<StepCost> _pool;

		List<Node> _result = new List<Node>();


		/// <summary>
		/// このノードまでのコスト
		/// </summary>
		class StepCost
		{
			// 対象ノード
			public Node Node { get; private set; }
			// ここから来た
			public StepCost From { get; private set; }
			// スタートからの合計コスト
			public uint TotalCost { get; private set; }
			// スタートからのヒューリスティックコスト
			public uint HeuristicCost { get; private set; }
			// ここまでの方向
			public Manager.Dir Dir { get; private set; }
			// ここまでの歩数
			public int Steps { get; private set; }


			public StepCost()
			{
			}

			public void init(Node node, StepCost from, uint fromCost, uint totalCost, uint heuristicCost, Manager.Dir dir)
			{
				Node = node;
				From = from;

				TotalCost = totalCost;
				HeuristicCost = heuristicCost;
				Dir = dir;
				Steps = 1;
				if (from != null)
				{
					Steps = from.Steps + 1;
				}
			}
		}

		/// <summary>
		/// 検索用
		/// </summary>
		class Predicate
		{
			Node target_;

			public System.Predicate<StepCost> Match;

			public Predicate()
			{
				Match = match;
			}

			public void setup(Node target)
			{
				target_ = target;
			}


			bool match(StepCost t)
			{
				return t.Node.Position.Equals(target_.Position);
			}
		}
		Predicate predicate_ = new Predicate();


		public Finder()
		{
			_updater = updateImpl();
			_sorter = descendingSorter;

			_pool = new Core.RecycleList<StepCost>(512);

			_calcHeuristicCostMD = calcHeuristicCostMD;
			_calcHeuristicCostEmpty = calcHeuristicCostEmpty;
			_calcHeuristicCost = _calcHeuristicCostMD;

			_lessAdditionalCost = lessAdditionalCost;
			_lessTotalCost = lessTotalCost;

			_getGoalCostAsHeuristicCost = getGoalCostAsHeuristicCost;
			_getGoalCostAsAdditionalCost = getGoalCostAsAdditionalCost;
		}

		public void setup(IGraph graph)
		{
			Status = IFinder.Statuses.Idle;
			_graph = graph;
		}

		public void request(Node start, Node end, Manager.Dir dir, uint terrains, Func<int2, int2, uint> getCost)
		{
			if (Status != IFinder.Statuses.Idle)
			{
				return;
			}
			_method = Methods.AStar;
			_start = start;
			_end = end;
			_dir = dir;
			_terrains = terrains;
			_calcHeuristicCost = getCost;
			if (getCost == null)
			{
				_calcHeuristicCost = _calcHeuristicCostMD;
			}
			_less = _lessTotalCost;
			_getGoalCost = _getGoalCostAsHeuristicCost;

			Status = IFinder.Statuses.Running;
		}

		public void request(Node start, Manager.Dir dir, uint terrains, Func<int2, int2, uint> getCost)
		{
			if (Status != IFinder.Statuses.Idle)
			{
				return;
			}
			_method = Methods.Dijkstra;
			_end = _start = start;
			_dir = dir;
			_terrains = terrains;
			_calcHeuristicCost = getCost;
			if (getCost == null)
			{
				_calcHeuristicCost = _calcHeuristicCostEmpty;
			}
			// less_ = lessAdditionalCost_;
			_less = _lessTotalCost;
			_getGoalCost = _getGoalCostAsAdditionalCost;

			Status = IFinder.Statuses.Running;
		}

		public void update()
		{
			_updater.MoveNext();
		}

		public bool isCompleted()
		{
			return Status > IFinder.Statuses.Running;
		}

		public List<Node> getResult()
		{
			if (isCompleted())
			{
				return _result;
			}
			return null;
		}

		IEnumerator updateImpl()
		{
			while (true)
			{
				yield return null;
				if (Status == IFinder.Statuses.Idle)
				{
					continue;
				}
				if (Status != IFinder.Statuses.Running)
				{
					// 一定期間過ぎたら Idle に戻る
					if (--lifetime_ > 0)
					{
						continue;
					}
					Status = IFinder.Statuses.Idle;
					continue;
				}

				_open.Clear();
				_closed.Clear();
				_pool.clear();
				_result.Clear();

				// 開始地点を open
				{
					var hcost = _calcHeuristicCost(_start.Position, _end.Position);
					var tcost = hcost;
					var o = alloc();
					o.init(_start, null, 0, tcost, hcost, _dir);
					push(_open, o);
				}
				var isFound = false;

				var loopCount = 100;
				var loopCounter = loopCount;
				while (_open.Count > 0)
				{
					var node = pop(_open);
					if (!Manager.hasTerrainRole(_terrains, (TerrainRoles)node.Node.Terrain))
					{
						_pool.remove(node);
						continue;
					}
					push(_closed, node);

					// ゴール判定
					if (_method == Methods.AStar)
					{
						if (node.Node.Position.Equals(_end.Position))
						{
							isFound = true;
							push(_closed, node);

							var r = node;
							while (r != null)
							{
								_result.Add(r.Node);
								r = r.From;
							}
							_result.Reverse();
							break;
						}
					}

					// 接続点を open
					foreach (var m in node.Node.Connections)
					{
						var turnPenalty = Manager.BASE_COST / 2;
						var dir = Manager.getDir(m.Node.Position - node.Node.Position);
						var hcost = _calcHeuristicCost(m.Node.Position, _end.Position);
						var tcost = node.TotalCost + m.Cost + hcost;

						// 方向転換をしたらペナルティーを与える
						{
							var turned = node.Dir != dir;
							if (turned)
							{
								tcost += turnPenalty;
							}
						}

						var requireOpen = true;

						// closed リストを検索
						predicate_.setup(m.Node);
						StepCost sc = null;
						// var index = closed_.FindIndex( predicate_.Match );
						var index = indexOf(_closed, m.Node);
						// 見つかればコストを比較
						if (index >= 0)
						{
							if (_less(tcost, hcost, _closed[index]))
							{
								// 置き換える
								sc = _closed[index];
								_closed.RemoveAt(index);
							}
							else
							{
								requireOpen = false;
							}
						}
						else
						{
							// open リストを検索
							// index = open_.FindIndex( predicate_.Match );
							index = indexOf(_open, m.Node);
							if (index >= 0)
							{
								if (_less(tcost, hcost, _open[index]))
								{
									// 置き換える
									sc = _open[index];
									_open.RemoveAt(index);
								}
								else
								{
									requireOpen = false;
								}
							}
						}

						if (!requireOpen)
						{
							continue;
						}

						// 新規 open
						if (sc == null)
						{
							sc = alloc();
						}
						sc.init(m.Node, node, m.Cost, tcost, hcost, dir);
						push(_open, sc);
					}


					// 降順ソート (最後が一番コストが低い)
					// 速度を求めるなら Priority Queue にしたほうがいい
					_open.Sort(_sorter);

					--loopCounter;
					if (loopCounter <= 0)
					{
						loopCounter = loopCount;
						yield return null;
					}
				}

				if (isFound)
				{
					Status = IFinder.Statuses.Found;
					continue;
				}

				// 到達できなかったので、ゴールに一番近い場所までの経路＝一番ヒューリスティックコストの低い場所までの経路を返す
				{
					StepCost nearest = null;
					var gcost = uint.MaxValue;
					foreach (var n in _closed)
					{
						var goalCost = _getGoalCost(n);
						if (goalCost <= gcost)
						{
							if (goalCost == gcost)
							{
								if (nearest.TotalCost < n.TotalCost)
								{
									continue;
								}
							}
							nearest = n;
							gcost = goalCost;
						}
					}

					var r = nearest;
					while (r != null)
					{
						_result.Add(r.Node);
						r = r.From;

						if (_result.Count > 1000)
						{
							var text = "";
							foreach (var m in _result)
							{
								text = $"{text}{m.Index}:{m.Position}\n";
							}
							UnityEngine.Debug.Log(text);
							break;
						}
					}
					_result.Reverse();
				}
				Status = IFinder.Statuses.NotFound;
			}
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="node"></param>
		/// <param name="totalCost"></param>
		/// <param name="heuristicCost"></param>
		/// <returns></returns>
		StepCost alloc()
		{
			if (_pool.Remains == 0)
			{
				_pool.register(new StepCost());
			}
			return _pool.add();
		}

		/// <summary>
		/// 降順
		/// </summary>
		/// <param name="a"></param>
		/// <param name="b"></param>
		/// <returns></returns>
		int descendingSorter(StepCost a, StepCost b)
		{
			return (int)b.TotalCost - (int)a.TotalCost;
		}

		void push(List<StepCost> list, StepCost node)
		{
			list.Add(node);
		}

		StepCost pop(List<StepCost> list)
		{
			if (list.Count == 0)
			{
				return null;
			}
			var last = list.Count - 1;
			var n = list[last];
			list.RemoveAt(last);
			return n;
		}

		int indexOf(List<StepCost> list, Node n)
		{
			for (var i = 0; i < list.Count; ++i)
			{
				if (list[i].Node == n)
				{
					return i;
				}
			}
			return -1;
		}


		uint getGoalCostAsHeuristicCost(StepCost sc)
		{
			return sc.HeuristicCost;
		}

		uint getGoalCostAsAdditionalCost(StepCost sc)
		{
			return sc.TotalCost / (uint)(sc.Steps);
			// return sc.AdditionalCost;
		}

		uint calcHeuristicCostEmpty(int2 pos, int2 goal)
		{
			return 0u;
		}

		/// <summary>
		/// Manhattan distance
		/// </summary>
		/// <param name="pos"></param>
		/// <returns></returns>
		uint calcHeuristicCostMD(int2 pos, int2 goal)
		{
			var diff = abs(pos - goal);
			return (uint)(diff.x + diff.y) * Manager.BASE_COST;
		}

		bool lessTotalCost(uint tcost, uint hcost, StepCost sc)
		{
			return tcost < sc.TotalCost;
		}

		bool lessAdditionalCost(uint tcost, uint hcost, StepCost sc)
		{
			return hcost < sc.HeuristicCost;
		}
	}

}