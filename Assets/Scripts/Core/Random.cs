
namespace Logic.Core
{
	interface IRandom
	{
		void init(uint seed);

		uint rand();
	}

	/// <summary>
	/// 参考: https://ja.wikipedia.org/wiki/Xorshift
	/// </summary>
	public class Xorshift128 : IRandom
	{
		private uint _x;
		private uint _y;
		private uint _z;
		private uint _w;

		public Xorshift128(uint seed = 88675123u)
		{
			init(seed);
		}

		public void init(uint seed)
		{
			_x = 123456789u;
			_y = 362436069u;
			_z = 521288629u;
			_w = seed;
		}

		public uint rand()
		{
			var t = _x ^ (_x << 11);
			_x = _y;
			_y = _z;
			_z = _w;
			_w = (_w ^ (_w >> 19)) ^ (t ^ (t >> 8));
			return _w;
		}
	}

}