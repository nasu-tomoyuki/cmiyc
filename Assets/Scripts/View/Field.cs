using Unity.Mathematics;
using UnityEngine;


namespace View
{
	class Field : MonoBehaviour
	{
		GameObject[,] _map;
		int2 _size;

		public void setup(int2 size)
		{
			_size = size;

			_map = new GameObject[size.y, size.x];
			for (var y = 0; y < size.y; ++y)
			{
				for (var x = 0; x < size.x; ++x)
				{
					var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
					go.transform.parent = transform;
					go.transform.localPosition = new Vector3(x, 0.0f, y);
					_map[y, x] = go;
				}
			}
		}

		void Update()
		{
			apply();
		}

		void apply()
		{
			var heatMap = WorldManager.Instance.LogicManager.HunterViewMap;
			if (WorldManager.Instance.IsViewHazard)
			{
				heatMap = WorldManager.Instance.LogicManager.HazardMap;
			}
			var map = WorldManager.Instance.LogicManager.PathfindingManager.Graph.Map;
			for (var y = 0; y < _size.y; ++y)
			{
				for (var x = 0; x < _size.x; ++x)
				{
					var go = _map[y, x];
					Material mat = null;
					var h = 1.0f;
					var terrain = (Logic.Pathfinding.TerrainRoles)map[y, x].Terrain;
					if (terrain == Logic.Pathfinding.TerrainRoles.None)
					{
						go.SetActive(false);
						continue;
					}
					switch (terrain)
					{
						case Logic.Pathfinding.TerrainRoles.Ground:
							mat = WorldManager.Instance.MatWalkable;
							h = 0.1f;
							break;
						case Logic.Pathfinding.TerrainRoles.Wall:
							mat = WorldManager.Instance.MatWall;
							h = 1.0f;
							break;
					}

					if (!WorldManager.Instance.IsViewHazard)
					{
						if (heatMap[y, x] > 0)
						{
							mat = WorldManager.Instance.MatHunterView;
						}
					}
					else
					{
						h = heatMap[y, x] / (float)(Logic.WorldManager.HUNTER_HAZARD / 10);
					}
					go.transform.localScale = new Vector3(0.9f, h, 0.9f);
					go.GetComponent<Renderer>().material = mat;
					go.SetActive(true);
				}
			}
		}
	}

}