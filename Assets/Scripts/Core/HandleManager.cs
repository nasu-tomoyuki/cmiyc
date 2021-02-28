
namespace Logic.Core
{
	/// <summary>
	/// ハンドルを発行する
	/// ハンドルからインデックスへ変換できるので、実際にはインデックスでアクセスを行う
	/// タグというオプションを追加可能
	/// フォーマット
	/// MSB <serial> <tab> <index> LSB
	/// -1 を無効なハンドルとする
	/// </summary>
	class HandleManager
	{
		public const uint INVALID = ~0u;

		public const int INDEX_LIMIT = INDEX_MASK;
		public const int TAG_LIMIT = TAG_MASK;

		const int INDEX_MASK = (1 << INDEX_BITS) - 1;

		const int TAG_MASK = (1 << TAG_BITS) - 1;

		const int SERIAL_MASK = (1 << SERIAL_BITS) - 1;
		const int SERIAL_LIMIT = SERIAL_MASK - 1;       // すべてのビットを立てないために -1

		const int INDEX_BITS = 14;
		const int TAG_BITS = 2;
		const int TAG_SHIFT = INDEX_BITS;
		const int SERIAL_BITS = 32 - TAG_BITS - INDEX_BITS;
		const int SERIAL_SHIFT = TAG_BITS + INDEX_BITS;

		int _serial;

		public HandleManager()
		{
		}

		/// <summary>
		/// ハンドルを発行する
		/// </summary>
		/// <param name="index">対象のインデックス</param>
		/// <param name="tag">タグ</param>
		/// <returns></returns>
		public uint issue(int index, int tag = 0)
		{
			if (index >= INDEX_LIMIT || tag >= TAG_LIMIT)
			{
				return INVALID;
			}
			_serial = (_serial + 1) % SERIAL_LIMIT;
			return (uint)((_serial << SERIAL_SHIFT) | (tag << TAG_SHIFT) | index);
		}

		/// <summary>
		/// ハンドルが無効なら true を返す
		/// </summary>
		/// <param name="handle"></param>
		/// <returns></returns>
		public static bool isInvalid(uint handle)
		{
			return handle == INVALID;
		}

		public static int getTag(uint handle)
		{
			if (isInvalid(handle))
			{
				return -1;
			}
			return (int)((handle >> TAG_SHIFT) & TAG_MASK);
		}

		public static int getIndex(uint handle)
		{
			if (isInvalid(handle))
			{
				return -1;
			}
			return (int)(handle & INDEX_MASK);
		}

	}

}