using DG.Tweening;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class Player : MonoBehaviour
{
    public Transform Camera;
    public GameObject Stage;
    public Text ScoreText;
    public float Factor = 200;
    private Rigidbody _rigidbody;
    private float _startTime;
    private int _score;

    private Vector3 _distanceToCamera;
    private GameObject _lastStage;
    private Collider _lastCollisionStage;

    // Use this for initialization
    void Start()
    {
        _rigidbody = GetComponent<Rigidbody>();
        _rigidbody.centerOfMass = new Vector3(0, 0.01f, 0);
        _distanceToCamera = Camera.position - transform.position;
        _lastStage = Stage;
        _lastCollisionStage = Stage.GetComponent<Collider>();

        SpawnNextStage();
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
            if (_lastCollisionStage != collision.collider)
            {
                _score++;
                ScoreText.text = _score.ToString();
                SpawnNextStage();
                MoveCamera();
            }
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
        stage.transform.position = _lastStage.transform.position + new Vector3(Random.Range(1.1f, 5), 0, 0);
        _lastStage = stage;

        //random scale
        var originalScale = Stage.transform.localScale;
        var scaleFactor = Random.Range(0.5f, 1);
        var newScale = originalScale * scaleFactor;
        newScale.y = originalScale.y;
        _lastStage.transform.localScale = newScale;
    }

    private void Restart()
    {
        Debug.Log("restart");
        SceneManager.LoadScene(0, LoadSceneMode.Single);
    }
}