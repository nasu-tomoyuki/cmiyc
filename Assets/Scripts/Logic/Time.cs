namespace Logic
{
	class Time
	{
		public const int FPS = 60;
		public const int Second = 300;
		public const int Thick = Second / FPS;

		public int Now { get; private set; }

		public Time()
		{
		}

		public void reset()
		{
			Now = 0;
		}

		public void update()
		{
			Now += Thick;
		}
	}
}