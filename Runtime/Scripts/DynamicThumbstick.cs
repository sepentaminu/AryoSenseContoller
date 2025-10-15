using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using System.Collections.Generic;

public class DynamicThumbstick : MonoBehaviour, IDragHandler, IPointerDownHandler, IPointerUpHandler
{
    [Header("UI")]
    [Tooltip("دایره/پنل پس‌زمینه جوی‌استیک")]
    public Image background;       // دایره اصلی
    [Tooltip("دایره کوچیک هندل جوی‌استیک")]
    public Image handle;           // دایره کوچیک (هندل)

    [Header("Line Segments (Straight Trail)")]
    [Tooltip("Prefab یک نقطه/سگمنت UI (مثلاً Image 8-12px)")]
    public GameObject segmentPrefab;
    [Tooltip("والد سگمنت‌ها (زیر Canvas)")]
    public Transform segmentParent;
    [Tooltip("فاصله بین سگمنت‌ها (px)")]
    public float segmentSpacing = 20f;
    [Tooltip("حداکثر تعداد سگمنت‌ها (ایمنی)")]
    public int maxSegments = 200;

    // وضعیت داخلی
    private readonly List<GameObject> segments = new List<GameObject>();
    private Vector2 inputVector;
    private bool pressed = false;

    // خروجی/دسترسی عمومی
    public int ActivePointerId { get; private set; } = -1;     // آیدی انگشتِ جوی‌استیک
    public bool Pressed => pressed;                             // آیا جوی‌استیک درگ می‌شود؟
    public Vector2 Direction => inputVector;                    // جهت نرمال‌شده از مرکز به هندل
    public Vector2 StartScreenPos { get; private set; }         // نقطه شروع لمس
    public Vector2 CurrentTouchScreenPos { get; private set; }  // موقعیت فعلی لمسِ جوی‌استیک

    private void Start()
    {
        background = GameObject.FindWithTag("JoyStickBase").GetComponent<Image>();
        handle = GameObject.FindWithTag("JoyStickHandle").GetComponent<Image>();
        segmentPrefab = GameObject.FindWithTag("Foot");
        segmentParent = GameObject.FindWithTag("FTparent").transform;
        if (background != null) background.enabled = false;
        if (handle != null) handle.enabled = false;
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        // اگر از قبل انگشت فعالی داریم، تاچ‌های بعدی را نپذیر (جلوگیری از دزدیدن کنترل)
        if (pressed) return;

        pressed = true;
        if (background != null) background.enabled = true;
        if (handle != null) handle.enabled = true;

        // قرار دادن مرکز جوی‌استیک و هندل روی نقطه لمس
        if (background != null) background.rectTransform.position = eventData.position;
        if (handle != null) handle.rectTransform.position = eventData.position;

        ActivePointerId = eventData.pointerId;
        StartScreenPos = eventData.position;
        CurrentTouchScreenPos = eventData.position;

        
        // شروع خط (طول صفر)
        UpdateSegments(StartScreenPos, StartScreenPos);
    }

    public void OnDrag(PointerEventData eventData)
    {
        // فقط همان انگشتی که شروع کرده، اجازه‌ی درگ دارد
        if (eventData.pointerId != ActivePointerId) return;

        CurrentTouchScreenPos = eventData.position;

        // جهت نرمال‌شده از مرکز پس‌زمینه تا هندل
        if (background != null)
        {
            Vector2 delta = eventData.position - (Vector2)background.rectTransform.position;
            inputVector = delta.sqrMagnitude > 0.0001f ? delta.normalized : Vector2.zero;
        }
        else
        {
            inputVector = Vector2.zero;
        }

        if (handle != null) handle.rectTransform.position = eventData.position;

        // به‌روزرسانی خط یک‌راستا از نقطه شروع تا محل فعلی هندل
        UpdateSegments(StartScreenPos, eventData.position);
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        // فقط اگر همون انگشت اولیه رها شد، ریست کن
        if (eventData.pointerId != ActivePointerId) return;

        pressed = false;
        inputVector = Vector2.zero;

        // ریست پوزیشن هندل
        if (background != null && handle != null)
            handle.rectTransform.position = background.rectTransform.position;

        if (background != null) background.enabled = false;
        if (handle != null) handle.enabled = false;

        ActivePointerId = -1;
        CurrentTouchScreenPos = Vector2.zero;

        // پاک کردن همه‌ی سگمنت‌ها
        ClearSegments();
    }

    /// <summary>
    /// آیا مختصاتِ صفحه داخل ناحیه‌ی بک‌گراند جوی‌استیک است؟
    /// </summary>
    public bool IsTouchOnJoystick(Vector2 screenPos)
    {
        if (background == null) return false;
        return RectTransformUtility.RectangleContainsScreenPoint(background.rectTransform, screenPos);
    }

    // ----------------- مدیریت سگمنت‌ها (خط یک‌راستا) -----------------

    private void UpdateSegments(Vector2 from, Vector2 to)
    {
        if (segmentPrefab == null || segmentParent == null) return;

        Vector2 dir = to - from;
        float len = dir.magnitude;

        // تعداد سگمنت‌های مورد نیاز بر اساس طول
        int targetCount = Mathf.Clamp(
            Mathf.FloorToInt(len / Mathf.Max(1f, segmentSpacing)),
            0,
            maxSegments
        );

        // افزایش تا رسیدن به targetCount
        while (segments.Count < targetCount)
        {
            var dot = Instantiate(segmentPrefab, segmentParent);
            segments.Add(dot);
        }
        // کاهش تا رسیدن به targetCount
        while (segments.Count > targetCount)
        {
            int last = segments.Count - 1;
            Destroy(segments[last]);
            segments.RemoveAt(last);
        }

        if (targetCount == 0) return;

        Vector2 dirN = dir.normalized;

        // جای‌گذاری همه سگمنت‌ها روی یک راستا
        for (int i = 0; i < segments.Count; i++)
        {
            Vector2 p = from + dirN * ((i + 1) * segmentSpacing);
            var rt = segments[i].GetComponent<RectTransform>();
            if (rt != null)
            {
                rt.position = p;

                // اگر Prefab مستطیلی است و می‌خواهی هم‌جهت خط بچرخد، این را باز کن:
                // rt.right = new Vector3(dirN.x, dirN.y, 0f);
            }
        }
    }

    private void ClearSegments()
    {
        for (int i = segments.Count - 1; i >= 0; i--)
        {
            if (segments[i] != null) Destroy(segments[i]);
        }
        segments.Clear();
    }
}
