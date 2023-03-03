using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TownManager : MonoBehaviour
{
    public delegate void OnLocationSelectedCallback(Location location);
    public OnLocationSelectedCallback onLocationSelected;
    public OnLocationSelectedCallback onLocationViewSet;

    public delegate void OnSwitchViewCallback(float timeToFade);
    public OnSwitchViewCallback onBeginSwitchView;

    [SerializeField] private float _timeToFade = 1f;
    [SerializeField] private Transform _cam;
    [SerializeField] private Transform[] _cameraLocation;
    private bool _inTransition;

    private void Start()
    {
        onLocationSelected += OnLocationSelected;
    }
    
    public void ReturnToTownView()
    {
        onBeginSwitchView?.Invoke(_timeToFade); //UI will begin to fade to black
        Invoke("SetView_Town", _timeToFade);
    }

    private void OnLocationSelected(Location location)
    {
        if (_inTransition) return;
        _inTransition = true;

        onBeginSwitchView?.Invoke(_timeToFade); //UI will begin to fade to black
        //Wait for UI to fade to black completely
        Invoke("SetView_" + location.ToString(), _timeToFade);
    }

    private void SetView_Town()
    {
        _cam.position = _cameraLocation[(int)Location.Square].position;
        _cam.rotation = _cameraLocation[(int)Location.Square].rotation;
        onLocationViewSet?.Invoke(Location.Square);
        _inTransition = false;
    }

    private void SetView_Merchant()
    {
        _cam.position = _cameraLocation[(int)Location.Merchant].position;
        _cam.rotation = _cameraLocation[(int)Location.Merchant].rotation;
        onLocationViewSet?.Invoke(Location.Merchant);
        _inTransition = false;
    }

    private void SetView_Armorer()
    {
        _cam.position = _cameraLocation[(int)Location.Armorer].position;
        _cam.rotation = _cameraLocation[(int)Location.Armorer].rotation;
        onLocationViewSet?.Invoke(Location.Armorer);
        _inTransition = false;
    }

    private void SetView_Tavern()
    {
        _cam.position = _cameraLocation[(int)Location.Tavern].position;
        _cam.rotation = _cameraLocation[(int)Location.Tavern].rotation;
        onLocationViewSet?.Invoke(Location.Tavern);
        _inTransition = false;
    }
}
public enum Location { Square, Merchant, Armorer, Tavern, QuestBoard }