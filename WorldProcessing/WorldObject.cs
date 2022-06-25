using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class WorldObject 
{
    /*--------------------------------------------------------------------------------------------------*/
    /*
                                WORLD OBJECT - Traegis Creative

    Class tied to 'WorldObjectProcessor.cs' to load and unload GameObject around a Transform at runtime.

    --> Make sure you set up each World Object properly, 'WorldObjectProcessor.cs' will access this lass to 
    get the GameObject to Instantiate at runtime
    --> Modify this script to fit your needs.
    --> Externally edit 'OnWorldObjectSetupFunc' to create custom setup methods for instantiated gameObject.





    */
    /*--------------------------------------------------------------------------------------------------*/

    private Vector3 worldPosition;
    private GameObject instantiateObject;

    private GameObject objectCache;

    public Action<GameObject> OnWorldObjectSetupFunc = null;
    public Action OnWorldObjectDestroyFunc = null;


    //Constructor:
    public WorldObject(GameObject instantiateObject, Vector3 pos)
    {
        worldPosition = pos;
        this.instantiateObject = instantiateObject;
    }

    public Vector3 GetPosition()
    {
        return worldPosition;
    }

    public GameObject GetInstantiateObject()
    {
        return instantiateObject;
    }

    public void SetupWorldObject(GameObject instantiatedGameOject)
    {
        //Method called when this object is spawned at runtime (for loading).
        this.objectCache = instantiatedGameOject;

        if (OnWorldObjectSetupFunc != null)
        {
            OnWorldObjectSetupFunc(instantiatedGameOject);
        }
    }

    public GameObject GetObjectCache()
    {
        return objectCache;
    }

    public void DestroyObject()
    {
        //Method called when this object is destroyed at runtime (for unloading).
        if (OnWorldObjectDestroyFunc != null)
        {
            OnWorldObjectDestroyFunc();
        }
    }
}
