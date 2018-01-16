using DG.Tweening;
using UnityEngine;
using UnityEngine.SceneManagement;

public class Player : MonoBehaviour
{
    public Transform Camera;
    public GameObject Stage;
    public float Factor = 200;
    private Rigidbody _rigidbody;
    private float _startTime;
    private int _score;

    private Vector3 _distanceToCamera;
    private GameObject _lastStage;

    // Use this for initialization
    void Start()
    {
        _rigidbody = GetComponent<Rigidbody>();
        _rigidbody.centerOfMass = new Vector3(0, 0.01f, 0);
        _distanceToCamera = Camera.position - transform.position;
        _lastStage = Stage;
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
            MoveCamera();
        }
    }

    private void MoveCamera()
    {
        var nextPosition = transform.position + _distanceToCamera;
        Camera.DOMove(nextPosition, 1);
    }

    private void SpawnNextStage()
    {
        var stage = Instantiate(Stage);
        stage.transform.position = _lastStage.transform.position + new Vector3(Random.Range(1.1f,5),0,0);
        _lastStage = stage;
    }

    private void Restart()
    {
        Debug.Log("restart");
        SceneManager.LoadScene(0, LoadSceneMode.Single);
    }
}