using MathNet.Numerics;
using System;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

[RequireComponent(typeof(NeuralNet))]
public class CarController : MonoBehaviour
{
    private NeuralNet network;

    private const string HORIZONTAL = "Horizontal";
    private const string VERTICAL = "Vertical";

    private float horizontalInput;
    private float verticalInput;
    private float speed;
    private float angularVelocityY;
    private float currentSteerAngle;
    private float currentbreakForce;
    private bool isBreaking;

    private Transform[] waypoints;

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

    private Rigidbody myRB;

    private float sensorL, sensorFL, sensorF, sensorFR, sensorR;

    public int HiddenLayerCount = 1;
    public int HiddenNeuronCount = 10;

    public bool UserController;

    private void Awake()
    {
        network = GetComponent<NeuralNet>();

        network.Init(HiddenLayerCount, HiddenNeuronCount);
    }

    private void Start()
    {
        myRB= this.GetComponent<Rigidbody>();
        myRB.centerOfMass -= new Vector3(0, .2f, 0);

        var Gc = FindObjectOfType<GameController>();
        while (Gc.waypoints.Length == 0) ;
        waypoints = Gc.waypoints;

//        for (int i = 0; i < waypoints.Length; i++)
            //Debug.Log(waypoints[i].position);
        
    }

    private void FixedUpdate()
    {
        if (UserController)
            GetInput();
        else
            HandleInput();
        HandleMotor();
        HandleSteering();
        UpdateWheels();
    }

    private void HandleInput()
    {
        ComputeSensors();

        const float turnThreshold = 0.05f;
        const float motorThreshold = .05f;
        const float brakeThreshold = 0.6f;

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
            sensor = hit.distance / 50;  // need to test this value, done to normalize between 0 and 1
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