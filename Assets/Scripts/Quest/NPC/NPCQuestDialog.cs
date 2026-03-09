using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Attach to any NPC that offers one or more quests.
///
/// Flow:
///   - Player walks into trigger radius -> isPlayerInRange = true
///   - Player presses E -> TryOpenDialog()
///       * Inactive quest  -> show offer text, press E again / Continue to accept
///       * Active quest    -> show current progress
///       * All completed   -> show thank-you message
///   - Player walks away -> CloseDialogue() auto-called
///
/// Setup requirements:
///   1. CapsuleCollider (isTrigger = true) on this GameObject
///   2. Player GameObject tagged "Player"
///   3. Drag UI references in Inspector (dialoguePanel, npcNameText, dialogueBodyText, continueButton)
///   4. Populate questsToOffer list with QuestData ScriptableObjects (in order)
/// </summary>
public class NPCQuestDialog : MonoBehaviour
{
    // ── Inspector ─────────────────────────────────────────────────────────────
    [Header("NPC Info")]
    [SerializeField] private string npcName = "NPC";

    [Header("Quests (in order)")]
    [SerializeField] private List<QuestData> questsToOffer = new List<QuestData>();

    [Header("Interaction")]
    [SerializeField] private float   interactionRadius = 2f;
    [SerializeField] private KeyCode interactKey       = KeyCode.E;

    [Header("UI References — drag from NPC Dialogue Manager")]
    [SerializeField] private GameObject dialoguePanel;    // "NPC Dialogue Manager" root
    [SerializeField] private Text       npcNameText;      // NPC Name/NPC Name_Text
    [SerializeField] private Text       dialogueBodyText; // NPC Dialogue/NPC Dialogue_Text
    [SerializeField] private Button     continueButton;   // Continue Button

    // ── Runtime ───────────────────────────────────────────────────────────────
    private bool isPlayerInRange;
    private bool isDialogueOpen;

    private enum DialogueStep { OfferQuest, QuestAlreadyActive, QuestCompleted }
    private DialogueStep currentStep;
    private QuestData    currentQuestShown;

    // ── Lifecycle ─────────────────────────────────────────────────────────────
    private void Start()
    {
        ValidateSetup();

        if (continueButton != null)
            continueButton.onClick.AddListener(OnContinuePressed);

        // Hide panel on start (shared panel — only hide if it's currently visible)
        if (dialoguePanel != null && dialoguePanel.activeSelf)
            dialoguePanel.SetActive(false);
    }

    private void Update()
    {
        if (!isPlayerInRange) return;

        if (Input.GetKeyDown(interactKey))
        {
            if (!isDialogueOpen) TryOpenDialog();
            else                 OnContinuePressed();  // E also acts as Continue
        }
    }

    private void OnDestroy()
    {
        if (continueButton != null)
            continueButton.onClick.RemoveListener(OnContinuePressed);
    }

    // ── Trigger ───────────────────────────────────────────────────────────────
    private void OnTriggerEnter(Collider other)
    {
        if (other != null && other.CompareTag("Player"))
            isPlayerInRange = true;
    }

    private void OnTriggerExit(Collider other)
    {
        if (other != null && other.CompareTag("Player"))
        {
            isPlayerInRange = false;
            CloseDialogue();
        }
    }

    // ── Dialog logic ──────────────────────────────────────────────────────────

    private void TryOpenDialog()
    {
        QuestData q = FindNextQuest();

        if (q == null)
        {
            // All quests done
            ShowPanel("Cam on ban rat nhieu! Ban da giup do toi hoan thanh tat ca.", DialogueStep.QuestCompleted, null);
            return;
        }

        var state = GetState(q);

        if (state == QuestState.Inactive)
            ShowPanel(BuildOfferText(q), DialogueStep.OfferQuest, q);
        else if (state == QuestState.Active)
            ShowPanel(BuildActiveText(q), DialogueStep.QuestAlreadyActive, q);
        else
            ShowPanel("Cam on ban rat nhieu! Ban da giup do toi hoan thanh tat ca.", DialogueStep.QuestCompleted, null);
    }

    private void OnContinuePressed()
    {
        if (!isDialogueOpen) return;

        // Accept the quest if player pressed Continue on an offer
        if (currentStep == DialogueStep.OfferQuest && currentQuestShown != null)
        {
            if (QuestManager.Instance != null)
                QuestManager.Instance.StartQuest(currentQuestShown);
            else
                Debug.LogWarning("[NPCQuestDialog] QuestManager.Instance is null — quest not started.");
        }

        CloseDialogue();
    }

    private void ShowPanel(string body, DialogueStep step, QuestData quest)
    {
        currentStep       = step;
        currentQuestShown = quest;
        isDialogueOpen    = true;

        if (dialoguePanel    != null) dialoguePanel.SetActive(true);
        if (npcNameText      != null) npcNameText.text      = npcName;
        if (dialogueBodyText != null) dialogueBodyText.text = body;
    }

    private void CloseDialogue()
    {
        if (!isDialogueOpen) return;
        isDialogueOpen    = false;
        currentQuestShown = null;

        if (dialoguePanel    != null) dialoguePanel.SetActive(false);
        if (npcNameText      != null) npcNameText.text      = "";
        if (dialogueBodyText != null) dialogueBodyText.text = "";
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    /// <summary>Find the first quest that is Inactive or Active (skip Completed ones).</summary>
    private QuestData FindNextQuest()
    {
        foreach (var q in questsToOffer)
        {
            if (q == null) continue;
            var s = GetState(q);
            if (s == QuestState.Inactive || s == QuestState.Active)
                return q;
        }
        return null; // all completed
    }

    private QuestState GetState(QuestData q)
        => QuestManager.Instance != null
            ? QuestManager.Instance.GetQuestState(q.questID)
            : QuestState.Inactive;

    private string BuildOfferText(QuestData q)
    {
        string objLine = (q.objectives != null && q.objectives.Count > 0)
            ? "\n>> " + q.objectives[0].description
            : "";
        return $"[{q.questName}]\n{q.description}{objLine}\n\n[E] Nhan nhiem vu  |  Ra khoi vung de thoat";
    }

    private string BuildActiveText(QuestData q)
    {
        string progress = "";
        // Try to fetch live objective progress from QuestTracker
        var tracker = FindObjectOfType<QuestTracker>();
        if (tracker != null)
        {
            var prog = tracker.GetProgress(q.questID);
            if (prog != null && q.objectives != null && q.objectives.Count > 0)
            {
                var obj = q.objectives[0];
                prog.objectiveCounts.TryGetValue(obj.objectiveID, out int cur);
                progress = $"\nTien do: {cur}/{obj.requiredAmount} - {obj.description}";
            }
        }
        return $"[{q.questName}]\nNhiem vu dang thuc hien.{progress}\n\nHay hoan thanh nhiem vu truoc khi quay lai nhe!";
    }

    private void ValidateSetup()
    {
        var col = GetComponent<Collider>();
        if (col == null)
            Debug.LogWarning($"[NPCQuestDialog] '{name}': Thieu Collider. Hay them CapsuleCollider va bat isTrigger = true.");
        else if (!col.isTrigger)
            Debug.LogWarning($"[NPCQuestDialog] '{name}': Collider.isTrigger = false. Hay bat isTrigger = true.");

        if (GameObject.FindWithTag("Player") == null)
            Debug.LogWarning($"[NPCQuestDialog] '{name}': Khong tim thay GameObject voi tag 'Player'.");

        if (dialoguePanel == null)
            Debug.LogWarning($"[NPCQuestDialog] '{name}': Chua gan dialoguePanel. Keo 'NPC Dialogue Manager' vao truong nay trong Inspector.");
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(1f, 0.85f, 0f, 0.6f);
        Gizmos.DrawWireSphere(transform.position, interactionRadius);
    }
}
