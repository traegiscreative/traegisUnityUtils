using System.Collections;
using System;
using System.Collections.Generic;
using UnityEngine;

public class WorldObjectProcessor : MonoBehaviour
{
    /*--------------------------------------------------------------------------------------------------*/
    /*
                                WORLD OBJECT PROCESSOR - Traegis Creative

    Script that loads in GameObjects at runtime based on the location of the 'followTransform' and bounding rectangle.

    --> For use in Unity 2D.
    --> Grid system GameObject loading at runtime.
    --> You will need to develop your own script to setup the dictionary of world objects. See WorldObject.cs
    --> External script should call 'Setup()' method to begin.



    */
    /*--------------------------------------------------------------------------------------------------*/



    //Input Parameters:
    [Header("Object Follow Inputs:")]
    [SerializeField] private Transform followTransform; //Transform to follow
    [SerializeField] private Vector3 boundSize; //Size of bounding rectangle to load in GameObjects as followTransform moves --> Vector2(width, height)
    
    [Header("Specify Distance for Refresh:")]
    [SerializeField] private float movementFollowLimit;

    //Collections:
    private Dictionary<Vector3Int, List<WorldObject>> worldObjectDictionary;
    //--> General dictionary for processing
    //--> All objects within grid space
    private List<WorldObject> worldObjectCache; //Cache of currently spawned world objects

    //Cache:
    private Vector3Int currentMinBound;
    private Vector3Int currentMaxBound;

    private Vector3 cachedPosition;

    //Booleans:
    private bool firstFrame = false;

    [Header("Is Processor Running?")]
    public bool isActive = false;


    /*--------------------------------------------------------------------------------------------------*/


    //MONOBEHAVIOUR:
    void Update()
    {
        if (!isActive)
            return;

        Vector3 followPos = followTransform.position;

        if (firstFrame)
        {
            firstFrame = false;
        }
        else
        {
            //Test for movement distance max
            if (Vector3.Distance(followPos, cachedPosition) < movementFollowLimit)
                return;
        }

        cachedPosition = followPos;

        Vector3 currentMinBound = followPos - boundSize;
        Vector3 currentMaxBound = followPos + boundSize;

        Refresh(currentMinBound, currentMaxBound);
    }


    //SETUP METHODS:
    public void Setup()
    {
        if (worldObjectDictionary == null)
            worldObjectDictionary = new Dictionary<Vector3Int, List<WorldObject>>();

        worldObjectCache = new List<WorldObject>();

        firstFrame = true;

        SetInitial();

        isActive = true;
    }

    private void SetInitial()
    {
        Vector3 currentMinBound = followTransform.position - boundSize;
        Vector3 currentMaxBound = followTransform.position + boundSize;

        Vector3Int newMinBound = new Vector3Int(Mathf.FloorToInt(currentMinBound.x), Mathf.FloorToInt(currentMinBound.y), 0);
        Vector3Int newMaxBound = new Vector3Int(Mathf.FloorToInt(currentMaxBound.x), Mathf.FloorToInt(currentMaxBound.y), 0);

        this.currentMinBound = newMinBound;
        this.currentMaxBound = newMaxBound;

        AddRange(this.currentMinBound, this.currentMaxBound);
    }


    //COLLECTION METHODS:
    public void SetDictionary(Dictionary<Vector3Int, List<WorldObject>> dicIn)
    {
        //For external dictionary setup:
        worldObjectDictionary = dicIn;
    }

    public void AddDictionaryRange(Dictionary<Vector3Int, List<WorldObject>> dicIn)
    {
        //Adding range to main dictionary

        //Test for null case:
        if (worldObjectDictionary == null)
            worldObjectDictionary = new Dictionary<Vector3Int, List<WorldObject>>();


        foreach (Vector3Int vectorInt in dicIn.Keys)
        {
            if (worldObjectDictionary.ContainsKey(vectorInt))
            {
                //Override:
                worldObjectDictionary[vectorInt] = dicIn[vectorInt];
            }
            else
            {
                //New entry:
                worldObjectDictionary.Add(vectorInt, dicIn[vectorInt]);
            }

            //If adding objects at runtime, check if they should be spawned in automatically
            if (isActive)
            {
                if (vectorInt.x >= currentMinBound.x && vectorInt.y >= currentMinBound.y)
                {
                    if (vectorInt.x < currentMaxBound.x && vectorInt.y < currentMaxBound.y)
                    {
                        foreach (WorldObject worldObject in dicIn[vectorInt])
                        {
                            if (worldObjectCache.Contains(worldObject))
                                continue;

                            SpawnWorldObject(worldObject);
                        }
                    }
                }
            }
        }

        //worldObjectList.AddRange(dicIn.Values);
    }

    public void RemoveDictionaryRange(Dictionary<Vector3Int, List<WorldObject>> dicIn)
    {
        foreach (Vector3Int vector in dicIn.Keys)
        {
            if (worldObjectDictionary.ContainsKey(vector))
            {
                foreach (WorldObject worldObject in worldObjectDictionary[vector])
                {
                    DestroyWorldObject(worldObject);
                }

                worldObjectDictionary.Remove(vector);
            }
        }
    }

    public void AddWorldObject(WorldObject worldObject)
    {
        Vector3 worldPos = worldObject.GetPosition();
        Vector3Int pos = new Vector3Int(Mathf.FloorToInt(worldPos.x), Mathf.FloorToInt(worldPos.y), 0);

        if (worldObjectDictionary.ContainsKey(pos))
        {
            //Add to list of objects for that position:
            worldObjectDictionary[pos].Add(worldObject);
        }
        else
        {
            //New entry:
            worldObjectDictionary.Add(pos, new List<WorldObject>() { worldObject });
        }

        if (isActive)
        {
            if (pos.x >= currentMinBound.x && pos.y >= currentMinBound.y)
            {
                if (pos.x < currentMaxBound.x && pos.y < currentMaxBound.y)
                {
                    if (worldObjectCache.Contains(worldObject))
                        return;

                    SpawnWorldObject(worldObject);
                }
            }
        }
    }


    //REFRESH METHODS:
    public void Refresh(Vector3 worldPosA, Vector3 worldPosB)
    {
        //Convert to grid positions:
        Vector3Int newMinBound = new Vector3Int(Mathf.FloorToInt(worldPosA.x), Mathf.FloorToInt(worldPosA.y), 0);
        Vector3Int newMaxBound = new Vector3Int(Mathf.FloorToInt(worldPosB.x), Mathf.FloorToInt(worldPosB.y), 0);

        //Compare to cached bounds and update:
        if (currentMinBound != Vector3Int.zero && currentMinBound != newMinBound)
        {
            //Assess the difference between min points (bottom left of rectangle)
            Vector3 minDifference = newMinBound - currentMinBound;

            //COLUMNS:
            if (minDifference.x > 0)
            {
                //Destroy left side column:
                int gridHeight1 = currentMaxBound.y - currentMinBound.y;
                int gridWidth1 = newMinBound.x - currentMinBound.x;
                Vector3Int min1 = currentMinBound;
                Vector3Int max1 = currentMinBound + new Vector3Int(gridWidth1, gridHeight1, 0);
                ClearRange(min1, max1);


                //Add right side column:
                int gridHeight2 = newMaxBound.y - newMinBound.y;
                int gridWidth2 = newMaxBound.x - currentMaxBound.x;
                Vector3Int min2 = new Vector3Int(currentMaxBound.x, newMinBound.y, currentMaxBound.z);
                Vector3Int max2 = min2 + new Vector3Int(gridWidth2, gridHeight2, 0);
                AddRange(min2, max2);

            }
            else
            {
                //Add left side column:
                int gridHeight1 = newMaxBound.y - newMinBound.y;
                int gridWidth1 = currentMinBound.x - newMinBound.x;
                Vector3Int min1 = newMinBound;
                Vector3Int max1 = min1 + new Vector3Int(gridWidth1, gridHeight1, 0);
                AddRange(min1, max1);



                //Destroy right side column:
                int gridHeight2 = currentMaxBound.y - currentMinBound.y;
                int gridWidth2 = currentMaxBound.x - newMaxBound.x;
                Vector3Int min2 = new Vector3Int(newMaxBound.x, currentMinBound.y, currentMinBound.z);
                Vector3Int max2 = min2 + new Vector3Int(gridWidth2, gridHeight2, 0);
                ClearRange(min2, max2);

            }

            //ROWS:
            if (minDifference.y > 0)
            {
                //Destroy bottom row:
                int gridWidth1 = currentMaxBound.x - currentMinBound.x;
                int gridHeight1 = newMinBound.y - currentMinBound.y;
                Vector3Int min1 = currentMinBound;
                Vector3Int max1 = currentMinBound + new Vector3Int(gridWidth1, gridHeight1, 0);
                ClearRange(min1, max1);


                //Add top row:
                int gridWidth2 = newMaxBound.x - newMinBound.x;
                int gridHeight2 = newMaxBound.y - currentMaxBound.y;
                Vector3Int min2 = new Vector3Int(newMinBound.x, currentMaxBound.y, currentMaxBound.z);
                Vector3Int max2 = min2 + new Vector3Int(gridWidth2, gridHeight2, 0);
                AddRange(min2, max2);

            }
            else
            {
                //Add bottom row:
                int gridWidth1 = newMaxBound.x - newMinBound.x;
                int gridHeight1 = currentMinBound.y - newMinBound.y;
                Vector3Int min1 = newMinBound;
                Vector3Int max1 = min1 + new Vector3Int(gridWidth1, gridHeight1, 0);
                AddRange(min1, max1);


                //Destroy top row:
                int gridWidth2 = currentMaxBound.x - currentMinBound.x;
                int gridHeight2 = currentMaxBound.y - newMaxBound.y;
                Vector3Int min2 = new Vector3Int(currentMinBound.x, newMaxBound.y, currentMinBound.z);
                Vector3Int max2 = min2 + new Vector3Int(gridWidth2, gridHeight2, 0);
                ClearRange(min2, max2);

            }

        }

        //Update bounds:
        currentMinBound = newMinBound;
        currentMaxBound = newMaxBound;
    }

    private void SpawnWorldObject(WorldObject worldObject)
    {
        GameObject worldObjectSpawn = Instantiate(worldObject.GetInstantiateObject(), worldObject.GetPosition(), Quaternion.identity, this.transform);
        worldObjectSpawn.transform.position = worldObject.GetPosition();
        worldObject.SetupWorldObject(worldObjectSpawn);

        //Add to cache:
        worldObjectCache.Add(worldObject);
    }

    private void DestroyWorldObject(WorldObject worldObject)
    {
        worldObject.DestroyObject();

        if (worldObjectCache.Contains(worldObject))
        {
            worldObjectCache.Remove(worldObject);
        }

        GameObject worldGameObject = worldObject.GetObjectCache();
        if (worldGameObject != null)
        {
            Destroy(worldGameObject);
        }
    }


    //RANGE MODIFICATION METHODS:
    private void AddRange(Vector3Int pos1, Vector3Int pos2)
    {
        for (int x = pos1.x; x < pos2.x; x++)
        {
            for (int y = pos1.y; y < pos2.y; y++)
            {
                Vector3Int pos = new Vector3Int(x, y, 0);

                if (worldObjectDictionary.ContainsKey(pos))
                {
                    List<WorldObject> worldObjectList = worldObjectDictionary[pos];

                    foreach (WorldObject worldObject in worldObjectList)
                    {
                        if (worldObjectCache.Contains(worldObject))
                            continue;

                        SpawnWorldObject(worldObject);
                    }
                }
            }
        }
    }

    private void ClearRange(Vector3Int pos1, Vector3Int pos2)
    {
        for (int x = pos1.x; x < pos2.x; x++)
        {
            for (int y = pos1.y; y < pos2.y; y++)
            {
                Vector3Int pos = new Vector3Int(x, y, 0);

                if (worldObjectDictionary.ContainsKey(pos))
                {
                    List<WorldObject> worldObjectList = worldObjectDictionary[pos];
                    foreach (WorldObject worldObject in worldObjectList)
                    {
                        DestroyWorldObject(worldObject);
                    }
                }
            }
        }
    }






}
