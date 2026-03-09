// TEMPORARY — self-deletes after running.
// Menu: Tools > Setup Interact Prompt + Cowboy Prerequisites
using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using System.Collections.Generic;

public static class SetupInteractPrompt
{
    // ── tuneable constants ────────────────────────────────────────────────────
    const string BLOCKED_MSG =
        "You haven't proven yourself yet.\nComplete Nolant's two quests first, then come back.";
    const string Q1_ID = "quest_01_nguyen_lieu_sa_mac";
    const string Q2_ID = "quest_02_noi_lo_vung_nghia_dia";

    // World-space canvas settings
    const float CANVAS_PIXELS_PER_UNIT = 100f;   // canvas reference px
    const float CANVAS_WORLD_SIZE      = 0.6f;   // metres wide in world
    const float PROMPT_HEIGHT_OFFSET   = 2.6f;   // metres above NPC pivot

    [MenuItem("Tools/Setup Interact Prompt + Cowboy Prerequisites")]
    public static void Run()
    {
        GameObject nolant = FindGO("Nolant");
        GameObject cowboy = FindGO("Cowboy");

        if (nolant == null) { Debug.LogError("[SetupPrompt] 'Nolant' not found!"); return; }
        if (cowboy == null) { Debug.LogError("[SetupPrompt] 'Cowboy' not found!"); return; }

        // ── 1. Create / refresh E-prompt on both NPCs ─────────────────────────
        GameObject nolantPrompt = BuildPrompt(nolant);
        GameObject cowboyPrompt = BuildPrompt(cowboy);

        // ── 2. Wire interactPrompt into NPCQuestDialog ────────────────────────
        AssignPrompt(nolant, nolantPrompt);
        AssignPrompt(cowboy, cowboyPrompt);

        // ── 3. Set Cowboy prerequisites ───────────────────────────────────────
        SetCowboyPrerequisites(cowboy);

        // ── 4. Save scene ─────────────────────────────────────────────────────
        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
        EditorSceneManager.SaveOpenScenes();
        Debug.Log("[SetupPrompt] Done! Scene saved.");

        // ── 5. Self-delete ────────────────────────────────────────────────────
        AssetDatabase.DeleteAsset("Assets/Editor/SetupInteractPrompt.cs");
    }

    // ── Build / rebuild the E-prompt child on an NPC ─────────────────────────
    static GameObject BuildPrompt(GameObject npc)
    {
        const string PROMPT_NAME = "InteractPrompt_E";

        // Remove old one if present so we start clean
        Transform existing = npc.transform.Find(PROMPT_NAME);
        if (existing != null)
        {
            Undo.DestroyObjectImmediate(existing.gameObject);
            Debug.Log($"[SetupPrompt] Removed old {PROMPT_NAME} from {npc.name}.");
        }

        // ── Canvas root ───────────────────────────────────────────────────────
        GameObject canvasGO = new GameObject(PROMPT_NAME);
        Undo.RegisterCreatedObjectUndo(canvasGO, "Create InteractPrompt");
        canvasGO.transform.SetParent(npc.transform, false);
        canvasGO.transform.localPosition = new Vector3(0f, PROMPT_HEIGHT_OFFSET, 0f);
        canvasGO.transform.localRotation = Quaternion.identity;
        canvasGO.layer = LayerMask.NameToLayer("UI");

        Canvas canvas = canvasGO.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;

        // Scale so the canvas world-unit size = CANVAS_WORLD_SIZE
        float s = CANVAS_WORLD_SIZE / CANVAS_PIXELS_PER_UNIT;
        canvasGO.transform.localScale = new Vector3(s, s, s);

        RectTransform rt = canvasGO.GetComponent<RectTransform>();
        rt.sizeDelta = new Vector2(CANVAS_PIXELS_PER_UNIT, CANVAS_PIXELS_PER_UNIT);

        canvasGO.AddComponent<CanvasScaler>(); // optional but keeps inspector clean

        // ── Text child ────────────────────────────────────────────────────────
        GameObject textGO = new GameObject("Label");
        Undo.RegisterCreatedObjectUndo(textGO, "Create InteractPrompt Label");
        textGO.transform.SetParent(canvasGO.transform, false);
        textGO.layer = LayerMask.NameToLayer("UI");

        RectTransform trt = textGO.AddComponent<RectTransform>();
        trt.anchorMin  = Vector2.zero;
        trt.anchorMax  = Vector2.one;
        trt.offsetMin  = Vector2.zero;
        trt.offsetMax  = Vector2.zero;

        Text txt = textGO.AddComponent<Text>();
        txt.text      = "E";
        txt.fontSize  = 80;
        txt.fontStyle = FontStyle.Bold;
        txt.color     = new Color(1f, 0.9f, 0.1f, 1f);   // golden-yellow
        txt.alignment = TextAnchor.MiddleCenter;

        // Use a built-in font that always exists
        Font arial = Resources.GetBuiltinResource<Font>("Arial.ttf");
        if (arial != null) txt.font = arial;

        // Outline for readability over any background
        Outline outline = textGO.AddComponent<Outline>();
        outline.effectColor    = new Color(0f, 0f, 0f, 0.85f);
        outline.effectDistance = new Vector2(3f, -3f);

        // Billboard: add NpcPromptBillboard so it always faces the camera
        canvasGO.AddComponent<NpcPromptBillboard>();

        // Starts hidden; NPCQuestDialog.Update controls visibility
        canvasGO.SetActive(false);

        EditorUtility.SetDirty(npc);
        Debug.Log($"[SetupPrompt] Created InteractPrompt_E on {npc.name}.");
        return canvasGO;
    }

    // ── Assign the prompt reference into NPCQuestDialog ───────────────────────
    static void AssignPrompt(GameObject npc, GameObject prompt)
    {
        NPCQuestDialog dialog = npc.GetComponent<NPCQuestDialog>();
        if (dialog == null) { Debug.LogError($"[SetupPrompt] NPCQuestDialog missing on {npc.name}!"); return; }

        var so = new SerializedObject(dialog);
        var prop = so.FindProperty("interactPrompt");
        if (prop == null) { Debug.LogError($"[SetupPrompt] 'interactPrompt' field not found on NPCQuestDialog!"); return; }

        prop.objectReferenceValue = prompt;
        so.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(dialog);
        Debug.Log($"[SetupPrompt] interactPrompt assigned on {npc.name}.");
    }

    // ── Wire Cowboy prerequisites ─────────────────────────────────────────────
    static void SetCowboyPrerequisites(GameObject cowboy)
    {
        NPCQuestDialog dialog = cowboy.GetComponent<NPCQuestDialog>();
        if (dialog == null) { Debug.LogError("[SetupPrompt] NPCQuestDialog missing on Cowboy!"); return; }

        var so = new SerializedObject(dialog);

        // prerequisiteQuestIDs
        var idsProp = so.FindProperty("prerequisiteQuestIDs");
        if (idsProp != null)
        {
            idsProp.ClearArray();
            idsProp.arraySize = 2;
            idsProp.GetArrayElementAtIndex(0).stringValue = Q1_ID;
            idsProp.GetArrayElementAtIndex(1).stringValue = Q2_ID;
        }
        else Debug.LogError("[SetupPrompt] 'prerequisiteQuestIDs' field not found!");

        // prerequisiteBlockedMessage
        var msgProp = so.FindProperty("prerequisiteBlockedMessage");
        if (msgProp != null)
            msgProp.stringValue = BLOCKED_MSG;
        else Debug.LogError("[SetupPrompt] 'prerequisiteBlockedMessage' field not found!");

        so.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(dialog);
        Debug.Log("[SetupPrompt] Cowboy prerequisites set.");
    }

    // ── Utility ───────────────────────────────────────────────────────────────
    static GameObject FindGO(string goName)
    {
        foreach (var go in Resources.FindObjectsOfTypeAll<GameObject>())
            if (go.scene.isLoaded && go.name == goName) return go;
        return null;
    }
}
