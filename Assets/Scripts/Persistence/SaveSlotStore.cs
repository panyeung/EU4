using System;
using System.IO;
using UnityEngine;

namespace SengokuChronicle.Persistence
{
    /// <summary>
    /// Reads and writes the single Phase 1 save slot as versioned JSON.
    /// Writes are atomic: a temporary file replaces the slot only after a successful write.
    /// </summary>
    public sealed class SaveSlotStore
    {
        public const string FileName = "save-slot-1.json";
        private const string DefaultFolderName = "SengokuChronicle";

        private readonly string _directory;

        public SaveSlotStore(string directory)
        {
            if (string.IsNullOrWhiteSpace(directory))
                throw new ArgumentException("Save directory must not be empty.", nameof(directory));
            _directory = directory;
        }

        /// <summary>Single user-writable slot under Application.persistentDataPath.</summary>
        public static SaveSlotStore CreateDefault()
        {
            return new SaveSlotStore(Path.Combine(Application.persistentDataPath, DefaultFolderName));
        }

        public string DirectoryPath => _directory;
        public string FilePath => Path.Combine(_directory, FileName);
        public bool Exists() => File.Exists(FilePath);

        public SaveOperationResult Save(SaveData data)
        {
            SaveOperationResult validation = SaveDataValidator.Validate(data);
            if (!validation.Succeeded)
                return validation;

            string tempPath = FilePath + ".tmp";
            try
            {
                Directory.CreateDirectory(_directory);
                string json = JsonUtility.ToJson(data, true);
                File.WriteAllText(tempPath, json);

                if (File.Exists(FilePath))
                    File.Replace(tempPath, FilePath, null);
                else
                    File.Move(tempPath, FilePath);

                return SaveOperationResult.Success();
            }
            catch (Exception exception)
            {
                TryDelete(tempPath);
                return SaveOperationResult.Failure(
                    SaveErrorCode.WriteFailed,
                    "无法写入存档：" + exception.Message);
            }
        }

        public SaveLoadResult Load()
        {
            if (!File.Exists(FilePath))
                return new SaveLoadResult(null, SaveErrorCode.NotFound, "尚未找到存档。");

            string json;
            try
            {
                json = File.ReadAllText(FilePath);
            }
            catch (Exception exception)
            {
                return new SaveLoadResult(null, SaveErrorCode.ReadFailed, "无法读取存档：" + exception.Message);
            }

            if (string.IsNullOrWhiteSpace(json))
                return new SaveLoadResult(null, SaveErrorCode.InvalidJson, "存档为空或已损坏。");

            SaveData data;
            try
            {
                data = JsonUtility.FromJson<SaveData>(json);
            }
            catch (Exception)
            {
                return new SaveLoadResult(null, SaveErrorCode.InvalidJson, "存档内容不是有效的 JSON。");
            }

            if (data == null)
                return new SaveLoadResult(null, SaveErrorCode.InvalidJson, "存档内容为空。");

            SaveOperationResult validation = SaveDataValidator.Validate(data);
            if (!validation.Succeeded)
                return new SaveLoadResult(null, validation.Error, validation.Message);

            return new SaveLoadResult(data, SaveErrorCode.None, string.Empty);
        }

        private static void TryDelete(string path)
        {
            try
            {
                if (File.Exists(path))
                    File.Delete(path);
            }
            catch (IOException)
            {
            }
            catch (UnauthorizedAccessException)
            {
            }
        }
    }
}
