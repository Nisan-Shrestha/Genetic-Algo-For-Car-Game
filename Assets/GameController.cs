using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.SceneManagement;

public class GameController : MonoBehaviour
{
    [SerializeField] public Transform [] waypoints;
    // Start is called before the first frame update
    void Start()
    {
        //for (int i = 0; i < waypoints.Length; i++)
        //{
        //    Debug.Log(waypoints[i]);
        //}
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
