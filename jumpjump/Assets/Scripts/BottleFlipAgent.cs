using System.Collections.Generic;
using System.Linq;
using DG.Tweening;
using UnityEngine;
using Random = UnityEngine.Random;

class Counter
{
    public static List<int> skippedFrame = new List<int>();

    public static void Add(int count)
    {
        if (count <= 0)
            return;

        skippedFrame.Add(count);
        if (skippedFrame.Count % 100 == 0)
            Debug.Log(skippedFrame.Average());
    }
}

public class BottleFlipAgent : Agent
{
    public Transform Body;
    public Transform Head;
    public Transform Camera;
    public bool IsMoveCamera = false;
    public GameObject Stage;
    public GameObject Particle;
    public float Factor = 5;
    public Transform Ground;

    private Rigidbody _rigidbody;

    private Vector3 _distanceToCamera;
    private GameObject _currentStage;

    private Vector3 _direction;

    private Vector3 _playerStartPosition;
    private GameObject _nextStage;
    List<GameObject> _spawnStages = new List<GameObject>();
    private Tweener _tween;
    private bool _enableInput = true;
    private int _score;
    private BottleFlipAcademy _academy;
    private int _lastReward = 1;

    //for debug
    private int _actionCount;

    private int _biggestActionCount = 137;
    public List<string> logs = new List<string>();
    private float _lastElapse;

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
        if (_direction.x > 0.5)
            state.Add((_nextStage.transform.localPosition.x - transform.localPosition.x) / 3);
        else
            state.Add((_nextStage.transform.localPosition.z - transform.localPosition.z) / 3);

        state.Add(_nextStage.transform.localScale.x);

        return state;
    }

    public override void AgentStep(float[] act)
    {
        Jump(act[0]);
        _actionCount += 1;

        // sometimes the player will go through the ground 
        if (transform.localPosition.y < -1)
            Restart();
    }

    void Jump(float elapse)
    {
        // player is in the air, stop jumping
        if (!_enableInput)
            return;
#if UNITY_EDITOR
        Counter.Add(_actionCount);
#endif
        if (_actionCount > _biggestActionCount)
        {
            _biggestActionCount = _actionCount;
            Debug.Log(id + "Skipped frames count:" + _biggestActionCount);
#if !UNITY_EDITOR
            foreach (var log in logs)
            {
                Debug.Log(log);
            }
#endif
        }
        _actionCount = 0;
        logs.Clear();
        elapse = Mathf.Clamp(elapse, 0.1f, 1);
        _lastElapse = elapse;

        var dir = _direction + new Vector3(0, 1.5f, 0);

        _rigidbody.AddForce(dir * elapse * Factor, ForceMode.Impulse);
        AddLog($"Action:{_lastElapse};Player:{transform.localPosition};Box:{_nextStage.transform.localPosition}");
    }

    public override void AgentReset()
    {
        AddLog($"(AgentReset)Player Position:{transform.localPosition}");
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
    }

    void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.name == "Ground")
        {
            AddLog($"Collide Ground({collision.gameObject.name}:{collision.gameObject.GetInstanceID()})");
            Restart();
        }
        else
        {
            if (collision.gameObject == _nextStage) // jump to the next box
            {
                var contacts = collision.contacts;

                //check if player's feet on the stage
                if (contacts.Length == 1 && contacts[0].normal == Vector3.up)
                {
                    AddLog(
                        $"Collide _nextStage top({collision.gameObject.name}:{collision.gameObject.GetInstanceID()})");
                    _currentStage = collision.gameObject;
                    AddScore(contacts);
                    RandomDirection();
                    SpawnNextStage();
                    MoveCamera();
                    MoveGround();

                    StartReceivingAction();
                    AddLog("EnableInput(_currentStage != collision.gameObject)");
                }
                else // body collides with the box
                {
                    AddLog(
                        $"Collide _nextStage side({collision.gameObject.name}:{collision.gameObject.GetInstanceID()})");
                    Restart();
                }
            }
            else if (collision.gameObject == _currentStage) //still on the same box
            {
                var contacts = collision.contacts;

                //check if player's feet on the stage
                if (contacts.Length == 1 && contacts[0].normal == Vector3.up)
                {
                    AddLog(
                        $"Collide _currentStage top({collision.gameObject.name}:{collision.gameObject.GetInstanceID()})");
                    StartReceivingAction();
                    AddLog("EnableInput(still on the same box)");
                }
                else // body just collides with this box
                {
                    AddLog(
                        $"Collide _currentStage side({collision.gameObject.name}:{collision.gameObject.GetInstanceID()})");
                    Restart();
                }
            }
            else
            {
                AddLog(
                    $"Collide may be previous box({collision.gameObject.name}:{collision.gameObject.GetInstanceID()})");
                Restart();
            }
        }

        Monitor.Log("LastScore", _lastReward, MonitorType.text, transform);
        Monitor.Log("Score", _score, MonitorType.text, transform);
        Monitor.Log("Reward", reward, MonitorType.slider, transform);
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

            var stagePos = _currentStage.transform.position;
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
        Ground.transform.localPosition = _currentStage.transform.localPosition - new Vector3(0, 0.75f, 0);
    }

    GameObject GetStage()
    {
        GameObject nextStage;
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
        StartReceivingAction();
        done = true;
    }

    void OnCollisionExit(Collision collision)
    {
        AddLog(
            $"OnCollisionExit({collision.gameObject.name}:{collision.gameObject.GetInstanceID()})");
        if (collision.gameObject.name.Contains("Cube"))
        {
            StopReceivingAction();
            AddLog("DisableInput(OnCollisionExit)");
        }
    }

    void AddLog(string log, bool print = false)
    {
        var l = $"[{id}]{log}:{Time.time}";
#if UNITY_EDITOR
        if (print)
            Debug.Log(l);
#endif
        logs.Add(l);
    }

    void StartReceivingAction()
    {
        _enableInput = true;
        // receive action from barin
        // SubscribeBrain();
    }

    void StopReceivingAction()
    {
        _enableInput = false;
        // receive no action from barin
        // UnsubscribeBarin();
    }

    void SubscribeBrain()
    {
        AddLog("SubscribeBrain");
        if (brain != null && !brain.agents.ContainsKey(id))
            brain.agents.Add(id, gameObject.GetComponent<Agent>());
    }

    void UnsubscribeBarin()
    {
        AddLog("UnsubscribeBarin");
        if (brain != null && brain.agents.ContainsKey(id))
            brain.agents.Remove(id);
    }
}