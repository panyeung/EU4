using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Verification helper for the `verify` skill. Drive it from the Unity Pipeline bridge:
///   unity command run_script --file .opencode/skills/verify/scripts/VerifyUi.cs --entry VerifyUi.Buttons
///   unity command run_script --file .opencode/skills/verify/scripts/VerifyUi.cs --entry VerifyUi.Click --args '["新游戏"]'
///   unity command run_script --file .opencode/skills/verify/scripts/VerifyUi.cs --entry VerifyUi.State
/// Lives outside Assets/ so running it never triggers an import or a domain reload.
/// Reflection only, so it needs no reference to the UI assemblies.
/// </summary>
public static class VerifyUi
{
    /// <summary>Every active Button in the active scene, as "label | interactable | path".</summary>
    public static string Buttons()
    {
        var builder = new StringBuilder();
        foreach (ButtonRef button in FindButtons())
            builder.AppendLine($"{Describe(button.Label)} | {button.Interactable} | {button.Path}");
        return builder.Length == 0 ? "(no active buttons)" : builder.ToString();
    }

    /// <summary>Press the active Button whose visible label matches exactly.</summary>
    public static string Click(string label)
    {
        var candidates = new List<ButtonRef>();
        foreach (ButtonRef button in FindButtons())
        {
            if (button.Label == label)
                candidates.Add(button);
        }

        if (candidates.Count == 0)
            throw new InvalidOperationException(
                $"No active button labelled '{label}'. On screen: {Buttons()}");
        if (candidates.Count > 1)
            throw new InvalidOperationException(
                $"{candidates.Count} active buttons are labelled '{label}'. Pick one by path with VerifyUi.ClickPath.");

        return ClickPath(candidates[0].Path);
    }

    /// <summary>Press the active Button at an exact hierarchy path, for duplicated labels.</summary>
    public static string ClickPath(string path)
    {
        foreach (ButtonRef button in FindButtons())
        {
            if (button.Path != path)
                continue;
            if (!button.Interactable)
                throw new InvalidOperationException($"Button at '{path}' is not interactable.");

            MethodInfo invoke = button.OnClick.GetType().GetMethod("Invoke", Type.EmptyTypes);
            if (invoke == null)
                throw new InvalidOperationException(
                    $"Button at '{path}' exposes no Invoke method; its onClick type is {button.OnClick.GetType().FullName}.");

            invoke.Invoke(button.OnClick, null);
            return $"clicked '{Describe(button.Label)}' at {path}";
        }

        throw new InvalidOperationException($"No active button at '{path}'. On screen: {Buttons()}");
    }

    /// <summary>Every non-empty visible text in the active scene, as "text | path".</summary>
    public static string Texts()
    {
        var builder = new StringBuilder();
        Scene scene = SceneManager.GetActiveScene();
        if (!scene.isLoaded)
            return "(no loaded scene)";

        foreach (GameObject root in scene.GetRootGameObjects())
        {
            foreach (Transform transform in root.GetComponentsInChildren<Transform>(false))
            {
                foreach (Component component in transform.GetComponents<Component>())
                {
                    if (component == null)
                        continue;
                    PropertyInfo textProperty = component.GetType().GetProperty("text",
                        BindingFlags.Public | BindingFlags.Instance);
                    if (textProperty == null || textProperty.PropertyType != typeof(string) || !textProperty.CanRead)
                        continue;

                    var text = textProperty.GetValue(component) as string;
                    if (string.IsNullOrWhiteSpace(text))
                        continue;

                    builder.AppendLine($"{text} | {PathOf(transform)}");
                }
            }
        }

        return builder.Length == 0 ? "(no visible text)" : builder.ToString();
    }

    /// <summary>Visible text at one hierarchy path, for quoting a single HUD value.</summary>
    public static string Text(string path)
    {
        Scene scene = SceneManager.GetActiveScene();
        if (!scene.isLoaded)
            return "(no loaded scene)";

        foreach (GameObject root in scene.GetRootGameObjects())
        {
            foreach (Transform transform in root.GetComponentsInChildren<Transform>(false))
            {
                if (PathOf(transform) != path)
                    continue;
                string label = LabelOf(transform.gameObject);
                return label ?? "(no text)";
            }
        }

        return $"(no active object at '{path}')";
    }

    /// <summary>Scene identity, play state, frame and game time.</summary>
    public static string State()
    {
        Scene scene = SceneManager.GetActiveScene();
        var builder = new StringBuilder();
        builder.AppendLine($"scene={scene.path} loaded={scene.isLoaded} dirty={scene.isDirty}");
        builder.AppendLine($"playing={Application.isPlaying} frame={Time.frameCount} time={Time.time:F2}");
        builder.AppendLine($"buildIndex={scene.buildIndex} resolution={Screen.width}x{Screen.height}");
        return builder.ToString();
    }

    private sealed class ButtonRef
    {
        public string Label;
        public bool Interactable;
        public string Path;
        public object OnClick;
    }

    private static IEnumerable<ButtonRef> FindButtons()
    {
        Scene scene = SceneManager.GetActiveScene();
        if (!scene.isLoaded)
            yield break;

        foreach (GameObject root in scene.GetRootGameObjects())
        {
            foreach (Transform transform in root.GetComponentsInChildren<Transform>(false))
            {
                object onClick = null;
                bool interactable = true;
                bool isButton = false;

                foreach (Component component in transform.GetComponents<Component>())
                {
                    if (component == null)
                        continue;
                    Type type = component.GetType();

                    if (type.Name == "Button" || type.Name == "TMP_Button")
                    {
                        PropertyInfo clickProperty = type.GetProperty("onClick",
                            BindingFlags.Public | BindingFlags.Instance);
                        if (clickProperty != null)
                        {
                            onClick = clickProperty.GetValue(component);
                            if (onClick != null)
                                isButton = true;
                        }

                        PropertyInfo interactableProperty = type.GetProperty("interactable",
                            BindingFlags.Public | BindingFlags.Instance);
                        if (interactableProperty != null && interactableProperty.PropertyType == typeof(bool))
                            interactable = (bool)interactableProperty.GetValue(component);
                    }
                }

                if (!isButton)
                    continue;

                yield return new ButtonRef
                {
                    Label = LabelOf(transform.gameObject),
                    Interactable = interactable,
                    Path = PathOf(transform),
                    OnClick = onClick,
                };
            }
        }
    }

    /// <summary>Visible text of a control: UGUI Text or TextMeshPro, whichever is present.</summary>
    private static string LabelOf(GameObject owner)
    {
        foreach (Component component in owner.GetComponents<Component>())
        {
            if (component == null)
                continue;
            PropertyInfo textProperty = component.GetType().GetProperty("text",
                BindingFlags.Public | BindingFlags.Instance);
            if (textProperty != null && textProperty.PropertyType == typeof(string) && textProperty.CanRead)
            {
                var text = textProperty.GetValue(component) as string;
                if (!string.IsNullOrEmpty(text))
                    return text;
            }
        }

        // Label text usually sits on a child (TMP label object).
        foreach (Component component in owner.GetComponentsInChildren<Component>(true))
        {
            if (component == null)
                continue;
            PropertyInfo textProperty = component.GetType().GetProperty("text",
                BindingFlags.Public | BindingFlags.Instance);
            if (textProperty != null && textProperty.PropertyType == typeof(string) && textProperty.CanRead)
            {
                var text = textProperty.GetValue(component) as string;
                if (!string.IsNullOrEmpty(text))
                    return text;
            }
        }

        return owner.name;
    }

    private static string PathOf(Transform transform)
    {
        var builder = new StringBuilder(transform.name);
        for (Transform current = transform.parent; current != null; current = current.parent)
            builder.Insert(0, current.name + "/");
        return builder.ToString();
    }

    private static string Describe(string label) => label == null ? "(unnamed)" : label;
}
