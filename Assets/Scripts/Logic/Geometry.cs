using System;
using static Unity.Mathematics.math;

namespace Logic
{
	class Geometry
	{
		static public void line(int ax, int ay, int bx, int by, Action<int, int> plotAction)
		{
			var dx = bx - ax;
			var dy = by - ay;
			var sx = 1;
			var sy = 1;
			if (ax > bx)
			{
				sx = -1;
				dx = -dx;
			}
			if (ay > by)
			{
				sy = -1;
				dy = -dy;
			}
			var err = dx - dy;

			while (true)
			{
				plotAction(ax, ay);
				if (ax == bx && ay == by)
				{
					break;
				}
				var e2 = 2 * err;
				if (e2 > -dy)
				{
					err = err - dy;
					ax = ax + sx;
				}
				if (e2 < dx)
				{
					err = err + dx;
					ay = ay + sy;
				}
			}
		}


		// 参考 https://qiita.com/minosys/items/5375e6ffa049226943b0
		public static void drawTriangle(int x1, int y1, int x2, int y2, int x3, int y3, int c1, int c2, int c3, Action<int, int, int> plot)
		{
			// y1 < y2 < y3 となるように並べ直す
			if (y1 > y2)
			{
				swap(ref x1, ref x2);
				swap(ref y1, ref y2);
				swap(ref c1, ref c2);
			}
			if (y1 > y3)
			{
				swap(ref x1, ref x3);
				swap(ref y1, ref y3);
				swap(ref c1, ref c3);
			}
			if (y2 > y3)
			{
				swap(ref x2, ref x3);
				swap(ref y2, ref y3);
				swap(ref c2, ref c3);
			}

			// 例外的なパターンの排除
			if (y1 == y3)
			{
				return;
			}
			if (x1 == x2 && x2 == x3)
			{
				return;
			}

			if (y1 == y2)
			{
				drawFlatTriangle(x3, y3, x1, y1, x2, c3, c1, c2, plot);
			}
			else if (y2 == y3)
			{
				drawFlatTriangle(x1, y1, x2, y2, x3, c1, c2, c3, plot);
			}
			else
			{
				var xa = (x3 * (y2 - y1) / (y3 - y1) + x1 * (y2 - y3) / (y1 - y3));
				var ca = (c3 * (y2 - y1) / (y3 - y1) + c1 * (y2 - y3) / (y1 - y3));
				drawFlatTriangle(x1, y1, xa, y2, x2, c1, ca, c2, plot);
				drawFlatTriangle(x3, y3, xa, y2, x2, c3, ca, c2, plot);
			}
		}

		static void swap<T>(ref T a, ref T b)
		{
			var tmp = a;
			a = b;
			b = tmp;
		}

		static int sgn(int x, int d)
		{
			return x > 0 ? d : (x < 0 ? -d : 0);
		}

		static void drawXAxis(int y, int x2, int x3, int c2, int c3, Action<int, int, int> plot)
		{
			var d = 0;
			var c = c2;
			var x = x2;
			var adx = abs(x3 - x2);
			var sdx = sgn(x3 - x2, 1);
			var adc = abs(c3 - c2);
			var sdc = sgn(c3 - c2, 1);
			var dc = (sdx == sdc) ? 1 : -1;

			if (adx == 0)
			{
				// 書き込みが一点のケース
				plot(x, y, c);
				return;
			}

			for (var pos = 0; pos <= adx; x += sdx, pos++)
			{
				plot(x, y, c);
				d += adc;
				while (d >= adx)
				{
					c += sdc;
					d -= adx;
				}
			}
		}

		static void drawFlatTriangle(int x1, int y1, int x2, int y2, int x3, int c1, int c2, int c3, Action<int, int, int> plot)
		{
			var ady = abs(y1 - y2);
			var sdy = sgn(y1 - y2, 1);
			var adxa = abs(x1 - x2);
			var sdxa = sgn(x1 - x2, 1);
			var adxb = abs(x1 - x3);
			var sdxb = sgn(x1 - x3, 1);
			var adca = abs(c1 - c2);
			var sdca = sgn(c1 - c2, 1);
			var adcb = abs(c1 - c3);
			var sdcb = sgn(c1 - c3, 1);
			var dxa = (sdxa == sdy) ? 1 : -1;
			var dxb = (sdxb == sdy) ? 1 : -1;
			var dca = (sdca == sdy) ? 1 : -1;
			var dcb = (sdcb == sdy) ? 1 : -1;
			var xa = x2;
			var xb = x3;
			var ca = c2;
			var cb = c3;
			var xda = 0;
			var xdb = 0;
			var cda = 0;
			var cdb = 0;
			var y = y2;

			for (var ypos = 0; ypos <= ady; y += sdy, ypos++)
			{
				drawXAxis(y, xa, xb, ca, cb, plot);
				xda += adxa;
				while (xda >= ady)
				{
					xa += dxa;
					xda -= ady;
				}
				xdb += adxb;
				while (xdb >= ady)
				{
					xb += dxb;
					xdb -= ady;
				}
				cda += adca;
				while (cda >= ady)
				{
					ca += dca;
					cda -= ady;
				}
				cdb += adcb;
				while (cdb >= ady)
				{
					cb += dcb;
					cdb -= ady;
				}
			}
		}



	}
}