using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TownManager : MonoBehaviour
{
    public delegate void OnLocationSelectedCallback(Location location);
    public OnLocationSelectedCallback onLocationSelected;

    [SerializeField] private TownLocation[] _townLocations;

    private void Start()
    {
        onLocationSelected += OnLocationSelected;
    }




    private void OnLocationSelected(Location location)
    {
        print(location.ToString() + " selected");

        //Fade to black....

        switch (location)
        {
            case Location.Merchant:
                //Changes view to the merchant screen
                break;
            case Location.Armorer:
                //Changes view to the armorer screen
                break;
            case Location.Tavern:
                //Changes view to the guild hall screen
                break;
            case Location.QuestBoard:
                //Changes view to the Quest board screen
                break;
        }

        //Fade from black...
    }
}
