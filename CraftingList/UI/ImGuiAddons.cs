using Dalamud.Interface;
using Dalamud.Interface.Colors;
using Dalamud.Interface.Style;
using ImGuiNET;
using ImGuiScene;
using ImPlotNET;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace CraftingList.UI
{
    internal static class ImGuiAddons
    {
        static Stack<(Vector2, Vector2)> s_GroupPanelLabelStack = new();
        public static void IconText(FontAwesomeIcon icon)
        {
            ImGui.PushFont(UiBuilder.IconFont);
            ImGui.Text(icon.ToIconString());
            ImGui.PopFont();
        }

        public static void IconTextColored(Vector4 color, FontAwesomeIcon icon)
        {
            ImGui.PushFont(UiBuilder.IconFont);
            ImGui.TextColored(color, icon.ToIconString());
            ImGui.PopFont();

        }

        public static void UnderlinedText(string text)
        {
            ImGui.Text(text);
            Vector2 min = ImGui.GetItemRectMin();
            Vector2 max = ImGui.GetItemRectMax();
            min.Y = max.Y;

            ImGui.GetWindowDrawList().AddLine(min, max, ImGui.GetColorU32(ImGuiCol.Text), 1.0f);
        }

        public static bool BoundedInputInt(string text, ref int val, int min, int max)
        {
            int copy = val;
            if (ImGui.InputInt(text, ref copy, 0, 0))
            {
                if (copy >= min && copy <= max)
                { 
                    val = copy;
                    return true;
                }
            }
            return false;
        }

        public static bool BoundedAutoWidthInputInt(string text, ref int val, int min, int max)
        {
            ImGui.SetNextItemWidth(ImGui.CalcTextSize(val.ToString()).X + 20);
            return BoundedInputInt(text, ref val, min, max);
        }

        public static bool IconButton(FontAwesomeIcon icon, string tooltip, string extraID = "")
        {
            ImGui.PushFont(UiBuilder.IconFont);
            var result = ImGui.Button($"{icon.ToIconString()}##{icon.ToIconString()}-{tooltip}-{extraID}");
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

        internal static unsafe ImGuiListClipperPtr Clipper(int itemsCount)
        {
            ImGuiListClipperPtr guiListClipperPtr = new(ImGuiNative.ImGuiListClipper_ImGuiListClipper());
            // ISSUE: explicit constructor call
            guiListClipperPtr.Begin(itemsCount);
            return guiListClipperPtr;
        }

        internal static void ScaledImageY(
      IntPtr handle,
      int iconWidth,
      int iconHeight,
      float scaledHeight)
        {
            float num = scaledHeight / (float)iconHeight;
            float x = (float)iconWidth * num;
            ImGui.Image(handle, new Vector2(x, scaledHeight));
        }

        internal static unsafe void ToggleSwitch(string id, ref bool val)
        {
            Vector2 p = ImGui.GetCursorScreenPos();
            ImDrawListPtr drawList = ImGui.GetWindowDrawList();

            float height = ImGui.GetFrameHeight();
            float width = height * 1.55f;
            float radius = height * 0.50f;

            if (ImGui.InvisibleButton(id, new Vector2(width, height)))
            {
                val = !val;
            }

            uint col_bg;
            if (ImGui.IsItemHovered())
                col_bg = val ? ImGui.ColorConvertFloat4ToU32(*ImGui.GetStyleColorVec4(ImGuiCol.FrameBgActive)) : ImGui.ColorConvertFloat4ToU32(*ImGui.GetStyleColorVec4(ImGuiCol.FrameBgActive));
            else
                col_bg = val ? ImGui.ColorConvertFloat4ToU32(*ImGui.GetStyleColorVec4(ImGuiCol.FrameBgHovered)) : ImGui.ColorConvertFloat4ToU32(*ImGui.GetStyleColorVec4(ImGuiCol.FrameBgHovered));

            drawList.AddRectFilled(p, new Vector2(p.X + width, p.Y + height), col_bg, height * 0.5f);
            drawList.AddCircleFilled(new Vector2(val ? (p.X + width - radius) : (p.X + radius), p.Y + radius), radius - 1.5f, ImGui.ColorConvertFloat4ToU32(ImGuiColors.DalamudGrey));
        }

        public static void BeginGroupPanel(string name, Vector2 size)
        {
            ImGui.BeginGroup();

            var cursorPos = ImGui.GetCursorScreenPos();
            var itemSpacing = ImGui.GetStyle().ItemSpacing;
            ImGui.PushStyleVar(ImGuiStyleVar.FramePadding, new Vector2(0, 0));
            ImGui.PushStyleVar(ImGuiStyleVar.ItemSpacing, new Vector2(0, 0));

            var frameHeight = ImGui.GetFrameHeight();
            ImGui.BeginGroup();

            Vector2 effectiveSize = size;
            if (size.X < 0f)
                effectiveSize.X = ImGui.GetContentRegionAvail().X;
            ImGui.Dummy(new Vector2(effectiveSize.X, 0));

            ImGui.Dummy(new Vector2(frameHeight * 0.5f, 0f));
            ImGui.SameLine(0f, 0f);
            ImGui.BeginGroup();
            ImGui.Dummy(new Vector2(frameHeight * 0.5f, 0f));
            ImGui.SameLine(0f, 0f);
            ImGui.TextUnformatted(name);
            Vector2 labelMin = ImGui.GetItemRectMin();
            Vector2 labelMax = ImGui.GetItemRectMax();
            ImGui.SameLine(0.0f, 0.0f);
            ImGui.Dummy(new Vector2(0, frameHeight + itemSpacing.Y));
            ImGui.BeginGroup();

            ImGui.PopStyleVar(2);

            var itemWidth = ImGui.CalcItemWidth();
            ImGui.PushItemWidth(Math.Max(0, itemWidth - frameHeight));

            s_GroupPanelLabelStack.Push((labelMin, labelMax));
        }
        public static bool BeginGroupPanelCollapsing(string name, Vector2 size)
        {
            ImGui.BeginGroup();

            var cursorPos = ImGui.GetCursorScreenPos();
            var itemSpacing = ImGui.GetStyle().ItemSpacing;
            ImGui.PushStyleVar(ImGuiStyleVar.FramePadding, new Vector2(0, 0));
            ImGui.PushStyleVar(ImGuiStyleVar.ItemSpacing, new Vector2(0, 0));

            var frameHeight = ImGui.GetFrameHeight();
            ImGui.BeginGroup();

            Vector2 effectiveSize = size;
            if (size.X < 0f)
                effectiveSize.X = ImGui.GetContentRegionAvail().X;
            ImGui.Dummy(new Vector2(effectiveSize.X, 0));

            ImGui.Dummy(new Vector2(frameHeight * 0.5f, 0f));
            ImGui.SameLine(0f, 0f);
            ImGui.BeginGroup();
            ImGui.Dummy(new Vector2(frameHeight * 0.5f, 0f));
            ImGui.SameLine(0f, 0f);
            ImGui.SetNextItemWidth(ImGui.CalcTextSize(name).X);
            bool ret = ImGui.CollapsingHeader(name, ImGuiTreeNodeFlags.DefaultOpen);
            Vector2 labelMin = ImGui.GetItemRectMin();
            Vector2 labelMax = ImGui.GetItemRectMax();
            ImGui.SameLine(0.0f, 0.0f);
            ImGui.Dummy(new Vector2(0, frameHeight + itemSpacing.Y));
            ImGui.BeginGroup();

            ImGui.PopStyleVar(2);

            var itemWidth = ImGui.CalcItemWidth();
            ImGui.PushItemWidth(Math.Max(0, itemWidth - frameHeight));

            s_GroupPanelLabelStack.Push((labelMin, labelMax));
            return ret;
        }

        public unsafe static void EndGroupPanel()
        {
            ImGui.PopItemWidth();

            var itemSpacing = ImGui.GetStyle().ItemSpacing;

            ImGui.PushStyleVar(ImGuiStyleVar.FramePadding, new Vector2(0, 0));
            ImGui.PushStyleVar(ImGuiStyleVar.ItemSpacing, new Vector2(0, 0));

            var frameHeight = ImGui.GetFrameHeight();

            ImGui.EndGroup();

            ImGui.EndGroup();

            ImGui.SameLine(0.0f, 0.0f);
            ImGui.Dummy(new Vector2(frameHeight * 0.5f, 0f));
            ImGui.Dummy(new Vector2(0f, frameHeight - frameHeight * 0.5f - itemSpacing.Y));

            ImGui.EndGroup();

            var itemMin = ImGui.GetItemRectMin();
            var itemMax = ImGui.GetItemRectMax();

            var labelRect = s_GroupPanelLabelStack.Pop();

            Vector2 halfFrame = new Vector2(frameHeight * .25f, frameHeight) * 0.5f;
            var frameRect = (itemMin + halfFrame, itemMax - new Vector2(halfFrame.X, 0f));

            labelRect.Item1.X -= itemSpacing.X;
            labelRect.Item2.X -= itemSpacing.X;
            for (int i = 0; i < 4; i++)
            {

                switch (i)
                {
                    case 0:
                        ImGui.PushClipRect(new Vector2(-float.MaxValue, -float.MaxValue), new Vector2(labelRect.Item1.X, float.MaxValue), true);
                        break;
                    case 1:
                        ImGui.PushClipRect(new Vector2(labelRect.Item2.X, -float.MaxValue), new Vector2(float.MaxValue, float.MaxValue), true);
                        break;
                    case 2:
                        ImGui.PushClipRect(new Vector2(labelRect.Item1.X, -float.MaxValue), new Vector2(labelRect.Item2.X, labelRect.Item1.Y), true);
                        break;
                    case 3:
                        ImGui.PushClipRect(new Vector2(labelRect.Item1.X, labelRect.Item2.Y), new Vector2(labelRect.Item2.X, float.MaxValue), true);
                        break;
                }

                ImGui.GetWindowDrawList().AddRect(
                    frameRect.Item1, frameRect.Item2,
                    ImGui.ColorConvertFloat4ToU32(*ImGui.GetStyleColorVec4(ImGuiCol.Border)),
                    halfFrame.X);

                ImGui.PopClipRect();
            }

            ImGui.PopStyleVar(2);

            ImGui.Dummy(new Vector2(0, 0));
            ImGui.EndGroup();
                
        }
    }
}
