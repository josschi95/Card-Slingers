using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Interactions;

public class InputHandler : MonoBehaviour
{
    #region - Singleton -
    private static InputHandler instance;
    private void Awake()
    {
        instance = this;
        cam = Camera.main;
    }
    #endregion

    [SerializeField] private PlayerInput playerInput;
    private Camera cam;

    Ray ray;
    RaycastHit hit;

    public void OnEnable()
    {
        playerInput.actions["Left Click"].performed += i => OnLeftClick();
        playerInput.actions["Right Click"].performed += i => OnRightClick();

    }

    public void OnDisable()
    {
        playerInput.actions["Left Click"].performed -= i => OnLeftClick();
        playerInput.actions["Right Click"].performed -= i => OnRightClick();
    }

    private void Update()
    {
        RaycastMousePosition();
    }

    private void RaycastMousePosition()
    {
        ray = cam.ScreenPointToRay(GetMousePos());
        if (Physics.Raycast(ray, out hit))
        {
            hit.transform?.GetComponent<IInteractable>()?.OnMouseEnter();
        }
    }

    private void OnLeftClick() => hit.transform?.GetComponent<IInteractable>()?.OnLeftClick();

    private void OnRightClick()
    {
        DuelManager.instance.deselectCard = true;
        hit.transform?.GetComponent<IInteractable>()?.OnRightClick();
    }

    public static Vector2 GetMousePosition() => instance.GetMousePos();

    private Vector2 GetMousePos() => playerInput.actions["Mouse Position"].ReadValue<Vector2>();
}
