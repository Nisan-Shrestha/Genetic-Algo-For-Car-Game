using System.Collections.Generic;
using UnityEngine;
using System;

using MathNet.Numerics.LinearAlgebra;
using Random = UnityEngine.Random;

public class NeuralNet : MonoBehaviour
{
    public Matrix<float> inputLayer = Matrix<float>.Build.Dense(1, 7);
    public List<Matrix<float>> hiddenLayers = new List<Matrix<float>>();
    public Matrix<float> outputLayer = Matrix<float>.Build.Dense(1, 5);
    public List<Matrix<float>> weights = new List<Matrix<float>>();
    public List<float> biases = new List<float>();
    public float fitness = 0;
    public void Init(int countHiddenLayers, int countHiddenNeurons)
    {
        //Clear all neural layers
        inputLayer.Clear();
        hiddenLayers.Clear();
        outputLayer.Clear();
        weights.Clear();
        biases.Clear();
        // for each hidden layer initialize the required number of weights and biases
        for (int i = 0; i < countHiddenLayers + 1; i++)
        {
            Matrix<float> f = Matrix<float>.Build.Dense(1, countHiddenNeurons);
            hiddenLayers.Add(f);
            biases.Add(Random.Range(-1f, 1f));
            if (i == 0)
            {
                Matrix<float> inputLayerToFirstHiddenLayer = Matrix<float>.Build.Dense(7, countHiddenNeurons);
                weights.Add(inputLayerToFirstHiddenLayer);
            }
            Matrix<float> hiddenLayerToHiddenLayer = Matrix<float>.Build.Dense(countHiddenNeurons, countHiddenNeurons);
            weights.Add(hiddenLayerToHiddenLayer);
        }
        Matrix<float> OutputWeight = Matrix<float>.Build.Dense(countHiddenNeurons, 5);
        weights.Add(OutputWeight);
        biases.Add(Random.Range(-1f, 1f));
        RandomizeWeights();
    }

    public void InitHidden(int countHiddenLayers, int countHiddenNeurons)
    {
        inputLayer.Clear();
        hiddenLayers.Clear();
        outputLayer.Clear();
        for (int i = 0; i < countHiddenLayers + 1; i++) {
            Matrix<float> newHiddenLayer = Matrix<float>.Build.Dense(1, countHiddenNeurons);
            hiddenLayers.Add(newHiddenLayer);
        }

    }

    public NeuralNet Copy(int countHiddenLayers, int countHiddenNeurons)
    {
        NeuralNet n = new NeuralNet();
        List<Matrix<float>> newWeights = new List<Matrix<float>>();
        for (int i = 0; i < this.weights.Count; i++) {
            Matrix<float> currentWeight = Matrix<float>.Build.Dense(weights[i].RowCount, weights[i].ColumnCount);
            for (int x = 0; x < currentWeight.RowCount; x++) {
                for (int y = 0; y < currentWeight.ColumnCount; y++) {
                    currentWeight[x, y] = weights[i][x, y];
                }
            }
            newWeights.Add(currentWeight);
        }
        List<float> newBiases = new List<float>();
        newBiases.AddRange(biases);
        n.weights = newWeights;
        n.biases = newBiases;
        n.InitHidden(countHiddenLayers, countHiddenNeurons);
        return n;
    }

    public void RandomizeWeights()
    {
        for (int i = 0; i < weights.Count; i++)
            for (int x = 0; x < weights[i].RowCount; x++)
                for (int y = 0; y < weights[i].ColumnCount; y++)
                {
                    weights[i][x, y] = Random.Range(-100f, 100f);
                    //weights[i][x, y] = Random.Range(-1.0f, 1.0f);
                    //Debug.Log(weights[i][x, y]);
                }
    }

    public (float, float, float, float, float) Run (float a, float b, float c, float d, float e, float speed, float angularVelocityY)
    {
        inputLayer[0, 0] = a;
        inputLayer[0, 1] = b;
        inputLayer[0, 2] = c;
        inputLayer[0, 3] = d;
        inputLayer[0, 4] = e;
        inputLayer[0, 5] = speed;
        inputLayer[0, 6] = angularVelocityY;

        inputLayer = inputLayer.PointwiseTanh();
        hiddenLayers[0] = ((inputLayer * weights[0]) + biases[0]).PointwiseTanh();
        for (int i = 1; i < hiddenLayers.Count; i++)
            hiddenLayers[i] = ((hiddenLayers[i - 1] * weights[i]) + biases[i]).PointwiseTanh();
        outputLayer = ((hiddenLayers[hiddenLayers.Count - 1] * weights[weights.Count - 1]) + biases[biases.Count - 1]).PointwiseTanh();
        Func<float, float> Sigmoid = (x) => (1f / (1 + Mathf.Exp(-x)));
        //Left, Right, Forwards, Backwards, Brake
        return (Sigmoid(outputLayer[0, 0]), Sigmoid(outputLayer[0, 1]), Sigmoid(outputLayer[0, 2]), Sigmoid(outputLayer[0, 3]), Sigmoid(outputLayer[0, 4]));
    }

}
