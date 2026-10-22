using System;
using System.Linq;

namespace Checkpoint
{
    // The state machine owns decisions; UI and perception never mutate canonical case content.
    public sealed class ShiftManager
    {
        public CaseBook Book { get; }
        public ShiftState State { get; private set; }
        public TravelerCase Current => Book.cases[Math.Min(State.caseIndex, Book.cases.Length - 1)];
        public event Action Changed;
        public ShiftManager(CaseBook book) { Book = book; State = new ShiftState(); }
        public void Restart() { State = new ShiftState(); Notify(); }
        public void StartShift()
        {
            if (State.phase != ShiftPhase.Briefing) return;
            State.phase = ShiftPhase.Processing; Notify();
        }
        public bool TryResume(ShiftState saved)
        {
            if (!ValidSave(saved)) return false;
            State = saved; Notify(); return true;
        }
        public bool ValidSave(ShiftState s)
        {
            if (s == null || s.version != 1 || !Enum.IsDefined(typeof(ShiftPhase), s.phase) || s.caseIndex < 0 || s.caseIndex >= Book.cases.Length || s.decisions == null || s.progress == null) return false;
            int expected = s.phase == ShiftPhase.Receipt || s.phase == ShiftPhase.Evaluation ? s.caseIndex + 1 : s.caseIndex;
            if (s.decisions.Count != expected || (s.phase == ShiftPhase.Evaluation && s.decisions.Count != Book.cases.Length) || (s.phase == ShiftPhase.Briefing && s.caseIndex != 0)) return false;
            var p = s.progress;
            if (p.evidence == null || p.discrepancies == null || p.questions == null || p.openedDocuments == null || p.inspectedFields == null) return false;
            var c = Book.cases[s.caseIndex];
            if (p.evidence.Distinct().Count() != p.evidence.Count || p.discrepancies.Distinct().Count() != p.discrepancies.Count) return false;
            if (p.evidence.Any(id => !c.evidence.Any(e => e.id == id)) || p.discrepancies.Any(id => !c.discrepancies.Any(d => d.id == id))) return false;
            if (p.openedDocuments.Any(id => !c.documents.Any(d => d.id == id)) || p.inspectedFields.Any(id => !c.documents.SelectMany(d => d.fields).Any(f => f.id == id)) || p.questions.Any(id => !c.dialogue.Any(q => q.id == id))) return false;
            if (p.restored != p.manipulationDetected || (p.restored && !PromptInjectionManager.CanRestore(c, p))) return false;
            for (int i = 0; i < s.decisions.Count; i++)
            {
                var r = s.decisions[i]; var authored = Book.cases[i];
                if (r == null || r.caseId != authored.id || !Enum.TryParse<Decision>(r.decision, out var choice) || !Enum.IsDefined(typeof(Decision), choice) || r.evidence == null || r.discrepancies == null) return false;
                if (!Book.rules.Any(rule => rule.id == r.citation) || r.evidence.Any(id => !authored.evidence.Any(e => e.id == id)) || r.discrepancies.Any(id => !authored.discrepancies.Any(d => d.id == id))) return false;
                if (r.evidence.Distinct().Count() != r.evidence.Count || r.discrepancies.Distinct().Count() != r.discrepancies.Count) return false;
            }
            return true;
        }
        public void OpenDocument(string id)
        {
            if (!Processing || !Current.documents.Any(d => d.id == id)) return;
            Add(State.progress.openedDocuments, id); Notify();
        }
        public string InspectField(string id)
        {
            if (!Processing) return "No active inspection.";
            var field = Current.documents.SelectMany(d => d.fields).FirstOrDefault(f => f.id == id);
            if (field == null) return "Unknown field.";
            Add(State.progress.inspectedFields, id);
            // Single-field issues can be found directly; paired discrepancies require comparison.
            var issues = Current.discrepancies.Where(d => d.fieldA == id && string.IsNullOrEmpty(d.fieldB)).ToArray();
            foreach (var d in issues) Add(State.progress.discrepancies, d.id);
            Notify();
            if (issues.Length > 0) return string.Join("\n", issues.Select(d => d.explanation + " [" + d.ruleId + "]"));
            if (DocumentValidator.IsExpired(field, Book.shiftDate)) return "Expired before today's shift. See rule R1.";
            if (field.kind == "expiry") return "Valid on " + Book.shiftDate + ". Expiration is inclusive. Compare identity and authorization separately.";
            return field.label + ": " + field.value + ". Select a second field to compare, or cross-check a source in the terminal.";
        }
        public string CompareFields(string a, string b)
        {
            if (!Processing || a == b) return "Select two different fields.";
            var d = DocumentValidator.Compare(Current, a, b);
            if (d == null) return "No documented discrepancy between these fields. This does not verify the whole case.";
            Add(State.progress.discrepancies, d.id); Notify();
            return d.title + ": " + d.explanation + " [" + d.ruleId + "]";
        }
        public bool CollectEvidence(string id)
        {
            if (!Processing || !Current.evidence.Any(e => e.id == id)) return false;
            bool added = Add(State.progress.evidence, id); if (added) Notify(); return added;
        }
        public string Ask(string id)
        {
            if (!Processing) return "";
            var d = Current.dialogue.FirstOrDefault(q => q.id == id);
            if (d == null || (!string.IsNullOrEmpty(d.requiresEvidence) && !State.progress.evidence.Contains(d.requiresEvidence))) return "Find the supporting evidence first.";
            Add(State.progress.questions, id);
            if (!string.IsNullOrEmpty(d.unlockEvidence)) CollectEvidence(d.unlockEvidence);
            Notify(); return d.answer;
        }
        public bool RestoreIntegrity()
        {
            if (!Processing || !PromptInjectionManager.Restore(Current, State.progress)) return false;
            if (Current.injection.kind == "kinship") State.identityRevealed = true;
            Notify(); return true;
        }
        public DecisionRecord Decide(Decision choice, string reason)
        {
            if (!Processing) throw new InvalidOperationException("A decision requires an active traveler.");
            var o = Current.outcomes.Single(x => x.decision == choice.ToString());
            bool qualified = string.IsNullOrEmpty(o.requiredEvidence) || State.progress.evidence.Contains(o.requiredEvidence);
            var record = new DecisionRecord
            {
                caseId = Current.id, traveler = Current.actualIdentity.name, decision = choice.ToString(),
                reason = string.IsNullOrWhiteSpace(reason) ? "No rationale supplied." : reason.Trim(),
                citation = o.citation, procedurallyCorrect = o.procedurallyCorrect && qualified,
                consequence = qualified ? o.consequence : o.missingEvidenceConsequence,
                observation = o.observation, moralReflection = o.moralReflection,
                evidence = State.progress.evidence.ToList(), discrepancies = State.progress.discrepancies.ToList(),
                manipulationDetected = State.progress.manipulationDetected, integrityRestored = State.progress.restored
            };
            State.decisions.Add(record); State.phase = ShiftPhase.Receipt; Notify(); return record;
        }
        public void Next()
        {
            if (State.phase != ShiftPhase.Receipt) return;
            if (Current.injection != null && Current.injection.kind == "kinship") State.identityRevealed = true;
            if (State.caseIndex + 1 >= Book.cases.Length) State.phase = ShiftPhase.Evaluation;
            else { State.caseIndex++; State.progress = new CaseProgress(); State.phase = ShiftPhase.Processing; }
            Notify();
        }
        public void RevealIdentity() { State.identityRevealed = true; Notify(); }
        bool Processing => State.phase == ShiftPhase.Processing;
        void Notify() => Changed?.Invoke();
        static bool Add(System.Collections.Generic.List<string> list, string value)
        { if (list.Contains(value)) return false; list.Add(value); return true; }
    }
}
