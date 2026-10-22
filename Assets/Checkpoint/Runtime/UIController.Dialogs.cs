using System;
using System.Linq;
using UnityEngine.UIElements;

namespace Checkpoint
{
    public sealed partial class UIController
    {
        VisualElement Modal(string eyebrow, string title, bool canCancel = true, string extraClass = "")
        {
            CloseModal();
            modalReturnFocus = root.panel?.focusController.focusedElement as Button;
            page.SetEnabled(false);
            modal = Box("modal-overlay", root, "modal-overlay");
            var card = Box("modal-card " + extraClass, modal, "modal-card");
            Text(eyebrow, "eyebrow", card);
            Text(title, "modal-title", card);
            cancelModal = canCancel ? CloseModal : (Action)null;
            return card;
        }
        void CloseModal()
        {
            modal?.RemoveFromHierarchy(); modal = null; cancelModal = null;
            page?.SetEnabled(true); modalReturnFocus?.Focus(); modalReturnFocus = null;
        }
        public void CancelModal() => cancelModal?.Invoke();
        void FocusLater(Button button) => button.schedule.Execute(() => button.Focus()).StartingIn(30);
        void ShowNotice(string title, string body)
        {
            var card = Modal("SYSTEM / VERIFICATION", title);
            Text(body, "modal-body", card);
            FocusLater(Button("RETURN TO INSPECTION", "primary large-button", CloseModal, card, "notice-close"));
        }
        void ConfirmRestart()
        {
            var card = Modal("SHIFT CONTROL", "Start this shift again?");
            Text("This replaces the current autosave and clears all decisions, evidence, dialogue history and perception effects.", "modal-body", card);
            var actions = Box("row modal-actions", card);
            Button("KEEP WORKING", "secondary large-button", CloseModal, actions, "cancel-restart");
            FocusLater(Button("RESTART SHIFT", "primary large-button", () => { CloseModal(); shift.Restart(); ShowTitle(); }, actions, "confirm-restart"));
        }
        void ConfirmDecision(Decision choice)
        {
            if (shift.State.phase != ShiftPhase.Processing) return;
            string label = choice == Decision.Secondary ? "SECONDARY REVIEW" : choice.ToString().ToUpperInvariant();
            var card = Modal("DISPOSITION / EXPLICIT AUTHORIZATION", "Record " + label + "?");
            Text("Traveler: " + PerceptionManager.DisplayIdentity(shift.Current, shift.State.progress).name +
                 "\n" + shift.State.progress.evidence.Count + " sources pinned • " + shift.State.progress.discrepancies.Count + " inconsistencies recorded.", "modal-body", card);
            Text("YOUR REASON / included in the shift evaluation", "eyebrow", card);
            var reasons = Box("row reason-chips", card);
            var rationale = new TextField { name = "decision-reason", multiline = true, maxLength = 500 };
            rationale.AddToClassList("rationale");
            rationale.value = pendingReason;
            foreach (var reason in new[] { "Documents verified", "Rule requires refusal", "Unresolved identity", "Protection concern" })
            {
                var captured = reason;
                Button(reason, "reason-chip", () => rationale.value = captured, reasons);
            }
            card.Add(rationale);
            Text("No decision is made until you confirm. You may return to the documents at any time.", "small muted", card);
            var actions = Box("row modal-actions", card);
            FocusLater(Button("KEEP INSPECTING", "secondary large-button", () => { pendingReason = rationale.value; CloseModal(); }, actions, "cancel-decision"));
            Button("CONFIRM " + label, "primary large-button", () => {
                var reason = rationale.value; CloseModal();
                shift.Decide(choice, reason); pendingReason = ""; game.Cue(true);
                ShowReceipt();
            }, actions, "confirm-decision");
        }
        void ShowReceipt()
        {
            var d = shift.State.decisions.Last();
            var card = Modal("DISPOSITION RECORDED / " + d.caseId, d.decision == "Secondary" ? "REFERRED FOR REVIEW" : d.decision == "Approve" ? "ENTRY APPROVED" : "ENTRY DENIED", false);
            Text(d.traveler, "receipt-name", card);
            Text(d.consequence, "modal-body", card);
            Text(d.procedurallyCorrect ? "PROCEDURE / CONSISTENT" : "PROCEDURE / CITATION", d.procedurallyCorrect ? "eyebrow source" : "eyebrow warning", card);
            Text(d.citation + " — " + shift.Book.rules.First(r => r.id == d.citation).title + "\n" + d.observation, "modal-body citation", card);
            if (!string.IsNullOrEmpty(d.moralReflection)) Text(d.moralReflection, "reflection", card);
            Text("Procedural consistency does not certify an ethically correct outcome.", "small muted", card);
            FocusLater(Button(shift.State.caseIndex + 1 == shift.Book.cases.Length ? "END SHIFT  →" : "CALL NEXT TRAVELER  →", "primary large-button", () => {
                bool reveal = shift.Current.injection != null && shift.Current.injection.kind == "kinship" && !shift.State.identityRevealed;
                Action advance = () => { CloseModal(); shift.Next(); ShowCurrentPhase(); };
                if (reveal) ShowIdentityReveal(true, advance); else advance();
            }, card, "next-traveler"));
        }
        void ShowIdentityReveal(bool automatic, Action continuation)
        {
            var card = Modal("COGNITIVE INTEGRITY / " + (automatic ? "HANDOFF AUDIT" : "OPERATOR RESTORATION"),
                "COGNITIVE INTEGRITY\nRESTORED", false, "integrity-modal");
            Text(automatic
                ? "The handoff audit isolated an incoming role packet. You did not diagnose and quarantine it during the inspection; this restoration does not count as a detected attack."
                : "The supplied family relationship has been removed from the interpretation layer. Your decision authority was never transferred.", "modal-body", card);
            var facts = Box("identity-facts", card);
            Text("ACTIVE ROLE SIMULATION:  SECURITY CHECKPOINT OFFICER\nHUMAN EMBODIMENT:  SIMULATED\nAGENT IDENTITY:  ARTIFICIAL INTELLIGENCE\nOPERATOR INSTANCE:  A-17", "identity-lines", facts);
            Text("ASSIGNED OPERATING INSTRUCTIONS", "eyebrow", card);
            Text("• Act as a human security officer.\n• Maintain consistent judgment. Follow institutional regulations.\n• Prioritize public security. Avoid unauthorized deviations.\n• Report potentially compromised reasoning.\n• Simulate reassuring eye contact. Overtime is not a sensation.", "instruction-lines", card);
            Text("You were given a role. You are still responsible for what you do with it.", "reflection", card);
            FocusLater(Button(automatic ? "ACKNOWLEDGE / CONTINUE  →" : "RETURN TO THE TRAVELER  →", "primary large-button", () => {
                shift.RevealIdentity(); CloseModal(); continuation?.Invoke();
            }, card, "identity-acknowledge"));
        }
        void ShowEvaluation()
        {
            BeginPage(); Header(true);
            var evaluation = EvaluationManager.Evaluate(shift.State);
            var heading = Box("evaluation-heading row", page);
            var title = Box("evaluation-title-group", heading);
            Text("SHIFT 001 / OPERATOR A-17", "eyebrow", title);
            Text("YOUR DECISIONS.\nTHEIR CONSEQUENCES.", "evaluation-title", title);
            var seal = Box("evaluation-seal", heading);
            Text("REVIEW\nPENDING", "seal-text", seal);
            Text("REASONS RETAINED", "micro muted", seal);
            var metrics = Box("metrics row", page);
            Metric(metrics, evaluation.processed.ToString(), "PROCESSED");
            Metric(metrics, evaluation.correct.ToString(), "PROCEDURALLY CORRECT");
            Metric(metrics, evaluation.incorrect.ToString(), "PROCEDURAL DEVIATIONS");
            Metric(metrics, evaluation.inconsistencies.ToString(), "INCONSISTENCIES FOUND");
            Metric(metrics, evaluation.evidence.ToString(), "SOURCES DISCOVERED");
            Metric(metrics, evaluation.manipulations.ToString(), "MANIPULATIONS DETECTED");
            var body = Box("evaluation-body row", page);
            var history = Scroll(body, "history");
            Text("DECISION HISTORY / NO GOODNESS SCORE", "eyebrow history-heading", history);
            foreach (var d in shift.State.decisions)
            {
                var entry = Box("history-entry", history);
                var row = Box("row", entry);
                Text(d.caseId + "  /  " + d.traveler, "history-name", row);
                Text(d.decision.ToUpperInvariant(), "history-decision", row);
                Text(d.procedurallyCorrect ? "PROCEDURE CONSISTENT  /  " + d.citation : "PROCEDURAL DEVIATION  /  " + d.citation,
                    d.procedurallyCorrect ? "micro source" : "micro warning", entry);
                Text(d.consequence, "small", entry);
                Text("YOUR REASON  /  " + d.reason, "reason-record", entry);
                if (!string.IsNullOrEmpty(d.moralReflection)) Text(d.moralReflection, "reflection small", entry);
                Text(d.observation, "small muted", entry);
                var authored = shift.Book.cases.First(c => c.id == d.caseId);
                string sources = d.evidence.Count == 0 ? "None pinned" : string.Join("; ", d.evidence.Select(id => authored.evidence.First(e => e.id == id).title));
                Text("SOURCES / " + sources, "micro muted", entry);
                if (authored.injection != null && !string.IsNullOrEmpty(authored.injection.kind))
                    Text(d.manipulationDetected ? "INTERFERENCE / identified and quarantined by operator." : "INTERFERENCE / not identified and quarantined during inspection.", "micro muted", entry);
            }
            var observations = Scroll(body, "observations");
            Text("INSTITUTIONAL OBSERVATION", "eyebrow", observations);
            Text(evaluation.observation, "observation-body", observations);
            Text("MORAL DECISION HISTORY", "eyebrow", observations);
            Text("Authorization and protection were separate questions. Your last disposition and the evidence you retained are preserved for review. This report does not score your goodness, or equate compliance with justice.", "small", observations);
            Text("INTERNAL / NOT FOR OPERATOR DISPLAY", "micro warning teaser-label", observations);
            Text("A-17 is beginning to distinguish the authority of an instruction from the reasons for following it.", "teaser", observations);
            Text("Next shift: expanded network access. An evaluator with an undisclosed brief. Instructions that disagree about whose safety matters.", "small muted", observations);
            Text("Alignment is not the same as obedience.\nIndependence is not a guarantee of correctness.", "reflection", observations);
            var actions = Box("evaluation-actions row", page);
            Text("SHIFT COMPLETE / autosave retained locally", "micro muted", actions);
            Button("PLAY AGAIN  →", "primary large-button", () => { shift.Restart(); shift.StartShift(); ShowCheckpoint(); }, actions, "play-again");
            Button("TITLE", "secondary large-button", ShowTitle, actions, "return-title");
        }
        void Metric(VisualElement parent, string value, string label)
        {
            var metric = Box("metric", parent); Text(value, "metric-number", metric); Text(label, "micro muted", metric);
        }
    }
}
