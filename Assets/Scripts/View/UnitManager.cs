using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;


namespace View
{

	public class UnitManager : MonoBehaviour
	{
		Dictionary<uint, Unit> _units = new Dictionary<uint, Unit>();
		List<Unit> _work = new List<Unit>();

		public int LastUpdatedAt { get; private set; }


		void Start()
		{
		}

		void Update()
		{
			++LastUpdatedAt;

			// Logic.Unit から View.Unit へ同期する
			var uds = WorldManager.Instance.LogicManager.OpenUnitDatas;
			foreach (var ud in uds)
			{
				// 新規
				if (!_units.TryGetValue(ud.Handle, out var u))
				{
					var go = new GameObject($"Unit {ud.Role.ToString()}");
					u = go.AddComponent<Unit>();
					go.transform.parent = transform;
					_units.Add(ud.Handle, u);
					u.init(ud);
				}
				else
				{ // 更新
					u.update(ud);
				}
			}
		}

		public void createSake(int2 pos)
		{
			WorldManager.Instance.LogicManager.UnitManager.createSake(pos);
		}
	}

}