using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GPS : MonoBehaviour
{
    public static GPS instance;

    public float latitude;
    public float longitude;

    public int mostRecentDistance;

    [HideInInspector] public string directionLR = "";
    [HideInInspector] public string directionFB = "";

    void Start()
    {
        // Find the device location
        StartCoroutine(StartLocationService());

        // Initialisation
        instance = this;
        mostRecentDistance = 0;
    }

    private IEnumerator StartLocationService()
    {
        // Did the user allow location services
        if (!Input.location.isEnabledByUser)
        {
            Debug.Log("User has not enabled the GPS");
            yield break;
        }

        // Get the location every X seconds for X duration moved
        Input.location.Start(1f, 0.1f);
        int maxWait = 20;

        // Try to find location for maxWait seconds
        while (Input.location.status == LocationServiceStatus.Initializing && maxWait > 0)
        {
            yield return new WaitForSeconds(1);
            maxWait--;
        }

        // Stop trying and time out
        if (maxWait <= 0)
        {
            Debug.Log("Timed out");
            yield break;
        }

        // Debug that location could not be found
        if (Input.location.status == LocationServiceStatus.Failed)
        {
            Debug.Log("Unable to find device location");
            yield break;
        }

        // If found, update location information
        UpdateCurrentLocation();
    }
    
    public void UpdateCurrentLocation()
    {
        // Store the user location
        latitude = Input.location.lastData.latitude;
        longitude = Input.location.lastData.longitude;

        // Find the distance from the cube to the device and store this
        mostRecentDistance = (int)FindHaversineDistance(longitude, latitude, AR_ObjectInformation.instance.longitude, AR_ObjectInformation.instance.latitude);

        // Convert the real-world co-ords to postion in Unity scene
        ConvertLocationToWorldPos();

        // Update the HUD
        HUD.instance.ApplyCoordsToHUD();
        // Update cube position
        AR_ObjectInformation.instance.UpdateCubeScenePosition();
    }

    public float FindHaversineDistance(float lng1, float lat1, float lng2, float lat2)
    {
        // Radius of the Earth in metres
        float R = 6372797.560856f;

        float omega1 = ((lat1 / 180) * Mathf.PI);
        float omega2 = ((lat2 / 180) * Mathf.PI);
        float variacionomega1 = (((lat2 - lat1) / 180) * Mathf.PI);
        float variacionomega2 = (((lng2 - lng1) / 180) * Mathf.PI);

        float a = Mathf.Sin(variacionomega1 / 2) * Mathf.Sin(variacionomega1 / 2) +
            Mathf.Cos(omega1) * Mathf.Cos(omega2) *
            Mathf.Sin(variacionomega2 / 2) * Mathf.Sin(variacionomega2 / 2);
        float c = 2 * Mathf.Asin(Mathf.Sqrt(a));

        float d = R * c;

        // If the reticle cannot hit the cube, don't show the cube
        if (d > ViewReticleLine.instance.raycastRange)
        {
            AR_ObjectInformation.instance.mesh.enabled = false;
        }
        else
            AR_ObjectInformation.instance.mesh.enabled = true;

        return d;
    }

    private void ConvertLocationToWorldPos()
    {
        // Find the distance in latitude
        AR_ObjectInformation.instance.cubeZ = (int)FindHaversineDistance(0f, latitude, 0f, AR_ObjectInformation.instance.latitude);
        // Find the distance in longitude
        AR_ObjectInformation.instance.cubeX = (int)FindHaversineDistance(longitude, 0f, AR_ObjectInformation.instance.longitude, 0f);

        // Direct the player where to go in the scene and real-world
        if ((AR_ObjectInformation.instance.latitude - latitude) > 0)
        {
            directionFB = "Forward: ";
            AR_ObjectInformation.instance.cubeZ *= -1;
        }
        else
            directionFB = "Behind: ";

        if ((AR_ObjectInformation.instance.longitude - longitude) < 0)
        {
            directionLR = "Left: ";
            AR_ObjectInformation.instance.cubeX *= -1;
        }
        else
            directionLR = "Right: ";
    }
}