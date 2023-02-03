using Dalamud.Interface;
using FFXIVClientStructs.FFXIV.Common.Math;
using ImGuiNET;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CraftingList.UI
{
    internal static class ImGuiAddons
    {
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
    }
}
