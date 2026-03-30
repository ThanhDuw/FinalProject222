using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Attach to any NPC that offers one or more quests.
/// Quest turn-in flow: Active -> objectives done -> ReadyToTurnIn
/// -> Player returns to NPC + presses E -> reward summary shown
/// -> Player confirms -> CompleteQuest() -> item reward granted
/// </summary>
public class NPCQuestDialog : MonoBehaviour
{
    [Header("NPC Info")]
    [SerializeField] private string npcName = "NPC";

    [Header("Quests (in order)")]
    [SerializeField] private List<QuestData> questsToOffer = new List<QuestData>();

    [Header("Interaction")]
    [SerializeField] private float   interactionRadius = 2f;

    [Header("UI References - drag from NPC Dialogue Manager")]
    [SerializeField] private GameObject dialoguePanel;
    [SerializeField] private Text       npcNameText;
    [SerializeField] private Text       dialogueBodyText;
    [SerializeField] private Button     continueButton;

    [Header("Prerequisites")]
    [Tooltip("Quest IDs that must ALL be Completed before this NPC offers any quest.")]
    [SerializeField] private List<string> prerequisiteQuestIDs = new List<string>();
    [TextArea(2, 4)]
    [SerializeField] private string prerequisiteBlockedMessage =
        "You haven't proven yourself yet. Come back when you're ready.";

    [Header("Interact Prompt")]
    [Tooltip("Child world-space GameObject with 'E' label - blinks when player is in range.")]
    [SerializeField] private GameObject interactPrompt;

    private bool  isPlayerInRange;
    private bool  isDialogueOpen;
    private float _blinkTimer;

    private enum DialogueStep
    {
        OfferQuest,
        QuestAlreadyActive,
        QuestCompleted,
        Locked,
        TurnInQuest
    }

    private DialogueStep currentStep;
    private QuestData    currentQuestShown;

    private void Start()
    {
        ValidateSetup();
        if (continueButton != null)
            continueButton.onClick.AddListener(OnContinuePressed);
        if (dialoguePanel != null && dialoguePanel.activeSelf)
            dialoguePanel.SetActive(false);
        if (interactPrompt != null)
            interactPrompt.SetActive(false);
    }

    private void Update()
    {
        if (interactPrompt != null)
        {
            if (isPlayerInRange && !isDialogueOpen)
            {
                _blinkTimer += Time.deltaTime;
                if (_blinkTimer >= 0.45f) _blinkTimer = 0f;
                interactPrompt.SetActive(_blinkTimer < 0.30f);
            }
            else
            {
                interactPrompt.SetActive(false);
                _blinkTimer = 0f;
            }
        }
        if (!isPlayerInRange) return;
        if (GameInput.Instance != null && GameInput.Instance.InteractPressed)
        {
            if (!isDialogueOpen) TryOpenDialog();
            else                 OnContinuePressed();
        }
    }

    private void OnDestroy()
    {
        if (continueButton != null)
            continueButton.onClick.RemoveListener(OnContinuePressed);
    }

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

    private void TryOpenDialog()
    {
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
        var state = GetQuestState(q);
        if      (state == QuestState.Inactive)      ShowPanel(BuildOfferText(q),  DialogueStep.OfferQuest,         q);
        else if (state == QuestState.Active)        ShowPanel(BuildActiveText(q), DialogueStep.QuestAlreadyActive, q);
        else if (state == QuestState.ReadyToTurnIn) ShowPanel(BuildTurnInText(q), DialogueStep.TurnInQuest,         q);
        else ShowPanel("Thank you for everything! You've completed all my requests.",
                       DialogueStep.QuestCompleted, null);
    }

    private void OnContinuePressed()
    {
        if (!isDialogueOpen) return;
        if (currentStep == DialogueStep.OfferQuest && currentQuestShown != null)
        {
            if (QuestManager.Instance != null)
                QuestManager.Instance.StartQuest(currentQuestShown);
            else
                Debug.LogWarning("[NPCQuestDialog] QuestManager.Instance is null.");
        }
        else if (currentStep == DialogueStep.TurnInQuest && currentQuestShown != null)
        {
            if (QuestManager.Instance != null)
                QuestManager.Instance.CompleteQuest(currentQuestShown.questID);
            else
                Debug.LogWarning("[NPCQuestDialog] QuestManager.Instance is null.");
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

    private bool ArePrerequisitesMet()
    {
        if (prerequisiteQuestIDs == null || prerequisiteQuestIDs.Count == 0) return true;
        if (QuestManager.Instance == null) return false;
        foreach (var id in prerequisiteQuestIDs)
            if (QuestManager.Instance.GetQuestState(id) != QuestState.Completed) return false;
        return true;
    }

    private QuestData FindNextQuest()
    {
        foreach (var q in questsToOffer)
        {
            if (q == null) continue;
            var s = GetQuestState(q);
            if (s == QuestState.Inactive || s == QuestState.Active || s == QuestState.ReadyToTurnIn)
                return q;
        }
        return null;
    }

    private QuestState GetQuestState(QuestData q)
        => QuestManager.Instance != null
            ? QuestManager.Instance.GetQuestState(q.questID)
            : QuestState.Inactive;

    private string BuildOfferText(QuestData q)
    {
        string lines = "";
        if (q.objectives != null)
            foreach (var obj in q.objectives)
                lines += $"\n  >> {obj.description}";
        return $"[ {q.questName} ]\n{q.description}\n{lines}\n\n[E] Accept Quest  |  Leave area to close";
    }

    private string BuildActiveText(QuestData q)
    {
        string prog = "";
        var tracker = FindFirstObjectByType<QuestTracker>();
        if (tracker != null && q.objectives != null)
        {
            var p = tracker.GetProgress(q.questID);
            if (p != null)
                foreach (var obj in q.objectives)
                {
                    p.objectiveCounts.TryGetValue(obj.objectiveID, out int cur);
                    prog += $"\n  {obj.description.Split('(')[0].Trim()}: {cur}/{obj.requiredAmount}";
                }
        }
        return $"[ {q.questName} ]\nQuest in progress.{prog}\n\nFinish the quest before coming back!";
    }

    private string BuildTurnInText(QuestData q)
    {
        string r = "";
        if (q.itemReward != null)
        {
            string displayName = string.IsNullOrEmpty(q.itemReward.ItemName) ? q.itemReward.name : q.itemReward.ItemName;
            r += $"\n  Item:  {displayName}";
        }
        if (q.goldReward > 0)       r += $"\n  Gold:  {q.goldReward}";
        if (q.experienceReward > 0) r += $"\n  EXP:   {q.experienceReward}";
        string section = r.Length > 0 ? $"\n\nRewards:{r}" : "";
        return $"[ {q.questName} - Complete! ]\nAll objectives done. Ready to turn in?{section}\n\n[E] Turn In Quest  |  Leave area to close";
    }

    private void ValidateSetup()
    {
        var col = GetComponent<Collider>();
        if (col == null)
            Debug.LogWarning($"[NPCQuestDialog] '{name}': Missing Collider.");
        else if (!col.isTrigger)
            Debug.LogWarning($"[NPCQuestDialog] '{name}': Collider.isTrigger is false.");
        if (GameObject.FindWithTag("Player") == null)
            Debug.LogWarning($"[NPCQuestDialog] '{name}': No GameObject tagged 'Player' found.");
        if (dialoguePanel == null)
            Debug.LogWarning($"[NPCQuestDialog] '{name}': dialoguePanel not assigned.");
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(1f, 0.85f, 0f, 0.6f);
        Gizmos.DrawWireSphere(transform.position, interactionRadius);
    }
}
