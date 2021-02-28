using System.Collections.Generic;


namespace Logic
{
	interface IWatcher
	{
		void register(Unit u);
		void update();
		bool isRunning();
	}

	class WatcherManager
	{
		List<IWatcher> watchers_ = new List<IWatcher>();


		public WatcherManager()
		{
		}

		public void setup()
		{
		}

		public void start()
		{
		}

		public void addWatcher(IWatcher o)
		{
			watchers_.Add(o);
		}

		public void register(Unit u)
		{
			foreach (var o in watchers_)
			{
				o.register(u);
			}
		}

		public void update()
		{
			foreach (var o in watchers_)
			{
				o.update();
			}
		}
	}
}