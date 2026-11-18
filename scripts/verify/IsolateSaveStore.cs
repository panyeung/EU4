using System;
using System.IO;
using System.Text;
using SengokuChronicle.Application;
using SengokuChronicle.Persistence;
using SengokuChronicle.Presentation;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// M00 存读档隔离验证入口。只在运行中的 Editor（PID 88424 / 端口 7800）里通过 run_script 调用。
/// 作用：把场景里 SaveLoadPanel（以及 TitleScreenController，防止误触“继续游戏”写真实槽）
/// 的 SaveSlotStore 指向独立目录，并回读目录与 HUD 三值。
/// </summary>
public static class IsolateSaveStore
{
    private const string IsolatedRoot =
        @"C:\Users\panye\AppData\LocalLow\DefaultCompany\My project\ISO-PlayTest-20261118";

    /// <summary>把存档目录切到隔离槽，回传生效后的 DirectoryPath。</summary>
    public static string Set()
    {
        SaveLoadPanel panel = FindSceneComponent<SaveLoadPanel>();
        GameSession session = FindSceneComponent<GameSession>();
        if (panel == null)
            return "BLOCKED: no live SaveLoadPanel in scene";
        if (session == null)
            return "BLOCKED: no live GameSession in scene";

        var store = new SaveSlotStore(IsolatedRoot);
        if (File.Exists(store.FilePath) || File.Exists(store.BackupFilePath))
            return "BLOCKED: isolated target already holds slot data: " + store.FilePath;

        panel.SetStore(store);

        int titleControllers = 0;
        foreach (TitleScreenController title in Resources.FindObjectsOfTypeAll<TitleScreenController>())
        {
            if (title == null || !title.gameObject.scene.IsValid())
                continue;
            title.SetStore(new SaveSlotStore(IsolatedRoot));
            titleControllers++;
        }

        string defaultDir = SaveSlotStore.CreateDefault().DirectoryPath;
        return "panelStore=" + panel.Store.DirectoryPath
            + ";isDefault=" + (panel.Store.DirectoryPath == defaultDir)
            + ";filePath=" + panel.Store.FilePath
            + ";titleControllersRedirected=" + titleControllers
            + ";sessionDate=" + session.WorldClock.CurrentDate;
    }

    /// <summary>读 HUD 三值（日期 / 资金 / 人口）+ 物资 + panel 状态，用于保存前后比对。</summary>
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
        return builder.ToString();
    }

    /// <summary>推进一天，制造与保存时不同的日期/资源状态。</summary>
    public static string AdvanceOneDay()
    {
        GameSession session = FindSceneComponent<GameSession>();
        if (session == null)
            return "BLOCKED: no live GameSession";
        string before = session.WorldClock.CurrentDate.ToString();
        bool advanced = session.WorldClock.AdvanceOneDay();
        return "advanced=" + advanced + ";before=" + before + ";after=" + session.WorldClock.CurrentDate;
    }

    /// <summary>真实存档目录指纹，用于证明未被动过。</summary>
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

    private static string HeaderText(string objectName)
    {
        foreach (Text text in Resources.FindObjectsOfTypeAll<Text>())
        {
            if (text == null || !text.gameObject.scene.IsValid())
                continue;
            if (text.gameObject.name == objectName)
                return text.text;
        }
        return "(missing:" + objectName + ")";
    }

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
