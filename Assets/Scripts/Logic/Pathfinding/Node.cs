using System.Collections.Generic;
using Unity.Mathematics;


namespace Logic.Pathfinding
{
	class Node
	{
		public List<Connection> Connections { get; private set; } = new List<Connection>();
		public int Index { get; set; }
		public int2 Position { get; private set; }

		/// <summary>
		/// 地形の種類。ユーザー任意の値。 0 - 31
		/// </summary>
		/// <value></value>
		public uint Terrain { get; private set; }

		public Node(int2 pos, uint terrain)
		{
			Position = pos;
			Terrain = terrain;
		}

		public void addConnection(Node node, uint cost)
		{
			Connections.Add(new Connection(node, cost));
		}
	}

}