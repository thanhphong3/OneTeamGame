using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;

[RequireComponent(typeof(ARTrackedImageManager))]
public class PlaceTrackedImage : MonoBehaviour
{
    private ARTrackedImageManager _trackedImageManager;
    public GameObject[] ArPrefabs;
    public PlaceBrick placeBrick;
    private readonly Dictionary<string, GameObject> _instantiatedPrefabs = new Dictionary<string, GameObject>();

    private void Awake()
    {
        _trackedImageManager = GetComponent<ARTrackedImageManager>();
    }
    private void OnEnable()
    {
        _trackedImageManager.trackedImagesChanged += OnTrackedImagesChanged;
    }
    private void OnDisable()
    {
        _trackedImageManager.trackedImagesChanged -= OnTrackedImagesChanged;
    }

    private void OnTrackedImagesChanged(ARTrackedImagesChangedEventArgs eventArgs)
    {
        foreach (var trackerImage in eventArgs.added)
        {
            var imageName = trackerImage.referenceImage.name;
            placeBrick.transform.parent = trackerImage.transform;
            placeBrick.transform.localPosition = Vector3.zero;

            // foreach (var curPrefab in ArPrefabs)
            // {
            //     if (string.Compare(curPrefab.name, imageName, System.StringComparison.OrdinalIgnoreCase) == 0)
            //     {
            //         if (!_instantiatedPrefabs.ContainsKey(imageName))
            //         {
            //             var newPrefab = Instantiate(curPrefab, trackerImage.transform);
            //             _instantiatedPrefabs[imageName] = newPrefab;
            //         }
            //         else
            //         {
            //             _instantiatedPrefabs[imageName].transform.parent = trackerImage.transform;
            //             _instantiatedPrefabs[imageName].transform.localPosition = Vector3.zero;
            //         }
            //     }
            // }
        }



        foreach (var trackedImage in eventArgs.updated)
        {
            //  _instantiatedPrefabs[trackedImage.referenceImage.name].SetActive(trackedImage.trackingState == TrackingState.Tracking);
        }

        foreach (var trackedImage in eventArgs.removed)
        {
            // Destroy(_instantiatedPrefabs[trackedImage.referenceImage.name]);
            //_instantiatedPrefabs.Remove(trackedImage.referenceImage.name);
            //_instantiatedPrefabs[trackedImage.referenceImage.name].transform.parent = null;
            placeBrick.transform.parent = null;
        }
    }
}
