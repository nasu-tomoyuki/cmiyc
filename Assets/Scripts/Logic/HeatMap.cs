using Unity.Mathematics;
using static Unity.Mathematics.math;

namespace Logic
{
	class HeatMap
	{
		int2 _size;
		int _readIndex;
		int _writeIndex;
		int[][,] _map;

		public int2 Size => _size;
		public int[,] WriteMap => _map[_writeIndex];
		public int[,] ReadMap => _map[_readIndex];
		public int this[int y, int x] => _map[_readIndex][y, x];

		public HeatMap()
		{
		}

		public void setup(int2 size)
		{
			_size = size;
			_map = new int[2][,];
			for (var i = 0; i < 2; ++i)
			{
				_map[i] = new int[size.y, size.x];
			}
			_writeIndex = 0;
			_readIndex = 1;

			clear(0);
		}

		void clear(int init)
		{
			for (var y = 0; y < _size.y; ++y)
			{
				for (var x = 0; x < _size.x; ++x)
				{
					_map[_writeIndex][y, x] = init;
				}
			}
		}

		public void flip(int init = 0)
		{
			_readIndex = _writeIndex;
			_writeIndex = (_writeIndex + 1) % 2;

			clear(init);
		}

		public void write(int x, int y, int c)
		{
			if ((uint)y >= _size.y)
			{
				return;
			}
			if ((uint)x >= _size.x)
			{
				return;
			}
			_map[_writeIndex][y, x] = max( _map[_writeIndex][y, x], c );
		}

		public void add(int x, int y, int c)
		{
			if ((uint)y >= _size.y)
			{
				return;
			}
			if ((uint)x >= _size.x)
			{
				return;
			}
			_map[_writeIndex][y, x] += c;
		}

		public void copyTo(HeatMap other)
		{
			if (!_size.Equals(other._size))
			{
				return;
			}

			var src = ReadMap;
			var dst = other.WriteMap;
			for (var y = 0; y < _size.y; ++y)
			{
				for (var x = 0; x < _size.x; ++x)
				{
					dst[y, x] = src[y, x];
				}
			}
		}

		public void addTo(HeatMap other)
		{
			if (!_size.Equals(other._size))
			{
				return;
			}

			var src = ReadMap;
			var dst = other.WriteMap;
			for (var y = 0; y < _size.y; ++y)
			{
				for (var x = 0; x < _size.x; ++x)
				{
					dst[y, x] += src[y, x];
				}
			}
		}

	}
}