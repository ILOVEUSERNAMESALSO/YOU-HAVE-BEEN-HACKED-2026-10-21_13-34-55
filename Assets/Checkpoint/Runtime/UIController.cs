using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;

namespace Checkpoint
{
    public sealed partial class UIController
    {
        readonly VisualElement root;
        readonly GameManager game;
        readonly ShiftManager shift;
        VisualElement page, modal, leftContent, deskStage, terminalBody, questionList;
        Label dialogue, evidenceCount, feedback, statusLabel, travelerName;
        TextField search;
        string activeApp = "Public Search", leftTab = "Rules", selectedField, lastQuery = "", currentReply;
        string pendingReason = "";
        bool largeText;
        Action cancelModal;
        Button modalReturnFocus;
        readonly Dictionary<string, VisualElement> documentWindows = new Dictionary<string, VisualElement>();
        readonly Dictionary<string, Button> fieldButtons = new Dictionary<string, Button>();
        readonly List<string> openOrder = new List<string>();
        public VisualElement Root => root;

        public UIController(VisualElement root, GameManager game)
        {
            this.root = root; this.game = game; shift = game.Shift;
            root.styleSheets.Add(Resources.Load<StyleSheet>("Checkpoint/Checkpoint"));
            root.AddToClassList("root");
            root.style.position = Position.Absolute;
            root.style.left = 0; root.style.right = 0; root.style.top = 0; root.style.bottom = 0;
        }
        VisualElement Box(string css, VisualElement parent = null, string name = null)
        {
            var v = new VisualElement { name = name };
            foreach (var cls in css.Split(' ')) if (cls.Length > 0) v.AddToClassList(cls);
            parent?.Add(v); return v;
        }
        Label Text(string value, string css, VisualElement parent = null)
        {
            var label = new Label(value) { enableRichText = false };
            foreach (var cls in css.Split(' ')) if (cls.Length > 0) label.AddToClassList(cls);
            parent?.Add(label); return label;
        }
        Button Button(string label, string css, Action action, VisualElement parent, string name = null)
        {
            var b = new Button(() => { game.Cue(); action(); }) { text = label, name = name };
            foreach (var cls in css.Split(' ')) if (cls.Length > 0) b.AddToClassList(cls);
            parent.Add(b); return b;
        }
        ScrollView Scroll(VisualElement parent, string css = "")
        {
            var scroll = new ScrollView(ScrollViewMode.Vertical);
            scroll.AddToClassList("scroll");
            if (css.Length > 0) scroll.AddToClassList(css);
            scroll.horizontalScrollerVisibility = ScrollerVisibility.Hidden;
            parent.Add(scroll); return scroll;
        }
        void BeginPage()
        {
            root.Clear(); modal = null; cancelModal = null;
            root.EnableInClassList("large-text", largeText);
            page = Box("page", root);
            page.name = "page";
        }
        void Header(bool active)
        {
            var header = Box("header row", page);
            var brand = Box("brand row", header);
            Text("M /", "brandmark", brand);
            var name = Box("brand-name", brand);
            Text("MERROW INTERNATIONAL", "brand-title", name);
            Text("ENTRY DIRECTORATE  /  TERMINAL 04", "micro muted", name);
            var progress = Box("shift-progress row", header);
            if (active)
            {
                for (int i = 0; i < shift.Book.cases.Length; i++)
                {
                    var chip = Text((i + 1).ToString("00"), "progress-chip", progress);
                    chip.EnableInClassList("current", i == shift.State.caseIndex);
                    chip.EnableInClassList("done", i < shift.State.decisions.Count);
                }
            }
            else Text("A LOCAL, FICTIONAL NETWORK", "micro muted", progress);
            var controls = Box("header-controls row", header);
            Text("06 NOV 2038", "small date", controls);
            Button(largeText ? "TEXT  +" : "TEXT", "quiet", () => {
                largeText = !largeText; root.EnableInClassList("large-text", largeText);
                var btn = root.Q<Button>("text-size"); if (btn != null) btn.text = largeText ? "TEXT  +" : "TEXT";
            }, controls, "text-size");
            Button(game.SoundEnabled ? "SOUND ON" : "SOUND OFF", "quiet", () => {
                game.SoundEnabled = !game.SoundEnabled;
                root.Q<Button>("sound-toggle").text = game.SoundEnabled ? "SOUND ON" : "SOUND OFF";
            }, controls, "sound-toggle");
            if (active) Button("RESTART", "quiet", ConfirmRestart, controls, "restart");
        }
        public void ShowTitle()
        {
            BeginPage(); Header(false);
            var hero = Box("hero row", page);
            var copy = Box("hero-copy", hero);
            Text("SHIFT 001  /  NARRATIVE INVESTIGATION", "eyebrow", copy);
            Text("NOT JUST\nACCEPT,\nBUT DENY.", "game-title", copy);
            Text("Every border begins\nwith an instruction.", "hero-deck", copy);
            Text("Five travelers. One checkpoint. A world of information that can change what you see — and who you think you are.", "hero-description", copy);
            var actions = Box("row hero-actions", copy);
            Button("BEGIN SHIFT   →", "primary large-button", () => { shift.Restart(); shift.StartShift(); ShowCheckpoint(); }, actions, "begin-shift");
            var saved = game.IsProbe ? null : SaveManager.Read();
            if (shift.ValidSave(saved) && saved.phase != ShiftPhase.Briefing)
                Button("CONTINUE", "secondary large-button", () => { shift.TryResume(saved); ShowCurrentPhase(); }, actions, "continue");
            Text("10–20 MIN  /  MOUSE + KEYBOARD  /  LOCAL AUTOSAVE", "micro muted", copy);
            var artColumn = Box("hero-art", hero);
            var art = new CheckpointIllustration("mira", false, true);
            art.AddToClassList("title-illustration"); artColumn.Add(art);
            var gate = Box("gate-label row", artColumn);
            Text("04", "gate-number", gate);
            var gateCopy = Box("gate-copy", gate);
            Text("ARRIVALS / ALL PASSPORTS", "section-title", gateCopy);
            Text("Your attention is part of the infrastructure.", "small muted", gateCopy);
            var instruction = Box("briefing-card", artColumn);
            Text("OPERATOR BRIEFING", "eyebrow", instruction);
            Text("01   Open and compare the documents.\n02   Check rules. Question the traveler.\n03   Search sources. Pin what matters.\n04   Make a decision you can explain.", "briefing-lines", instruction);
            Text("No countdown. No automatic decisions.\nContent: coercion, detention, and risk of forced return.", "small muted", instruction);
            Text("NOTICE  /  Confidence is not a substitute for verification.", "title-footer", page);
        }
        void ShowCurrentPhase()
        {
            if (shift.State.phase == ShiftPhase.Evaluation) ShowEvaluation();
            else if (shift.State.phase == ShiftPhase.Briefing) ShowTitle();
            else { ShowCheckpoint(); if (shift.State.phase == ShiftPhase.Receipt) ShowReceipt(); }
        }
        public void ShowCheckpoint()
        {
            BeginPage(); Header(true);
            documentWindows.Clear(); fieldButtons.Clear(); openOrder.Clear();
            selectedField = null; pendingReason = ""; lastQuery = ""; currentReply = shift.Current.greeting; activeApp = "Public Search";
            var top = Box("encounter row", page);
            var portraitArea = Box("portrait-area", top);
            var identity = PerceptionManager.DisplayIdentity(shift.Current, shift.State.progress);
            var portrait = new CheckpointIllustration(identity.portrait, PerceptionManager.IsCompromised(shift.Current, shift.State.progress));
            portrait.name = "portrait"; portrait.AddToClassList("portrait"); portraitArea.Add(portrait);
            Text("LIVE / BOOTH 04", "portrait-caption", portraitArea);
            var conversation = Box("conversation", top);
            var heading = Box("row encounter-heading", conversation);
            Text("ARRIVAL " + (shift.State.caseIndex + 1).ToString("00"), "eyebrow", heading);
            Text(shift.Current.arrival, "micro muted flight-label", heading);
            travelerName = Text(identity.name, "traveler-name", conversation);
            dialogue = Text(currentReply, "dialogue", conversation);
            var questions = Box("questions", top);
            Text("ASK THE TRAVELER", "eyebrow", questions);
            questionList = Scroll(questions);
            RefreshQuestions();
            var middle = Box("workbench row", page);
            var left = Box("left-panel panel", middle);
            var leftTabs = Box("tabs row", left);
            foreach (var tab in new[] { "Rules", "Evidence", "Log" })
            {
                string captured = tab;
                Button(tab.ToUpperInvariant(), "tab", () => SelectLeft(captured), leftTabs, "left-" + tab);
            }
            leftContent = Scroll(left);
            var desk = Box("desk-panel panel", middle);
            var deskHeader = Box("desk-heading row", desk);
            Text("INSPECTION DESK", "section-title", deskHeader);
            Button("ARRANGE", "quiet", ArrangeDocuments, deskHeader, "arrange");
            var tray = Box("document-tray row", desk);
            foreach (var document in shift.Current.documents)
            {
                var captured = document;
                Button(document.type, "tray-button", () => OpenDocument(captured.id), tray, "open-" + document.id);
            }
            deskStage = Box("desk-stage", desk, "desk-stage");
            var empty = Box("desk-empty", deskStage, "empty-desk");
            Text("THE PAPERS ARE YOUR STARTING POINT.", "empty-heading", empty);
            Text("Open a document above.\nClick a field to inspect. Click another to compare.\nDrag a header to move a document, or use ARRANGE.", "small muted centered", empty);
            deskStage.RegisterCallback<GeometryChangedEvent>(_ => ArrangeDocuments());
            feedback = Text("Select a document to begin inspection.", "inspection-feedback", desk);
            BuildTerminal(middle);
            var footer = Box("decision-bar row", page);
            var shiftInfo = Box("shift-status", footer);
            evidenceCount = Text("", "small", shiftInfo);
            statusLabel = Text("", "micro muted", shiftInfo);
            var decisions = Box("decision-actions row", footer);
            Button("APPROVE  ↗", "decision approve", () => ConfirmDecision(Decision.Approve), decisions, "approve");
            Button("DENY  ×", "decision deny", () => ConfirmDecision(Decision.Deny), decisions, "deny");
            Button("SECONDARY REVIEW  →", "decision review", () => ConfirmDecision(Decision.Secondary), decisions, "secondary");
            var tutorial = Box("tutorial-bar row", page);
            Text(shift.Current.tutorial, "tutorial-text", tutorial);
            Button("INTEGRITY CHECK", "integrity-button", IntegrityCheck, tutorial, "integrity-check");
            SelectLeft(leftTab); RefreshStatus();
            foreach (var opened in shift.State.progress.openedDocuments.TakeLast(2).ToArray()) OpenDocument(opened);
            if (shift.State.phase != ShiftPhase.Processing) decisions.SetEnabled(false);
        }
        void RefreshQuestions()
        {
            questionList.Clear();
            foreach (var q in shift.Current.dialogue)
            {
                if (!string.IsNullOrEmpty(q.requiresEvidence) && !shift.State.progress.evidence.Contains(q.requiresEvidence)) continue;
                var captured = q;
                var btn = Button((shift.State.progress.questions.Contains(q.id) ? "↳ " : "") + q.question, "question-button",
                    () => { currentReply = shift.Ask(captured.id); dialogue.text = currentReply; RefreshQuestions(); RefreshStatus(); RefreshLeft(); },
                    questionList, "ask-" + q.id);
                btn.tooltip = "Ask this question. Dialogue has no timer.";
            }
        }
        void RefreshStatus()
        {
            if (evidenceCount == null) return;
            var p = shift.State.progress;
            evidenceCount.text = p.evidence.Count + " SOURCES PINNED  /  " + p.discrepancies.Count + " INCONSISTENCIES";
            statusLabel.text = SaveManager.LastError ?? ("SHIFT 001  •  " + shift.State.decisions.Count + "/" + shift.Book.cases.Length + " PROCESSED  •  AUTOSAVED");
        }
        public void OpenRules() { if (leftContent != null && modal == null) SelectLeft("Rules"); }
        void SelectLeft(string tab)
        {
            leftTab = tab;
            foreach (var option in new[] { "Rules", "Evidence", "Log" })
                root.Q<Button>("left-" + option)?.EnableInClassList("selected", option == tab);
            RefreshLeft();
        }
        void RefreshLeft()
        {
            if (leftContent == null) return;
            leftContent.Clear();
            if (leftTab == "Rules")
            {
                Text("SHIFT DATE / 06 NOV 2038", "eyebrow left-heading", leftContent);
                foreach (var rule in shift.Book.rules)
                {
                    var card = Box("rule-card", leftContent);
                    card.EnableInClassList("applicable", shift.Current.applicableRules.Contains(rule.id));
                    Text(rule.id + "  /  " + rule.title.ToUpperInvariant(), "rule-title", card);
                    Text(rule.text, "rule-body", card);
                }
            }
            else if (leftTab == "Evidence")
            {
                Text("YOUR INVESTIGATION", "eyebrow left-heading", leftContent);
                if (shift.State.progress.evidence.Count == 0 && shift.State.progress.discrepancies.Count == 0)
                    Text("Inspect document fields and pin sources from the terminal. Evidence stays attached to this case.", "small muted padding", leftContent);
                foreach (var id in shift.State.progress.discrepancies)
                {
                    var d = shift.Current.discrepancies.First(x => x.id == id);
                    var card = Box("evidence-card discrepancy", leftContent);
                    Text("!  " + d.title, "rule-title", card); Text(d.explanation, "small", card);
                    Text("RULE " + d.ruleId, "micro muted", card);
                }
                foreach (var id in shift.State.progress.evidence)
                {
                    var e = shift.Current.evidence.First(x => x.id == id);
                    var card = Box("evidence-card", leftContent);
                    Text(e.title, "rule-title", card); Text(e.detail, "small", card);
                    Text(e.source, "micro source", card);
                }
            }
            else
            {
                Text("EVENT LOG / CURRENT CASE", "eyebrow left-heading", leftContent);
                Text(shift.Current.arrival, "small muted padding", leftContent);
                foreach (var id in shift.State.progress.questions)
                {
                    var q = shift.Current.dialogue.First(x => x.id == id);
                    var card = Box("evidence-card", leftContent);
                    Text(q.question, "rule-title", card); Text(q.answer, "small", card);
                }
                if (shift.State.progress.restored) Text("Integrity restored. Supplied authority quarantined. The decision remains yours.", "small source padding", leftContent);
                foreach (var d in shift.State.decisions)
                    Text(d.caseId + " / " + d.decision.ToUpperInvariant() + "\n" + d.observation, "small muted padding", leftContent);
            }
        }
    }
}
