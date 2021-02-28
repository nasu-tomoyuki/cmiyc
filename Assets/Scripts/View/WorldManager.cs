using UnityEngine;
using static Unity.Mathematics.math;


namespace View
{

	public class WorldManager : MonoBehaviour
	{
		static public WorldManager Instance;

		[SerializeField]
		public Material MatWalkable = null;

		[SerializeField]
		public Material MatWall = null;

		[SerializeField]
		public Material MatHunterView = null;

		[SerializeField]
		public TextAsset Map = null;

		Camera _camera;

		Logic.WorldManager _logicManager = new Logic.WorldManager();
		public Logic.WorldManager LogicManager => _logicManager;

		Field _field;
		UnitManager _unitManager;

		public GameObject MeModel { get; private set; }
		public GameObject HunterModel { get; private set; }
		public GameObject GemModel { get; private set; }
		public GameObject SakeModel { get; private set; }


		// Start is called before the first frame update
		void Start()
		{
			Instance = this;

			// logic
			_logicManager.setup(Map.text);
			_logicManager.start();

			var size = _logicManager.PathfindingManager.Graph.Size;

			// view
			_camera = Camera.main;

			var go = new GameObject("Field");
			go.transform.parent = transform;
			_field = go.AddComponent<Field>();
			_field.setup(size);

			go = new GameObject("UnitManager");
			go.transform.parent = transform;
			_unitManager = go.AddComponent<UnitManager>();



			transform.localPosition = new Vector3(-size.x * 0.5f, 0.0f, -size.y * 0.5f);


			// リソース検索
			MeModel = transform.Find("Me").gameObject;
			MeModel.SetActive(false);
			MeModel.transform.localPosition = Vector3.zero;

			HunterModel = transform.Find("Hunter").gameObject;
			HunterModel.SetActive(false);
			HunterModel.transform.localPosition = Vector3.zero;

			GemModel = transform.Find("Gem").gameObject;
			GemModel.SetActive(false);
			GemModel.transform.localPosition = Vector3.zero;

			SakeModel = transform.Find("Sake").gameObject;
			SakeModel.SetActive(false);
			SakeModel.transform.localPosition = Vector3.zero;
		}

		void Update()
		{

			// クリックした場所に酒を配置
			if (Input.GetButtonDown("Fire1"))
			{
				var camPos = _camera.transform.position;
				var mousePos = Input.mousePosition;
				mousePos.z = 10.0f;
				var pos = _camera.ScreenToWorldPoint(mousePos);
				if (pos.y != 0.0f)
				{
					var diff = pos - camPos;
					var dir = Vector3.Normalize(diff);
					var dist = camPos.y / dir.y;
					pos = camPos - dir * dist;
				}
				pos = pos - _field.transform.position + Vector3.one * 0.5f;
				_unitManager.createSake(int2((int)pos.x, (int)pos.z));
			}
		}

		// Update is called once per frame
		void FixedUpdate()
		{
			// logic
			_logicManager.update();
		}
	}

}