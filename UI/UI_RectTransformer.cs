using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

public class UI_RectTransformer : MonoBehaviour
{
    /*--------------------------------------------------------------------------------------------------*/
    /*
                                UI RECT TRANSFORMER - Traegis Creative

    Perform UI object movements at runtime based on 'Transform Point' class. For use with 'UI_Button.cs'




    */
    /*--------------------------------------------------------------------------------------------------*/
    public List<TransformPoint> transformPointList;
    [SerializeField] private RectTransform rect;

    public Action OnTransformFunc;

    public int transformIndex;
    private Coroutine transformCoroutine;


    public void Setup(List<TransformPoint> transformPoints)
    {
        if (transformCoroutine != null)
        {
            StopCoroutine(transformCoroutine);
        }

        transformIndex = 0;

        this.transformPointList = transformPoints;
        TransformPoint point = transformPointList[transformIndex];
        Vector3 anchoredPos = rect.anchoredPosition;

        if (point.position == anchoredPos && point.rotation == rect.localEulerAngles && point.scale == rect.localScale)
        {
            return;
        }
        transformCoroutine = StartCoroutine(TransformCoroutineUI(point.time, point.position, point.rotation, point.scale));
    }

    public void SetRect(RectTransform rect)
    {
        this.rect = rect;
    }

    public Vector2 GetAnchoredPosition()
    {
        return rect.anchoredPosition;
    }


    public void SetTransformPoint(TransformPoint transformPoint)
    {
        rect.anchoredPosition = transformPoint.position;
        rect.localScale = transformPoint.scale;
        rect.localRotation = Quaternion.Euler(transformPoint.rotation);
    }

    public Vector3 GetPosition()
    {
        return rect.anchoredPosition;
    }

    public void SetScale(Vector3 scale)
    {
        if (transformCoroutine != null)
        {
            StopCoroutine(transformCoroutine);
        }

        rect.localScale = scale;
    }

    private IEnumerator TransformCoroutineUI(float time, Vector3 positionIn, Vector3 rotationIn, Vector3 scaleIn)
    {
        float t = 0f;

        Vector3 startPos = rect.anchoredPosition;
        Vector3 startScale = rect.localScale;
        Vector3 startRotation = rect.localRotation.eulerAngles;

        while (t <= 1.0f)
        {
            Vector3 pos = Vector3.Lerp(startPos, positionIn, t);
            Vector3 scale = Vector3.Lerp(startScale, scaleIn, t);
            float rotation = Mathf.Lerp(startRotation.z, rotationIn.z, t);
            Vector3 rotationVector = new Vector3(0f, 0f, rotation);

            rect.anchoredPosition = pos;
            rect.localScale = scale;
            rect.localRotation = Quaternion.Euler(rotationVector);


            t += Time.deltaTime / time;

            yield return null;
        }

        rect.anchoredPosition = positionIn;
        rect.localScale = scaleIn;
        rect.localRotation = Quaternion.Euler(rotationIn);

        if (transformIndex < transformPointList.Count - 1)
        {
            transformIndex++;
            TransformPoint point = transformPointList[transformIndex];
            transformCoroutine = StartCoroutine(TransformCoroutineUI(point.time, point.position, point.rotation, point.scale));
        }

        if (OnTransformFunc != null)
        {
            OnTransformFunc();
            //OnTransformFunc = null;
        }
    }
}

[Serializable]
public class TransformPoint
{
    public Vector3 position;
    public Vector3 rotation;
    public Vector3 scale;

    public float time;
}
