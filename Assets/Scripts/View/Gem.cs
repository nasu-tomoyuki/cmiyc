using UnityEngine;

public class Gem : MonoBehaviour
{
    // 往復する秒数
    [SerializeField]
    float BouncingTime = 0.5f;

    [SerializeField]
    Vector3 BasePosition = new Vector3( 0.0f, 0.5f, 0.0f );

    [SerializeField]
    Vector3 Bouncing = new Vector3( 0.0f, 1.0f, 0.0f );

    // 回転する秒数
    [SerializeField]
    float SwingingTime = 0.5f;

    [SerializeField]
    Vector3 BaseRotation = new Vector3( 0.0f, 0.0f, 0.0f );

    [SerializeField]
    Vector3 Swinging = new Vector3( 0.0f, 0.0f, 0.0f );


    float BouncingSpeed;
    float BouncingRad;

    float SwingingSpeed;
    float SwingingRad;


    // Start is called before the first frame update
    void Start()
    {
        BouncingSpeed = Mathf.PI / BouncingTime;
        BouncingRad = 0.0f;

        SwingingSpeed = Mathf.PI / SwingingTime;
        SwingingRad = 0.0f;
    }


    // Update is called once per frame
    void Update()
    {
        var s = Mathf.Sin( BouncingRad );
        BouncingRad += BouncingSpeed * Time.deltaTime;
        transform.localPosition = BasePosition + Bouncing * s;

        s = Mathf.Sin( SwingingRad );
        SwingingRad += SwingingSpeed * Time.deltaTime;
        var q = Quaternion.Euler( BaseRotation ) * Quaternion.Euler( Swinging * s );
        transform.localRotation = q;
    }
}
