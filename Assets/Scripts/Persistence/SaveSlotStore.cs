using System;
using System.Globalization;
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
        private const string BackupFileName = "save-slot-1.json.bak";

        private readonly string _directory;
        private readonly Func<SaveData, string> _serialize;

        public SaveSlotStore(string directory)
            : this(directory, data => JsonUtility.ToJson(data, true))
        {
        }

        /// <summary>Serializer injection seam for verifying temp-file validation and recovery behavior.</summary>
        public SaveSlotStore(string directory, Func<SaveData, string> serializer)
        {
            if (string.IsNullOrWhiteSpace(directory))
                throw new ArgumentException("Save directory must not be empty.", nameof(directory));
            if (serializer == null)
                throw new ArgumentNullException(nameof(serializer));
            _directory = directory;
            _serialize = serializer;
        }

        /// <summary>Single user-writable slot under Application.persistentDataPath.</summary>
        public static SaveSlotStore CreateDefault()
        {
            return new SaveSlotStore(Path.Combine(Application.persistentDataPath, DefaultFolderName));
        }

        public string DirectoryPath => _directory;
        public string FilePath => Path.Combine(_directory, FileName);
        public string BackupFilePath => Path.Combine(_directory, BackupFileName);

        /// <summary>
        /// Set after a save replaced an unreadable active slot. The original bytes
        /// remain at this path; empty when no recovery copy was made.
        /// </summary>
        public string LastRecoveryFilePath { get; private set; } = string.Empty;

        public bool Exists() => File.Exists(FilePath);

        public SaveOperationResult Save(SaveData data)
        {
            LastRecoveryFilePath = string.Empty;
            SaveOperationResult validation = SaveDataValidator.Validate(data);
            if (!validation.Succeeded)
                return validation;

            string tempPath = FilePath + ".tmp";
            string backupTempPath = BackupFilePath + ".tmp";
            try
            {
                Directory.CreateDirectory(_directory);
                string json = _serialize(data);
                if (string.IsNullOrWhiteSpace(json))
                    return SaveOperationResult.Failure(SaveErrorCode.InvalidJson, "无法序列化存档内容。");
                File.WriteAllText(tempPath, json);

                SaveData tempData;
                SaveDataPresenceProbe tempProbe;
                string tempJson;
                try
                {
                    tempJson = File.ReadAllText(tempPath);
                    if (string.IsNullOrWhiteSpace(tempJson))
                        return SaveOperationResult.Failure(SaveErrorCode.InvalidJson, "临时存档内容为空或已损坏。");
                    tempProbe = JsonUtility.FromJson<SaveDataPresenceProbe>(tempJson);
                    tempData = JsonUtility.FromJson<SaveData>(tempJson);
                }
                catch (Exception exception)
                {
                    return SaveOperationResult.Failure(SaveErrorCode.InvalidJson, "临时存档内容无法解析：" + exception.Message);
                }

                if (tempData == null || tempProbe == null)
                    return SaveOperationResult.Failure(SaveErrorCode.InvalidJson, "临时存档内容为空或已损坏。");
                SaveOperationResult tempPresence = SaveDataValidator.ValidatePresence(tempProbe, tempJson);
                if (!tempPresence.Succeeded)
                    return tempPresence;
                SaveOperationResult tempValidation = SaveDataValidator.Validate(tempData);
                if (!tempValidation.Succeeded)
                    return tempValidation;

                if (File.Exists(FilePath))
                {
                    SaveLoadResult previous = Load();
                    if (previous.Succeeded)
                    {
                        // Build the recovery file fully before replacing the active save.
                        File.Copy(FilePath, backupTempPath, true);
                        File.Copy(backupTempPath, BackupFilePath, true);
                        TryDelete(backupTempPath);
                    }
                    else
                    {
                        // A corrupt active slot must not block saving forever, and its
                        // original bytes must survive the replacement for manual recovery.
                        string recoveryPath = CreateRecoveryPath();
                        try
                        {
                            File.Copy(FilePath, recoveryPath, false);
                        }
                        catch (Exception exception)
                        {
                            return SaveOperationResult.Failure(
                                SaveErrorCode.WriteFailed,
                                "当前存档无效，且无法保留恢复副本：" + exception.Message);
                        }

                        LastRecoveryFilePath = recoveryPath;
                    }

                    File.Replace(tempPath, FilePath, null);
                }
                else
                {
                    File.Move(tempPath, FilePath);
                }

                return SaveOperationResult.Success();
            }
            catch (Exception exception)
            {
                return SaveOperationResult.Failure(
                    SaveErrorCode.WriteFailed,
                    "无法写入存档：" + exception.Message);
            }
            finally
            {
                TryDelete(tempPath);
                TryDelete(backupTempPath);
            }
        }

        public SaveLoadResult Load()
        {
            return LoadPath(FilePath, SaveErrorCode.NotFound, "尚未找到存档。");
        }

        /// <summary>Loads the preserved previous valid slot without changing either file.</summary>
        public SaveLoadResult LoadBackup()
        {
            return LoadPath(BackupFilePath, SaveErrorCode.NotFound, "尚未找到恢复备份。");
        }

        private static SaveLoadResult LoadPath(string path, SaveErrorCode missingError, string missingMessage)
        {
            if (!File.Exists(path))
                return new SaveLoadResult(null, missingError, missingMessage);

            string json;
            try
            {
                json = File.ReadAllText(path);
            }
            catch (Exception exception)
            {
                return new SaveLoadResult(null, SaveErrorCode.ReadFailed, "无法读取存档：" + exception.Message);
            }

            if (string.IsNullOrWhiteSpace(json))
                return new SaveLoadResult(null, SaveErrorCode.InvalidJson, "存档为空或已损坏。");

            SaveData data;
            SaveDataPresenceProbe probe;
            try
            {
                probe = JsonUtility.FromJson<SaveDataPresenceProbe>(json);
                data = JsonUtility.FromJson<SaveData>(json);
            }
            catch (Exception)
            {
                return new SaveLoadResult(null, SaveErrorCode.InvalidJson, "存档内容不是有效的 JSON。");
            }

            if (data == null || probe == null)
                return new SaveLoadResult(null, SaveErrorCode.InvalidJson, "存档内容为空。");

            SaveOperationResult presence = SaveDataValidator.ValidatePresence(probe, json);
            if (!presence.Succeeded)
                return new SaveLoadResult(null, presence.Error, presence.Message);

            if (!SaveDataMigrator.TryMigrate(data, json, out SaveData migrated,
                    out SaveOperationResult migration))
                return new SaveLoadResult(null, migration.Error, migration.Message);

            return new SaveLoadResult(migrated, SaveErrorCode.None, string.Empty);
        }

        private string CreateRecoveryPath()
        {
            string stamp = DateTime.UtcNow.ToString("yyyyMMdd'T'HHmmssfff", CultureInfo.InvariantCulture);
            string basePath = FilePath + ".corrupt-" + stamp;
            string candidate = basePath;
            int suffix = 1;
            while (File.Exists(candidate))
            {
                candidate = basePath + "-" + suffix.ToString(CultureInfo.InvariantCulture);
                suffix++;
            }
            return candidate;
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
