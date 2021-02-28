using static Unity.Mathematics.math;
using UnityEngine;


namespace View
{
	public class Unit : MonoBehaviour
	{
		UnitManager _manager;
		int _lastUpdatedAt;
		Vector3 _position;
		float _rotation;
		GameObject _model;
		GameObject _exclamation;
		GameObject _lookTarget;
		Logic.UnitStateFlags _states;

		private Animator _anim;                     // Animatorへの参照
		private AnimatorStateInfo _currentState;        // 現在のステート状態を保存する参照
		private AnimatorStateInfo _previousState;   // ひとつ前のステート状態を保存する参照


		void Start()
		{
		}

		void Update()
		{
			if (_lastUpdatedAt != _manager.LastUpdatedAt)
			{
				GameObject.Destroy(transform.gameObject);
				return;
			}
		}

		internal void init(Logic.WorldManager.OpenUnitData ud)
		{
			_manager = transform.parent.GetComponent<UnitManager>();

			switch (ud.Role)
			{
				case Logic.Roles.Me:
					{
						_model = Instantiate<GameObject>(WorldManager.Instance.MeModel);
					}
					break;
				case Logic.Roles.Hunter:
					{
						_model = Instantiate<GameObject>(WorldManager.Instance.HunterModel);
					}
					break;
				case Logic.Roles.Gem:
					{
						_model = Instantiate<GameObject>(WorldManager.Instance.GemModel);
					}
					break;
				case Logic.Roles.Sake:
					{
						_model = Instantiate<GameObject>(WorldManager.Instance.SakeModel);
					}
					break;
			}

			_model.transform.SetParent(transform, false);
			_model.SetActive(true);

			_exclamation = _model.transform.Find("Exclamation")?.gameObject;
			if (_exclamation != null)
			{
				_exclamation.SetActive(false);
			}

			_lookTarget = _model.transform.Find("LookTarget")?.gameObject;

			_anim = _model.transform.Find("Character")?.gameObject.GetComponent<Animator>();
			if (_anim != null)
			{
				_currentState = _anim.GetCurrentAnimatorStateInfo(0);
				_previousState = _currentState;
			}

			update(ud);
		}

		internal void update(Logic.WorldManager.OpenUnitData ud)
		{
			_lastUpdatedAt = _manager.LastUpdatedAt;
			_position = new Vector3(ud.Position.x, 0.0f, ud.Position.y);
			_rotation = ud.Rotation;
			_states = ud.States;

			transform.localPosition = _position;
			transform.localRotation = Quaternion.Euler(0.0f, -degrees(_rotation), 0.0f);

			if (_exclamation != null)
			{
				var hasTarget = (_states & Logic.UnitStateFlags.HasTarget) != 0;
				_exclamation.SetActive(hasTarget);
			}

			if (_lookTarget != null)
			{
				var rot = _lookTarget.transform.worldToLocalMatrix.rotation;
				var p = new Vector3(ud.TargetPosition.x, 0.0f, ud.TargetPosition.y) - _position;
				_lookTarget.transform.localPosition = rot * p;
			}

			changeAnimation("Walking", hasState(Logic.UnitStateFlags.Walking));
			changeAnimation("Taking", hasState(Logic.UnitStateFlags.Taking));
			changeAnimation("Drunk", hasState(Logic.UnitStateFlags.Drunk));
		}

		bool hasState(Logic.UnitStateFlags flag)
		{
			return (_states & flag) != 0;
		}

		void changeAnimation(string name, bool flag)
		{
			if (_anim == null)
			{
				return;
			}
			_anim.SetBool(name, flag);
		}
	}

}