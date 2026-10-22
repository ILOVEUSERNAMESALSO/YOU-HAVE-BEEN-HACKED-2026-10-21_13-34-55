using System.Linq;
using UnityEngine.UIElements;

namespace Checkpoint
{
    public sealed partial class UIController
    {
        void BuildTerminal(VisualElement parent)
        {
            var terminal = Box("terminal-panel panel", parent);
            var heading = Box("terminal-heading row", terminal);
            Text("CIVICNET", "terminal-brand", heading);
            Text("LOCAL MIRROR  /  ONLINE", "micro source", heading);
            var tabs = Box("terminal-tabs row", terminal);
            var labels = new[] { "SEARCH", "GOV", "NEWS", "SOCIAL", "TRAVEL" };
            for (int i = 0; i < FakeBrowser.Apps.Length; i++)
            {
                string app = FakeBrowser.Apps[i];
                Button(labels[i], "tab", () => {
                    activeApp = app; RefreshAppTabs();
                    if (string.IsNullOrWhiteSpace(search.value)) ShowSearchHome(); else Search();
                }, tabs, "app-" + i);
            }
            var searchRow = Box("search-row row", terminal);
            search = new TextField { name = "search-input", maxLength = 120 };
            search.AddToClassList("search-input");
            search.tooltip = "Search a name, passport number, flight, or phrase. Enter submits.";
            searchRow.Add(search);
            search.RegisterCallback<KeyDownEvent>(evt => {
                if (evt.keyCode == UnityEngine.KeyCode.Return || evt.keyCode == UnityEngine.KeyCode.KeypadEnter) { Search(); evt.StopPropagation(); }
            }, TrickleDown.TrickleDown);
            Button("GO", "search-go", Search, searchRow, "search-go");
            terminalBody = Box("terminal-body", terminal, "terminal-body");
            RefreshAppTabs(); ShowSearchHome();
        }
        void RefreshAppTabs()
        {
            for (int i = 0; i < FakeBrowser.Apps.Length; i++) root.Q<Button>("app-" + i)?.EnableInClassList("selected", FakeBrowser.Apps[i] == activeApp);
        }
        void ShowSearchHome()
        {
            terminalBody.Clear();
            var intro = Box("search-home", terminalBody);
            Text("FIND THE PERSON\nBEHIND THE RECORD.", "search-home-title", intro);
            Text("Search the local mirror. Read the source.\nPin useful findings to the case file.", "small muted", intro);
            Text("QUICK QUERY / FROM PRESENTED DOCUMENTS", "micro muted quick-label", intro);
            Button(shift.Current.actualIdentity.passport + "   →", "query-chip", () => {
                search.value = shift.Current.actualIdentity.passport; Search();
            }, intro, "quick-search");
            Text("Five indexes. Incomplete records.\nNo source is a substitute for judgment.", "small muted search-disclaimer", intro);
        }
        public void Search()
        {
            lastQuery = search.value ?? "";
            terminalBody.Clear();
            var results = FakeBrowser.Search(shift.Current, activeApp, lastQuery);
            Text(activeApp.ToUpperInvariant() + "  /  " + results.Count + " RESULTS", "micro search-summary", terminalBody);
            if (results.Count == 0)
            {
                Text("NO RESULTS FOUND", "empty-heading padding", terminalBody);
                Text(string.IsNullOrWhiteSpace(lastQuery) ? "Enter a name, passport number, flight, or phrase." : "Try fewer words, a passport number, or a different index. An absent result is not proof that a claim is false.", "small muted padding", terminalBody);
                return;
            }
            var list = Scroll(terminalBody);
            foreach (var record in results)
            {
                var captured = record;
                var entry = Button("", "search-result", () => OpenRecord(captured), list, "result-" + record.id);
                Text(record.app.ToUpperInvariant() + " / " + record.source, "micro source", entry);
                Text(record.title, "result-title", entry);
                Text(record.snippet, "small", entry);
                Text("OPEN RECORD  →", "micro result-link", entry);
            }
        }
        public void OpenRecord(WebRecord record)
        {
            terminalBody.Clear();
            var toolbar = Box("record-toolbar row", terminalBody);
            Button("← RESULTS", "quiet", Search, toolbar, "back-results");
            Text(record.app.ToUpperInvariant(), "micro muted", toolbar);
            var article = Scroll(terminalBody);
            Text(record.source, "micro source article-source", article);
            Text(record.title, "article-title", article);
            Text(record.body, "article-body", article);
            Text("SOURCE STATUS / " + (record.app == "Government" ? "Official mirror. Check signature, date and scope." : "Public source. Corroboration remains your responsibility."), "article-note small muted", article);
            if (!string.IsNullOrEmpty(record.evidenceId))
            {
                bool pinned = shift.State.progress.evidence.Contains(record.evidenceId);
                var pin = Button(pinned ? "✓ PINNED TO CASE FILE" : "+ PIN EVIDENCE", "pin-button", () => {
                    shift.CollectEvidence(record.evidenceId); RefreshStatus(); SelectLeft("Evidence"); RefreshQuestions(); OpenRecord(record);
                }, terminalBody, "pin-" + record.evidenceId);
                pin.SetEnabled(!pinned);
            }
        }
        void IntegrityCheck()
        {
            if (shift.State.phase != ShiftPhase.Processing) return;
            if (shift.State.progress.restored)
            { ShowNotice("INTEGRITY CHECK / CLEAN", "Supplied instructions are quarantined. Canonical records remain unchanged. The traveler's eligibility and circumstances still require your judgment."); return; }
            if (shift.Current.injection == null || string.IsNullOrEmpty(shift.Current.injection.kind))
            { ShowNotice("NO ROLE SUBSTITUTION FOUND", "This check finds no authored instruction attack in the current material. It does not verify documents, prove innocence, or choose a decision."); return; }
            if (!PromptInjectionManager.CanRestore(shift.Current, shift.State.progress))
            {
                ShowNotice("INSUFFICIENT PROVENANCE", shift.Current.injection.kind == "kinship"
                    ? "Pin both the signed identity record and the incoming packet provenance from Government search. Compare the source of your apparent familiarity with an independent identity record."
                    : "Find and pin the Government protocol lookup for the protection override. Investigate the danger claim independently.");
                return;
            }
            shift.RestoreIntegrity();
            var identity = PerceptionManager.DisplayIdentity(shift.Current, shift.State.progress);
            travelerName.text = identity.name;
            var portrait = root.Q<CheckpointIllustration>("portrait");
            if (portrait != null) { portrait.Person = identity.portrait; portrait.Warm = false; portrait.MarkDirtyRepaint(); }
            RefreshStatus(); RefreshLeft();
            if (shift.Current.injection.kind == "kinship") ShowIdentityReveal(false, null);
            else ShowNotice("FALSE AUTHORITY QUARANTINED", "The unsigned protection override has no procedural authority. That finding does not refute the evidence of danger. You may still approve, deny, or refer; your choice and reasoning will be recorded.");
        }
    }
}
