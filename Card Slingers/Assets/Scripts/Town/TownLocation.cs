using UnityEngine;

public class TownLocation : MonoBehaviour
{
    [SerializeField] private TownManager _townManager;
    [SerializeField] private Location _location;
    [SerializeField] private Animator _doorAnimator;


    public void OnMouseEnter()
    {
        //Highlight Location - player door opening animation?
        //Some other UI indication
        _doorAnimator.SetBool("isOpen", true);
    }

    public void OnMouseExit()
    {
        //Undo any highlighting done in OnMouseEnter
        _doorAnimator.SetBool("isOpen", false);
    }

    public void OnMouseDown()
    {
        _townManager.onLocationSelected?.Invoke(_location);
    }
}