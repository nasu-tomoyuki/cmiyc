using System.Collections.Generic;
using Unity.Mathematics;



namespace Logic
{
	public class WorldManager
	{
		static public WorldManager Instance { get; private set; }

		internal const int FPS = 50;
		internal Time Time { get; } = new Time();
		internal Core.IRandom Random { get; } = new Core.Xorshift128();
		internal UnitManager UnitManager { get; } = new UnitManager();
		internal Pathfinding.Manager PathfindingManager { get; } = new Pathfinding.Manager();
		internal WatcherManager WatcherManager { get; } = new WatcherManager();

		internal Core.AutoRecycleHandlePool<HeatMap> RentalHeatMap { get; } = new Core.AutoRecycleHandlePool<HeatMap>();

		// ハンターの危険度
		public const int HUNTER_HAZARD = 40000;

		// 酒の危険度
		public const int SAKE_HAZARD = 100;

		// 宝石の危険度
		public const int GEM_HAZARD = -500;

		public const int BASE_HAZARD = 1000;
		public const int HAZARD_STEP = 1000;

		// ハンターの視界
		internal HeatMap HunterViewMap { get; } = new HeatMap();
		// 危険度マップ
		internal HeatMap HazardMap { get; } = new HeatMap();
		// 配置マップ
		internal HeatMap ObjectMap { get; } = new HeatMap();


		public struct OpenUnitData
		{
			public uint Handle { get; }
			public Roles Role { get; }
			public float2 Position { get; }
			public float2 TargetPosition { get; }
			public float Rotation { get; }
			public UnitStateFlags States { get; }

			internal OpenUnitData(Unit u)
			{
				Handle = u.Handle;
				Role = u.Role;
				Position = u.Position;
				Rotation = u.Rotation;
				TargetPosition = u.TargetPosition;
				States = u.States;
			}
		}
		public List<OpenUnitData> OpenUnitDatas { get; private set; } = new List<OpenUnitData>();


		public WorldManager()
		{
		}

		public void setup(string map)
		{
			Instance = this;


			PathfindingManager.setup(3);
			PathfindingManager.loadMap(map);

			UnitManager.setup();

			WatcherManager.setup();
			WatcherManager.addWatcher(new HunterWatcher());

			for (var i = 0; i < 8; ++i)
			{
				var m = new HeatMap();
				m.setup(WorldManager.Instance.PathfindingManager.Graph.Size);
				RentalHeatMap.register(m);
			}

            var size = WorldManager.Instance.PathfindingManager.Graph.Size;
			HunterViewMap.setup(size);
			HazardMap.setup(size);
			ObjectMap.setup(size);
		}

		public void start()
		{
			WatcherManager.start();
			UnitManager.start();
		}

		public void update()
		{
			RentalHeatMap.update();
			PathfindingManager.update();
			Time.update();
			UnitManager.update();
			WatcherManager.update();

			OpenUnitDatas.Clear();
			foreach (var u in UnitManager.Actives)
			{
				var d = new OpenUnitData(u);
				OpenUnitDatas.Add(d);
			}
		}
	}
}