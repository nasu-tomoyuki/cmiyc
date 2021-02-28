using System.Collections.Generic;


namespace Logic.Pathfinding
{

	interface IFinder
	{
		enum Statuses
		{
			Idle,
			Running,

			Found,          // 成功終了 経路を見つけた
			NotFound,       // 失敗終了 到達できなかった
			Error,
		}
		Statuses Status { get; }

		void update();

		bool isCompleted();

		List<Node> getResult();
	}

}