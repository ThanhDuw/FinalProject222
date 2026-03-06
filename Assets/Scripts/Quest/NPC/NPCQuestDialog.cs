using UnityEngine;

/// <summary>
/// Attach to NPC GameObjects that offer quests.
/// Calls QuestManager.StartQuest() when the player accepts.
/// </summary>
public class NPCQuestDialog : MonoBehaviour
{
    [Header("Quest")]
    [SerializeField] private QuestData questToOffer;

    [Header("Dialog")]
    [SerializeField] private string    npcName;
    [SerializeField, TextArea] private string dialogueText;
    [SerializeField] private float     interactionRadius = 2f;

    private bool isPlayerInRange;
    private bool hasOfferedQuest;

    private void Start()
    {
        // Runtime validation to help designers: ensure NPC has a trigger collider
        var col = GetComponent<Collider>();
        if (col == null)
        {
            Debug.LogWarning($"NPC '{name}' is missing a Collider. Add a Collider and set 'isTrigger = true' so the dialog trigger works.");
        }
        else if (!col.isTrigger)
        {
            Debug.LogWarning($"NPC '{name}' Collider.isTrigger is false. Set isTrigger = true so OnTriggerEnter/Exit works for dialog.");
        }

        // Check if there's a Player tagged object in the scene (helps catch missing tag usage)
        var playerObj = GameObject.FindWithTag("Player");
        if (playerObj == null)
        {
            Debug.LogWarning($"No GameObject with tag 'Player' found in the scene. Ensure the player GameObject uses the 'Player' tag so NPC triggers detect it.");
        }
    }

    private void Update()
    {
        // Open dialog when player presses E while in range
        if (isPlayerInRange && Input.GetKeyDown(KeyCode.E))
        {
            OpenDialog();
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other == null) return;
        if (other.CompareTag("Player"))
        {
            isPlayerInRange = true;
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other == null) return;
        if (other.CompareTag("Player"))
        {
            isPlayerInRange = false;
            CloseDialog();
        }
    }

    /// <summary>Called when the player clicks Accept in the dialog UI.</summary>
    public void AcceptQuest()
    {
        if (questToOffer == null) return;
        QuestManager.Instance.StartQuest(questToOffer);
        hasOfferedQuest = true;
        CloseDialog();
    }

    public void OpenDialog()
    {
        // Minimal implementation: show debug log and set up state
        if (questToOffer == null)
        {
            Debug.Log($"{npcName}: No quest to offer.");
            return;
        }

        var state = QuestManager.Instance != null ? QuestManager.Instance.GetQuestState(questToOffer.questID) : QuestState.Inactive;

        // If already active or completed, we may show different dialog (here we only log)
        if (state == QuestState.Active)
        {
            Debug.Log($"{npcName}: You are already on quest '{questToOffer.questName}'.");
            return;
        }
        else if (state == QuestState.Completed)
        {
            Debug.Log($"{npcName}: You already completed '{questToOffer.questName}'.");
            return;
        }

        // Show dialog text (replace with real UI as needed)
        Debug.Log($"{npcName} says: {dialogueText}");

        // In a real UI you'd show Accept/Decline buttons. Here we mark that dialog is open and not yet offered.
        hasOfferedQuest = false;
    }

    public void CloseDialog()
    {
        // Close/hide dialog UI. Minimal: log and clear state
        Debug.Log($"{npcName}: Closing dialog.");
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, interactionRadius);
    }
}
