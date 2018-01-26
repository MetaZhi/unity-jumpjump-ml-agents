using System;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;
using Random = UnityEngine.Random;

public class BottleFlipAgent : Agent
{
    public Transform Body;
    public Transform Head;
    public Transform Camera;
    public bool IsMoveCamera = false;
    public GameObject Stage;
    public Text SingleScoreText;
    public Text TotalScoreText;
    public GameObject Particle;
    public float Factor = 5;
    public Transform Ground;

    private Rigidbody _rigidbody;

    private Vector3 _distanceToCamera;
    private GameObject _currentStage;

    private Vector3 _direction;

    private float _scoreAnimStartTime;
    private Vector3 _playerStartPosition;
    private GameObject _nextStage;
    List<GameObject> _spawnStages = new List<GameObject>();
    private Tweener _tween;
    private bool _disableInput = true;
    private int _score;
    private BottleFlipAcademy _academy;
    private int _lastReward = 1;

    public override void InitializeAgent()
    {
        _playerStartPosition = transform.localPosition;
        InitGame();
    }

    void InitGame()
    {
        _rigidbody = GetComponent<Rigidbody>();
        _rigidbody.centerOfMass = Vector3.zero;
        _distanceToCamera = Camera.position - transform.position;

        _academy = brain.transform.parent.GetComponent<BottleFlipAcademy>();
    }

    public override List<float> CollectState()
    {
        List<float> state = new List<float>();
        if (_direction.x == 1)
            state.Add(_nextStage.transform.localPosition.x - transform.localPosition.x);
        else
            state.Add(_nextStage.transform.localPosition.z - transform.localPosition.z);

        state.Add(_nextStage.transform.localScale.x);

        return state;
    }

    public override void AgentStep(float[] act)
    {
        var action = Mathf.Clamp01(act[0]);

        Jump(action);
    }

    void Jump(float elapse)
    {
        if (_disableInput)
        {
            return;
        }

        elapse = Mathf.Clamp(elapse, 0.1f, 2);

        var dir = _direction + new Vector3(0, 1.5f, 0);

        _rigidbody.AddForce(dir * elapse * Factor, ForceMode.Impulse);
    }

    public override void AgentReset()
    {
        _rigidbody.velocity = Vector3.zero;
        _rigidbody.isKinematic = true;
        transform.localPosition = _playerStartPosition;
        if (IsMoveCamera)
            Camera.position = transform.position + _distanceToCamera;
        _rigidbody.isKinematic = false;
        foreach (var s in _spawnStages)
        {
            s.SetActive(false);
        }

        _currentStage = Stage;
        _currentStage.GetComponent<Renderer>().material.color =
            new Color(Random.Range(0, 1f), Random.Range(0, 1f), Random.Range(0, 1f));

        _direction = new Vector3(1, 0, 0);

        _tween?.Kill();

        SpawnNextStage();
        MoveGround();
        _score = 0;
        _lastReward = 1;
        TotalScoreText.text = _score.ToString();
        _disableInput = false;
    }

    void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.name == "Ground")
        {
            Restart();
        }
        else
        {
            if (_currentStage != collision.gameObject)
            {
                var contacts = collision.contacts;

                //check if player's feet on the stage
                if (contacts.Length == 1 && Mathf.Abs(contacts[0].point.y) < 0.05f)
                {
                    _currentStage = collision.gameObject;
                    AddScore(contacts);
                    RandomDirection();
                    SpawnNextStage();
                    MoveCamera();
                    MoveGround();

                    _disableInput = false;
                }
                else // body collides with the box
                {
                    Restart();
                }
            }
            else //still on the same box
            {
                var contacts = collision.contacts;

                //check if player's feet on the stage
                if (contacts.Length == 1 && Mathf.Abs(contacts[0].point.y) < 0.05f)
                {
                    _disableInput = false;
                }
                else // body just collides with this box
                {
                    Restart();
                }
            }
        }
    }

    /// <summary>
    /// If you jump to the center of the box, the score will multiply the last reward score.
    /// </summary>
    private void AddScore(ContactPoint[] contacts)
    {
        if (contacts.Length > 0)
        {
            var hitPoint = contacts[0].point;
            hitPoint.y = 0;

            var stagePos = _currentStage.transform.localPosition;
            stagePos.y = 0;

            var precision = Vector3.Distance(hitPoint, stagePos);
            if (precision < 0.1)
            {
                _lastReward *= 2;
                reward += 1;
            }
            else
            {
                _lastReward = 1;
                reward += 0.5f;
            }
            _score += _lastReward;
            TotalScoreText.text = _score.ToString();
        }
    }

    private void MoveCamera()
    {
        if (!IsMoveCamera) return;
        var nextPosition = transform.position + _distanceToCamera;
        _tween = Camera.DOMove(nextPosition, 1f);
    }

    private void MoveGround()
    {
        Ground.transform.localPosition = _currentStage.transform.localPosition - new Vector3(0, 0.25f, 0);
    }

    GameObject GetStage()
    {
        GameObject nextStage = null;
        if (_spawnStages.Count < 10)
        {
            nextStage = Instantiate(Stage);
            nextStage.transform.SetParent(Stage.transform.parent);
            _spawnStages.Add(nextStage);
        }
        else
        {
            nextStage = _spawnStages[0];
            nextStage.SetActive(true);
            _spawnStages.RemoveAt(0);
            _spawnStages.Add(nextStage);
        }

        return nextStage;
    }

    private void SpawnNextStage()
    {
        var nextStage = GetStage();
        nextStage.transform.localPosition =
            _currentStage.transform.localPosition + _direction * Random.Range(1.1f, _academy.MaxDistance);

        //random scale
        var originalScale = Stage.transform.localScale;
        var scaleFactor = Random.Range(_academy.MinScale, 1);
        var newScale = originalScale * scaleFactor;
        newScale.y = originalScale.y;
        nextStage.transform.localScale = newScale;

        //random color
        nextStage.GetComponent<Renderer>().material.color =
            new Color(Random.Range(0, 1f), Random.Range(0, 1f), Random.Range(0, 1f));

        _nextStage = nextStage;
    }

    private void RandomDirection()
    {
        if (!_academy.IsRandomDirection)
            return;

        var dir = Random.Range(0, 2);
        _direction = dir == 0 ? new Vector3(1, 0, 0) : new Vector3(0, 0, 1);
    }

    private void Restart()
    {
        reward -= 1;
        done = true;
    }

    void OnCollisionExit(Collision collision)
    {
        if (collision.gameObject.name.Contains("Cube"))
        {
            _disableInput = true;
        }
    }
}