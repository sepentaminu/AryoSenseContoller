#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Globalization;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// Nora — «Sample Package» Auto Binder
///
/// این ادیتوراسکریپت وقتی فولدر  Assets/Sample  وجود داشته باشه،
/// به صورت خودکار اسکریپت‌های موردنظر رو به آبجکت‌های مربوطه اضافه می‌کنه
/// و فیلدهاشون رو با رفرنس‌هایی که این پایین تعریف کردی پر می‌کنه.
/// علاوه‌براین، از منوی  Tools ▸ Sample ▸ Bind Scene  هم قابل اجراست.
///
/// نکته‌ها:
/// 1) فقط وقتی اجرا می‌شه که فولدر  Assets/Sample  موجود باشه.
/// 2) آیدمپوتنت: اگر قبلاً انجام شده باشه، فقط کمبودها رو پر می‌کنه.
/// 3) هم فیلدهای public و هم private [SerializeField] با SerializedObject ست می‌شن.
/// 4) برای مقداردهی، اگر نوع فیلد یک Component باشه، سعی می‌کنه همان کامپوننت را از آبجکت هدف بردارد؛
///    در غیر این صورت اگر GameObject باشد، خود گیم‌آبجکت را ست می‌کند.
/// 5) اگر اسم فیلد را اشتباه بنویسی، به‌وضوح هشدار می‌دهد.
///
/// توسط: نورا ✨
/// </summary>
[InitializeOnLoad]
public static class SamplePackageAutoBinder
{
    // مسیر فولدر شرطی
    const string GuardFolder = "Assets/Sample";
    // کلید برای جلوگیری از اجرای پشت‌سرهم بی‌مورد در یک سشن ادیتور
    const string SessionKey = "IMAN_SAMPLE_BINDER_LAST_SCENE";

    // ⭐️ جدول مپینگ‌ها — اینجا را مطابق صحنه‌ات ادیت کن
    // ObjectName ➜ ScriptName + Fields(Name ➜ TargetObjectName)
    static readonly List<Entry> Entries = new()
    {
       

        // DynamicThumbstick روی Dark Joystick port
        new Entry(
            objectName: "Dark Joystick port",
            scriptName: "DynamicThumbstick",
            fieldMap: new()
            {
                { "background", "Joystick Base" },
                { "handle", "Joystick" },
                { "segmentPrefab", "footStep" },
                { "segmentParent", "Canvas" },
            }
        ),
        
        
        new Entry(
        objectName: "Dark Joystick land",
        scriptName: "UltimateJoystick",
        fieldMap: new()
        {
            { "joystickBase", "Joystick Base Land" },
            { "joystick", "Joystick Land" },
            {"customActivationRange" , "true"},
            {"activationWidth","50"},
            {"activationHeight","50"},
            {"dynamicPositioning", "true"},
            {"extendRadius","true"},
            {"joystickSize","1.3"}
            
            
        }
        ),
        
        new Entry(
            objectName: "Dark Joystick port",
            scriptName: "UltimateJoystick",
            fieldMap: new()
            {
                { "joystickBase", "Joystick Base" },
                { "joystick", "Joystick" },
                {"customActivationRange" , "true"},
                {"activationWidth","100"},
                {"activationHeight","33"},
                {"dynamicPositioning", "true"},
                {"extendRadius","true"},
            
            }
        ),
        // DynamicThumbstick روی Dark Joystick land
        new Entry(
            objectName: "Dark Joystick land",
            scriptName: "DynamicThumbstick",
            fieldMap: new()
            {
                { "background", "Joystick Base Land" },
                { "handle", "Joystick Land" },
                { "segmentPrefab", "footStep" },
                { "segmentParent", "Canvas" },
            }
        ),

        // OrbitCamera روی Main Camera
        new Entry(
            objectName: "Main Camera",
            scriptName: "OrbitCamera",
            fieldMap: new()
            {
                { "target", "Player" },
                { "portraitJoystick", "Dark Joystick port" },
                { "landscapeJoystick", "Dark Joystick land" },
                { "graphicRaycaster", "Panel port" },
                { "eventSystem", "EventSystem" },
            }
        ),

        // GyroOrientationHandler روی OrientationHandler
        new Entry(
            objectName: "OrientationHandler",
            scriptName: "GyroOrientationHandler",
            fieldMap: new()
            {
                { "portraitObject", "tumpstick port" },
                { "landscapeObject", "tumpstick Land" },
            }
        ),
        // PlayerController روی Player
        new Entry(
            objectName: "Player",
            scriptName: "PlayerController",
            fieldMap: new()
            {
                { "portraitJoystick", "Dark Joystick port" },
                { "landscapeJoystick", "Dark Joystick land" },
                { "cam", "Main Camera" },
            }
        ),

        // (اختیاری) SimpleFollowCamera اگر لازم بود
        new Entry(
            objectName: "Main Camera",
            scriptName: "SimpleFollowCamera",
            fieldMap: new()
            {
                { "target", "Player" },
            }
        ),

        // اگر از UltimateJoystick هم خواستی ست کنی، اینجا اضافه کن…
        // new Entry("Some Joystick Root", "UltimateJoystick", new(){ /* … */ }),
    };

    static SamplePackageAutoBinder()
    {
        // وقتی پروژه تغییر کرد (مثل اضافه شدن فولدر Sample) یا صحنه باز شد، تلاش به بایند کردن
        EditorApplication.projectChanged += TryBindIfGuardExists;
        EditorSceneManager.sceneOpened += (_, __) => TryBindIfGuardExists();
        // اجرای اولیه بعد از لود Domain
        EditorApplication.delayCall += TryBindIfGuardExists;
    }

    static bool IsBusyCompilingOrUpdating()
    {
        // Skip while editor is compiling or importing/updating assets
        return EditorApplication.isCompiling || EditorApplication.isUpdating;
    }

    static bool GuardExists() => AssetDatabase.IsValidFolder(GuardFolder);

    [MenuItem("Tools/Sample/Bind Scene", priority = 10)]
    public static void BindSceneNow()
    {
        if (IsBusyCompilingOrUpdating())
        {
            EditorApplication.delayCall += BindSceneNow;
            return;
        }

        if (!GuardExists())
        {
            EditorUtility.DisplayDialog(
                "Sample Binder",
                $"برای اجرا باید فولدر '{GuardFolder}' وجود داشته باشد.",
                "باشه");
            return;
        }
        RunBinding(verbose: true);
    }

    [MenuItem("Tools/Sample/Diagnostics/Log Known MonoBehaviours", priority = 50)]
    public static void LogKnownMonoBehaviours()
    {
        var list = new List<string>();
        try
        {
            foreach (var t in TypeCache.GetTypesDerivedFrom<MonoBehaviour>())
                list.Add(t.FullName);
        }
        catch
        {
            foreach (var asm in AppDomain.CurrentDomain.GetAssemblies())
            {
                try
                {
                    list.AddRange(asm.GetTypes()
                        .Where(x => typeof(MonoBehaviour).IsAssignableFrom(x))
                        .Select(x => x.FullName));
                }
                catch { }
            }
        }
        list.Sort(StringComparer.Ordinal);
        Debug.Log("[Sample Binder] Known MonoBehaviours (" + list.Count + "):\n- " + string.Join("\n- ", list.Take(100)));
    }

    static void TryBindIfGuardExists()
    {
        if (IsBusyCompilingOrUpdating())
        {
            EditorApplication.delayCall += TryBindIfGuardExists;
            return;
        }

        if (!GuardExists()) return;

        // جلوگیری از تکرار شدید هنگام رویدادهای سریالی
        var activeScenePath = EditorSceneManager.GetActiveScene().path;
        var lastScene = SessionState.GetString(SessionKey, string.Empty);
        if (lastScene == activeScenePath)
            return;

        RunBinding(verbose: false);
        SessionState.SetString(SessionKey, activeScenePath);
    }

    static IEnumerable<Type> FindSimilarTypes(string name)
    {
        var results = new HashSet<Type>();
        string n = name.ToLowerInvariant();

        // TypeCache first
        try
        {
            foreach (var t in TypeCache.GetTypesDerivedFrom<MonoBehaviour>())
            {
                var fn = t.FullName ?? t.Name;
                if (fn == null) continue;
                if (fn.IndexOf(name, StringComparison.OrdinalIgnoreCase) >= 0) results.Add(t);
                else if (t.Name.Equals(name, StringComparison.OrdinalIgnoreCase)) results.Add(t);
            }
        }
        catch { }

        // Assemblies fallback
        foreach (var asm in AppDomain.CurrentDomain.GetAssemblies())
        {
            Type[] types = Array.Empty<Type>();
            try { types = asm.GetTypes(); } catch { }
            foreach (var t in types)
            {
                if (!typeof(MonoBehaviour).IsAssignableFrom(t)) continue;
                var fn = t.FullName ?? t.Name;
                if (fn == null) continue;
                if (fn.IndexOf(name, StringComparison.OrdinalIgnoreCase) >= 0) results.Add(t);
            }
        }
        return results.Take(10);
    }

    static void RunBinding(bool verbose)
    {
        int ok = 0, warn = 0, fail = 0;

        foreach (var entry in Entries)
        {
            var go = FindGO(entry.ObjectName);
            if (!go)
            {
                // suggest similar names from scene
                var all = Resources.FindObjectsOfTypeAll<Transform>()
                    .Where(t => t.hideFlags == HideFlags.None)
                    .Select(t => t.name)
                    .Distinct()
                    .Where(n => n.IndexOf(entry.ObjectName, StringComparison.OrdinalIgnoreCase) >= 0)
                    .Take(10);
                var hint = all.Any() ? string.Join(", ", all) : "—";
                LogWarn($"[Binder] GameObject '{entry.ObjectName}' پیدا نشد. Candidates: {hint}");
                warn++; continue;
            }

            // پیدا کردن Type اسکریپت در همه اسمبلی‌ها
            var type = FindTypeByName(entry.ScriptName);
            if (type == null)
            {
                var hints = string.Join(", ", FindSimilarTypes(entry.ScriptName).Select(t => t.FullName));
                if (string.IsNullOrEmpty(hints)) hints = "—";
                LogWarn($"[Binder] Type با نام '{entry.ScriptName}' پیدا نشد (اسم/namespace را چک کن). Candidates: {hints}");
#if UNITY_EDITOR
                Debug.LogWarning($"[Binder][Diag] Couldn't resolve type '{entry.ScriptName}'. If this class is inside a package, ensure its assembly compiles without errors and that the class derives from MonoBehaviour and matches the file name. Also ensure any asmdef define constraints are satisfied for the current platform.");
#endif
                warn++; continue;
            }

            var comp = go.GetComponent(type) ?? go.AddComponent(type);

            // با SerializedObject ست می‌کنیم تا private [SerializeField] هم پشتیبانی شود
            var so = new SerializedObject(comp);
            foreach (var kv in entry.FieldMap)
            {
                string fieldName = kv.Key;
                string refName   = kv.Value;

                var sp = so.FindProperty(fieldName);
                if (sp == null)
                {
                    LogWarn($"[Binder] فیلد '{fieldName}' روی '{entry.ScriptName}' پیدا نشد.");
                    warn++; continue;
                }

                // ابتدا تلاش می‌کنیم مقدار اسکالر ست کنیم (صرف‌نظر از نوع گزارش‌شده‌ی SerializedProperty)
                if (AssignScalar(sp, refName))
                {
                    ok++;
                    continue;
                }
                // اگر SerializedProperty اجازه نداد، با Reflection تلاش می‌کنیم
                if (TryAssignScalarByReflection(comp, fieldName, refName))
                {
                    ok++;
                    continue;
                }
                // اگر هیچ‌کدام موفق نشدند، می‌رویم سراغ منطق رفرنس آبجکت

                // پیدا کردن آبجکت مرجع از روی اسم
                var refGO = FindGO(refName);
                if (!refGO)
                {
                    LogWarn($"[Binder] مرجع '{refName}' برای فیلد '{fieldName}' پیدا نشد.");
                    warn++; continue;
                }

                // بر اساس نوع فیلد، رفرنس مناسب را ست کن
                bool assigned = AssignProperty(sp, refGO);
                if (!assigned)
                {
                    // اگر نوع property ناشناخته بود، سعی می‌کنیم با Reflection ست کنیم
                    assigned = TryAssignByReflection(comp, fieldName, refGO);
                }

                if (!assigned)
                {
                    LogWarn($"[Binder] نتونستم فیلد '{fieldName}' را ست کنم — نوع ناسازگار؟");
                    warn++;
                }
                else ok++;
            }

            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(go);
        }

        if (verbose)
        {
            EditorUtility.DisplayDialog(
                "Sample Binder",
                $"تمام شد!\nموفق: {ok}\nاخطار: {warn}",
                "عالی" );
        }
        else
        {
            Debug.Log($"[Sample Binder] ok:{ok} warn:{warn} fail:{fail}");
        }
    }

    static Type FindTypeByName(string typeName)
    {
        // Normalize for case-insensitive compare
        string tn = typeName;
        StringComparison cmp = StringComparison.Ordinal;

        // A) Fast path: Unity's TypeCache (includes package assemblies)
        try
        {
            foreach (var t in TypeCache.GetTypesDerivedFrom<MonoBehaviour>())
            {
                if (t.Name.Equals(tn, cmp) || (t.FullName?.EndsWith("." + tn, cmp) ?? false))
                    return t;
            }
        }
        catch { /* TypeCache not available? fallback below */ }

        // B) All loaded assemblies
        foreach (var asm in AppDomain.CurrentDomain.GetAssemblies())
        {
            Type match = null;
            try { match = asm.GetTypes().FirstOrDefault(x => x.Name.Equals(tn, cmp)); } catch {}
            if (match != null) return match;
        }
        foreach (var asm in AppDomain.CurrentDomain.GetAssemblies())
        {
            Type match = null;
            try { match = asm.GetTypes().FirstOrDefault(x => x.FullName != null && x.FullName.EndsWith("." + tn, cmp)); } catch {}
            if (match != null) return match;
        }

#if UNITY_EDITOR
        // C) Search script assets (also looks in Packages/)
        var foundScriptPaths = new List<string>();
        var foundButNoClass = new List<string>();
        string[] guids = AssetDatabase.FindAssets($"t:MonoScript {tn}");
        foreach (var guid in guids)
        {
            var path = AssetDatabase.GUIDToAssetPath(guid);
            foundScriptPaths.Add(path);
            var ms = AssetDatabase.LoadAssetAtPath<MonoScript>(path);
            if (!ms) continue;
            var klass = ms.GetClass();
            if (klass == null)
            {
                foundButNoClass.Add(path);
                continue;
            }
            if (klass.Name.Equals(tn, cmp) || (klass.FullName?.EndsWith("." + tn, cmp) ?? false))
                return klass;
        }
        if (foundScriptPaths.Count > 0 && foundButNoClass.Count > 0)
        {
            Debug.LogWarning($"[Binder][FindTypeByName] Scripts named '{tn}' were found but their classes couldn't be loaded. Likely compile errors or class name/file name mismatch, or the class doesn't derive from MonoBehaviour.\nAll matches:\n- " + string.Join("\n- ", foundScriptPaths.Take(10)) + (foundScriptPaths.Count > 10 ? "\n..." : "") + "\nNoClass:\n- " + string.Join("\n- ", foundButNoClass.Take(10)));
        }
        else if (foundScriptPaths.Count > 0)
        {
            Debug.Log($"[Binder][FindTypeByName] Script assets for '{tn}' found at:\n- " + string.Join("\n- ", foundScriptPaths.Take(10)));
        }
#endif

        return null;
    }

    static GameObject FindGO(string nameOrPath)
    {
        // 1) Direct path or name (supports "/Root/Child")
        var go = GameObject.Find(nameOrPath);
        if (go) return go;

        // 2) Search among all scene objects, including inactive
        var all = Resources.FindObjectsOfTypeAll<Transform>();
        go = all.FirstOrDefault(t => t.hideFlags == HideFlags.None && t.name == nameOrPath)?.gameObject;
        if (go) return go;

        // 3) Fuzzy contains (case-insensitive)
        go = all.FirstOrDefault(t => t.hideFlags == HideFlags.None && t.name.IndexOf(nameOrPath, StringComparison.OrdinalIgnoreCase) >= 0)?.gameObject;
        return go;
    }

    static bool AssignScalar(SerializedProperty sp, string valueStr)
    {
        // Handles non-ObjectReference SerializedProperty types
        var inv = CultureInfo.InvariantCulture;
        switch (sp.propertyType)
        {
            case SerializedPropertyType.Boolean:
                {
                    bool b;
                    if (bool.TryParse(valueStr, out b)) { sp.boolValue = b; return true; }
                    if (valueStr == "1") { sp.boolValue = true; return true; }
                    if (valueStr == "0") { sp.boolValue = false; return true; }
                    return false;
                }
            case SerializedPropertyType.Integer:
                {
                    int i;
                    if (int.TryParse(valueStr, NumberStyles.Integer, inv, out i)) { sp.intValue = i; return true; }
                    return false;
                }
            case SerializedPropertyType.Float:
                {
                    float f;
                    if (float.TryParse(valueStr, NumberStyles.Float | NumberStyles.AllowThousands, inv, out f)) { sp.floatValue = f; return true; }
                    return false;
                }
            case SerializedPropertyType.String:
                sp.stringValue = valueStr;
                return true;
            default:
                return false;
        }
    }

    static bool AssignProperty(SerializedProperty sp, GameObject refGO)
    {
        // فقط برای ObjectReference ها اینجا ست می‌کنیم؛ سایر انواع (float/int/...) را با Reflection انجام بده
        if (sp.propertyType != SerializedPropertyType.ObjectReference)
            return false;

        // از نوع مورد انتظار کامپوننت حدس بزن
        var type = GetManagedFieldType(sp);
        UnityEngine.Object value = null;

        if (type == typeof(GameObject))
            value = refGO;
        else if (typeof(Component).IsAssignableFrom(type))
            value = refGO.GetComponent(type);
        else
            return false; // نوع ناشناخته برای ObjectReference

        if (value == null) return false;
        sp.objectReferenceValue = value;
        return true;
    }

    static bool TryAssignScalarByReflection(Component comp, string fieldName, string valueStr)
    {
        var type = comp.GetType();
        var flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
        var fi = type.GetField(fieldName, flags);
        if (fi == null) return false;

        var inv = CultureInfo.InvariantCulture;

        try
        {
            if (fi.FieldType == typeof(bool))
            {
                bool b;
                if (bool.TryParse(valueStr, out b)) { fi.SetValue(comp, b); return true; }
                if (valueStr == "1") { fi.SetValue(comp, true); return true; }
                if (valueStr == "0") { fi.SetValue(comp, false); return true; }
                return false;
            }
            if (fi.FieldType == typeof(int))
            {
                int i;
                if (int.TryParse(valueStr, NumberStyles.Integer, inv, out i)) { fi.SetValue(comp, i); return true; }
                return false;
            }
            if (fi.FieldType == typeof(float))
            {
                float f;
                if (float.TryParse(valueStr, NumberStyles.Float | NumberStyles.AllowThousands, inv, out f)) { fi.SetValue(comp, f); return true; }
                return false;
            }
            if (fi.FieldType == typeof(string))
            {
                fi.SetValue(comp, valueStr);
                return true;
            }
        }
        catch { }
        return false;
    }

    static bool TryAssignByReflection(Component comp, string fieldName, GameObject refGO)
    {
        var type = comp.GetType();
        var flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
        var fi = type.GetField(fieldName, flags);
        if (fi == null) return false;

        object val = null;
        if (fi.FieldType == typeof(GameObject))
            val = refGO;
        else if (typeof(Component).IsAssignableFrom(fi.FieldType))
            val = refGO.GetComponent(fi.FieldType);
        else if (fi.FieldType == typeof(Transform))
            val = refGO.transform;
        else if (fi.FieldType == typeof(string))
            val = refGO.name;
        else if (fi.FieldType == typeof(int))
        {
            // handled elsewhere; keep for completeness if someone passes numeric GameObject name
            if (int.TryParse(refGO.name, out var i)) { fi.SetValue(comp, i); return true; }
            return false;
        }
        else if (fi.FieldType == typeof(float))
        {
            if (float.TryParse(refGO.name, NumberStyles.Float | NumberStyles.AllowThousands, CultureInfo.InvariantCulture, out var f)) { fi.SetValue(comp, f); return true; }
            return false;
        }
        else if (fi.FieldType == typeof(bool))
        {
            if (bool.TryParse(refGO.name, out var b)) { fi.SetValue(comp, b); return true; }
            if (refGO.name == "1") { fi.SetValue(comp, true); return true; }
            if (refGO.name == "0") { fi.SetValue(comp, false); return true; }
            return false;
        }
        else
            return false; // انواع عددی یا پیچیده را در صورت نیاز دستی اضافه کن

        if (val == null) return false;
        fi.SetValue(comp, val);
        return true;
    }

    static Type GetManagedFieldType(SerializedProperty sp)
    {
        // راه ساده برای رسیدن به نوع فیلد از طریق Reflection روی مسیر property
        var obj = sp.serializedObject.targetObject;
        var type = obj.GetType();
        var field = type.GetField(sp.propertyPath, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        return field?.FieldType ?? typeof(UnityEngine.Object);
    }

    static void LogWarn(string msg) => Debug.LogWarning(msg);

    // ========================
    // مدل داده برای هر ورودی
    // ========================
    class Entry
    {
        public string ObjectName;
        public string ScriptName;
        public Dictionary<string, string> FieldMap;
        public Entry(string objectName, string scriptName, Dictionary<string,string> fieldMap)
        {
            ObjectName = objectName; ScriptName = scriptName; FieldMap = fieldMap;
        }
    }
}
#endif
