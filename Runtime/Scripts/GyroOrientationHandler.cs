using System;
using UnityEngine;

public class GyroOrientationHandler : MonoBehaviour
{
    [Header("Objects to toggle")]
    public GameObject portraitObject;
    public GameObject landscapeObject;

    private bool isLandscape;
    private Vector3 portraitInitialPos;
    private Vector3 landscapeInitialPos;

    private void Awake()
    {
       
    }

    void Start()
    {
        // پیدا کردن آبجکت‌های غیرفعال از کل صحنه
        var allObjects = Resources.FindObjectsOfTypeAll<GameObject>();
        foreach (var obj in allObjects)
        {
            if (obj.name == "tumpstick port")
                portraitObject = obj;
            else if (obj.name == "tumpstick Land")
                landscapeObject = obj;
            Camera.main.GetComponent<OrbitCamera>().enabled = true;
        }

        // ذخیره موقعیت اولیه جوی‌استیک‌ها
        if (portraitObject != null)
            portraitInitialPos = portraitObject.transform.position;
        if (landscapeObject != null)
            landscapeInitialPos = landscapeObject.transform.position;

        UpdateOrientation(force: true);
    }

    void Update()
    {
        UpdateOrientation();
    }

    private void UpdateOrientation(bool force = false)
    {
        bool newLandscape = Screen.width > Screen.height;

        // اگه جهت صفحه تغییر کرده یا Force Update اومده
        if (force || newLandscape != isLandscape)
        {
            isLandscape = newLandscape;

            if (portraitObject != null)
            {
                portraitObject.SetActive(!isLandscape);
                portraitObject.transform.position = portraitInitialPos; // ریست مکان
            }

            if (landscapeObject != null)
            {
                landscapeObject.SetActive(isLandscape);
                landscapeObject.transform.position = landscapeInitialPos; // ریست مکان
            }
        }
    }

    public bool IsLandscape()
    {
        return isLandscape;
    }
}