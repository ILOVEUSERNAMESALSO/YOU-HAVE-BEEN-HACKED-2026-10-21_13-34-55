using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Checkpoint/Computer Database", fileName = "Computer Database")]
public class ComputerDatabase : ScriptableObject
{
    [Serializable]
    public class Entry
    {
        public string title;
        public string category;
        public string keywords;
        [TextArea(5, 20)] public string content;
    }
    public List<Entry> entries = new List<Entry>();

    public List<Entry> Search(string query, string category, string inspectionDate)
    {
        var found = new List<Entry>();
        var words = (query ?? "").ToLowerInvariant().Split(new[] { ' ', '\t', '\n' }, StringSplitOptions.RemoveEmptyEntries);
        foreach (var entry in entries)
        {
            if (entry == null || (category != "All" && entry.category != category)) continue;
            string text = (entry.title + " " + entry.category + " " + entry.keywords + " " + entry.content)
                .Replace("{DATE}", inspectionDate ?? "").ToLowerInvariant();
            bool matches = true;
            foreach (var word in words) if (!text.Contains(word)) { matches = false; break; }
            if (matches) found.Add(entry);
        }
        return found;
    }
}
