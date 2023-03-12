using MathNet.Numerics.LinearAlgebra;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.SocialPlatforms.Impl;
using static Unity.VisualScripting.Metadata;

public class GameController : MonoBehaviour
{
    [SerializeField] public WayPoints[] waypoints;
    [SerializeField] public WayPoints startingPoint;
    
    public int numCars = 25;

    public int HiddenLayerCount = 3;
    public int HiddenNeuronCount = 7;
    
    private NeuralNet[] population;

    public GameObject carPrefab;

    public float mutationRate = 0.069f;

    public int currentGeneration = 0;
    
    private int naturallySelected = 0;

    public int bestAgentSelection = 8;
    public int worstAgentSelection = 3;
    public int crossOverCount = 10;

    private List<int> genePool = new List<int>();

    [Range(5.0f, 120.0f)]
    public float LifeTime = 30.0f;

    private float startTime;
    
    private List<GameObject> cars = new List<GameObject>();
    // Start is called before the first frame update
    void Start()
    {
        population = new NeuralNet[numCars];
        RandomizePopulation(population, 0);
        for (int i = 0; i < numCars; i++)
        {
            var car = GameObject.Instantiate(carPrefab, Vector3.zero, Quaternion.identity);
            car.GetComponent<CarController>().network = population[i];
            cars.Add(car);
            //cars[i].transform.position = startingPoint.transform.position;
        }
        startTime = Time.time;
        //for (int i = 0; i < waypoints.Length; i++)
        //{
        //    Debug.Log(waypoints[i]);
        //}
    }

    private void RandomizePopulation(NeuralNet[] newPopulation, int startingIndex)
    {
        while (startingIndex < numCars) {
            newPopulation[startingIndex] = new NeuralNet();
            newPopulation[startingIndex].Init(HiddenLayerCount, HiddenNeuronCount);
            startingIndex++;
        }
    }

    Matrix<float> MutateMatrix(Matrix<float> matrix)
    {
        int pointsToMutateCount = Random.Range(1, (matrix.RowCount * matrix.ColumnCount) / 7); // test the value
        Matrix<float> result = matrix;
        for (int i = 0; i < pointsToMutateCount; i++) {
            int randomRow = Random.Range(0, result.RowCount);
            int randomColumn = Random.Range(0, result.ColumnCount);
            result[randomRow, randomColumn] = Mathf.Clamp(result[randomRow, randomColumn] + Random.Range(-1f, 1f), -1f, 1f);
        }
        return result;
    }

    private void Mutate(NeuralNet[] newPopulation)
    {
        for (int i = 0; i < naturallySelected; i++) {
            for (int c = 0; c < newPopulation[i].weights.Count; c++) {
                if (Random.Range(0.0f, 1.0f) < mutationRate) {
                    newPopulation[i].weights[c] = MutateMatrix(newPopulation[i].weights[c]);
                }
            }
        }
    }

    // Update is called once per frame
    void Update()
    {
        //var cars_to_delete = cars.FindAll(x => x.GetComponent<CarController>().dead == true);
        //cars.RemoveAll(x => x.GetComponent<CarController>().dead == true);
        //if(cars.Count > 0 )
        //    Debug.Log(cars_to_delete.Count);

        // repopulate if all are either dead or have exceeded their lifetime

        if (Time.time - startTime >= LifeTime)
        {
            Repopulate();
        }

    }


    private void Repopulate()
    {
        startTime = Time.time;
        genePool.Clear();
        currentGeneration++;
        naturallySelected = 0;

        for (int i = 0; i < numCars; i++)
        {
            population[i].fitness = cars[i].GetComponent<CarController>().score;
        }

        SortPopulation();

        NeuralNet[] newPopulation = PickBestPopulation();

        Crossover(newPopulation);
        Mutate(newPopulation);

        RandomizePopulation(newPopulation, naturallySelected);

        population = newPopulation;

        for (int i = 0; i < numCars; i++)
        {
            cars[i].gameObject.GetComponent<CarController>().ResetNetwork(population[i]);

        }

    }

    private NeuralNet[] PickBestPopulation()
    {
        NeuralNet[] newPopulation = new NeuralNet[numCars];
        for (int i = 0; i < bestAgentSelection; i++) {
            newPopulation[naturallySelected] = population[i].Copy(HiddenLayerCount, HiddenNeuronCount);
            newPopulation[naturallySelected].fitness = 0;
            naturallySelected++;
            int f = Mathf.RoundToInt(population[i].fitness * 10);
            for (int c = 0; c < f; c++)
                genePool.Add(i);
        }

        for (int i = 0; i < worstAgentSelection; i++) {
            int last = population.Length - 1;
            last -= i;
            int f = Mathf.RoundToInt(population[last].fitness * 10);
            for (int c = 0; c < f; c++)
                genePool.Add(last);
        }

        return newPopulation;

    }

    private void Crossover(NeuralNet[] newPopulation)
    {
        for (int i = 0; i < crossOverCount; i += 2) {
            int AIndex = i;
            int BIndex = i + 1;

            if (genePool.Count >= 1) {
                for (int l = 0; l < 100; l++) {
                    AIndex = genePool[Random.Range(0, genePool.Count)];
                    BIndex = genePool[Random.Range(0, genePool.Count)];

                    if (AIndex != BIndex)
                        break;
                }
            }

            NeuralNet[] children = { new NeuralNet(), new NeuralNet() };

            for (int j = 0; j < 2; ++j) {
                children[j].Init(HiddenLayerCount, HiddenNeuronCount);
                children[j].fitness = 0;
            }

            for (int w = 0; w < children[0].weights.Count; w++) {
                if (Random.Range(0.0f, 1.0f) < 0.5f) {
                    children[0].weights[w] = population[AIndex].weights[w];
                    children[1].weights[w] = population[BIndex].weights[w];
                } else {
                    children[1].weights[w] = population[AIndex].weights[w];
                    children[0].weights[w] = population[BIndex].weights[w];
                }
            }

            for (int w = 0; w < children[0].biases.Count; w++) {
                if (Random.Range(0.0f, 1.0f) < 0.5f) {
                    children[0].biases[w] = population[AIndex].biases[w];
                    children[1].biases[w] = population[BIndex].biases[w];
                } else {
                    children[1].biases[w] = population[AIndex].biases[w];
                    children[0].biases[w] = population[BIndex].biases[w];
                }
            }

            newPopulation[naturallySelected++] = children[0];
            newPopulation[naturallySelected++] = children[1];
        }
    }

    private void SortPopulation()
    {
        for (int i = 0; i < population.Length; i++) {
            for (int j = i; j < population.Length; j++) {
                if (population[i].fitness < population[j].fitness) {
                    NeuralNet temp = population[i];
                    population[i] = population[j];
                    population[j] = temp;
                }
            }
        }
    }

}
