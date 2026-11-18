using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using SengokuChronicle.Application;
using SengokuChronicle.Domain;
using SengokuChronicle.Persistence;
using SengokuChronicle.Presentation;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// M00 存读档 gate 验证入口（SAVE-3 / SAVE-4）。只在运行中的 Editor（PID 88424 / 端口 7800）
/// 里通过 pipeline run_script 调用。放在 Assets/ 外，不触发 import 与域重载。
///
/// 与上一轮 IsolateSaveStore.cs 的区别：目录由参数传入，方便在同一轮里先指
/// ISO-SaveGate-20261118 再切到空的 ISO-Empty-20261118。
/// </summary>
public static class SaveGateProbe
{
    /// <summary>把存档目录切到 dir，并回读生效后的 DirectoryPath / isDefault。</summary>
    public static string SetStore(string dir)
    {
        if (string.IsNullOrEmpty(dir))
            return "BLOCKED: empty dir argument";

        SaveLoadPanel panel = FindSceneComponent<SaveLoadPanel>();
        GameSession session = FindSceneComponent<GameSession>();
        if (panel == null)
            return "BLOCKED: no live SaveLoadPanel in scene";
        if (session == null)
            return "BLOCKED: no live GameSession in scene";

        var store = new SaveSlotStore(dir);
        panel.SetStore(store);

        int titleControllers = 0;
        foreach (TitleScreenController title in Resources.FindObjectsOfTypeAll<TitleScreenController>())
        {
            if (title == null || !title.gameObject.scene.IsValid())
                continue;
            title.SetStore(new SaveSlotStore(dir));
            titleControllers++;
        }

        string defaultDir = SaveSlotStore.CreateDefault().DirectoryPath;
        return "panelStore=" + panel.Store.DirectoryPath
            + ";isDefault=" + (panel.Store.DirectoryPath == defaultDir)
            + ";filePath=" + panel.Store.FilePath
            + ";fileExists=" + File.Exists(panel.Store.FilePath)
            + ";backupExists=" + File.Exists(panel.Store.BackupFilePath)
            + ";titleControllersRedirected=" + titleControllers;
    }

    /// <summary>HUD 三值 + panel 状态 + 继承状态快照。</summary>
    public static string Probe()
    {
        SaveLoadPanel panel = FindSceneComponent<SaveLoadPanel>();
        GameSession session = FindSceneComponent<GameSession>();
        if (session == null)
            return "BLOCKED: no live GameSession";

        var builder = new StringBuilder();
        builder.Append("date=").Append(HeaderText("M02HeaderDateText"));
        builder.Append(";funds=").Append(HeaderText("M02HeaderFundsText"));
        builder.Append(";supplies=").Append(HeaderText("M02HeaderSuppliesText"));
        builder.Append(";people=").Append(HeaderText("M02HeaderPeopleText"));
        builder.Append(";sessionDate=").Append(session.WorldClock.CurrentDate);
        builder.Append(";store=").Append(panel != null && panel.Store != null ? panel.Store.DirectoryPath : "null");
        builder.Append(";panelStatus=").Append(panel != null ? panel.Status : "null");
        builder.Append(";lastOk=").Append(panel != null ? panel.LastOperationSucceeded.ToString() : "null");
        builder.Append(";slotExists=").Append(panel != null && panel.Store != null && panel.Store.Exists());
        builder.Append(";").Append(SuccessionProbe());
        return builder.ToString();
    }

    /// <summary>只读继承状态：phase、家主、待定继承人、待定摄政、界面文本与候选人标签。</summary>
    public static string SuccessionProbe()
    {
        GameSession session = FindSceneComponent<GameSession>();
        if (session == null)
            return "BLOCKED: no live GameSession";

        SuccessionState state = session.Succession;
        var builder = new StringBuilder();
        builder.Append("phase=").Append(state != null ? state.Phase.ToString() : "null");
        builder.Append(";headId=").Append(state != null ? state.HeadPersonId.Value : "?");
        builder.Append(";headName=").Append(session.SuccessionHeadName);
        builder.Append(";pendingSuccessorId=")
            .Append(state != null ? state.PendingSuccessorId.Value : "?");
        builder.Append(";pendingRegentId=")
            .Append(state != null ? state.PendingRegentId.Value : "?");
        builder.Append(";headText=").Append(SceneText("SuccessionHeadText"));
        builder.Append(";statusText=").Append(SceneText("SuccessionStatusText"));
        builder.Append(";modalStatusText=").Append(SceneText("SuccessionModalStatusText"));
        builder.Append(";feedbackText=").Append(SceneText("SuccessionFeedbackText"));
        builder.Append(";candidates=").Append(CandidateLabels());
        builder.Append(";familyId=")
            .Append(state != null && state.World != null && state.World.Family != null
                ? state.World.Family.Id.Value
                : "?");
        builder.Append(";canRetire=").Append(session.CanBeginSuccessionRetirement);
        builder.Append(";retireBlockReason=").Append(session.SuccessionRetirementBlockReason);
        builder.Append(";retireInteractable=").Append(ButtonInteractable("SuccessionRetireButton"));
        builder.Append(";confirmInteractable=").Append(ButtonInteractable("SuccessionConfirmButton"));
        return builder.ToString();
    }

    /// <summary>推进一天，制造与保存时不同的状态。</summary>
    public static string AdvanceOneDay()
    {
        GameSession session = FindSceneComponent<GameSession>();
        if (session == null)
            return "BLOCKED: no live GameSession";
        string before = session.WorldClock.CurrentDate.ToString();
        bool advanced = session.WorldClock.AdvanceOneDay();
        return "advanced=" + advanced + ";before=" + before + ";after=" + session.WorldClock.CurrentDate;
    }

    /// <summary>真实存档目录指纹（len + mtime），运行期旁证。</summary>
    public static string RealSaveFingerprint()
    {
        string dir = SaveSlotStore.CreateDefault().DirectoryPath;
        var builder = new StringBuilder();
        builder.Append("realDir=").Append(dir);
        foreach (string file in new[] { "save-slot-1.json", "save-slot-1.json.bak" })
        {
            string path = Path.Combine(dir, file);
            if (!File.Exists(path))
            {
                builder.Append(";").Append(file).Append("=missing");
                continue;
            }
            var info = new FileInfo(path);
            builder.Append(";").Append(file).Append("=len").Append(info.Length)
                .Append("@").Append(info.LastWriteTimeUtc.ToString("yyyy-MM-ddTHH:mm:ss.fffZ"));
        }
        return builder.ToString();
    }

    // ---- helpers ----

    private static string CandidateLabels()
    {
        // 只看当前可见的候选人按钮：旧轮次的按钮对象会被保留为 inactive，
        // 若把它们算进来，标记会和当前选择对不上。同一条文本只留一份。
        var labels = new List<string>();
        foreach (Text text in Resources.FindObjectsOfTypeAll<Text>())
        {
            if (text == null || !text.gameObject.scene.IsValid())
                continue;
            if (!text.gameObject.activeInHierarchy)
                continue;
            Transform owner = text.transform.parent;
            bool candidate = false;
            while (owner != null)
            {
                if (owner.name.StartsWith("SuccessionCandidate_", StringComparison.Ordinal))
                {
                    candidate = owner.gameObject.activeInHierarchy;
                    break;
                }
                owner = owner.parent;
            }
            if (!candidate)
                continue;
            string label = text.text.Replace("\n", " ");
            if (!labels.Contains(label))
                labels.Add(label);
        }
        labels.Sort(StringComparer.Ordinal);
        return labels.Count == 0 ? "(none)" : string.Join(" | ", labels.ToArray());
    }

    private static string ButtonInteractable(string objectName)
    {
        foreach (Button button in Resources.FindObjectsOfTypeAll<Button>())
        {
            if (button == null || !button.gameObject.scene.IsValid())
                continue;
            if (button.gameObject.name == objectName)
                return button.interactable.ToString()
                    + (button.gameObject.activeInHierarchy ? "" : "(inactive)");
        }
        return "(missing:" + objectName + ")";
    }

    private static string SceneText(string objectName)
    {
        foreach (Text text in Resources.FindObjectsOfTypeAll<Text>())
        {
            if (text == null || !text.gameObject.scene.IsValid())
                continue;
            if (text.gameObject.name == objectName)
                return text.text.Replace("\n", " / ");
        }
        return "(missing:" + objectName + ")";
    }

    private static string HeaderText(string objectName) => SceneText(objectName);

    private static T FindSceneComponent<T>() where T : Component
    {
        foreach (T candidate in Resources.FindObjectsOfTypeAll<T>())
        {
            if (candidate != null && candidate.gameObject.scene.IsValid())
                return candidate;
        }
        return null;
    }
}
