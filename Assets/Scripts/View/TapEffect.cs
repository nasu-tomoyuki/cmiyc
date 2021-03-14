using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace View
{
    public class TapEffect : MonoBehaviour
    {
		ParticleSystem _particleSystem;

        [SerializeField]
        Camera _camera;

        // Start is called before the first frame update
        void Start()
        {
            _particleSystem = GetComponent<ParticleSystem>();            
        }

        // Update is called once per frame
        void Update()
        {
			if (Input.GetButtonDown("Fire1"))
			{
				var mousePos = Input.mousePosition;
				mousePos.z = 10.0f;

				transform.position = _camera.ScreenToWorldPoint(mousePos);
				_particleSystem.Emit( 10 );
			}
            
        }

    }
}