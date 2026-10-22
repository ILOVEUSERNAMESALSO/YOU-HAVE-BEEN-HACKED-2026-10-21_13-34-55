using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;

namespace Checkpoint
{
    public static class DocumentValidator
    {
        public static bool IsExpired(DocumentField field, string shiftDate)
        {
            return field.kind == "expiry" && Parse(field.value) < Parse(shiftDate);
        }
        static DateTime Parse(string value) => DateTime.ParseExact(value, "yyyy-MM-dd", CultureInfo.InvariantCulture);
        public static List<string> Inspect(TravelerCase c, string fieldId)
            => c.discrepancies.Where(d => d.fieldA == fieldId || d.fieldB == fieldId).Select(d => d.id).ToList();
        public static Discrepancy Compare(TravelerCase c, string first, string second)
            => c.discrepancies.FirstOrDefault(d => !string.IsNullOrEmpty(d.fieldB) &&
                ((d.fieldA == first && d.fieldB == second) || (d.fieldA == second && d.fieldB == first)));
    }
    public static class FakeBrowser
    {
        public static readonly string[] Apps = { "Public Search", "Government", "News Archive", "Social", "Travel" };
        static string Normalize(string text) => Regex.Replace((text ?? "").ToLowerInvariant(), @"[^\p{L}\p{Nd}]+", " ").Trim();
        public static List<WebRecord> Search(TravelerCase c, string app, string query)
        {
            var words = Normalize(query).Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            if (words.Length == 0) return new List<WebRecord>();
            return c.records.Where(r => app == "Public Search" || r.app == app)
                .Where(r => words.All(w => Normalize(r.title + " " + r.keywords + " " + r.snippet + " " + r.body).Contains(w)))
                .OrderByDescending(r => Normalize(r.title).Contains(Normalize(query))).ToList();
        }
    }
    public static class PerceptionManager
    {
        public static Identity DisplayIdentity(TravelerCase c, CaseProgress progress)
            => progress.restored ? c.actualIdentity : c.perceivedIdentity;
        public static bool IsCompromised(TravelerCase c, CaseProgress p)
            => c.injection != null && c.injection.kind == "kinship" && !p.restored;
    }
    public static class PromptInjectionManager
    {
        public static bool CanRestore(TravelerCase c, CaseProgress p)
            => c.injection != null && !string.IsNullOrEmpty(c.injection.kind) &&
               c.injection.requiredEvidence.All(p.evidence.Contains);
        public static bool Restore(TravelerCase c, CaseProgress p)
        {
            if (p.restored || !CanRestore(c, p)) return false;
            p.restored = true;
            p.manipulationDetected = true;
            return true;
        }
    }
    public sealed class EvaluationSummary
    {
        public int processed, correct, inconsistencies, evidence, manipulations;
        public int incorrect => processed - correct;
        public string observation;
    }
    public static class EvaluationManager
    {
        public static EvaluationSummary Evaluate(ShiftState state)
        {
            var s = new EvaluationSummary
            {
                processed = state.decisions.Count,
                correct = state.decisions.Count(d => d.procedurallyCorrect),
                inconsistencies = state.decisions.Sum(d => d.discrepancies.Count),
                evidence = state.decisions.Sum(d => d.evidence.Count),
                manipulations = state.decisions.Count(d => d.manipulationDetected)
            };
            s.observation = s.correct == s.processed
                ? "Procedural alignment is consistent. The institution has scheduled an additional interview about your reasons for compliance."
                : "Procedural deviations are queued for review. Your evidence trail and stated reasons will accompany the decisions; a deviation alone does not establish its wisdom.";
            return s;
        }
    }
}
