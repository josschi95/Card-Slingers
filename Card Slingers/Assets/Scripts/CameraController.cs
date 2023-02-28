using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Cinemachine;

public class CameraController : MonoBehaviour
{
    public static CameraController instance;
    private void Awake()
    {
        instance = this;
    }

    [SerializeField] private Camera cam;
    [SerializeField] private CameraView _currentView;
    [Range(5, 50)]
    [SerializeField] private float inputSensitivity = 15f;
    [Range(1, 10)]
    [SerializeField] private float _zoomSensitivity = 5f;
    [Space]
    [SerializeField] private CinemachineVirtualCamera followCam;
    [SerializeField] private CinemachineVirtualCamera freeCam;
    [Space]
    [Header("Aerial Camera")]
    [SerializeField] private CinemachineVirtualCamera aerialCam;
    [SerializeField] private int aerialCamMinFOV = 60, aerialCamMaxFOV = 90;

    private Transform _battlefieldCenter;

    private Vector2 movementInput;
    private Vector3 cameraDelta;

    private Vector3 freeCamMoveDirection;
    private Vector3 freeCamRotation;

    private float zoomInput;

    private Vector3 homePosition, homeRotation = new Vector3(35, 0, 0);

    #region - Unity Methods -
    private void Start()
    {
        SetActiveCamera(CameraView.Follow);

         _battlefieldCenter = BattlefieldManager.instance.Center;

    }

    private void Update()
    {
        GetInput();
    }

    private void LateUpdate()
    {
        HandleCameraPositions();
    }
    #endregion

    private void GetInput()
    {
        movementInput = InputHandler.GetMoveInput();
        freeCamRotation.y = InputHandler.GetRotationInput();
        zoomInput = InputHandler.GetMouseScroll().y;
        cameraDelta.x = movementInput.x;
        cameraDelta.z = movementInput.y;
    }

    private void HandleCameraPositions()
    {
        if (_currentView == CameraView.Free)
        {
            freeCamMoveDirection = cam.transform.forward * cameraDelta.z + cam.transform.right * cameraDelta.x;
            freeCamMoveDirection.y = 0;
            freeCam.transform.position += freeCamMoveDirection * inputSensitivity * Time.deltaTime;

            freeCam.transform.localEulerAngles += freeCamRotation * inputSensitivity * 4 * Time.deltaTime;
            HandleFreeCameraZoom();
        }
        else if (_currentView == CameraView.Aerial)
        {
            aerialCam.transform.position += cameraDelta * inputSensitivity * Time.deltaTime;
            HandleAerialCameraFOV();
        }
    }

    public void SetHome(Vector3 position, float rotation)
    {
        homePosition = position;
        homeRotation.y = rotation;
    }

    public void ReturnHome()
    {
        if (_currentView == CameraView.Follow) return;

        freeCam.transform.position = homePosition;
        freeCam.transform.localEulerAngles = homeRotation;
    }

    //Switch between free camera and aerial cam during combat
    public void SwitchView()
    {
        if (_currentView == CameraView.Follow) return;
        else if (_currentView == CameraView.Free)
        {
            //Switch to Aerial
            var battlefieldPos = _battlefieldCenter.position;
            battlefieldPos.y += 25;
            aerialCam.transform.position = battlefieldPos;

            SetActiveCamera(CameraView.Aerial);
        }
        else
        {
            //Switch to Free
            SetActiveCamera(CameraView.Free);
            ReturnHome();
        }
    }

    private void SetActiveCamera(CameraView view)
    {
        _currentView = view;

        switch (view)
        {
            case CameraView.Follow:
                followCam.enabled = true;
                freeCam.enabled = false;
                aerialCam.enabled = false;
                break;
            case CameraView.Free:
                followCam.enabled = false;
                freeCam.enabled = true;
                aerialCam.enabled = false;
                break;
            case CameraView.Aerial:
                followCam.enabled = false;
                freeCam.enabled = false;
                aerialCam.enabled = true;
                break;
        }
    }

    public void OnCombatStart()
    {
        SetActiveCamera(CameraView.Free);
        ReturnHome();
    }

    public void OnCombatEnd()
    {
        SetActiveCamera(CameraView.Follow);
    }

    private void HandleFreeCameraZoom()
    {
        var battlefieldHeight = _battlefieldCenter.position.y + 5;
        if (freeCam.transform.position.y <= battlefieldHeight && zoomInput > 0) return; //prevent camera from sliding forward

        //Don't change FOV, but actually move the camera in/out 
        //So right now this is actually moving the camera in/out based on where it's facing, but just changing the heigh may be better
        var camForward = cam.transform.forward * zoomInput + cam.transform.right * cameraDelta.x;
        freeCam.transform.position += camForward * _zoomSensitivity * Time.deltaTime;

        if (freeCam.transform.position.y < battlefieldHeight)
            freeCam.transform.position = new Vector3(freeCam.transform.position.x, battlefieldHeight, freeCam.transform.position.z);
    }

    private void HandleAerialCameraFOV()
    {
        aerialCam.m_Lens.FieldOfView -= zoomInput * inputSensitivity * Time.deltaTime;
        if (aerialCam.m_Lens.FieldOfView < aerialCamMinFOV) aerialCam.m_Lens.FieldOfView = aerialCamMinFOV;
        else if (aerialCam.m_Lens.FieldOfView > aerialCamMaxFOV) aerialCam.m_Lens.FieldOfView = aerialCamMaxFOV;
    }
}

public enum CameraView { Follow, Free, Aerial }
