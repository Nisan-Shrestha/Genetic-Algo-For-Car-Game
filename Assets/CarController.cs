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
    private float currentSteerAngle;
    private float currentbreakForce;
    private bool isBreaking;

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

    private float sensorA, sensorB, sensorC, sensorD, sensorE;

    public int HiddenLayerCount = 1;
    public int HiddenNeuronCount = 10;

    public bool UserController;

    private void Awake()
    {
        network = GetComponent<NeuralNet>();

        network.Init(HiddenLayerCount, HiddenNeuronCount);
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

        const float threshold = 0.05f;
        float left, right, forwards, backwards, brake;
        (left, right, forwards, backwards, brake) = network.Run(sensorA, sensorB, sensorC, sensorD, sensorE);

        //horizontalInput = (right > threshold ? 1 : 0) - (left > threshold ? 1 : 0);
        //verticalInput = (forwards > threshold ? 1 : 0) - (backwards > threshold ? 1 : 0);
        //isBreaking = brake > threshold;
        horizontalInput = (right  - left) > 0 ? 1 : -1;
        verticalInput = (forwards - backwards) > 0 ? 1 : -1;
        //verticalInput = (forwards > threshold ? 1 : 0) - (backwards > threshold ? 1 : 0);
        isBreaking = brake > threshold;
    }

    (bool, float) RaySensor(Ray r)
    {
        RaycastHit hit;
        float sensor = 0;
        bool striked = false;
        if (striked = Physics.Raycast(r, out hit))
        {
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

        (striked, sensorVal) = RaySensor(new Ray(transform.position, a));
        sensorA = striked ? sensorVal : sensorA;
        (striked, sensorVal) = RaySensor(new Ray(transform.position, a));
        sensorB = striked ? sensorVal : sensorB;
        (striked, sensorVal) = RaySensor(new Ray(transform.position, a));
        sensorC = striked ? sensorVal : sensorC;
        (striked, sensorVal) = RaySensor(new Ray(transform.position, a));
        sensorD = striked ? sensorVal : sensorD;
        (striked, sensorVal) = RaySensor(new Ray(transform.position, a));
        sensorE = striked ? sensorVal : sensorE;
    }

    private void GetInput()
    {
        horizontalInput = Input.GetKey(KeyCode.A) ? -1 : Input.GetKey(KeyCode.D) ? 1 : 0;
        verticalInput = Input.GetKey(KeyCode.W) ? 1 : Input.GetKey(KeyCode.S) ? -1 : 0;
        isBreaking = Input.GetKey(KeyCode.Space);
        
    }

    private void HandleMotor()
    {
        rearLeftWheelCollider.motorTorque = verticalInput * motorForce;
        rearRightWheelCollider.motorTorque = verticalInput * motorForce;
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