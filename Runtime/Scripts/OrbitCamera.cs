using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.EnhancedTouch;
using ETouch = UnityEngine.InputSystem.EnhancedTouch.Touch;

public class OrbitCamera : MonoBehaviour
{
    public Transform target;

    [Header("Joysticks")]
    [SerializeField] private DynamicThumbstick portraitJoystick;
    [SerializeField] private DynamicThumbstick landscapeJoystick;

    [Header("Orbit Settings")]
    public float distance = 5f;
    public float xSpeed = 30f;
    public float ySpeed = 30f;
    public float yMinLimit = -20f;
    public float yMaxLimit = 60f;

    [Header("Zoom Settings")]
    public float minDistance = 2f;
    public float maxDistance = 15f;
    public float pinchZoomSpeed = 0.01f;

    [Header("Tablet Detection")]
    //public int tabletMinWidth = 1000;

    [Header("UI Settings")]
    public GraphicRaycaster graphicRaycaster;
    public EventSystem eventSystem;
    public List<Graphic> uiBlockers = new List<Graphic>();

    private float x = 0f, y = 20f;
    private float lastPinchDist = 0f;
    private bool pinchActive = false;
    private bool suppressOrbitThisFrame = false;

    private  UnityEngine.Gyroscope gyro;
    
    private bool isTablet;
    private bool isLandscape;
    private DynamicThumbstick currentJoystick;

    void OnEnable() { EnhancedTouchSupport.Enable(); TouchSimulation.Enable(); }
    void OnDisable() { TouchSimulation.Disable(); EnhancedTouchSupport.Disable(); }

    // private void InitInvok()
    // {
    //     // 🧩 پیدا کردن جوی‌استیک‌ها
    //     
    //     var allObjects = Resources.FindObjectsOfTypeAll<GameObject>();
    //     foreach (var obj in allObjects)
    //     {
    //         if (obj.name == "Dark Joystick port")
    //             portraitJoystick = obj.GetComponent<DynamicThumbstick>();
    //         else if (obj.name == "Dark Joystick land")
    //             landscapeJoystick = obj.GetComponent<DynamicThumbstick>();;
    //         Camera.main.GetComponent<OrbitCamera>().enabled = true;
    //     }
    //
    //     if (!portraitJoystick || !landscapeJoystick)
    //         Debug.LogError("❌ یکی از جوی‌استیک‌های OrbitCamera پیدا نشد!");
    //
    //     if (!target)
    //         target = GameObject.Find("Cube").transform;
    //     if (!graphicRaycaster & currentJoystick == portraitJoystick)
    //     {
    //         graphicRaycaster = GameObject.Find("Panel port").GetComponent<GraphicRaycaster>();
    //         uiBlockers[0] = GameObject.Find("Button port").GetComponent<Graphic>();
    //     }
    //         
    //     
    //     if (!graphicRaycaster & currentJoystick == landscapeJoystick)
    //     {
    //         graphicRaycaster = GameObject.Find("Panel land").GetComponent<GraphicRaycaster>();
    //         uiBlockers[0] = GameObject.Find("Button land").GetComponent<Graphic>();
    //     }
    //
    //     SelectActiveJoystick();
    // }
    void Start()
    {
       // Invoke("InitInvok" , 5);
        isLandscape = Screen.width > Screen.height;

        if (!portraitJoystick)
            portraitJoystick = GameObject.Find("Dark Joystick port")?.GetComponent<DynamicThumbstick>();
        if (!landscapeJoystick)
            landscapeJoystick = GameObject.Find("Dark Joystick land")?.GetComponent<DynamicThumbstick>();
        
        
        SelectActiveJoystick();

        var ang = transform.eulerAngles;
        x = ang.y;
        y = ang.x;
    }

    void LateUpdate()
    {
        if (!isTablet)
            UpdateOrientationFromGyro();

        SelectActiveJoystick();

        suppressOrbitThisFrame = false;
        HandleTouch();
        ApplyTransform();
    }

    private void UpdateOrientationFromGyro()
    {
        isLandscape = Screen.width > Screen.height;
    }

    private void SelectActiveJoystick()
    {
        if (portraitJoystick == null || landscapeJoystick == null)
            return;

        // گرفتن وضعیت فعلی از GyroOrientationHandler
        GyroOrientationHandler gyroHandler = FindObjectOfType<GyroOrientationHandler>();
        if (gyroHandler == null)
            return;

        bool isLandscapeNow = gyroHandler.IsLandscape();

        // فقط جوی‌استیک فعال رو مشخص کن (بدون تغییر SetActive)
        if (isLandscapeNow && landscapeJoystick.gameObject.activeInHierarchy)
        {
            currentJoystick = landscapeJoystick;
        }
        else if (!isLandscapeNow && portraitJoystick.gameObject.activeInHierarchy)
        {
            currentJoystick = portraitJoystick;
        }
    }

    private void HandleTouch()
    {
        if (target == null || currentJoystick == null) return;

        var touches = ETouch.activeTouches;
        if (touches.Count == 0) return;

        var allCandidates = new List<ETouch>(4);
        foreach (var t in touches)
        {
            if (IsOverUIBlocker(t.screenPosition)) continue;
            if (currentJoystick.Pressed &&
                Vector2.Distance(t.screenPosition, currentJoystick.CurrentTouchScreenPos) < 70f)
                continue;

            allCandidates.Add(t);
        }

        if (allCandidates.Count >= 2)
        {
            var a = allCandidates[0];
            var b = allCandidates[1];
            float curDist = Vector2.Distance(a.screenPosition, b.screenPosition);

            if (!pinchActive)
            {
                pinchActive = true;
                lastPinchDist = curDist;
            }
            else
            {
                float delta = curDist - lastPinchDist;
                distance = Mathf.Clamp(distance - delta * pinchZoomSpeed, minDistance, maxDistance);
                lastPinchDist = curDist;
            }
            suppressOrbitThisFrame = true;
            return;
        }
        else pinchActive = false;

        if (suppressOrbitThisFrame) return;

        foreach (var t in allCandidates)
        {
            if (t.phase == UnityEngine.InputSystem.TouchPhase.Moved)
            {
                Vector2 d = t.delta;
                x += d.x * xSpeed * 0.02f;
                y -= d.y * ySpeed * 0.02f;
                y = Mathf.Clamp(y, yMinLimit, yMaxLimit);
            }
        }
    }

    private bool IsOverUIBlocker(Vector2 pos)
    {
        if (!graphicRaycaster || !eventSystem) return false;
        var ped = new PointerEventData(eventSystem) { position = pos };
        var results = new List<RaycastResult>();
        graphicRaycaster.Raycast(ped, results);
        foreach (var r in results)
        {
            var g = r.gameObject.GetComponent<Graphic>();
            if (g != null && uiBlockers.Contains(g)) return true;
        }
        return false;
    }

    private void ApplyTransform()
    {
        Quaternion rot = Quaternion.Euler(y, x, 0);
        Vector3 pos = rot * new Vector3(0, 0, -distance) + target.position;
        transform.rotation = rot;
        transform.position = pos;
    }
}