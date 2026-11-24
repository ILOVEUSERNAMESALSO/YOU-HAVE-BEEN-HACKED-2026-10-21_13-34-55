using System;
using System.Linq;
using UnityEditor;
using UnityEngine;

public static class ComputerDatabaseChecks
{
    [MenuItem("Tools/Checkpoint/Validate Computer Database")]
    public static void Validate()
    {
        var data = AssetDatabase.LoadAssetAtPath<ComputerDatabase>("Assets/Computer/Staff Database.asset");
        Check(data != null, "Database missing");
        Check(data.entries.Count > 0, "Database has no articles");
        Check(data.Search("", "All", "16 NOV 2026").Count == data.entries.Count(e => e != null), "All articles");
        Check(data.Search("eXpIrY", "All", "16 NOV 2026").Count > 0, "Case insensitive search");
        Check(data.Search("", "Notices", "16 NOV 2026").Count == data.entries.Count(e => e != null && e.category == "Notices"), "Category filter");
        Check(data.Search("DOB", "Rulebook", "16 NOV 2026").Count > 0, "DOB search");
        Check(data.Search("no_such_record_123", "All", "16 NOV 2026").Count == 0, "Empty results");
        Check(data.Search("01 JAN 2030", "All", "01 JAN 2030").Count > 0, "Dynamic inspection date");
        Check(data.Search("seat", "Rulebook", "16 NOV 2026").Count == 0, "Search respects category");
        Debug.Log("Computer database checks passed: search, categories, no results and dynamic date.");
    }
    private static void Check(bool condition, string message)
    {
        if (!condition) throw new Exception("Computer database check failed: " + message);
    }
}
