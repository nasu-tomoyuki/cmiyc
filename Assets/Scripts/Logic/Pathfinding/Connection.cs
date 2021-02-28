

namespace Logic.Pathfinding
{
	class Connection
	{
		/// <summary>
		/// 接続先
		/// </summary>
		/// <value></value>
		public Node Node { get; private set; }

		/// <summary>
		/// Node へ移動するのに必要なコスト
		/// </summary>
		/// <value></value>
		public uint Cost { get; private set; }

		public Connection(Node node, uint cost)
		{
			Node = node;
			Cost = cost;
		}
	}

}