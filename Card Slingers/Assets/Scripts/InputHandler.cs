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
        playerInput.actions["Camera Home"].performed += i => CameraController.instance.ReturnHome();
        playerInput.actions["Change View"].performed += i => CameraController.instance.SwitchView();

        //playerInput.actions["Move"].performed += i => camController.movementInput = i.ReadValue<Vector2>();

    }

    public void OnDisable()
    {
        playerInput.actions["Left Click"].performed -= i => OnLeftClick();
        playerInput.actions["Right Click"].performed -= i => OnRightClick();
        playerInput.actions["Camera Home"].performed -= i => CameraController.instance.ReturnHome();
        playerInput.actions["Change View"].performed -= i => CameraController.instance.SwitchView();
    }

    private void OnLeftClick()
    {
        ray = cam.ScreenPointToRay(GetMousePos());
        if (Physics.Raycast(ray, out hit))
        {
            hit.transform?.GetComponent<IInteractable>()?.OnLeftClick();
        }
    }

    private void OnRightClick()
    {
        ray = cam.ScreenPointToRay(GetMousePos());
        if (Physics.Raycast(ray, out hit))
        {
            hit.transform?.GetComponent<IInteractable>()?.OnRightClick();
        }
    }

    public static Vector2 GetMousePosition() => instance.GetMousePos();
    public static Vector2 GetMouseScroll() => instance.GetMouseWheelScroll();
    public static Vector2 GetMoveInput() => instance.GetMovementInput();
    public static float GetRotationInput() => instance.GetRotation();

    private Vector2 GetMousePos() => playerInput.actions["Mouse Position"].ReadValue<Vector2>();
    private Vector2 GetMouseWheelScroll() => playerInput.actions["Mouse Scroll"].ReadValue<Vector2>();
    private Vector2 GetMovementInput() => playerInput.actions["Move"].ReadValue<Vector2>();
    private float GetRotation() => playerInput.actions["Rotate"].ReadValue<float>();
}
