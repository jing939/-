using UnityEngine;
using System.Collections;

public class CameraShake : MonoBehaviour
{
    public static CameraShake instance;

    private Vector3 originalPos;
    private float shakeDuration = 0f;
    private float shakeMagnitude = 0.7f;
    private float dampingSpeed = 1.0f;
    
    // 흔들림이 끝나고 복귀해야 할 기본 카메라 위치
    private Vector3 initialPosition;
    private float initialOrthoSize;
    private Camera cam;

    void Awake()
    {
        if (instance == null)
        {
            instance = this;
            initialPosition = transform.localPosition;
            originalPos = initialPosition;
            cam = GetComponent<Camera>();
            if (cam != null) initialOrthoSize = cam.orthographicSize;
        }
    }

    void OnEnable()
    {
        if (originalPos == Vector3.zero)
            originalPos = transform.localPosition;
        initialPosition = originalPos;
    }

    void Update()
    {
        if (shakeDuration > 0)
        {
            transform.localPosition = initialPosition + Random.insideUnitSphere * shakeMagnitude;
            shakeDuration -= Time.deltaTime * dampingSpeed;
        }
        else
        {
            shakeDuration = 0f;
            transform.localPosition = initialPosition;
        }
    }

    // 외부에서 흔들림을 요청하는 함수
    public void Shake(float duration, float magnitude)
    {
        shakeDuration = duration;
        shakeMagnitude = magnitude;
    }

    // 자주 쓰이는 프리셋
    public void LightShake() { Shake(0.1f, 0.1f); }     // 코인 굴러가면서 합을 맞댈 때
    public void MediumShake() { Shake(0.2f, 0.3f); }    // 일반 타격 시
    public void HeavyShake() { Shake(0.4f, 0.8f); }     // 치명타 또는 강력한 필살기

    private Coroutine zoomCoroutine;

    // [신규] 카메라 줌 및 이동
    public void FocusOnClash(Vector3 targetCenter, float zoomFactor = 0.7f, float duration = 0.5f)
    {
        if (cam == null) return;
        if (zoomCoroutine != null) StopCoroutine(zoomCoroutine);
        zoomCoroutine = StartCoroutine(DoZoomAndPan(targetCenter, initialOrthoSize * zoomFactor, duration));
    }

    // [신규] 카메라 원래 위치/크기로 복구
    public void ResetFocus(float duration = 0.5f)
    {
        if (cam == null) return;
        if (zoomCoroutine != null) StopCoroutine(zoomCoroutine);
        zoomCoroutine = StartCoroutine(DoZoomAndPan(originalPos, initialOrthoSize, duration));
    }

    private IEnumerator DoZoomAndPan(Vector3 targetPos, float targetSize, float duration)
    {
        float elapsed = 0f;
        Vector3 startPos = initialPosition;
        float startSize = cam.orthographicSize;

        // 타겟 위치는 카메라의 원래 Z값을 유지해야 합니다.
        Vector3 finalPos = new Vector3(targetPos.x, targetPos.y, originalPos.z);

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            t = t * t * (3f - 2f * t); // SmoothStep

            initialPosition = Vector3.Lerp(startPos, finalPos, t);
            cam.orthographicSize = Mathf.Lerp(startSize, targetSize, t);

            yield return null;
        }

        initialPosition = finalPos;
        cam.orthographicSize = targetSize;
    }
}
