using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

// Runs only when explicitly invoked in an isolated batch-mode project.
[InitializeOnLoad]
public static class ProjectHandoffChecks
{
    const string Running = "ProjectHandoff.Running";
    const string Started = "ProjectHandoff.LiveStarted";
    const string Result = "Library/PassengerChatResult.txt";

    static ProjectHandoffChecks()
    {
        if (Application.isBatchMode && SessionState.GetBool(Running, false))
            EditorApplication.update += Poll;
    }

    public static void RunBatch()
    {
        if (!Application.isBatchMode)
            throw new Exception("Use Tools/VerifyCurrent.ps1; this check opens scenes in an isolated project.");
        try
        {
            var scenes = EditorBuildSettings.scenes.Where(s => s.enabled).Select(s => s.path).ToArray();
            Require(scenes.SequenceEqual(new[] { "Assets/Scenes/Intro.unity", "Assets/Scenes/GameIntegration.unity" }),
                "Enabled build scenes must be Intro, then GameIntegration.");
            CharacterCardChecks.Validate();
            ComputerDatabaseChecks.Validate();
            var intro = EditorSceneManager.OpenScene(scenes[0], OpenSceneMode.Single);
            CheckMissingScripts(intro);
            var controller = UnityEngine.Object.FindAnyObjectByType<IntroController>();
            Require(controller != null && controller.introText != null && controller.buttonText != null &&
                controller.continueButton != null, "Intro controller references are missing.");
            var game = EditorSceneManager.OpenScene(scenes[1], OpenSceneMode.Single);
            CheckMissingScripts(game);
            PassengerChatSetup.Validate();
            var decision = UnityEngine.Object.FindAnyObjectByType<Decision>();
            Require(decision.arrivalAudioSource != null, "Passenger arrival AudioSource is not assigned.");
            var computer = UnityEngine.Object.FindAnyObjectByType<VirtualComputer>();
            Require(computer != null && computer.database != null && computer.checkpoint == decision,
                "Rulebook references are missing.");
            if (File.Exists(Result)) File.Delete(Result);
            SessionState.SetBool(Running, true);
            SessionState.SetBool(Started, false);
            EditorApplication.update -= Poll;
            EditorApplication.update += Poll;
            EditorApplication.isPlaying = true;
        }
        catch (Exception e) { Finish(false, e.ToString()); }
    }

    static void Poll()
    {
        if (!SessionState.GetBool(Running, false)) return;
        try
        {
            if (!EditorApplication.isPlaying || Time.frameCount < 2) return;
            if (!SessionState.GetBool(Started, false))
            {
                SessionState.SetBool(Started, true);
                PassengerChatChecks.RunLive();
            }
            if (File.Exists(Result))
            {
                string report = File.ReadAllText(Result);
                Finish(report.StartsWith("PASS:"), report);
            }
        }
        catch (Exception e) { Finish(false, e.ToString()); }
    }

    static void CheckMissingScripts(Scene scene)
    {
        foreach (var root in scene.GetRootGameObjects())
            foreach (var transform in root.GetComponentsInChildren<Transform>(true))
                Require(GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(transform.gameObject) == 0,
                    scene.name + ": missing script on " + transform.name);
    }

    static void Require(bool condition, string message)
    {
        if (!condition) throw new Exception(message);
    }

    static void Finish(bool success, string detail)
    {
        SessionState.SetBool(Running, false);
        EditorApplication.update -= Poll;
        string report = (success ? "PASS" : "FAIL") + ": current project handoff\n" + detail;
        File.WriteAllText("Library/HandoffResult.txt", report);
        Debug.Log(report);
        EditorApplication.Exit(success ? 0 : 1);
    }
}
