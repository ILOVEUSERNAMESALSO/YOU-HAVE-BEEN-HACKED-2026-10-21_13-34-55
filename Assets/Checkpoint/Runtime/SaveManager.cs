using System;
using System.IO;
using UnityEngine;

namespace Checkpoint
{
    public static class SaveManager
    {
        public static string PathName => Path.Combine(Application.persistentDataPath, "checkpoint-shift-v1.json");
        public static string LastError { get; private set; }
        public static bool Write(ShiftState state)
        {
            try
            {
                Directory.CreateDirectory(Application.persistentDataPath);
                var temp = PathName + ".tmp";
                File.WriteAllText(temp, JsonUtility.ToJson(state, true));
                if (File.Exists(PathName)) File.Replace(temp, PathName, null);
                else File.Move(temp, PathName);
                LastError = null; return true;
            }
            catch (Exception ex) when (ex is IOException || ex is UnauthorizedAccessException)
            { LastError = "Autosave unavailable. This shift remains playable in memory."; Debug.LogWarning(LastError + " " + ex.Message); return false; }
        }
        public static ShiftState Read()
        {
            try { return File.Exists(PathName) ? JsonUtility.FromJson<ShiftState>(File.ReadAllText(PathName)) : null; }
            catch (Exception ex) when (ex is IOException || ex is UnauthorizedAccessException || ex is ArgumentException)
            { LastError = "The previous save could not be read. Start a new shift."; return null; }
        }
    }
}
