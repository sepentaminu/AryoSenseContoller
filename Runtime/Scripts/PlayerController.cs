using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class PlayerController : MonoBehaviour
{
    [Header("Joysticks")]
    [SerializeField] private DynamicThumbstick portraitJoystick;
    [SerializeField] private DynamicThumbstick landscapeJoystick;

    [Header("Movement Settings")]
    public float moveSpeed = 5f;
    public Transform cam;

    [Header("Tablet Detection")]
    //public int tabletMinWidth = 1000;

    private Rigidbody rb;
    private Gyroscope gyro;
    private bool gyroSupported;
    private bool isTablet;
    private bool isLandscape;
    private DynamicThumbstick currentJoystick;

    // private void InitInvok()
    // {
    //     // 🧩 پیدا کردن جوی‌استیک‌ها اگر دستی ست نشده باشن
    //     
    //
    //    
    // }
    void Start()
    {
       // Invoke("InitInvok" , 5);

        if (!portraitJoystick)
            portraitJoystick = GameObject.Find("Dark Joystick port")?.GetComponent<DynamicThumbstick>();
        if (!landscapeJoystick)
            landscapeJoystick = GameObject.Find("Dark Joystick land")?.GetComponent<DynamicThumbstick>();
        
        if (!portraitJoystick || !landscapeJoystick)
            Debug.LogError("❌ یکی از جوی‌استیک‌ها پیدا نشد! اسم آبجکت‌ها رو چک کن.");
        rb = GetComponent<Rigidbody>();
        if(!cam)
            cam = Camera.main?.transform;

        // فعال‌سازی ژیروسکوپ
        gyroSupported = SystemInfo.supportsGyroscope;
        if (gyroSupported)
        {
            gyro = Input.gyro;
            gyro.enabled = true;
        }

        // تشخیص اولیه جهت صفحه (حتی بدون ژیروسکوپ)
        isLandscape = Screen.width > Screen.height;

        SelectActiveJoystick();
    }

    void Update()
    {
        if (!isTablet)
            UpdateOrientationFromGyro();

        SelectActiveJoystick();
    }

    void FixedUpdate()
    {
        if (currentJoystick == null || cam == null) return;

        Vector2 input = currentJoystick.Direction;

        if (input.magnitude > 0.1f)
        {
            Vector3 moveDir = cam.forward * input.y + cam.right * input.x;
            moveDir.y = 0;

            rb.MovePosition(rb.position + moveDir.normalized * moveSpeed * Time.fixedDeltaTime);

            Quaternion toRot = Quaternion.LookRotation(moveDir, Vector3.up);
            rb.rotation = Quaternion.Slerp(rb.rotation, toRot, 0.2f);
        }
    }

    private void UpdateOrientationFromGyro()
    {
        // برای ثبات بیشتر فقط بر اساس رزولوشن تست کن
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
 
}