using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Events;

public class OrientationManager : MonoBehaviour
{
    public static event UnityAction<ScreenOrientation> orientationChangedEvent;

    private ScreenOrientation _orientation;

    void Start()
    {
        _orientation = Screen.orientation;
        //InvokeRepeating("CheckForChange", 1, 1);
    }

    private static void OnOrientationChanged(ScreenOrientation orientation)
    {
        if (orientationChangedEvent != null)
            orientationChangedEvent(orientation);
    }

    void Update()
    {
        if (_orientation != Screen.orientation)
        {

            _orientation = Screen.orientation;
            OnOrientationChanged(_orientation);
        }
    }
}
