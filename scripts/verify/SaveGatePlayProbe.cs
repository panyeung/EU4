using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;
using SengokuChronicle.Application;
using SengokuChronicle.Domain;
using SengokuChronicle.Persistence;
using SengokuChronicle.Presentation;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// M00 收口 SAVE-3 / SAVE-4 的 Play Mode 面板层探针（20261117-1745 轮）。
/// 只做三件事：把存档目录重定向到隔离目录、点真实按钮、回读真实状态。
/// 放在 Assets/ 之外，import 与域重载都不会被触发。
/// </summary>
public static class SaveGatePlayProbe
{
    // SAVE-4 用的空目录；SAVE-3 复用同一目录（此时已被 SAVE-3 存档写入）。
    public const string PlayDir =
        @"C:\Users\panye\AppData\LocalLow\DefaultCompany\My project\ISO-1745-SaveGate";

    /// <summary>把 SaveLoadPanel 与 TitleScreenController 同时指向隔离目录，并回读生效结果。</summary>
    public static string SetStore()
    {
        SaveLoadPanel panel = FindSceneComponent<SaveLoadPanel>();
        GameSession session = FindSceneComponent<GameSession>();
        if (panel == null)
            return "BLOCKED: no live SaveLoadPanel in scene";
        if (session == null)
            return "BLOCKED: no live GameSession in scene";

        Directory.CreateDirectory(PlayDir);
        var store = new SaveSlotStore(PlayDir);
        panel.SetStore(store);

        int titleControllers = 0;
        foreach (TitleScreenController title in Resources.FindObjectsOfTypeAll<TitleScreenController>())
        {
            if (title == null || !title.gameObject.scene.IsValid())
                continue;
            title.SetStore(new SaveSlotStore(PlayDir));
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

    /// <summary>战役关键值 + 面板状态 + 继承待处理决定。</summary>
    public static string State()
    {
        GameSession session = FindSceneComponent<GameSession>();
        if (session == null)
            return "BLOCKED: no live GameSession";
        SaveLoadPanel panel = FindSceneComponent<SaveLoadPanel>();

        var b = new StringBuilder();
        b.Append("sessionDate=").Append(session.WorldClock.CurrentDate.ToString());
        b.Append(";hudDate=").Append(SceneText("M02HeaderDateText"));
        b.Append(";funds=").Append(SceneText("M02HeaderFundsText"));
        b.Append(";supplies=").Append(SceneText("M02HeaderSuppliesText"));
        b.Append(";people=").Append(SceneText("M02HeaderPeopleText"));
        b.Append(";livingMembers=").Append(LivingMembers(session));
        b.Append(";store=").Append(panel != null && panel.Store != null ? panel.Store.DirectoryPath : "null");
        b.Append(";slotExists=").Append(panel != null && panel.Store != null && panel.Store.Exists());
        b.Append(";status=").Append(panel != null ? panel.Status : "null");
        b.Append(";statusTextObj=").Append(SceneText("M5SaveStatusText"));
        b.Append(";lastOk=").Append(panel != null ? panel.LastOperationSucceeded.ToString() : "null");
        b.Append(";").Append(Succession());
        return b.ToString();
    }

    /// <summary>只读继承状态：阶段、家主、待处理继承人/摄政、候选人与按钮状态。</summary>
    public static string Succession()
    {
        GameSession session = FindSceneComponent<GameSession>();
        if (session == null)
            return "BLOCKED: no live GameSession";

        SuccessionState state = session.Succession;
        var b = new StringBuilder();
        b.Append("phase=").Append(state != null ? state.Phase.ToString() : "null");
        b.Append(";headId=").Append(state != null ? state.HeadPersonId.Value : "?");
        b.Append(";headName=").Append(HeadName(session, state));
        b.Append(";pendingSuccessor=").Append(state != null ? state.PendingSuccessorId.Value : "?");
        b.Append(";pendingRegent=").Append(state != null ? state.PendingRegentId.Value : "?");
        b.Append(";isPending=").Append(state != null && state.IsPending);
        b.Append(";modalVisible=").Append(ComponentOf<SuccessionPanel>() != null
            ? ComponentOf<SuccessionPanel>().IsModalVisible.ToString() : "noPanel");
        b.Append(";retireBtn=").Append(ButtonState("SuccessionRetireButton"));
        b.Append(";confirmBtn=").Append(ButtonState("SuccessionConfirmButton"));
        b.Append(";candidates=").Append(Candidates(session, state));
        b.Append(";canRetire=").Append(session.CanBeginSuccessionRetirement);
        b.Append(";retireBlock=").Append(session.SuccessionRetirementBlockReason);
        return b.ToString();
    }

    /// <summary>按 GameObject 名字点真实 Button（走 onClick 的 UnityAction 链）。</summary>
    public static string Click(string objectName)
    {
        foreach (Button button in Resources.FindObjectsOfTypeAll<Button>())
        {
            if (button == null || !button.gameObject.scene.IsValid())
                continue;
            if (button.gameObject.name != objectName)
                continue;
            if (!button.gameObject.activeInHierarchy)
                return "BLOCKED: button inactive: " + objectName;
            if (!button.interactable)
                return "BLOCKED: button not interactable: " + objectName;

            // 真实按钮路径：触发 onClick 上登记的全部监听器（面板自己的处理方法）。
            button.onClick.Invoke();
            return "clicked=" + objectName + ";label=" + LabelOf(button);
        }
        return "BLOCKED: button not found: " + objectName + ";activeButtons=" + ActiveButtons();
    }

    /// <summary>按候选人稳定 ID 点候选人按钮（对象名 SuccessionCandidate_&lt;id&gt;）。</summary>
    public static string ClickCandidate(string personId)
    {
        string name = "SuccessionCandidate_" + personId;
        string result = Click(name);
        return result + ";candidatesNow=" + Candidates(FindSceneComponent<GameSession>(),
            FindSceneComponent<GameSession>().Succession);
    }

    /// <summary>真实存档目录指纹（长度 + mtime），运行期旁证。</summary>
    public static string RealSaveFingerprint()
    {
        string dir = SaveSlotStore.CreateDefault().DirectoryPath;
        var b = new StringBuilder();
        b.Append("realDir=").Append(dir);
        foreach (string file in new[] { "save-slot-1.json", "save-slot-1.json.bak" })
        {
            string path = Path.Combine(dir, file);
            if (!File.Exists(path))
            {
                b.Append(";").Append(file).Append("=missing");
                continue;
            }
            var info = new FileInfo(path);
            b.Append(";").Append(file).Append("=len").Append(info.Length)
                .Append("@").Append(info.LastWriteTimeUtc.ToString("yyyy-MM-ddTHH:mm:ss.fffZ"));
        }
        return b.ToString();
    }

    /// <summary>隔离目录里的文件清单，用于证明 SAVE-4 没有写新档。</summary>
    public static string IsolatedFiles()
    {
        if (!Directory.Exists(PlayDir))
            return "dir=missing";
        var b = new StringBuilder();
        b.Append("dir=").Append(PlayDir);
        foreach (string path in Directory.GetFiles(PlayDir))
        {
            var info = new FileInfo(path);
            b.Append(";").Append(info.Name).Append("=").Append(info.Length)
                .Append("@").Append(info.LastWriteTimeUtc.ToString("yyyy-MM-ddTHH:mm:ss.fffZ"));
        }
        return b.ToString();
    }

    // ---- helpers ----

    private static string LivingMembers(GameSession session)
    {
        SuccessionState state = session.Succession;
        if (state == null || state.World == null || state.World.Family == null)
            return "?";
        int living = 0;
        int total = 0;
        foreach (FamilyMembership membership in state.World.Family.Members)
        {
            total++;
            if (state.World.IsLiving(membership.PersonId))
                living++;
        }
        return living + "/" + total;
    }

    private static string HeadName(GameSession session, SuccessionState state)
    {
        if (state == null || state.World == null)
            return "?";
        PersonState head;
        return state.World.TryGetPerson(state.HeadPersonId, out head) ? head.DisplayName : "?";
    }

    private static string Candidates(GameSession session, SuccessionState state)
    {
        if (session == null || state == null)
            return "?";
        IReadOnlyList<SuccessionCandidate> candidates = session.SuccessionCandidates;
        if (candidates == null || candidates.Count == 0)
            return "(none)";
        var parts = new List<string>();
        foreach (SuccessionCandidate candidate in candidates)
        {
            parts.Add(candidate.PersonId.Value + "=" + candidate.DisplayName
                + "(" + candidate.Age + ","
                + (candidate.PersonId == state.PendingSuccessorId ? "已选" : "未选") + ")");
        }
        return string.Join(" | ", parts.ToArray());
    }

    private static string ButtonState(string objectName)
    {
        foreach (Button button in Resources.FindObjectsOfTypeAll<Button>())
        {
            if (button == null || !button.gameObject.scene.IsValid())
                continue;
            if (button.gameObject.name != objectName)
                continue;
            return button.interactable.ToString()
                + (button.gameObject.activeInHierarchy ? "" : "(inactive)");
        }
        return "(missing)";
    }

    private static string ActiveButtons()
    {
        var names = new List<string>();
        foreach (Button button in Resources.FindObjectsOfTypeAll<Button>())
        {
            if (button == null || !button.gameObject.scene.IsValid() || !button.gameObject.activeInHierarchy)
                continue;
            names.Add(button.gameObject.name + "[" + LabelOf(button) + "]");
        }
        names.Sort(StringComparer.Ordinal);
        return names.Count == 0 ? "(none)" : string.Join(", ", names.ToArray());
    }

    private static string LabelOf(Button button)
    {
        Text text = button.GetComponentInChildren<Text>(true);
        return text != null ? text.text.Replace("\n", " ") : "(noLabel)";
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

    private static T FindSceneComponent<T>() where T : Component
    {
        foreach (T candidate in Resources.FindObjectsOfTypeAll<T>())
        {
            if (candidate != null && candidate.gameObject.scene.IsValid())
                return candidate;
        }
        return null;
    }

    private static T ComponentOf<T>() where T : Component => FindSceneComponent<T>();
}
