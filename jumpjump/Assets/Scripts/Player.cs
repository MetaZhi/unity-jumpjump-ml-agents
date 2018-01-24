using DG.Tweening;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Random = UnityEngine.Random;

public class Player : MonoBehaviour
{
    public Transform Body;
    public Transform Head;
    public Transform Camera;
    public GameObject Stage;
    public Text SingleScoreText;
    public Text TotalScoreText;
    public GameObject Particle;
    public float Factor = 200;
    private Rigidbody _rigidbody;
    private float _startTime;
    private int _score;

    private Vector3 _distanceToCamera;
    private GameObject _currentStage;

    private Vector3 _direction;

    private bool _isPlayingScoreAnim;
    private float _scoreAnimStartTime;

    // Use this for initialization
    void Start()
    {
        _rigidbody = GetComponent<Rigidbody>();
        _rigidbody.centerOfMass = Vector3.zero;
        _distanceToCamera = Camera.position - transform.position;

        _currentStage = Stage;
        _currentStage.GetComponent<Renderer>().material.color =
            new Color(Random.Range(0, 1f), Random.Range(0, 1f), Random.Range(0, 1f));

        _direction = new Vector3(1, 0, 0);

        SpawnNextStage();
    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Space))
        {
            _startTime = Time.time;
            OnTappingStart();
        }

        if (Input.GetKeyUp(KeyCode.Space))
        {
            var elapse = Time.time - _startTime;
            Jump(elapse);
            OnTappingEnd();
        }

        if (Input.GetKey(KeyCode.Space))
        {
            _currentStage.transform.localScale -= new Vector3(0, 0.15f, 0) * Time.deltaTime;
            _currentStage.transform.position -= new Vector3(0, 0.15f, 0) * Time.deltaTime;

            Body.transform.localScale -= new Vector3(-0.05f, 0.05f, -0.05f) * Time.deltaTime;
            Body.transform.position -= new Vector3(0, 0.05f, 0) * Time.deltaTime;
            Head.transform.position -= new Vector3(0, 0.1f, 0) * Time.deltaTime;
        }

        if (_isPlayingScoreAnim)
        {
            UpdateScoreAnim();
        }
    }

    private void OnTappingStart()
    {
        Particle.SetActive(true);
    }

    private void OnTappingEnd()
    {
        Particle.SetActive(false);
        _currentStage.transform.DOScaleY(0.5f, 0.2f);
        _currentStage.transform.DOMoveY(-0.25f, 0.2f);

        Body.transform.DOScale(0.1f, 0.2f);
        Body.transform.DOLocalMoveY(0.1f, 0.2f);
        Head.transform.DOLocalMoveY(0.29f, 0.2f);
    }

    void Jump(float elapse)
    {
        Debug.Log(elapse);
        _rigidbody.AddForce((_direction + new Vector3(0, 1, 0)) * elapse * Factor, ForceMode.Impulse);
    }


    private void ShowScoreAnim()
    {
        _isPlayingScoreAnim = true;
        _scoreAnimStartTime = Time.time;
    }

    private void UpdateScoreAnim()
    {
        if (Time.time - _scoreAnimStartTime > 1)
        {
            _isPlayingScoreAnim = false;
        }

        var pos = RectTransformUtility.WorldToScreenPoint(Camera.GetComponent<Camera>(), transform.position);
        SingleScoreText.transform.position =
            pos + Vector2.Lerp(Vector2.zero, new Vector2(0, 200), Time.time - _scoreAnimStartTime);

        SingleScoreText.GetComponent<Text>().color =
            Color.Lerp(Color.black, new Color(0, 0, 0, 0), Time.time - _scoreAnimStartTime);
    }

    private void MoveCamera()
    {
        var nextPosition = transform.position + _distanceToCamera;
        Camera.DOMove(nextPosition, 1f);
    }

    private void SpawnNextStage()
    {
        var nextStage = Instantiate(Stage);
        nextStage.transform.position = _currentStage.transform.position + _direction * Random.Range(1.1f, 3);

        //random scale
        var originalScale = Stage.transform.localScale;
        var scaleFactor = Random.Range(0.5f, 1);
        var newScale = originalScale * scaleFactor;
        newScale.y = originalScale.y;
        nextStage.transform.localScale = newScale;

        //random color
        nextStage.GetComponent<Renderer>().material.color =
            new Color(Random.Range(0, 1f), Random.Range(0, 1f), Random.Range(0, 1f));
    }

    private void RandomDirection()
    {
        var dir = Random.Range(0, 2);
        _direction = dir == 0 ? new Vector3(1, 0, 0) : new Vector3(0, 0, 1);
    }

    private void Restart()
    {
        Debug.Log("restart");
        SceneManager.LoadScene(0, LoadSceneMode.Single);
    }
}