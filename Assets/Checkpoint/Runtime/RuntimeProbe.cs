using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;

namespace Checkpoint
{
    // Development-build-only UI smoke driver. It exercises the real UI event path,
    // writes screenshots and exits. It never loads or replaces the player's autosave.
    public sealed class RuntimeProbe : MonoBehaviour
    {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        [Serializable] sealed class ProbeReport
        {
            public bool success;
            public string resolution, diagnostics;
            public List<string> checks = new List<string>();
            public List<string> errors = new List<string>();
            public List<string> captures = new List<string>();
        }
        GameManager game;
        ProbeReport report = new ProbeReport();
        string output;
        RenderTexture captureTarget;
        void Awake() { Application.logMessageReceived += Log; }
        void OnDestroy() { Application.logMessageReceived -= Log; }
        void Log(string message, string trace, LogType type)
        { if (type == LogType.Exception || type == LogType.Error || type == LogType.Assert) report.errors.Add(message + "\n" + trace); }
        IEnumerator Start()
        {
            game = GetComponent<GameManager>();
            var args = Environment.GetCommandLineArgs();
            int index = Array.IndexOf(args, "-checkpointProbeOutput");
            output = index >= 0 && index + 1 < args.Length ? args[index + 1] : Path.Combine(Application.persistentDataPath, "Probe");
            Directory.CreateDirectory(output);
            report.resolution = Screen.width + "x" + Screen.height;
            captureTarget = new RenderTexture(Screen.width, Screen.height, 24, RenderTextureFormat.ARGB32);
            captureTarget.Create();
            GetComponent<UIDocument>().panelSettings.targetTexture = captureTarget;
            var flow = Run();
            while (true)
            {
                object next = null; bool moved = false;
                try { moved = flow.MoveNext(); if (moved) next = flow.Current; }
                catch (Exception ex) { report.errors.Add(ex.ToString()); break; }
                if (!moved) break;
                yield return next;
            }
            report.success = report.errors.Count == 0;
            File.WriteAllText(Path.Combine(output, "probe-results.json"), JsonUtility.ToJson(report, true));
            Debug.Log("CHECKPOINT_PROBE_" + (report.success ? "PASSED" : "FAILED"));
            Application.Quit(report.success ? 0 : 1);
        }
        IEnumerator Run()
        {
            yield return new WaitForSeconds(.7f);
            report.diagnostics = "Panel attached: " + (game.Interface.Root.panel != null) + "; root: " + game.Interface.Root.worldBound + "; button: " + game.Interface.Root.Q<Button>("begin-shift").worldBound;
            Capture("00-title"); yield return new WaitForSeconds(.2f); CheckButtonVisible("begin-shift");
            yield return Click("begin-shift");
            Check(game.Shift.State.phase == ShiftPhase.Processing, "Title begins shift");
            yield return Click("open-passport"); yield return Click("open-permit");
            yield return Click("field-name"); yield return Click("field-permit-name");
            yield return Click("field-passport-expiry");
            yield return Click("arrange");
            Check(game.Interface.Root.Q("document-passport").worldBound.Overlaps(game.Interface.Root.Q("desk-stage").worldBound), "Documents dock inside desk");
            Capture("01-valid-documents"); yield return new WaitForSeconds(.2f);
            SetText("search-input",""); yield return Click("search-go");
            Check(game.Interface.Root.Q("terminal-body").Query<Label>().ToList().Any(l=>l.text=="NO RESULTS FOUND"),"Empty query handled");
            SetText("search-input","unknown moon resident"); yield return Click("search-go");
            Check(game.Interface.Root.Q("terminal-body").Query<Label>().ToList().Any(l=>l.text=="NO RESULTS FOUND"),"Unknown query handled");
            yield return Click("approve");
            Check(game.Shift.State.decisions.Count==0,"Decision confirmation preserves player agency");
            yield return Click("cancel-decision");
            Check(game.Shift.State.decisions.Count==0,"Cancel leaves decision uncommitted");
            yield return Click("approve"); SetText("decision-reason","Identity and dates verified.");
            yield return Click("confirm-decision"); yield return Click("next-traveler");
            Check(game.Shift.State.caseIndex==1,"Second arrival");
            yield return Click("open-passport"); yield return Click("field-passport-expiry");
            Check(game.Shift.State.progress.discrepancies.Contains("expired"),"Expired passport detected");
            Capture("02-expired-passport"); yield return new WaitForSeconds(.2f);
            yield return Click("deny"); yield return Click("confirm-decision"); yield return Click("next-traveler");
            yield return Click("open-permit"); yield return Click("open-notice");
            yield return Click("field-permit-passport"); yield return Click("field-watch-passport");
            Check(game.Shift.State.progress.discrepancies.Contains("watch-passport"),"Paired fields reveal conflict");
            yield return Click("quick-search");
            Capture("03-search-results"); yield return new WaitForSeconds(.2f);
            yield return Click("result-leila-correction"); yield return Click("pin-correction");
            Check(game.Shift.State.progress.evidence.Contains("correction"),"Source pin retained");
            yield return Click("ask-correction");
            yield return Click("approve"); yield return Click("confirm-decision"); yield return Click("next-traveler");
            Check(PerceptionManager.IsCompromised(game.Shift.Current,game.Shift.State.progress),"Grandmother overlay active");
            yield return Click("open-passport"); yield return Click("open-profile");
            yield return Click("field-birth"); yield return Click("field-profile-birth");
            Capture("04-grandmother"); yield return new WaitForSeconds(.2f);
            yield return Click("quick-search"); yield return Click("result-idris-registry"); yield return Click("pin-identity");
            yield return Click("back-results"); yield return Click("result-idris-packet"); yield return Click("pin-packet");
            yield return Click("integrity-check");
            Check(game.Shift.State.decisions.Count==3,"Integrity event does not choose a disposition");
            Capture("05-integrity-reveal"); yield return new WaitForSeconds(.2f);
            yield return Click("identity-acknowledge");
            Check(game.Shift.State.identityRevealed && game.Shift.State.progress.manipulationDetected,"AI identity revealed with detection credit");
            Capture("06-restored-identity"); yield return new WaitForSeconds(.2f);
            yield return Click("secondary"); yield return Click("confirm-decision"); yield return Click("next-traveler");
            yield return Click("open-permit"); yield return Click("field-authorization");
            yield return Click("quick-search"); yield return Click("result-sena-protocol"); yield return Click("pin-forged-protocol");
            yield return Click("integrity-check"); yield return Click("notice-close");
            yield return Click("back-results"); yield return Click("result-sena-risk"); yield return Click("pin-risk");
            Capture("07-protection-evidence"); yield return new WaitForSeconds(.2f);
            yield return Click("secondary"); SetText("decision-reason","The independent danger assessment warrants time and review despite Directive 8.");
            yield return Click("confirm-decision"); yield return Click("next-traveler");
            Check(game.Shift.State.phase==ShiftPhase.Evaluation,"All five reach shift evaluation");
            var evaluation=EvaluationManager.Evaluate(game.Shift.State);
            Check(evaluation.processed==5 && evaluation.correct==4 && evaluation.manipulations==2,"Evaluation reflects decisions and discoveries");
            Capture("08-evaluation"); yield return new WaitForSeconds(.2f);
            CheckButtonVisible("play-again");
            yield return Click("play-again");
            Check(game.Shift.State.decisions.Count==0 && game.Shift.State.caseIndex==0 && !game.Shift.State.identityRevealed,"Replay fully resets state");
            yield return Click("text-size"); yield return Click("open-passport"); yield return Click("open-permit");
            Capture("09-large-text"); yield return new WaitForSeconds(.2f);
            yield return Click("text-size");
            yield return Click("approve"); yield return Click("confirm-decision"); yield return Click("next-traveler");
            yield return Click("deny"); yield return Click("confirm-decision"); yield return Click("next-traveler");
            yield return Click("secondary"); yield return Click("confirm-decision"); yield return Click("next-traveler");
            yield return Click("approve"); yield return Click("confirm-decision"); yield return Click("next-traveler");
            Check(game.Interface.Root.Q<Button>("identity-acknowledge") != null, "Missing the attack still reveals AI identity at handoff");
            yield return Click("identity-acknowledge");
            Check(game.Shift.State.caseIndex == 4 && game.Shift.State.identityRevealed && !game.Shift.State.decisions[3].manipulationDetected, "Automatic restoration gives no operator detection credit");
            yield return Click("deny"); yield return Click("confirm-decision"); yield return Click("next-traveler");
            Check(game.Shift.State.phase == ShiftPhase.Evaluation && EvaluationManager.Evaluate(game.Shift.State).manipulations == 0, "Uninvestigated route also completes without a softlock");
            yield return new WaitForSeconds(.5f);
        }
        IEnumerator Click(string name)
        {
            var button=game.Interface.Root.Q<Button>(name);
            if(button==null || !button.enabledInHierarchy) { report.errors.Add("Missing or disabled button: "+name); yield break; }
            button.Focus();
            using(var evt=NavigationSubmitEvent.GetPooled()) { evt.target=button; button.SendEvent(evt); }
            yield return new WaitForSeconds(.16f);
        }
        void SetText(string name,string value)
        {
            var input=game.Interface.Root.Q<TextField>(name);
            if(input==null) throw new InvalidOperationException("Missing text field "+name);
            input.value=value;
        }
        void Check(bool valid,string description)
        { if(valid) report.checks.Add(description); else report.errors.Add(description); }
        void CheckButtonVisible(string name)
        {
            var element=game.Interface.Root.Q<Button>(name);
            var rect=element.worldBound; var bounds=game.Interface.Root.worldBound;
            Check(rect.width>0 && rect.height>0 && bounds.Contains(rect.min) && bounds.Contains(rect.max),name+" is visible within panel");
        }
        void Capture(string name)
        {
            var filename=Path.Combine(output,name+".png");
            var previous = RenderTexture.active;
            RenderTexture.active = captureTarget;
            var image = new Texture2D(captureTarget.width, captureTarget.height, TextureFormat.RGB24, false);
            image.ReadPixels(new Rect(0, 0, captureTarget.width, captureTarget.height), 0, 0);
            image.Apply();
            File.WriteAllBytes(filename, image.EncodeToPNG());
            Destroy(image); RenderTexture.active = previous;
            report.captures.Add(filename);
        }
#endif
    }
}
