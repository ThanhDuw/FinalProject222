using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Attach to any NPC that offers one or more quests.
///
/// Flow:
///   - Player walks into trigger radius  -> isPlayerInRange = true, "E" prompt blinks
///   - Player presses E                  -> TryOpenDialog()
///       * Prerequisites not met         -> show locked message, close on Continue / E
///       * Inactive quest                -> show offer text, press E / Continue to accept
///       * Active quest                  -> show current progress per objective
///       * All completed                 -> show thank-you message
///   - Player walks away                 -> CloseDialogue() auto-called, prompt hidden
///
/// Setup requirements:
///   1. CapsuleCollider (isTrigger = true) on this GameObject
///   2. Player GameObject tagged "Player"
///   3. Drag UI refs in Inspector (dialoguePanel, npcNameText, dialogueBodyText, continueButton)
///   4. Populate questsToOffer list with QuestData ScriptableObjects (in order)
///   5. (Optional) Fill prerequisiteQuestIDs to gate the NPC behind earlier quests
///   6. (Optional) Assign interactPrompt - a child world-space "E" label GameObject
/// </summary>
public class NPCQuestDialog : MonoBehaviour
{
    // ── Inspector — NPC info ──────────────────────────────────────────────────
    [Header("NPC Info")]
    [SerializeField] private string npcName = "NPC";

    // ── Inspector — Quests ────────────────────────────────────────────────────
    [Header("Quests (in order)")]
    [SerializeField] private List<QuestData> questsToOffer = new List<QuestData>();

    // ── Inspector — Interaction ───────────────────────────────────────────────
    [Header("Interaction")]
    [SerializeField] private float   interactionRadius = 2f;
    [SerializeField] private KeyCode interactKey       = KeyCode.E;

    // ── Inspector — UI references ─────────────────────────────────────────────
    [Header("UI References - drag from NPC Dialogue Manager")]
    [SerializeField] private GameObject dialoguePanel;    // "NPC Dialogue Manager" root
    [SerializeField] private Text       npcNameText;      // NPC Name/NPC Name_Text
    [SerializeField] private Text       dialogueBodyText; // NPC Dialogue/NPC Dialogue_Text
    [SerializeField] private Button     continueButton;   // Continue Button

    // ── Inspector — Prerequisites ─────────────────────────────────────────────
    [Header("Prerequisites")]
    [Tooltip("Quest IDs that must ALL be Completed before this NPC offers any quest.")]
    [SerializeField] private List<string> prerequisiteQuestIDs = new List<string>();
    [TextArea(2, 4)]
    [SerializeField] private string prerequisiteBlockedMessage =
        "You haven't proven yourself yet. Come back when you're ready.";

    // ── Inspector — Interact prompt ───────────────────────────────────────────
    [Header("Interact Prompt")]
    [Tooltip("Child world-space GameObject with an 'E' label - shown and blinks when player is in range.")]
    [SerializeField] private GameObject interactPrompt;

    // ── Runtime ───────────────────────────────────────────────────────────────
    private bool  isPlayerInRange;
    private bool  isDialogueOpen;
    private float _blinkTimer;

    private enum DialogueStep { OfferQuest, QuestAlreadyActive, QuestCompleted, Locked }
    private DialogueStep currentStep;
    private QuestData    currentQuestShown;

    // ── Lifecycle ─────────────────────────────────────────────────────────────
    private void Start()
    {
        ValidateSetup();

        if (continueButton != null)
            continueButton.onClick.AddListener(OnContinuePressed);

        // Shared panel — hide on start
        if (dialoguePanel != null && dialoguePanel.activeSelf)
            dialoguePanel.SetActive(false);

        // Prompt starts hidden until player is near
        if (interactPrompt != null)
            interactPrompt.SetActive(false);
    }

    private void Update()
    {
        // ── Interact prompt blink ─────────────────────────────────────────────
        if (interactPrompt != null)
        {
            if (isPlayerInRange && !isDialogueOpen)
            {
                _blinkTimer += Time.deltaTime;
                if (_blinkTimer >= 0.45f) _blinkTimer = 0f;
                // ON 0.30s / OFF 0.15s — snappy, eye-catching blink
                interactPrompt.SetActive(_blinkTimer < 0.30f);
            }
            else
            {
                interactPrompt.SetActive(false);
                _blinkTimer = 0f;
            }
        }

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
        // Gate: all prerequisite quests must be Completed
        if (!ArePrerequisitesMet())
        {
            ShowPanel(prerequisiteBlockedMessage, DialogueStep.Locked, null);
            return;
        }

        QuestData q = FindNextQuest();

        if (q == null)
        {
            ShowPanel("Thank you for everything! You've completed all my requests.",
                      DialogueStep.QuestCompleted, null);
            return;
        }

        var state = GetState(q);

        if (state == QuestState.Inactive)
            ShowPanel(BuildOfferText(q), DialogueStep.OfferQuest, q);
        else if (state == QuestState.Active)
            ShowPanel(BuildActiveText(q), DialogueStep.QuestAlreadyActive, q);
        else
            ShowPanel("Thank you for everything! You've completed all my requests.",
                      DialogueStep.QuestCompleted, null);
    }

    private void OnContinuePressed()
    {
        if (!isDialogueOpen) return;

        // Only OfferQuest step starts a quest; Locked / Completed / Active just close
        if (currentStep == DialogueStep.OfferQuest && currentQuestShown != null)
        {
            if (QuestManager.Instance != null)
                QuestManager.Instance.StartQuest(currentQuestShown);
            else
                Debug.LogWarning("[NPCQuestDialog] QuestManager.Instance is null - quest not started.");
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

    /// <summary>Returns true when ALL prerequisiteQuestIDs are in Completed state.</summary>
private bool ArePrerequisitesMet()
    {
        if (prerequisiteQuestIDs == null || prerequisiteQuestIDs.Count == 0) return true;

        // Fail-closed: nếu QuestManager chưa sẵn sàng, coi như prerequisites chưa thoả
        if (QuestManager.Instance == null)
        {
            Debug.LogWarning($"[NPCQuestDialog] '{name}': QuestManager.Instance is null — treating prerequisites as NOT met.");
            return false;
        }

        foreach (var id in prerequisiteQuestIDs)
            if (QuestManager.Instance.GetQuestState(id) != QuestState.Completed) return false;

        return true;
    }

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
        // List ALL objectives so the player sees the full scope upfront
        string objLines = "";
        if (q.objectives != null)
            foreach (var obj in q.objectives)
                objLines += $"\n  >> {obj.description}";

        return $"[ {q.questName} ]\n{q.description}\n{objLines}\n\n[E] Accept Quest  |  Leave area to close";
    }

    private string BuildActiveText(QuestData q)
    {
        string progress = "";
        var tracker = FindFirstObjectByType<QuestTracker>();
        if (tracker != null && q.objectives != null)
        {
            var prog = tracker.GetProgress(q.questID);
            if (prog != null)
            {
                foreach (var obj in q.objectives)
                {
                    prog.objectiveCounts.TryGetValue(obj.objectiveID, out int cur);
                    progress += $"\n  {obj.description.Split('(')[0].Trim()}: {cur}/{obj.requiredAmount}";
                }
            }
        }
        return $"[ {q.questName} ]\nQuest in progress.{progress}\n\nFinish the quest before coming back!";
    }

    private void ValidateSetup()
    {
        var col = GetComponent<Collider>();
        if (col == null)
            Debug.LogWarning($"[NPCQuestDialog] '{name}': Missing Collider. Add CapsuleCollider with isTrigger = true.");
        else if (!col.isTrigger)
            Debug.LogWarning($"[NPCQuestDialog] '{name}': Collider.isTrigger is false. Set isTrigger = true.");

        if (GameObject.FindWithTag("Player") == null)
            Debug.LogWarning($"[NPCQuestDialog] '{name}': No GameObject with tag 'Player' found.");

        if (dialoguePanel == null)
            Debug.LogWarning($"[NPCQuestDialog] '{name}': dialoguePanel not assigned. Drag 'NPC Dialogue Manager' here.");
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(1f, 0.85f, 0f, 0.6f);
        Gizmos.DrawWireSphere(transform.position, interactionRadius);
    }
}
