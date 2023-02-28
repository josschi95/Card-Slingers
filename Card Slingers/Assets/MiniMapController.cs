using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class MiniMapController : MonoBehaviour, IScrollHandler, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    [SerializeField] private Camera _cam;
    [Space]
    [SerializeField] private Button _mapToggleButton;
    [SerializeField] private RectTransform _mapRect;

    [SerializeField] private int _minCamSize, _maxCamSize;
    [SerializeField] private float _scrollSensitivity = 5f;
    
    private Transform _camTransform;
    private Vector3 _camCenter;
    private Vector3 _dragDelta;
    private float minBoundsX, maxBoundsX, minBoundsZ, maxBoundsZ;
    private float height;

    private bool _isShown;
    private Vector2 _hiddenPos, _shownPos;
    private Coroutine toggleMapCoroutine;

    private void Start()
    {
        _camTransform = _cam.transform;
        height = _camTransform.position.y;

        _shownPos = Vector2.zero;
        _hiddenPos = _shownPos - new Vector2(0, _mapRect.sizeDelta.y);
        _mapRect.anchoredPosition = _hiddenPos;

        _mapToggleButton.onClick.AddListener(ToggleDisplay);
    }

    private void ToggleDisplay()
    {
        _isShown = !_isShown;

        if (toggleMapCoroutine != null) StopCoroutine(toggleMapCoroutine);

        if (_isShown) toggleMapCoroutine = StartCoroutine(LerpRectTransform(_hiddenPos));
        else toggleMapCoroutine = StartCoroutine(LerpRectTransform(_shownPos));
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        _dragDelta = Vector3.zero;
    }

    public void OnDrag(PointerEventData eventData)
    {
        MoveCamera(eventData);
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        _dragDelta = Vector3.zero;
    }

    //Handle mini map zoom
    public void OnScroll(PointerEventData eventData)
    {
        _cam.orthographicSize -= eventData.scrollDelta.y * _scrollSensitivity;
        if (_cam.orthographicSize > _maxCamSize) _cam.orthographicSize = _maxCamSize;
        else if (_cam.orthographicSize < _minCamSize) _cam.orthographicSize = _minCamSize;
    }

    public void SetBounds(DungeonRoom[] rooms)
    {
        for (int i = 0; i < rooms.Length; i++)
        {
            if (rooms[i].Transform.position.x < minBoundsX) minBoundsX = rooms[i].Transform.position.x;
            if (rooms[i].Transform.position.x > maxBoundsX) maxBoundsX = rooms[i].Transform.position.x;

            if (rooms[i].Transform.position.z < minBoundsZ) minBoundsZ = rooms[i].Transform.position.z;
            if (rooms[i].Transform.position.z > maxBoundsZ) maxBoundsZ = rooms[i].Transform.position.z;
        }

        _camCenter.x = (minBoundsX + maxBoundsX) * 0.5f;
        _camCenter.y = _camTransform.transform.position.y;
        _camCenter.z = (minBoundsZ + maxBoundsZ) * 0.5f;

        _camTransform.transform.position = _camCenter;
    }

    private void MoveCamera(PointerEventData data)
    {
        _dragDelta.x = data.delta.x;
        _dragDelta.z = data.delta.y;

        _camTransform.position -= _dragDelta;

        if (_camTransform.position.x < minBoundsX) _camTransform.position = new Vector3(minBoundsX, height, _camTransform.position.z);
        else if (_camTransform.position.x > maxBoundsX) _camTransform.position = new Vector3(maxBoundsX, height, _camTransform.position.z);
        
        if (_camTransform.position.z < minBoundsZ) _camTransform.position = new Vector3(_camTransform.position.x, height, minBoundsZ);
        else if (_camTransform.position.z > maxBoundsZ) _camTransform.position = new Vector3(_camTransform.position.x, height, maxBoundsZ);
    }

    private IEnumerator LerpRectTransform(Vector3 endPos, float timeToMove = 0.5f)
    {
        float timeElapsed = 0;

        while (timeElapsed < timeToMove)
        {
            _mapRect.anchoredPosition = Vector3.Lerp(_mapRect.anchoredPosition, endPos, (timeElapsed / timeToMove));
            timeElapsed += Time.deltaTime;
            yield return null;
        }
        _mapRect.anchoredPosition = endPos;
    }
}
