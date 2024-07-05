using Dalamud;
using Dalamud.Game;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CraftingList.Utility
{
    internal class Localization
    {
        readonly private static Dictionary<ClientLanguage, string> WellFedStrings = new()
        {
            { ClientLanguage.English, "Well Fed" },
            { ClientLanguage.German, "Gut Gesättigt"},
            { ClientLanguage.French, "Repu" }
        };

        readonly private static Dictionary<ClientLanguage, string> RepairCommandStrings = new()
        {
            { ClientLanguage.English, "repair" },
            { ClientLanguage.Japanese, "修理" }
        };

        readonly private static Dictionary<ClientLanguage, string> MedicatedStrings = new()
        {
            { ClientLanguage.English, "Medicated" },
            { ClientLanguage.German, "Stärkung" },
            { ClientLanguage.French, "Médicamenté" }
        };

        public static string GetWellFedStatusString()
        {
            return WellFedStrings.TryGetValue(Service.ClientState.ClientLanguage, out string? ret) ? ret! : WellFedStrings[ClientLanguage.English];
        }

        public static string GetMedicatedString()
        {
            return MedicatedStrings.TryGetValue(Service.ClientState.ClientLanguage, out string? ret) ? ret! : WellFedStrings[ClientLanguage.English];
        }

        public static string GetRepairString()
        {
            return RepairCommandStrings.TryGetValue(Service.ClientState.ClientLanguage, out string? ret) ? ret! : WellFedStrings[ClientLanguage.English];
        }
    }
}
