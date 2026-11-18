using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using SengokuChronicle.Application;
using SengokuChronicle.Domain;
using SengokuChronicle.Persistence;
using SengokuChronicle.Presentation;
using UnityEngine;

/// <summary>
/// M00 T3 存读档验证入口：SAVE-4（空槽位干净拒绝）与 SAVE-3（待处理继承决定跨存读档存活）。
/// 只在已开的 Editor 里通过 pipeline run_script 调用，且必须在 Play Mode 中执行，
/// 因为 SaveLoadPanel 在 Start() 里才自绑定 GameSession。
/// 所有写入都落在隔离目录，绝不触碰 SaveSlotStore.CreateDefault() 指向的真实槽。
/// </summary>
public static class SaveLoadPendingChecks
{
    private const string Root =
        @"C:\Users\panye\AppData\LocalLow\DefaultCompany\My project";

    /// <summary>SAVE-4：隔离槽为空时读档，必须给出「无存档」提示且战役状态不变。</summary>
    public static string EmptySlot()
    {
        string dir = Path.Combine(Root, "ISO-Empty-20261118");
        string prep = PrepareEmpty(dir);
        if (prep != "ok")
            return "BLOCKED:" + prep;

        SaveLoadPanel panel = Find<SaveLoadPanel>();
        GameSession session = Find<GameSession>();
        if (panel == null)
            return "BLOCKED:no live SaveLoadPanel";
        if (session == null)
            return "BLOCKED:no live GameSession";

        var b = new StringBuilder();
        b.Append("dir=").Append(dir);
        b.Append(";redirect=").Append(Redirect(dir, panel));
        b.Append(";before=").Append(State(session, panel));
        panel.Load();
        b.Append(";loadStatus=").Append(Quote(panel.Status));
        b.Append(";loadOk=").Append(panel.LastOperationSucceeded);
        b.Append(";after=").Append(State(session, panel));
        b.Append(";filesAfter=").Append(SlotFiles(panel));
        return b.ToString();
    }

    /// <summary>
    /// SAVE-3：选候选人 A 不确认 → 存档 → 改选候选人 B → 读档，
    /// 读回值必须仍是 A 与同一个待处理决定。
    /// </summary>
    public static string PendingSuccession()
    {
        string dir = Path.Combine(Root, "ISO-SaveGate-20261118");
        string prep = PrepareEmpty(dir);
        if (prep != "ok")
            return "BLOCKED:" + prep;

        SaveLoadPanel panel = Find<SaveLoadPanel>();
        GameSession session = Find<GameSession>();
        if (panel == null)
            return "BLOCKED:no live SaveLoadPanel";
        if (session == null)
            return "BLOCKED:no live GameSession";

        var b = new StringBuilder();
        b.Append("dir=").Append(dir);
        b.Append(";redirect=").Append(Redirect(dir, panel));

        SuccessionState state = session.Succession;
        GameDate date = session.WorldClock.CurrentDate;
        b.Append(";stateBefore=").Append(State(session, panel));

        SuccessionResult begin = SuccessionService.BeginRetirement(state, date);
        b.Append(";begin=").Append(begin.Outcome).Append("/").Append(begin.Phase)
         .Append("/").Append(Quote(begin.Message));
        if (!state.IsPending)
            return b.Append(";ABORT=phase-not-pending:").Append(state.Phase).ToString();

        IReadOnlyList<SuccessionCandidate> candidates = SuccessionService.GetCandidates(state, date);
        b.Append(";candidateCount=").Append(candidates.Count);
        if (candidates.Count < 2)
            return b.Append(";ABORT=fewer-than-two-candidates").ToString();
        b.Append(";candidates=").Append(CandidateIds(candidates));

        SuccessionResult pickA = SuccessionService.SelectSuccessor(state, candidates[0].PersonId, date);
        string pendingA = IdOf(state.PendingSuccessorId);
        b.Append(";pickA=").Append(pickA.Outcome).Append("/").Append(pickA.Phase)
         .Append("/pendingA=").Append(pendingA);

        panel.Save();
        b.Append(";saveOk=").Append(panel.LastOperationSucceeded)
         .Append(";saveStatus=").Append(Quote(panel.Status))
         .Append(";slotAfterSave=").Append(SlotFiles(panel));

        SuccessionResult pickB = SuccessionService.SelectSuccessor(state, candidates[1].PersonId, date);
        string pendingB = IdOf(state.PendingSuccessorId);
        b.Append(";pickB=").Append(pickB.Outcome).Append("/").Append(pickB.Phase)
         .Append("/pendingB=").Append(pendingB);

        panel.Load();
        b.Append(";loadOk=").Append(panel.LastOperationSucceeded)
         .Append(";loadStatus=").Append(Quote(panel.Status));

        SuccessionState after = session.Succession;
        string pendingLoaded = IdOf(after.PendingSuccessorId);
        b.Append(";pendingLoaded=").Append(pendingLoaded);
        b.Append(";phaseLoaded=").Append(after.Phase);
        b.Append(";headLoaded=").Append(IdOf(after.HeadPersonId));
        b.Append(";dateLoaded=").Append(session.WorldClock.CurrentDate);
        b.Append(";isPendingLoaded=").Append(after.IsPending);
        b.Append(";match=").Append(pendingLoaded == pendingA);
        b.Append(";revertedToB=").Append(pendingLoaded == pendingB);
        b.Append(";stateAfter=").Append(State(session, panel));
        return b.ToString();
    }

    /// <summary>只读快照，供人工核对与证据引用。</summary>
    public static string Snapshot()
    {
        SaveLoadPanel panel = Find<SaveLoadPanel>();
        GameSession session = Find<GameSession>();
        if (session == null)
            return "BLOCKED:no live GameSession";
        return State(session, panel);
    }

    private static string PrepareEmpty(string dir)
    {
        try
        {
            if (!Directory.Exists(dir))
                Directory.CreateDirectory(dir);
            string[] files = Directory.GetFiles(dir);
            return files.Length == 0 ? "ok" : "dir-not-empty:" + files.Length;
        }
        catch (Exception ex)
        {
            return "prepare-failed:" + ex.GetType().Name + ":" + ex.Message;
        }
    }

    /// <summary>把面板与标题界面的存储同时指向隔离目录，避免「继续游戏」写真实槽。</summary>
    private static string Redirect(string dir, SaveLoadPanel panel)
    {
        panel.SetStore(new SaveSlotStore(dir));
        int titles = 0;
        foreach (TitleScreenController title in Resources.FindObjectsOfTypeAll<TitleScreenController>())
        {
            if (title == null || !title.gameObject.scene.IsValid())
                continue;
            title.SetStore(new SaveSlotStore(dir));
            titles++;
        }
        string defaultDir = SaveSlotStore.CreateDefault().DirectoryPath;
        return "panelStore=" + panel.Store.DirectoryPath
            + ";isDefault=" + (panel.Store.DirectoryPath == defaultDir)
            + ";titles=" + titles;
    }

    private static string State(GameSession session, SaveLoadPanel panel)
    {
        SuccessionState state = session.Succession;
        var b = new StringBuilder();
        b.Append("date=").Append(session.WorldClock.CurrentDate);
        b.Append(";persons=").Append(state != null ? state.World.PersonCount.ToString() : "null");
        b.Append(";phase=").Append(state != null ? state.Phase.ToString() : "null");
        b.Append(";head=").Append(state != null ? IdOf(state.HeadPersonId) : "null");
        b.Append(";pending=").Append(state != null ? IdOf(state.PendingSuccessorId) : "null");
        b.Append(";successionCount=").Append(state != null ? state.SuccessionCount.ToString() : "null");
        b.Append(";panelStatus=").Append(panel != null ? Quote(panel.Status) : "null");
        b.Append(";lastOk=").Append(panel != null ? panel.LastOperationSucceeded.ToString() : "null");
        return b.ToString();
    }

    private static string SlotFiles(SaveLoadPanel panel)
    {
        if (panel == null || panel.Store == null)
            return "null";
        string dir = panel.Store.DirectoryPath;
        if (!Directory.Exists(dir))
            return "(dir-missing)";
        string[] files = Directory.GetFiles(dir);
        Array.Sort(files, StringComparer.Ordinal);
        if (files.Length == 0)
            return "(empty)";
        var b = new StringBuilder();
        for (int i = 0; i < files.Length; i++)
        {
            if (i > 0)
                b.Append(",");
            b.Append(Path.GetFileName(files[i])).Append(":").Append(new FileInfo(files[i]).Length);
        }
        return b.ToString();
    }

    private static string CandidateIds(IReadOnlyList<SuccessionCandidate> candidates)
    {
        var b = new StringBuilder();
        for (int i = 0; i < candidates.Count; i++)
        {
            if (i > 0)
                b.Append(",");
            b.Append(IdOf(candidates[i].PersonId));
        }
        return b.ToString();
    }

    private static string IdOf(PersonId id) => id.IsEmpty ? "(empty)" : id.Value;

    private static string Quote(string value) =>
        value == null ? "null" : "\"" + value.Replace("\"", "'") + "\"";

    private static T Find<T>() where T : Component
    {
        foreach (T candidate in Resources.FindObjectsOfTypeAll<T>())
        {
            if (candidate != null && candidate.gameObject.scene.IsValid())
                return candidate;
        }
        return null;
    }
}
