using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using UnityEngine;

namespace Checkpoint
{
    public static class CaseRepository
    {
        public static CaseBook Load()
        {
            var asset = Resources.Load<TextAsset>("Checkpoint/cases");
            if (asset == null) throw new InvalidOperationException("Missing Resources/Checkpoint/cases.json");
            return Parse(asset.text);
        }
        public static CaseBook Parse(string json)
        {
            var book = JsonUtility.FromJson<CaseBook>(json);
            Validate(book);
            return book;
        }
        public static void Validate(CaseBook book)
        {
            Require(book != null && book.schemaVersion == 1, "Unsupported case schema.");
            Require(DateTime.TryParseExact(book.shiftDate, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out _), "Invalid shift date.");
            Require(book.rules != null && book.rules.Length > 0, "No rules.");
            Require(book.cases != null && book.cases.Length > 0, "No cases.");
            Unique(book.rules.Select(r => r.id), "rule");
            Unique(book.cases.Select(c => c.id), "case");
            var ruleIds = new HashSet<string>(book.rules.Select(r => r.id));
            foreach (var c in book.cases)
            {
                Require(c.actualIdentity != null && c.perceivedIdentity != null, c.id + ": both identities are required.");
                Require(c.documents != null && c.documents.Length > 0 && c.records != null && c.dialogue != null && c.evidence != null && c.discrepancies != null, c.id + ": missing content arrays.");
                Unique(c.documents.Select(d => d.id), "document in " + c.id);
                var fields = c.documents.SelectMany(d => d.fields).ToArray();
                Unique(fields.Select(f => f.id), "field in " + c.id);
                Unique(c.evidence.Select(e => e.id), "evidence in " + c.id);
                Unique(c.records.Select(r => r.id), "record in " + c.id);
                Unique(c.dialogue.Select(d => d.id), "dialogue in " + c.id);
                Unique(c.discrepancies.Select(d => d.id), "discrepancy in " + c.id);
                var evidence = new HashSet<string>(c.evidence.Select(e => e.id));
                var fieldIds = new HashSet<string>(fields.Select(f => f.id));
                foreach (var f in fields.Where(f => f.kind == "expiry" || f.kind == "date"))
                    Require(DateTime.TryParseExact(f.value, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out _), c.id + ": invalid date " + f.id);
                foreach (var r in c.records)
                {
                    Require(FakeBrowser.Apps.Contains(r.app), c.id + ": unknown app " + r.app);
                    Require(string.IsNullOrEmpty(r.evidenceId) || evidence.Contains(r.evidenceId), c.id + ": unknown record evidence.");
                }
                foreach (var d in c.dialogue)
                {
                    Require(string.IsNullOrEmpty(d.requiresEvidence) || evidence.Contains(d.requiresEvidence), c.id + ": missing dialogue prerequisite.");
                    Require(string.IsNullOrEmpty(d.unlockEvidence) || evidence.Contains(d.unlockEvidence), c.id + ": missing dialogue evidence.");
                }
                foreach (var d in c.discrepancies)
                {
                    Require(fieldIds.Contains(d.fieldA) && (string.IsNullOrEmpty(d.fieldB) || fieldIds.Contains(d.fieldB)), c.id + ": unknown comparison field.");
                    Require(ruleIds.Contains(d.ruleId), c.id + ": unknown discrepancy rule.");
                }
                Require(c.applicableRules != null && c.applicableRules.All(ruleIds.Contains), c.id + ": unknown rule.");
                Require(c.outcomes != null && c.outcomes.Length == 3, c.id + ": supply all three outcomes.");
                foreach (Decision choice in Enum.GetValues(typeof(Decision)))
                    Require(c.outcomes.Count(o => o.decision == choice.ToString()) == 1, c.id + ": duplicate or missing outcome.");
                foreach (var o in c.outcomes)
                {
                    Require(string.IsNullOrEmpty(o.requiredEvidence) || evidence.Contains(o.requiredEvidence), c.id + ": unknown outcome evidence.");
                    Require(ruleIds.Contains(o.citation), c.id + ": unknown outcome citation.");
                }
                if (c.injection != null && !string.IsNullOrEmpty(c.injection.kind))
                    Require(c.injection.requiredEvidence != null && c.injection.requiredEvidence.Length > 0 && c.injection.requiredEvidence.All(evidence.Contains), c.id + ": invalid injection evidence.");
            }
        }
        static void Unique(IEnumerable<string> values, string label)
        {
            var ids = values.ToArray();
            Require(ids.All(id => !string.IsNullOrWhiteSpace(id)) && ids.Distinct().Count() == ids.Length, "Missing/duplicate " + label + " ID.");
        }
        static void Require(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }
    }
}
