using Dalamud.Data.LuminaExtensions;
using Dalamud.Game.Chat.SeStringHandling;
using Dalamud.Interface;
using Dalamud.Plugin;
using FFXIVClientStructs.FFXIV.Component.GUI;
using ImGuiNET;
using ImGuiScene;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Reflection;
using System.Text;

namespace BluDex
{
    internal class PluginUI : IDisposable
    {
        private readonly BluDexPlugin plugin;

        public PluginUI(BluDexPlugin plugin)
        {
            this.plugin = plugin;

            PopulateSpellFilter();
            LoadTextures();

            plugin.Interface.UiBuilder.OnOpenConfigUi += UiBuilder_OnOpenConfigUi;
            plugin.Interface.UiBuilder.OnBuildUi += UiBuilder_OnBuildUi;
        }

        public void Dispose()
        {
            plugin.Interface.UiBuilder.OnBuildUi -= UiBuilder_OnBuildUi;
            plugin.Interface.UiBuilder.OnOpenConfigUi -= UiBuilder_OnOpenConfigUi;

            TextureStorage.Values.ToList().ForEach(v => v?.Dispose());
            TextureStorage.Clear();
        }

#if DEBUG
        private bool IsImguiSetupOpen = true;
#else
        private bool IsImguiSetupOpen = false;
#endif

        public void Open() => IsImguiSetupOpen = true;

        public void UiBuilder_OnOpenConfigUi(object sender, EventArgs args) => IsImguiSetupOpen = true;

        private void PopulateSpellFilter()
        {
            VisibleActions.AddRange(plugin.ActionDataStorage);
            Enum.GetValues(typeof(SpellRank)).Cast<SpellRank>().ToList().ForEach(value => SpellFilter[value] = false);
            Enum.GetValues(typeof(SpellType)).Cast<SpellType>().ToList().ForEach(value => SpellFilter[value] = false);
            Enum.GetValues(typeof(SpellAspect)).Cast<SpellAspect>().ToList().ForEach(value => SpellFilter[value] = false);
            Enum.GetValues(typeof(SpellEffect)).Cast<SpellEffect>().ToList().ForEach(value => SpellFilter[value] = false);
            Enum.GetValues(typeof(SpellTarget)).Cast<SpellTarget>().ToList().ForEach(value => SpellFilter[value] = false);
            Enum.GetValues(typeof(SpellCast)).Cast<SpellCast>().ToList().ForEach(value => SpellFilter[value] = false);
            Enum.GetValues(typeof(SpellRecast)).Cast<SpellRecast>().ToList().ForEach(value => SpellFilter[value] = false);
        }

        private void LoadTextures()
        {
            LoadIcon(MissingIconID);
            LoadIcon(ClearFilterIconID);

            foreach (SpellRank value in Enum.GetValues(typeof(SpellRank)))
                LoadIcon(value.GetAttribute<UiDataAttribute>().IconID);

            foreach (SpellType value in Enum.GetValues(typeof(SpellType)))
                LoadIcon(value.GetAttribute<UiDataAttribute>().IconID);

            foreach (SpellAspect value in Enum.GetValues(typeof(SpellAspect)))
                LoadIcon(value.GetAttribute<UiDataAttribute>().IconID);

            foreach (SpellEffect value in Enum.GetValues(typeof(SpellEffect)))
                LoadIcon(value.GetAttribute<UiDataAttribute>().IconID);

            foreach (SpellTarget value in Enum.GetValues(typeof(SpellTarget)))
                LoadIcon(value.GetAttribute<UiDataAttribute>().IconID);

            foreach (ActionData value in plugin.ActionDataStorage)
                LoadIcon(value.IconID);

            var assemblyDir = Path.GetDirectoryName(plugin.AssemblyLocation);
            LoadImage(-1, $"{assemblyDir}/res/TargetCombined.png");
        }

        private TextureWrap GetTex(int id)
        {
            if (TextureStorage.TryGetValue(id, out var tex) && tex?.ImGuiHandle != IntPtr.Zero)
                return tex;

            LoadIcon(id);
            tex = TextureStorage[MissingIconID];

            if (tex?.ImGuiHandle == IntPtr.Zero)
                throw new NullReferenceException("Texture failed");

            return tex;
        }

        private void LoadIcon(int iconID)
        {
            if (iconID <= 0)
                return;

            var iconTex = plugin.Interface.Data.GetIcon(iconID);
            TextureStorage[iconID] = plugin.Interface.UiBuilder.LoadImageRaw(iconTex.GetRgbaImageData(), iconTex.Header.Width, iconTex.Header.Height, 4);
        }

        private void LoadImage(int id, string filePath)
        {
            if (id >= 0)
                throw new ArgumentException($"Image IDs should be negative: {id} = {filePath}");

            TextureStorage[id] = plugin.Interface.UiBuilder.LoadImage(filePath);
        }

        private const int MissingIconID = 60861;
        private const int ClearFilterIconID = 16005;
        private readonly ConcurrentDictionary<int, TextureWrap> TextureStorage = new();
        private readonly Dictionary<Enum, bool> SpellFilter = new();
        private List<ActionData> VisibleActions = new();

        private readonly Vector2 FilterIconSize = new(24, 32);
        private readonly Vector2 ActionIconSize = new(32, 32);
        private readonly Vector4 FilterButtonBlue = ConvertRGBA(46, 91, 136, 255);
        private readonly Vector4 FilterButtonBlueDisabled = ConvertRGBA(24, 46, 69, 255);

        private readonly Vector4 TintEnabledColor = Vector4.One;
        private readonly Vector4 TintDisabledColor = new(.5f, .5f, .5f, 1);

        public void UiBuilder_OnBuildUi()
        {
            if (!IsImguiSetupOpen)
                return;

            ImGui.SetNextWindowSize(new Vector2(525, 600), ImGuiCond.FirstUseEver);

            ImGui.PushStyleColor(ImGuiCol.ResizeGrip, 0);

            if (!ImGui.Begin(plugin.Name, ref IsImguiSetupOpen))
                return;

#if DEBUG
            //UiBuilder_DebugAddonInspector();
            //UiBuilderDebugLuminaInspector();
#endif

            var windowSizeX = UiBuilder_FilterIcons();

            ImGui.Dummy(Vector2.Zero);

            UiBuilder_DisplayActions(windowSizeX);

            ImGui.End();

            ImGui.PopStyleColor();  // ResizeGrip
        }

        private float UiBuilder_FilterIcons()
        {
            var style = ImGui.GetStyle();

            ImGui.PushStyleVar(ImGuiStyleVar.ItemSpacing, new Vector2(2, -2));
            ImGui.PushStyleVar(ImGuiStyleVar.WindowPadding, new Vector2(4, 4));

            var windowSizeX = FilterIconSize.X * 10 + style.ItemSpacing.X * 9 + style.WindowPadding.X * 2;
            var windowSizeY = FilterIconSize.Y * 3 + style.ItemSpacing.Y * 4 + style.WindowPadding.Y * 2 + 2 + 50;
            var windowSize = new Vector2(windowSizeX, windowSizeY);

            ImGui.BeginChild("FilterIcons", windowSize, true, ImGuiWindowFlags.NoScrollbar);

            ImGui.PopStyleVar(1); // WindowPadding

            FilterIconImageButtons<SpellRank>();

            ImGui.SameLine();
            FilterIconImageButtons<SpellType>();

            ImGui.SameLine();
            FilterIconImageButtons(SpellTarget.SelfAllyOrEnemy);

            ImGui.SameLine();
            ClearFilterIconImageButton();

            FilterIconImageButtons<SpellAspect>();

            FilterIconImageButtons<SpellEffect>();

            ImGui.PopStyleVar(); // ItemSpacing

            ImGui.PushStyleVar(ImGuiStyleVar.ItemSpacing, new Vector2(4, 4));

            FilterTextButtons<SpellCast>("{0} cast");
            FilterTextButtons<SpellRecast>("{0} cooldown");

            ImGui.PopStyleVar(); // ItemSpacing

            ImGui.EndChild();

            return windowSizeX;
        }

        private void FilterIconImageButtons<T>(params T[] skipValue) where T : Enum
        {
            var values = Enum.GetValues(typeof(T)).Cast<T>()
                .Where(value => !skipValue.Contains(value))
                .ToList();
            var last = values.Last();

            foreach (T value in values)
            {
                FilterIconImageButton(value);

                if (!value.Equals(last))
                    ImGui.SameLine();
            }
        }

        private void FilterIconImageButton<T>(T value) where T : Enum
        {
            var enabled = SpellFilter[value];
            var tint = enabled ? TintEnabledColor : TintDisabledColor;

            var attr = value.GetAttribute<UiDataAttribute>();
            var tex = GetTex(attr.IconID);

            ImGui.PushStyleColor(ImGuiCol.Button, Vector4.Zero);
            ImGui.PushStyleColor(ImGuiCol.ButtonHovered, Vector4.Zero);
            ImGui.PushStyleColor(ImGuiCol.ButtonActive, Vector4.Zero);

            if (ImGui.ImageButton(tex.ImGuiHandle, FilterIconSize, Vector2.Zero, Vector2.One, 0, Vector4.Zero, tint))
            {
                SpellFilter[value] = !enabled;
                RecalculateVisibleActions();
            }

            ImGui.PopStyleColor(3);

            var uiAttr = value.GetAttribute<UiDataAttribute>();
            var tooltipText = uiAttr?.Text ?? value.ToString();
            ImGuiEx.TextTooltip(tooltipText);
        }

        private void ClearFilterIconImageButton()
        {
            var tex = GetTex(ClearFilterIconID);

            ImGui.PushStyleColor(ImGuiCol.Button, Vector4.Zero);
            ImGui.PushStyleColor(ImGuiCol.ButtonHovered, Vector4.Zero);
            ImGui.PushStyleColor(ImGuiCol.ButtonActive, Vector4.Zero);

            //var style = ImGui.GetStyle();
            //var positionX = ImGui.GetWindowWidth() - style.WindowPadding.X - FilterIconSize.X;
            //ImGui.SetCursorPosX(positionX);

            if (ImGui.ImageButton(tex.ImGuiHandle, FilterIconSize, Vector2.Zero, Vector2.One, 0, Vector4.Zero, Vector4.One))
            {
                foreach (var key in SpellFilter.Keys)
                    SpellFilter[key] = false;
                RecalculateVisibleActions();
            }

            ImGui.PopStyleColor(3);

            ImGuiEx.TextTooltip("Clear Filters");
        }

        private void FilterTextButtons<T>(string tooltipFormat) where T : Enum
        {
            var values = Enum.GetValues(typeof(T)).Cast<T>().ToList();
            var last = values.Last();

            ImGui.Dummy(new Vector2(2f, 0f));
            ImGui.SameLine(0f, 0f);

            foreach (T value in values)
            {
                FilterTextButton(value, tooltipFormat, values.Count);

                if (!value.Equals(last))
                    ImGui.SameLine(0f, 4f);
            }
        }

        private void FilterTextButton<T>(T value, string tooltipFormat, int buttonCount) where T : Enum
        {
            var enabled = SpellFilter[value];
            var tint = enabled ? TintEnabledColor : TintDisabledColor;
            var buttonColor = FilterButtonBlue * tint;

            var uiAttr = value.GetAttribute<UiDataAttribute>();
            var text = uiAttr?.Text ?? value.ToString();

            var style = ImGui.GetStyle();
            var availableWidth = ImGui.GetWindowWidth() - style.WindowPadding.X * 2 - (style.ItemSpacing.X * (buttonCount - 1));

            var buttonWidth = availableWidth / buttonCount;
            var buttonHeight = ImGui.CalcTextSize(text).Y;
            var buttonSize = new Vector2(buttonWidth, buttonHeight);

            ImGui.PushStyleColor(ImGuiCol.Button, buttonColor);
            ImGui.PushStyleColor(ImGuiCol.ButtonHovered, buttonColor);
            ImGui.PushStyleColor(ImGuiCol.ButtonActive, buttonColor);
            ImGui.PushStyleVar(ImGuiStyleVar.FramePadding, new Vector2(0f, 0f));
            ImGui.PushStyleVar(ImGuiStyleVar.ButtonTextAlign, new Vector2(.5f, .5f));

            if (ImGui.Button(text, buttonSize))
            {
                SpellFilter[value] = !enabled;
                RecalculateVisibleActions();
            }

            ImGui.PopStyleVar(2);  // FramePadding, ButtonTextAlign
            ImGui.PopStyleColor(3);  // Button, ButtonHovered, ButtonActive

            ImGuiEx.TextTooltip(string.Format(tooltipFormat, text));
        }

        private void RecalculateVisibleActions()
        {
            VisibleActions = plugin.ActionDataStorage.Where(action => !IsActionFiltered(action)).ToList();
        }

        private bool IsActionFiltered(ActionData action)
        {
            bool TypeIsSet<T>() where T : Enum => SpellFilter.Keys.OfType<T>().Count(key => SpellFilter[key]) > 0;

            return TypeIsSet<SpellAspect>() && !SpellFilter.Keys.OfType<SpellAspect>().Any(aspect => action.Aspects.Contains(aspect) && SpellFilter[aspect]) ||
                   TypeIsSet<SpellEffect>() && !SpellFilter.Keys.OfType<SpellEffect>().Any(effect => action.Effects.Contains(effect) && SpellFilter[effect]) ||
                   TypeIsSet<SpellTarget>() && !SpellFilter.Keys.OfType<SpellTarget>().Any(target => action.Target.HasFlag(target) && SpellFilter[target]) ||
                   TypeIsSet<SpellType>() && !SpellFilter[action.Type] ||
                   TypeIsSet<SpellRank>() && !SpellFilter[action.Rank] ||
                   TypeIsSet<SpellCast>() && !SpellFilter[action.CastTime] ||
                   TypeIsSet<SpellRecast>() && !SpellFilter[action.RecastTime];
        }

        private void UiBuilder_DisplayActions(float windowSizeX)
        {
            ImGui.BeginChild("ActionDataList", new(windowSizeX, -1), true);

            foreach (var action in VisibleActions)
                DisplayAction(action);

            ImGui.EndChild();
        }

        private void DisplayAction(ActionData action)
        {
            ImGui.PushStyleColor(ImGuiCol.Button, Vector4.Zero);
            ImGui.PushStyleColor(ImGuiCol.ButtonHovered, Vector4.Zero);
            ImGui.PushStyleColor(ImGuiCol.ButtonActive, Vector4.Zero);

            var tex = GetTex(action.IconID);
            if (ImGui.ImageButton(tex.ImGuiHandle, ActionIconSize, Vector2.Zero, Vector2.One, 0, Vector4.Zero, Vector4.One))
            {
            }

            ImGui.PopStyleColor(3);

            ImGuiEx.WrappedTextTooltip(action.Description, 400);

            ImGui.SameLine();
            ImGui.Dummy(new(4, 0));
            ImGui.SameLine();

            var rankText = action.Rank.GetAttribute<UiDataAttribute>().Text;
            ImGui.Text($"#{action.Number}: {action.Name}\n{rankText}");

            ImGui.PushStyleVar(ImGuiStyleVar.ItemSpacing, Vector2.Zero);

            var style = ImGui.GetStyle();
            var positionX = ImGui.GetWindowContentRegionWidth() + style.WindowPadding.X;

            void IconImage<T>(T value) where T : Enum
            {
                var attr = value.GetAttribute<UiDataAttribute>();

                ImGui.SameLine();
                var tex = GetTex(attr.IconID);

                positionX -= FilterIconSize.X;
                ImGui.SetCursorPosX(positionX);
                ImGui.Image(tex.ImGuiHandle, FilterIconSize);

                var uiAttr = value.GetAttribute<UiDataAttribute>();
                var tooltipText = uiAttr?.Text ?? value.ToString();
                ImGuiEx.TextTooltip(tooltipText);
            }

            // Backwards to insert from the right
            ImGui.SetCursorPosX(positionX);
            action.Effects.ToList().ForEach(v => IconImage(v));
            action.Aspects.ToList().ForEach(v => IconImage(v));
            IconImage(action.Target);
            IconImage(action.Type);

            ImGui.PopStyleVar();  // ItemSpacing
        }

        private static uint ConvertRGBA(Vector4 col)
        {
            return ((uint)(col.Y * 255) << 24) +
                   ((uint)(col.Z * 255) << 16) +
                   ((uint)(col.X * 255) << 8) +
                   ((uint)(col.W * 255));
        }
        /*
         ZYX = deep red
         XYZ = brown
         XZY = no
         ZXY = no
         YXZ = 
         YZX = 
         */

        private static Vector4 ConvertRGBA(int r, int g, int b, int a = 255) => new(r / 255f, g / 255f, b / 255f, a / 255f);

        #region Debug

        private string DebugAddonName = "";
        private string DebugAddonData = "";

        private void UiBuilder_DebugAddonInspector()
        {
            ImGui.InputText("Addon Name", ref DebugAddonName, 100);
            ImGui.SameLine();
            if (ImGuiEx.IconButton(FontAwesomeIcon.Check, "Get vtbl address"))
            {
                try
                {
                    DebugAddonName ??= "";
                    DebugAddonName = DebugAddonName.Trim();
                    var addonPtr = plugin.Interface.Framework.Gui.GetUiObjectByName(DebugAddonName, 1);
                    plugin.PrintMessage($"{DebugAddonName}=0x{addonPtr.ToInt64():X}");
                    if (addonPtr != IntPtr.Zero)
                    {
                        unsafe
                        {
                            var addon = (AtkUnitBase*)addonPtr;
                            var addr = (long)addon->AtkEventListener.vtbl;
                            var actual = addr - plugin.Interface.TargetModuleScanner.Module.BaseAddress.ToInt64() + 0x140000000;
                            plugin.PrintMessage($"vtbl=0x{addr:X} actual=0x{actual:X}");
                            ImGui.SetClipboardText($"0x{actual:X}");
                            DebugAddonData = $"  Client::UI:Addon{DebugAddonName}:\n    inherits_from: Component::GUI::AtkUnitBase\n    vtbl: 0x{actual:X}\n";
                        }
                    }
                }
                catch (Exception ex)
                {
                    plugin.PrintError(ex.Message);
                }
            }
            ImGui.InputTextMultiline("##AddonData", ref DebugAddonData, 10_000, new(400, 100), ImGuiInputTextFlags.ReadOnly);
        }

        private void UiBuilderDebugLuminaInspector()
        {
            foreach (var data in plugin.ActionDataStorage)
            {
                var aspects = "[" + string.Join(", ", data.Aspects) + "]";
                var effects = "[" + string.Join(", ", data.Effects) + "]";
                ImGui.Text($"{data.ActionID} // #{data.Number} {data.Name} // {data.CastTime} // {data.RecastTime}");
                ImGuiEx.TextTooltip(data.Fluff);
            }
        }

        #endregion
    }

    internal static class ImGuiEx
    {
        public static bool IconButton(FontAwesomeIcon icon) => IconButton(icon);

        public static bool IconButton(FontAwesomeIcon icon, string tooltip)
        {
            ImGui.PushFont(UiBuilder.IconFont);
            var result = ImGui.Button($"{icon.ToIconString()}##{icon.ToIconString()}-{tooltip}");
            ImGui.PopFont();

            if (tooltip != null)
                TextTooltip(tooltip);

            return result;
        }

        public static void TextTooltip(string text)
        {
            if (ImGui.IsItemHovered())
            {
                ImGui.BeginTooltip();
                ImGui.TextUnformatted(text);
                ImGui.EndTooltip();
            }
        }

        public static void WrappedTextTooltip(string text, float wrapWidth)
        {
            if (ImGui.IsItemHovered())
            {
                var style = ImGui.GetStyle();
                var size = ImGui.CalcTextSize(text, wrapWidth);
                size += style.FramePadding * 2;

                ImGui.SetNextWindowSize(size);

                ImGui.BeginTooltip();
                ImGui.TextWrapped(text);
                ImGui.EndTooltip();
            }
        }

        #region rotation

        private static int rotation_start_index;

        public static Vector2 Min(Vector2 lhs, Vector2 rhs) => new(lhs.X < rhs.X ? lhs.X : rhs.X, lhs.Y < rhs.Y ? lhs.Y : rhs.Y);

        public static Vector2 Max(Vector2 lhs, Vector2 rhs) => new(lhs.X >= rhs.X ? lhs.X : rhs.X, lhs.Y >= rhs.Y ? lhs.Y : rhs.Y);

        private static Vector2 Rotate(Vector2 v, float cos_a, float sin_a) => new(v.X * cos_a - v.Y * sin_a, v.X * sin_a + v.Y * cos_a);

        public static void RotateStart()
        {
            rotation_start_index = ImGui.GetWindowDrawList().VtxBuffer.Size;
        }

        public static void RotateEnd(double rad) => RotateEnd(rad, RotationCenter());

        public static void RotateEnd(double rad, Vector2 center)
        {
            var sin = (float)Math.Sin(rad);
            var cos = (float)Math.Cos(rad);
            center = Rotate(center, sin, cos) - center;

            var buf = ImGui.GetWindowDrawList().VtxBuffer;
            for (int i = rotation_start_index; i < buf.Size; i++)
                buf[i].pos = Rotate(buf[i].pos, sin, cos) - center;
        }

        private static Vector2 RotationCenter()
        {
            var l = new Vector2(float.MaxValue, float.MaxValue);
            var u = new Vector2(float.MinValue, float.MinValue);

            var buf = ImGui.GetWindowDrawList().VtxBuffer;
            for (int i = rotation_start_index; i < buf.Size; i++)
            {
                l = Min(l, buf[i].pos);
                u = Max(u, buf[i].pos);
            }

            return new Vector2((l.X + u.X) / 2, (l.Y + u.Y) / 2);
        }

        #endregion
    }
}
