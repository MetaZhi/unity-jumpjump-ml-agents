using UnityEngine;

public class Player : MonoBehaviour
{
    public Transform Camera;
    public float Factor = 200;
    private Rigidbody _rigidbody;
    private float _startTime;
    private int _score;

    // Use this for initialization
    void Start()
    {
        _rigidbody = GetComponent<Rigidbody>();
        _rigidbody.centerOfMass = new Vector3(0, 0.01f, 0);
    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Space))
        {
            _startTime = Time.time;
        }

        if (Input.GetKeyUp(KeyCode.Space))
        {
            var elapse = Time.time - _startTime;
            Jump(elapse);
        }
    }

    void Jump(float elapse)
    {
        Debug.Log(elapse);
        _rigidbody.AddForce(new Vector3(1, 1, 0) * elapse * Factor);
    }

    void OnCollisionEnter(Collision collision)
    {
        Debug.Log(collision.gameObject.name);
        if (collision.gameObject.name == "Ground")
        {
            Restart();
        }
        else
        {
            _score++;
            SpawnNextStage();
        }
    }

    private void SpawnNextStage()
    {
        throw new System.NotImplementedException();
    }

    private void Restart()
    {
        
    }
}