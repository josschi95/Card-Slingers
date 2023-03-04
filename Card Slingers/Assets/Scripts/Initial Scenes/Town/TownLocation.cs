using UnityEngine;

public class TownLocation : MonoBehaviour
{
    [SerializeField] private TownManager _townManager;
    [SerializeField] private Location _location;
    [SerializeField] private Animator _doorAnimator;
    private float _hoverTime;

    public void OnMouseOver()
    {
        _hoverTime += Time.deltaTime;
       
        if (_hoverTime >= 0.2f)
        {
            _doorAnimator.SetBool("isOpen", true);
            //Some other UI indication like a text display
            //Maybe a light coming out of the doorway?
        }
    }

    public void OnMouseExit()
    {
        _hoverTime = 0;
        //Undo any highlighting done in OnMouseEnter
        _doorAnimator.SetBool("isOpen", false);
    }

    public void OnMouseDown()
    {
        _townManager.onLocationSelected?.Invoke(_location);
    }
}