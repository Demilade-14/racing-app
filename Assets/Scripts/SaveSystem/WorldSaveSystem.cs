using UnityEngine;
using System.IO;
using System.Collections.Generic;
namespace RacingGame.SaveSystem
{
    /// <summary>
    /// Core save/load engine for the entire F1 world state.
    /// Handles file I/O, JSON serialization, and version compatibility.
    /// </summary>
    public static class WorldSaveSystem
    {
        private const string SAVE_FOLDER = "/CareerSaves/";
        private const string SAVE_EXTENSION = ".json";
        private const int MAX_AUTO_SAVES = 3;
        /// <summary>
        /// Get the full path to the save folder
        /// </summary>
        public static string GetSaveFolderPath()
        {
            string path = Application.persistentDataPath + SAVE_FOLDER;
            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
            }
            return path;
        }
        /// <summary>
        /// Save the world state to a specific slot
        /// </summary>
        public static bool SaveWorld(WorldSaveData data, string slotName, bool isAutoSave = false)
        {
            try
            {
                string folder = GetSaveFolderPath();
                string fileName = isAutoSave ? $"autosave_{slotName}{SAVE_EXTENSION}" : $"{slotName}{SAVE_EXTENSION}";
                string path = Path.Combine(folder, fileName);
                data.saveTimestamp = System.DateTime.Now;
                data.saveSlotName = slotName;
                string json = JsonUtility.ToJson(data, true);
                File.WriteAllText(path, json);
                Debug.Log($"💾 [SaveSystem] World saved to: {path}");
                // Manage auto-saves (keep only last 3)
                if (isAutoSave)
                {
                    CleanupAutoSaves(slotName);
                }
                return true;
            }
            catch (System.Exception e)
            {
                Debug.LogError($"❌ [SaveSystem] Save failed: {e.Message}");
                return false;
            }
        }
        /// <summary>
        /// Load the world state from a specific slot
        /// </summary>
        public static WorldSaveData LoadWorld(string slotName)
        {
            try
            {
                string folder = GetSaveFolderPath();
                string path = Path.Combine(folder, $"{slotName}{SAVE_EXTENSION}");
                if (!File.Exists(path))
                {
                    Debug.LogWarning($"⚠ [SaveSystem] No save file found: {path}");
                    return null;
                }
                string json = File.ReadAllText(path);
                WorldSaveData data = JsonUtility.FromJson<WorldSaveData>(json);
                if (!data.Validate())
                {
                    Debug.LogError("❌ [SaveSystem] Save data validation failed");
                    return null;
                }
                Debug.Log($"📦 [SaveSystem] World loaded: {data.saveSlotName} (Season {data.currentSeason})");
                return data;
            }
            catch (System.Exception e)
            {
                Debug.LogError($"❌ [SaveSystem] Load failed: {e.Message}");
                return null;
            }
        }
        /// <summary>
        /// Delete a save slot
        /// </summary>
        public static bool DeleteSave(string slotName)
        {
            try
            {
                string folder = GetSaveFolderPath();
                string path = Path.Combine(folder, $"{slotName}{SAVE_EXTENSION}");
                if (File.Exists(path))
                {
                    File.Delete(path);
                    Debug.Log($"🗑 [SaveSystem] Deleted save: {slotName}");
                    return true;
                }
                return false;
            }
            catch (System.Exception e)
            {
                Debug.LogError($"❌ [SaveSystem] Delete failed: {e.Message}");
                return false;
            }
        }
        /// <summary>
        /// Get all available save slots
        /// </summary>
        public static List<string> GetAvailableSaves()
        {
            var saves = new List<string>();
            try
            {
                string folder = GetSaveFolderPath();
                var files = Directory.GetFiles(folder, $"*{SAVE_EXTENSION}");
                foreach (var file in files)
                {
                    string fileName = Path.GetFileNameWithoutExtension(file);
                    if (!fileName.StartsWith("autosave_"))
                    {
                        saves.Add(fileName);
                    }
                }
            }
            catch (System.Exception e)
            {
                Debug.LogError($"❌ [SaveSystem] Failed to list saves: {e.Message}");
            }
            return saves;
        }
        /// <summary>
        /// Get save slot metadata without loading full data
        /// </summary>
        public static WorldSaveData GetSaveMetadata(string slotName)
        {
            try
            {
                string folder = GetSaveFolderPath();
                string path = Path.Combine(folder, $"{slotName}{SAVE_EXTENSION}");
                if (!File.Exists(path)) return null;
                string json = File.ReadAllText(path);
                return JsonUtility.FromJson<WorldSaveData>(json);
            }
            catch
            {
                return null;
            }
        }
        /// <summary>
        /// Cleanup old auto-saves (keep only last MAX_AUTO_SAVES)
        /// </summary>
        private static void CleanupAutoSaves(string slotName)
        {
            try
            {
                string folder = GetSaveFolderPath();
                var autoSaves = Directory.GetFiles(folder, $"autosave_{slotName}_*{SAVE_EXTENSION}");
                if (autoSaves.Length > MAX_AUTO_SAVES)
                {
                    // Sort by creation time and delete oldest
                    System.Array.Sort(autoSaves, (a, b) => File.GetCreationTime(a).CompareTo(File.GetCreationTime(b)));
                    int toDelete = autoSaves.Length - MAX_AUTO_SAVES;
                    for (int i = 0; i < toDelete; i++)
                    {
                        File.Delete(autoSaves[i]);
                    }
                }
            }
            catch (System.Exception e)
            {
                Debug.LogWarning($"⚠ [SaveSystem] Auto-save cleanup failed: {e.Message}");
            }
        }
        /// <summary>
        /// Create a backup of current save
        /// </summary>
        public static void BackupSave(string slotName)
        {
            try
            {
                string folder = GetSaveFolderPath();
                string originalPath = Path.Combine(folder, $"{slotName}{SAVE_EXTENSION}");
                string backupPath = Path.Combine(folder, $"backup_{slotName}_{System.DateTime.Now:yyyyMMdd_HHmmss}{SAVE_EXTENSION}");
                if (File.Exists(originalPath))
                {
                    File.Copy(originalPath, backupPath, true);
                    Debug.Log($"💾 [SaveSystem] Backup created: {backupPath}");
                }
            }
            catch (System.Exception e)
            {
                Debug.LogWarning($"⚠ [SaveSystem] Backup failed: {e.Message}");
            }
        }
    }
}
