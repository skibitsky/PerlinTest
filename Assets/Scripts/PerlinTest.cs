﻿using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;

public class PerlinTest : MonoBehaviour
{
    [SerializeField]
    int worldsWidth = 400;
    [SerializeField]
    int worldHeight = 400;
    [SerializeField]
    float amplitude = 50;
    [SerializeField]
    int seedsCount = 30;
    [SerializeField]
    float scale = 20;
    [SerializeField]
    int chunkHeightLimit = 4;
    [SerializeField]
    List<Renderer> textureViewers = new List<Renderer>();
    [SerializeField]
    GameObject cubePrefab;

    List<int> Seeds = new List<int>();
    List<GameObject> cubeWorlds = new List<GameObject>();
    int cubeWorldsCount = 0;

    [ContextMenu("Refersh")]
    void Start()
    {
        GenerateSeeds();

        foreach (var t in cubeWorlds)
            Destroy(t);
        cubeWorlds.Clear();
        cubeWorldsCount = 0;

        DoTest("Unity Perlin Noise", Mathf.PerlinNoise, textureViewers[0]);

        DoTest("Madweedfall Perlin Noise",
            (x, y) =>
            {
                return 2f * (float)Math.Sin(Vector2.Dot(new Vector2(x, y), new Vector2(12.9898f, 78.233f)) * 43758.5453f) - 1.0f;
            },
            textureViewers[1]);

        DoTest("keijiro Perlin", (x, y) => keijiro.Perlin.Noise(x, y), textureViewers[2], true);

        // https://github.com/WardBenjamin/SimplexNoise
        DoTest("Simplex Noise",
            (x, y, seed) =>
            {
                Simplex.Noise.Seed = seed;
                return Simplex.Noise.CalcPixel2D((int)x, (int)y, scale) / 255;
            },
            textureViewers[3], true);
    }

    public void GenerateSeeds()
    {
        Debug.Log("Generating seeds...");
        for (int i = 0; i < seedsCount; i++)
            Seeds.Add(Random.Range(1000, 9999));

        Debug.Log("Seeds were generated!");

    }

    public void DoTest(string name, Func<float, float, int, float> generatorWithSeed, Renderer renderer = null, bool generateCubeWorld = false)
    {
        Debug.LogFormat("Starting tests for <b>{0}</b>...", name);
        var result = string.Format("Result for <b>{0}</b>", name);
        foreach (var seed in Seeds)
        {
            var world = new float[worldsWidth, worldHeight];
            for (int y = 0; y < worldHeight; y++)
            {
                for (int x = 0; x < worldsWidth; x++)
                {
                    world[x, y] = generatorWithSeed(((float)x) / worldsWidth * scale, ((float)y) / worldHeight * scale, seed) * amplitude;
                }
            }
            result += string.Format("\nSeed: <b>{0}</b>  Result: <b>{1}</b>", seed, CheckForLimit(world).ToString());
            if (seed == Seeds[Seeds.Count - 1])
            {
                if (renderer != null)
                    RenderWorld(world, renderer);
                if (generateCubeWorld)
                    GenerateCubeWorld(name, world);
            }
        }
        Debug.Log(result);
    }

    public void DoTest(string name, Func<float, float, float> generator, Renderer renderer = null, bool generateCubeWorld = false)
    {
        Debug.LogFormat("Starting tests for <b>{0}</b>...", name);
        var result = string.Format("<b>Result</b> for <b>{0}</b>", name);
        foreach (var seed in Seeds)
        {
            var world = new float[worldsWidth, worldHeight];
            for (int y = 0; y < worldHeight; y++)
            {
                for (int x = 0; x < worldsWidth; x++)
                {
                    world[x, y] = generator(((float)x + seed) / worldsWidth * scale, ((float)y + seed) / worldHeight * scale) * amplitude;
                }
            }
            result += string.Format("\nSeed: <b>{0}</b>  Result: <b>{1}</b>", seed, CheckForLimit(world).ToString());
            if (seed == Seeds[Seeds.Count - 1])
            {
                if (renderer != null)
                    RenderWorld(world, renderer);
                if (generateCubeWorld)
                    GenerateCubeWorld(name, world);
            }
        }
        Debug.Log(result);
    }

    void GenerateCubeWorld(string name, float[,] world)
    {
        Debug.LogFormat("Started generating cube world for <b>{0}</b>....", name);
        var parent = new GameObject(name).transform;
        parent.position = new Vector3(worldsWidth * cubeWorldsCount + 10, 0, worldHeight * cubeWorldsCount + 10);
        cubeWorlds.Add(parent.gameObject);
        for (int x = 0; x < worldsWidth; x++)
        {
            for (int y = 0; y < worldHeight; y++)
            {
                var height = Mathf.Round(world[x, y] * 2) / 2; // position Y in world space. We use 0.5 step
                var go = Instantiate(cubePrefab, new Vector3(x + worldsWidth * cubeWorldsCount + 10, height, y + worldHeight * cubeWorldsCount + 10), Quaternion.identity);
                go.transform.SetParent(parent);                
            }
        }
        cubeWorldsCount++;
    }

    void RenderWorld(float[,] world, Renderer renderer)
    {
        var texture = new Texture2D(worldsWidth, worldHeight);
        for (int y = 0; y < worldHeight; y++)
        {
            for (int x = 0; x < worldsWidth; x++)
            {
                var clr = new Color(world[x, y] / amplitude, world[x, y] / amplitude, world[x, y] / amplitude);
                texture.SetPixel(x, y, clr);
            }
        }
        texture.Apply();
        renderer.material.mainTexture = texture;
    }

    // In our project we spawn premade chunks of 8x8 hight points. The differece betwen two chanks 
    // cannot be more than chunkHeightLimit.
    // false is failed
    bool CheckForLimit(float[,] world)
    {
        for (int y = 0; y < worldHeight - 8; y += 8)
        {
            for (int x = 0; x < worldsWidth - 8; x += 8)
            {

                if (world[x, y] - world[x, y + 1] > chunkHeightLimit)
                    return false;
                if (world[x, y] - world[x + 1, y] > chunkHeightLimit)
                    return false;
                if (world[x, y] - world[x + 1, y + 1] > chunkHeightLimit)
                    return false;
            }
        }

        return true;
    }
}