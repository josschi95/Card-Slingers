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
    private float boundsX, boundsZ; //limit the movement of the free cam to the current room

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
            HandleFreeCamPosition();
            HandleFreeCameraZoom();
        }
        else if (_currentView == CameraView.Aerial)
        {
            aerialCam.transform.position += cameraDelta * inputSensitivity * Time.deltaTime;
            HandleAerialCameraFOV();
        }
    }

    private void HandleFreeCamPosition()
    {
        freeCamMoveDirection = cam.transform.forward * cameraDelta.z + cam.transform.right * cameraDelta.x;
        freeCamMoveDirection.y = 0;
        freeCam.transform.position += freeCamMoveDirection * inputSensitivity * Time.deltaTime;
        
        var camPos = freeCam.transform.position;
        camPos.x = Mathf.Clamp(camPos.x, _battlefieldCenter.position.x - boundsX, _battlefieldCenter.position.x + boundsX);
        camPos.y = Mathf.Clamp(camPos.y, _battlefieldCenter.position.y + 5, _battlefieldCenter.position.y + 15);
        camPos.z = Mathf.Clamp(camPos.z, _battlefieldCenter.position.z - boundsZ, _battlefieldCenter.position.z + boundsZ);
        freeCam.transform.position = camPos;

        freeCam.transform.localEulerAngles += freeCamRotation * inputSensitivity * 4 * Time.deltaTime;
    }

    private void HandleFreeCameraZoom()
    {
        if (freeCam.transform.position.y <= _battlefieldCenter.position.y + 5 && zoomInput > 0) return;
        else if (freeCam.transform.position.y >= _battlefieldCenter.position.y + 10 && zoomInput < 0) return;

        //var camForward = cam.transform.forward * zoomInput + cam.transform.right * cameraDelta.x;
        var camForward = Vector3.down * zoomInput; //move camera up/down
        freeCam.transform.position += camForward * _zoomSensitivity * Time.deltaTime;
    }

    private void HandleAerialCameraFOV()
    {
        aerialCam.m_Lens.FieldOfView -= zoomInput * inputSensitivity * Time.deltaTime;
        if (aerialCam.m_Lens.FieldOfView < aerialCamMinFOV) aerialCam.m_Lens.FieldOfView = aerialCamMinFOV;
        else if (aerialCam.m_Lens.FieldOfView > aerialCamMaxFOV) aerialCam.m_Lens.FieldOfView = aerialCamMaxFOV;
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

        var room = PlayerController.instance.currentRoom;
        boundsX = room.RoomDimensions.x * 0.5f;
        boundsZ = room.RoomDimensions.y * 0.5f;

        aerialCam.transform.eulerAngles = new Vector3(90, _battlefieldCenter.eulerAngles.x, 0);
    }

    public void OnCombatEnd()
    {
        SetActiveCamera(CameraView.Follow);
    }
}

public enum CameraView { Follow, Free, Aerial }
