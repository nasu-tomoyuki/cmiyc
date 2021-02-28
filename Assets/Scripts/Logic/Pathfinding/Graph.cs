using System.Collections.Generic;
using Unity.Mathematics;
using static Unity.Mathematics.math;


namespace Logic.Pathfinding
{
	interface IGraph
	{
		List<Node> Nodes { get; }

		Node[,] Map { get; }
		int2 Size { get; }


		Node find(int2 pos);

		void addNode(Node node);

		void build();
	}

	/// <summary>
	/// 2D グリッドのグラフ
	/// </summary>
	class GridGraph : IGraph
	{
		public List<Node> Nodes { get; private set; } = new List<Node>();

		public Node[,] Map { get; private set; }
		public int2 Size { get; private set; }


		public GridGraph()
		{
		}

		public Node find(int2 pos)
		{
			return Map[pos.y, pos.x];
		}

		public void addNode(Node node)
		{
			node.Index = Nodes.Count;
			Nodes.Add(node);
			Size = max(Size, node.Position + int2(1, 1));
		}

		/// <summary>
		/// addNode() で追加したノードから map を作成する
		/// </summary>
		public void build()
		{
			Map = new Node[Size.y, Size.x];
			foreach (var n in Nodes)
			{
				Map[n.Position.y, n.Position.x] = n;
			}


			// 接続
			for (var y = 0; y < Size.y; ++y)
			{
				for (var x = 0; x < Size.x; ++x)
				{
					var cost = Manager.BASE_COST;
					var na = Map[y, x];
					// 右
					if (x + 1 < Size.x)
					{
						var nb = Map[y, x + 1];
						na.addConnection(nb, cost);
						nb.addConnection(na, cost);
					}
					// 下
					if (y + 1 < Size.y)
					{
						var nc = Map[y + 1, x];
						na.addConnection(nc, cost);
						nc.addConnection(na, cost);
					}
				}
			}
		}

	}
}