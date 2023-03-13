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
    [SerializeField] private CinemachineFreeLook _freeCam;
    [SerializeField] private CinemachineVirtualCamera _unlockedCam;
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
    //private float boundsX, boundsZ; //limit the movement of the free cam to the current room

    #region - Unity Methods -
    private void Start()
    {
        SetActiveCamera(CameraView.FreeLook);

         _battlefieldCenter = BattlefieldManager.instance.Center;
        DuelManager.instance.onCombatBegin += delegate { OnMatchStart(); };
    }

    private void OnDestroy()
    {
        DuelManager.instance.onCombatBegin -= delegate { OnMatchStart(); };
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

    private void SetActiveCamera(CameraView view)
    {
        _currentView = view;

        switch (view)
        {
            case CameraView.FreeLook:
                _freeCam.enabled = true;
                _unlockedCam.enabled = false;
                aerialCam.enabled = false;
                break;
            case CameraView.Unlocked:
                _freeCam.enabled = false;
                _unlockedCam.enabled = true;
                aerialCam.enabled = false;

                _unlockedCam.transform.position = _freeCam.transform.position;
                _unlockedCam.transform.localEulerAngles = new Vector3(_unlockedCam.transform.localEulerAngles.x, cam.transform.localEulerAngles.y, 0);
                break;
            case CameraView.Aerial:
                _freeCam.enabled = false;
                _unlockedCam.enabled = false;
                aerialCam.enabled = true;

                var newPos = aerialCam.transform.position;
                newPos.x = PlayerController.instance.transform.position.x;
                newPos.z = PlayerController.instance.transform.position.z;
                aerialCam.transform.position = newPos;
                break;
        }
    }

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
        if (_currentView == CameraView.Unlocked)
        {
            HandleFreeCameraZoom();
            HandleFreeCamPosition();
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
        _unlockedCam.transform.position += freeCamMoveDirection * inputSensitivity * Time.deltaTime;
        
        var camPos = _unlockedCam.transform.position;
        //camPos.x = Mathf.Clamp(camPos.x, _battlefieldCenter.position.x - boundsX, _battlefieldCenter.position.x + boundsX);
        camPos.y = Mathf.Clamp(camPos.y, _battlefieldCenter.position.y + 5, _battlefieldCenter.position.y + 15);
        //camPos.z = Mathf.Clamp(camPos.z, _battlefieldCenter.position.z - boundsZ, _battlefieldCenter.position.z + boundsZ);
        _unlockedCam.transform.position = camPos;

        _unlockedCam.transform.localEulerAngles += freeCamRotation * inputSensitivity * 4 * Time.deltaTime;
    }

    private void HandleFreeCameraZoom()
    {
        if (_unlockedCam.transform.position.y <= _battlefieldCenter.position.y + 5 && zoomInput > 0) return;
        else if (_unlockedCam.transform.position.y >= _battlefieldCenter.position.y + 15 && zoomInput < 0) return;

        //var camForward = cam.transform.forward * zoomInput + cam.transform.right * cameraDelta.x;
        var camForward = Vector3.down * zoomInput; //move camera up/down
        _unlockedCam.transform.position += camForward * _zoomSensitivity * Time.deltaTime;
    }

    private void HandleAerialCameraFOV()
    {
        aerialCam.m_Lens.FieldOfView -= zoomInput * inputSensitivity * Time.deltaTime;
        if (aerialCam.m_Lens.FieldOfView < aerialCamMinFOV) aerialCam.m_Lens.FieldOfView = aerialCamMinFOV;
        else if (aerialCam.m_Lens.FieldOfView > aerialCamMaxFOV) aerialCam.m_Lens.FieldOfView = aerialCamMaxFOV;
    }

    //Switch between free camera and aerial cam during combat
    public void SwitchView()
    {
        if (_currentView == CameraView.FreeLook) return;
        else if (_currentView == CameraView.Unlocked)
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
            SetActiveCamera(CameraView.Unlocked);
        }
    }

    private void OnMatchStart()
    {
        SetActiveCamera(CameraView.Unlocked);
    }

    public void OnCombatEnd()
    {
        SetActiveCamera(CameraView.FreeLook);
    }
}

public enum CameraView { FreeLook, Unlocked, Aerial }
