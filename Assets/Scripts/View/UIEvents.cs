using UnityEngine;
using UnityEngine.UI;
using static Unity.Mathematics.math;


namespace View
{
	public class UIEvents : MonoBehaviour
	{
		public void OnIsVisibleHazardClick(Toggle change)
		{
			WorldManager.Instance.IsViewHazard = change.isOn;
		}
	}
}
