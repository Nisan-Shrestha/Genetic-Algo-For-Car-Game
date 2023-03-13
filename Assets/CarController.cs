using MathNet.Numerics;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.VisualScripting;
using UnityEngine;

[RequireComponent(typeof(NeuralNet))]
public class CarController : MonoBehaviour
{
    public NeuralNet network;

    private const string HORIZONTAL = "Horizontal";
    private const string VERTICAL = "Vertical";

    private int WPpassed = 0;
    private float horizontalInput;
    private float verticalInput;
    private float speed;
    private float angularVelocityY;
    private float currentSteerAngle;
    private float currentbreakForce;
    private bool isBreaking;

    private List<WayPoints> waypoints;
    private WayPoints startWaypoint;
    private List<Collider> wpColliders = new List<Collider>();
    public float score = 0f;

    public bool dead = false;

    [SerializeField] private float motorForce;
    [SerializeField] private float breakForce;
    [SerializeField] private float maxSteerAngle;


    [SerializeField] private WheelCollider frontLeftWheelCollider;
    [SerializeField] private WheelCollider frontRightWheelCollider;
    [SerializeField] private WheelCollider rearLeftWheelCollider;
    [SerializeField] private WheelCollider rearRightWheelCollider;

    [SerializeField] private Transform frontLeftWheelTransform;
    [SerializeField] private Transform frontRightWheeTransform;
    [SerializeField] private Transform rearLeftWheelTransform;
    [SerializeField] private Transform rearRightWheelTransform;

    [SerializeField] private int WPScore = 5;


    private Rigidbody myRB;
    private BoxCollider myCollider;

    private float sensorL, sensorFL, sensorF, sensorFR, sensorR;


    public bool UserController;

    private Vector3 initialPosition, initialRotation;

    private void Awake()
    {
        network = GetComponent<NeuralNet>();
        initialPosition = transform.position;
        initialRotation = transform.eulerAngles;
        //network.Init(HiddenLayerCount, HiddenNeuronCount);
    }

    private void Start()
    {
        myRB= this.GetComponent<Rigidbody>();
        myRB.centerOfMass -= new Vector3(0, .2f, 0);
        myCollider = this.GetComponent<BoxCollider>();
        var Gc = FindObjectOfType<GameController>();
        while (Gc.waypoints.Length == 0) ;
        waypoints = Gc.waypoints.ToList();
        startWaypoint = Gc.startingPoint;
        for (int i = 0; i < waypoints.Count(); i++)
        {
            var wpc = waypoints[i].GetComponent<Collider>();
            
            wpColliders.Add(wpc);

        }
    }

    private void FixedUpdate()
    {
        if (dead && !UserController)
        {
            horizontalInput = 0;
            verticalInput = 0;
            HandleMotor();
            currentbreakForce = breakForce / 5;
            ApplyBreaking();
            return;
        }
        if (waypoints.Count() ==0)
        {
            horizontalInput = 0;
            verticalInput = 0;
            HandleMotor();
            currentbreakForce = breakForce/5;
            ApplyBreaking();
            var Gc = FindObjectOfType<GameController>();
            Gc.LifeTime -= 5.0f;
            Gc.Repopulate();
            return;
        }
        if (UserController)
            GetInput();
        else
            HandleInput();
        HandleMotor();
        HandleSteering();
        UpdateWheels();
        UpdateScore();
       
    }

    public void ResetNetwork(NeuralNet net)
    {
        network = net;
        Reset();
    }

    private void Reset()
    {
        score = 0;
        transform.position = initialPosition;
        transform.eulerAngles = initialRotation;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (wpColliders.Count() == 0)
            return;
        if (other.GetInstanceID() == wpColliders[0].GetInstanceID())
        {
            WPpassed += 1;
            //Debug.Log("WP:" + waypoints[0].name + "reached. Score:" + score);
            wpColliders.RemoveAt(0);
            startWaypoint = waypoints[0];
            waypoints.RemoveAt(0);
        }
    }

    private void OnCollisionEnter(Collision collision)
    {
        // maybe delete them later?
        if (!dead)
            score /= 4;
        dead = true;
        //Debug.Log("KILLLEEEEEEEDDDDDDDDDD");
    }

    private void UpdateScore()
    {
        float dist = Vector3.Distance(this.transform.position, waypoints[0].transform.position);
        float distBetWP = Vector3.Distance(startWaypoint.transform.position, waypoints[0].transform.position);
        //Debug.Log(distBetWP + " " + dist);
        score = (distBetWP - dist) + WPpassed * WPScore;
        //Debug.Log(score);
    }

    private void HandleInput()
    {
        ComputeSensors();

        const float turnThreshold = 0.05f;
        const float motorThreshold = .001f;
        const float brakeThreshold = 0.8f;

        speed = myRB.velocity.magnitude;
        angularVelocityY = myRB.angularVelocity.y;

        float left, right, forwards, backwards, brake;
        (left, right, forwards, backwards, brake) = network.Run(sensorL, sensorFL, sensorF, sensorFR, sensorR, speed,angularVelocityY);
        
        isBreaking = brake > brakeThreshold;
        horizontalInput = (Math.Abs(left - right) < turnThreshold) ? 0 : ((left - right) > 0) ? -1 : 1;
        verticalInput   = (Math.Abs(backwards - forwards) < motorThreshold) ? 0 : ( (backwards- forwards) > 0) ? -1 : 1;
        //Debug.Log(horizontalInput + " " + isBreaking + " " + verticalInput );
        //Debug.Log(left + " " + right + " " + forwards + " " + backwards + " " + brake);

        //horizontalInput = (right > threshold ? 1 : 0) - (left > threshold ? 1 : 0);
        //verticalInput = (forwards > threshold ? 1 : 0) - (backwards > threshold ? 1 : 0);
        //
        //horizontalInput = (right  - left) > 0 ? 1 : -1;
        //verticalInput = (forwards - backwards) > 0 ? 1 : -1;
        //verticalInput = (forwards > threshold ? 1 : 0) - (backwards > threshold ? 1 : 0);
    }

    (bool, float) RaySensor(Ray r)
    {
        RaycastHit hit;
        float sensor = 0;
        bool striked = false;
        int layer_mask = LayerMask.GetMask("Barrier");
        if (striked = Physics.Raycast(r, out hit,float.MaxValue,layer_mask))
        {
            
            //Debug.Log(hit.distance, hit.rigidbody);
            sensor =  Mathf.Sqrt( hit.distance);  // need to test this value, done to normalize between 0 and 1
            Debug.DrawLine(r.origin, hit.point, Color.red);
        }
        return (striked, sensor);
    }

    void ComputeSensors()
    {

        /*
            b  c  d
             \ | /
         a  --   --  e
         */

        Vector3 a = (-transform.right).normalized;
        Vector3 b = (transform.forward - transform.right).normalized;
        Vector3 c = transform.forward.normalized;
        Vector3 d = (transform.forward + transform.right).normalized;
        Vector3 e = transform.right.normalized;

        bool striked;
        float sensorVal;

        (striked, sensorVal) = RaySensor(new Ray(transform.position + transform.up * .7f , a));
        sensorL = striked ? sensorVal : sensorL;
        (striked, sensorVal) = RaySensor(new Ray(transform.position + transform.up * .7f, b));
        sensorFL = striked ? sensorVal : sensorFL;
        (striked, sensorVal) = RaySensor(new Ray(transform.position + transform.up * .7f, c));
        sensorF = striked ? sensorVal : sensorF;
        (striked, sensorVal) = RaySensor(new Ray(transform.position + transform.up * .7f, d));
        sensorFR = striked ? sensorVal : sensorFR;
        (striked, sensorVal) = RaySensor(new Ray(transform.position + transform.up * .7f, e));
        sensorR = striked ? sensorVal : sensorR;
    }

    private void GetInput()
    {
        horizontalInput = Input.GetKey(KeyCode.A) ? -1 : Input.GetKey(KeyCode.D) ? 1 : 0;
        verticalInput = Input.GetKey(KeyCode.W) ? 1 : Input.GetKey(KeyCode.S) ? -1 : 0;
        isBreaking = Input.GetKey(KeyCode.Space);
        
    }

    private void HandleMotor()
    {
        frontLeftWheelCollider.motorTorque = verticalInput * motorForce;
        frontRightWheelCollider.motorTorque = verticalInput * motorForce;
        currentbreakForce = isBreaking ? breakForce : 0f;
        ApplyBreaking();
    }

    private void ApplyBreaking()
    {
        frontRightWheelCollider.brakeTorque = currentbreakForce;
        frontLeftWheelCollider.brakeTorque = currentbreakForce;
        rearLeftWheelCollider.brakeTorque = currentbreakForce;
        rearRightWheelCollider.brakeTorque = currentbreakForce;
    }

    private void HandleSteering()
    {
        currentSteerAngle = maxSteerAngle * horizontalInput;
        frontLeftWheelCollider.steerAngle = currentSteerAngle;
        frontRightWheelCollider.steerAngle = currentSteerAngle;
    }

    private void UpdateWheels()
    {
        UpdateSingleWheel(frontLeftWheelCollider, frontLeftWheelTransform);
        UpdateSingleWheel(frontRightWheelCollider, frontRightWheeTransform);
        UpdateSingleWheel(rearRightWheelCollider, rearRightWheelTransform);
        UpdateSingleWheel(rearLeftWheelCollider, rearLeftWheelTransform);
    }

    private void UpdateSingleWheel(WheelCollider wheelCollider, Transform wheelTransform)
    {
        Vector3 pos;
        Quaternion rot;
        wheelCollider.GetWorldPose(out pos, out rot);
        wheelTransform.rotation = rot;
        wheelTransform.position = pos;
    }
}