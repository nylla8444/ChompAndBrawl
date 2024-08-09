using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraHandler : MonoBehaviour {
    [SerializeField] private BrawlManager brawlManager;
    [SerializeField] private Camera _camera;
    [SerializeField] private float minCamSize;
    [SerializeField] private float maxCamSize;
    [SerializeField] private float maxCamYValue;
    [SerializeField] private float cameraMargin;
    [SerializeField] private float smoothTime;
    [SerializeField, Tooltip("Default: -10")] private float cameraZValue;
    [HideInInspector] public float shakeDuration;
    [HideInInspector] public float shakeStrength;

    private Vector3 currentCamVelocity;
    private Vector3 initialCamPosition;
    private GameObject pacman;
    private GameObject ghost;

    private void Start() {
        pacman = brawlManager.GetPacman().gameObject;
        ghost = brawlManager.GetGhost().gameObject;

        shakeDuration = 0;
        initialCamPosition = _camera.transform.position;
    }

    private void Update() {
        // Handle camera shake
        if (shakeDuration > 0) {
            Vector3 shakeOffset = Random.insideUnitSphere * shakeStrength;
            shakeOffset.z = 0;
            _camera.transform.localPosition = initialCamPosition + shakeOffset;

            shakeDuration -= Time.deltaTime;
            if (shakeDuration <= 0) {
                _camera.transform.localPosition = initialCamPosition;
            }
        } else {
            Vector3 targetPosition = Vector3.SmoothDamp(
                _camera.transform.position,
                getMidpoint(),
                ref currentCamVelocity,
                smoothTime
            );
            _camera.transform.position = new Vector3(targetPosition.x, targetPosition.y, cameraZValue);

            AdjustCameraSize();
        }
    }

    private Vector3 getMidpoint() {
        Vector3 midpoint = (pacman.transform.position + ghost.transform.position) / 2;
        return new Vector3(midpoint.x, Mathf.Max(midpoint.y, getMaxCamYValue()), cameraZValue);
    }

    private float getMaxCamYValue() {
        float size = _camera.orthographicSize;
        float visibleBottom = _camera.transform.position.y - size;

        return Mathf.Max(0, visibleBottom);
    }

    private void AdjustCameraSize() {
        float distance = Vector3.Distance(pacman.transform.position, ghost.transform.position);
        float requiredSize = (distance / 2) + cameraMargin;

        _camera.orthographicSize = Mathf.Clamp(requiredSize, minCamSize, maxCamSize);
    }

    public void Shake(float _shakeDuration, float _shakeStrength) {
        shakeDuration = _shakeDuration;
        shakeStrength = _shakeStrength;
        initialCamPosition = _camera.transform.position;
    }
}
