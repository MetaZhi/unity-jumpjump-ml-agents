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
    public GameObject Stage;
    public Text SingleScoreText;
    public Text TotalScoreText;
    public GameObject Particle;
    public float Factor = 5;
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

    public override void InitializeAgent()
    {
        Debug.Log("InitializeAgent");
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
        state.Add(transform.localPosition.x);
        state.Add(transform.localPosition.z);
        state.Add(_nextStage.transform.localPosition.x);
        state.Add(_nextStage.transform.localPosition.z);
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
        foreach (var s in _spawnStages)
        {
            Destroy(s);
        }
        _spawnStages.Clear();

        _currentStage = Stage;
        _currentStage.GetComponent<Renderer>().material.color =
            new Color(Random.Range(0, 1f), Random.Range(0, 1f), Random.Range(0, 1f));

        _direction = new Vector3(1, 0, 0);

        _tween?.Kill();

        SpawnNextStage();
        _score = 0;
        TotalScoreText.text = _score.ToString();
        _disableInput = false;

        transform.position = _playerStartPosition;
        Camera.position = transform.position + _distanceToCamera;
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
                if (contacts.Length == 1 && Math.Abs(contacts[0].point.y) < 0.05f)
                {
                    _currentStage = collision.gameObject;
                    reward += 1;
                    _score += 1;

                    TotalScoreText.text = _score.ToString();

                    RandomDirection();
                    SpawnNextStage();
                    MoveCamera();

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
                if (contacts.Length == 1 && Math.Abs(contacts[0].point.y) < 0.05f)
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

    private void MoveCamera()
    {
        var nextPosition = transform.position + _distanceToCamera;
        _tween = Camera.DOMove(nextPosition, 1f);
    }

    private void SpawnNextStage()
    {
        var nextStage = Instantiate(Stage);
        nextStage.transform.position = _currentStage.transform.position + _direction * Random.Range(1.1f, _academy.MaxDistance);

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
        _spawnStages.Add(_nextStage);
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