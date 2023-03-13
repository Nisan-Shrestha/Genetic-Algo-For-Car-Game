using MathNet.Numerics.LinearAlgebra;
using System;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using System.IO;

using Random = UnityEngine.Random;

public class GameController : MonoBehaviour
{
    string filename = "";
    private float startTime;
    private NeuralNet[] population;
    private List<GameObject> cars = new List<GameObject>();
    
    [SerializeField] public Material bestCarMat;
    [SerializeField] public WayPoints[] waypoints;
    [SerializeField] public WayPoints startingPoint;
    
    public int HiddenLayerCount = 3;
    public int HiddenNeuronCount = 7;
    
    public GameObject carPrefab;

    [SerializeField] public float crossoverProbability = 0.1f;
    [SerializeField] public float mutationRate = 0.069f;
    [SerializeField] public int numCars = 25;
    [Range(0, 100)]
    public int bestSelectionPercentage = 20;
    [Range(0, 100)]
    public int worstSelectionPercentage = 5;
    [Range(5.0f, 360.0f)]
    public float LifeTime = 30.0f;
    [SerializeField] bool spedup = false;
    
    public int naturallySelected = 0;

    public int currentGeneration = 0;
    public float LastGenBestAvgScore = 0.0f;
    public float LastGenBestScore = 0.0f;
    public float completionTime = -1.0f;
    
    // Start is called before the first frame update
    void Start()
    {
        filename = Application.dataPath + "/f2.csv";
        TextWriter tw = new StreamWriter(filename,false);
        tw.WriteLine("Gen, Top10AvgScore, TopScore, completionTime");
        tw.Close();
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
    }

    void Update()
    {
        if (spedup)
            Time.timeScale = 4.0f;
        else
            Time.timeScale = 1.0f;
        if ((Time.time - startTime >= LifeTime) || (cars.FindAll(x => x.GetComponent<CarController>().dead == true).Count == numCars))
            Repopulate();
    }

    public void PrintCompletionTime()
    {
        Debug.Log("A car completed the course in: " + (Time.time - startTime) + " seconds.");
        completionTime = Time.time - startTime;
        waypoints[waypoints.Length - 1].enabled= false;
        Repopulate();
    }

    private void RandomizePopulation(NeuralNet[] newPopulation, int startingIndex)
    {
        while (startingIndex < numCars) {
            newPopulation[startingIndex] = new NeuralNet();
            newPopulation[startingIndex].Init(HiddenLayerCount, HiddenNeuronCount);
            startingIndex++;
        }
    }

    
    public void Repopulate()
    {
        startTime = Time.time;
        currentGeneration++;
        //if (currentGeneration == 30)
        //{
        //    LifeTime = 30.0f;
        //}
        if (currentGeneration <= 7)
            LifeTime = 5f;
        else
            LifeTime = (currentGeneration / 7 + 1) * 5f;

        //LifeTime = ;
        if (completionTime!=-1)
        {
            LifeTime = completionTime+2f;
        }
        naturallySelected = 0;

        for (int i = 0; i < numCars; i++)
        {
            population[i].fitness = cars[i].GetComponent<CarController>().score;
            if (population[i].fitness<0)
            {
                population[i] = new NeuralNet();
                population[i].Init(HiddenLayerCount,HiddenNeuronCount);
            }
        }

        SortPopulation();
        PrintTopScore();
        NeuralNet[] newPopulation = PickBestPopulation();

        Crossover(newPopulation);
        Mutate(newPopulation);

        RandomizePopulation(newPopulation, naturallySelected);

        population = newPopulation;


        for (int i = 0; i < numCars; i++)
        {
            GameObject.Destroy(cars[i]);
        }
        cars.Clear();

        for (int i = 0; i < numCars; i++)
        {
            var car = GameObject.Instantiate(carPrefab, Vector3.zero, Quaternion.identity);
            car.GetComponent<CarController>().network = population[i];
            if (i< Mathf.RoundToInt((bestSelectionPercentage / 100.0f) * numCars))
            {

            car.GetComponentInChildren<MeshRenderer>().material = bestCarMat;
            }
            cars.Add(car);
            
        }
        waypoints[waypoints.Length - 1].enabled = true;

        //for (int i = 0; i < numCars; i++)
        //cars[i].gameObject.GetComponent<CarController>().ResetNetwork(population[i]);
    }

    private void PrintTopScore()
    {
        LastGenBestAvgScore = 0;
        for (int i = 0; i < Mathf.RoundToInt(.1f* numCars) ; i++)
        {
            LastGenBestAvgScore += population[i].fitness;
        }
        LastGenBestAvgScore /= Mathf.RoundToInt(.1f * numCars);
        LastGenBestScore = population[0].fitness;
        
        TextWriter tw = new StreamWriter(filename, true);
        tw.WriteLine(currentGeneration +", " + LastGenBestAvgScore + ", " + LastGenBestScore + ", " + completionTime);
        tw.Close();

        return;
    }
    private NeuralNet[] PickBestPopulation()
    {
        NeuralNet[] newPopulation = new NeuralNet[numCars];
        int bestPopulationCount = Mathf.RoundToInt((bestSelectionPercentage / 100.0f) * numCars);
        int worstPopulationCount = Mathf.RoundToInt((worstSelectionPercentage / 100.0f) * numCars);
        Debug.Log($"Picking the best: {bestPopulationCount}");
        //best
        for (int i = 0; i < bestPopulationCount; i++) {
            newPopulation[naturallySelected] = population[i].Copy(HiddenLayerCount, HiddenNeuronCount);
            newPopulation[naturallySelected].fitness = 0;
            naturallySelected++;
        }
        //worst
        for (int i = 0; i < (worstPopulationCount); i++)
        {
            newPopulation[naturallySelected] = population[numCars-1 - i].Copy(HiddenLayerCount, HiddenNeuronCount);
            newPopulation[naturallySelected].fitness = 0;
            naturallySelected++;
        }
        Debug.Log("Best Selected = " + naturallySelected +" = " + bestPopulationCount + " & " + worstPopulationCount);
        return newPopulation;
    }

    private void CrossWeights(Matrix<float> a, Matrix<float> b, Matrix<float> c, Func<float, float, float> func)
    {
        for (int x = 0; x < c.RowCount; x++)
            for (int y = 0; y < c.ColumnCount; y++)
                c[x, y] = func(a[x, y], b[x,y]);
    }

    // look these over
    private void Crossover(NeuralNet[] newPopulation)
    {
        Func<float, float, float>[] crossover_equations =
        {
            (x, y)  => 0.5f * x + 0.5f * y,
            (x, y)  => 2.0f * x - 1.0f * y,
            (y, x)  => 2.0f * x - 1.0f * y,
        };

        for (int j = 0; j < 3; ++j)
        {
            // cross over the two best parents 1..2 3..4 5..6 and so on
            for (int i = 0; i < Mathf.RoundToInt(((bestSelectionPercentage+worstSelectionPercentage )/ 100.0f) * numCars); i += 1)
            {
                if (naturallySelected > (numCars -1))
                    break;
                int parent1 = i;
                int parent2 = (i + 1) % Mathf.RoundToInt(((bestSelectionPercentage + worstSelectionPercentage) / 100.0f) * numCars);

                if (parent1 > parent2) {
                    int temp = parent1;
                    parent1 = parent2;
                    parent2 = temp;
                }

                //NeuralNet[] children = { new NeuralNet(), new NeuralNet() };
                NeuralNet child = population[parent1].Copy(HiddenLayerCount, HiddenNeuronCount);
                
                for (int w = 0; w < child.weights.Count; w++)
                    if (Random.Range(0.0f, 1.0f) < crossoverProbability)
                        CrossWeights(population[parent1].weights[w], population[parent2].weights[w], child.weights[w], crossover_equations[j]);

                for (int w = 0; w < child.biases.Count; w++)
                    if (Random.Range(0.0f, 1.0f) < crossoverProbability)
                        child.biases[w] = crossover_equations[j](population[parent1].biases[w], population[parent2].biases[w]);
                //Debug.Log("Crossover selection");
                newPopulation[naturallySelected] = child;
                naturallySelected++;
            }

        }
        Debug.Log("Crossover Selected = " + (naturallySelected - Mathf.RoundToInt(((bestSelectionPercentage + worstSelectionPercentage) / 100.0f) * numCars)));

    }

    Matrix<float> MutateMatrix(Matrix<float> matrix)
    {
        int pointsToMutateCount = Random.Range(1, (matrix.RowCount * matrix.ColumnCount) / 7); // test the value
        Matrix<float> result = matrix;
        for (int i = 0; i < pointsToMutateCount; i++)
        {
            int randomRow = Random.Range(0, result.RowCount);
            int randomColumn = Random.Range(0, result.ColumnCount);
            if (Random.Range(0.0f, 1.0f) < mutationRate)
                result[randomRow, randomColumn] = Mathf.Clamp(result[randomRow, randomColumn] + Random.Range(-.5f, .5f), -100f, 100f);
        }
        return result;
    }

    private void Mutate(NeuralNet[] newPopulation)
    {
        for (int i = 0; i < naturallySelected; i++)
        {
            for (int c = 0; c < newPopulation[i].weights.Count; c++)
            {
                newPopulation[i].weights[c] = MutateMatrix(newPopulation[i].weights[c]);
            }
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

/*
            for (int j = 0; j < 2; ++j) {
                children[j].Init(HiddenLayerCount, HiddenNeuronCount);
                children[j].fitness = 0;
            }
            
            for (int w = 0; w < children[0].weights.Count; w++) {
                if (Random.Range(0.0f, 1.0f) < crossoverProbability) {
                    children[0].weights[w] = population[Parent1].weights[w];
                    children[1].weights[w] = population[Parent2].weights[w];
                } else {
                    children[0].weights[w] = population[Parent2].weights[w];
                    children[1].weights[w] = population[Parent1].weights[w];
                }
            }

            for (int w = 0; w < children[0].biases.Count; w++) {
                if (Random.Range(0.0f, 1.0f) < crossoverProbability) {
                    children[0].biases[w] = population[Parent1].biases[w];
                    children[1].biases[w] = population[Parent2].biases[w];
                } else {
                    children[1].biases[w] = population[Parent1].biases[w];
                    children[0].biases[w] = population[Parent2].biases[w];
                }
            }

            newPopulation[naturallySelected++] = children[0];
            newPopulation[naturallySelected++] = children[1];
 */