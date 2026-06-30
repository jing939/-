using UnityEngine;
using TMPro;
using System.Text;

public class ArchiveUI : MonoBehaviour
{
    public GameObject archivePanel;
    public TextMeshProUGUI archiveText;

    void Start()
    {
        if (archivePanel != null) archivePanel.SetActive(false);
    }

    public void ToggleArchive()
    {
        if (archivePanel == null) return;
        
        bool nextState = !archivePanel.activeSelf;
        archivePanel.SetActive(nextState);
        
        if (nextState) RefreshArchive();
    }

    public void RefreshArchive()
    {
        if (GameManager.instance == null || archiveText == null) return;

        StringBuilder sb = new StringBuilder();
        sb.AppendLine("<size=120%><b>[ ARCHIVE - RECORDS ]</b></size>\n");

        if (GameManager.instance.collectedLores.Count == 0)
        {
            sb.AppendLine("<i>No data recovered from the ruins yet.</i>");
        }
        else
        {
            foreach (var lore in GameManager.instance.collectedLores)
            {
                sb.AppendLine($"<color=#88ffff><b>■ {lore.title}</b></color> <size=80%>(Origin: {lore.originCorp} Corp)</size>");
                sb.AppendLine($"{lore.content}");
                sb.AppendLine("<color=#555555>----------------------------------</color>\n");
            }
        }
        archiveText.text = sb.ToString();
    }
}
