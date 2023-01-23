using Dalamud;
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

        readonly private static Dictionary<ClientLanguage, string> MedicatedStrings = new()
        {
            { ClientLanguage.English, "Medicated" },
            { ClientLanguage.German, "Stärkung" },
            { ClientLanguage.French, "Médicamenté" }
        };

        public static string GetWellFedStatusString()
        {
            return WellFedStrings[Service.ClientState.ClientLanguage];
        }

        public static string GetMedicatedString()
        {
            return MedicatedStrings[Service.ClientState.ClientLanguage];
        }
    }
}
