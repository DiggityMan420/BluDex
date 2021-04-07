using Dalamud.Game.Chat.SeStringHandling.Payloads;
using Dalamud.Game.Command;
using Dalamud.Plugin;
using Lumina.Excel.GeneratedSheets;
using System;
using System.Text;
using System.Linq;
using System.Collections.Generic;
using System.Reflection;
using System.IO;

namespace BluDex
{
    public class BluDexPlugin : IDalamudPlugin
    {
        public string Name => "BluDex";
        public string Command => "/pblu";

        public string AssemblyLocation { get; set; }

        internal BluDexConfiguration Configuration;
        internal DalamudPluginInterface Interface;
        internal PluginAddressResolver Address;
        private PluginUI PluginUi;

        public void Initialize(DalamudPluginInterface pluginInterface)
        {
            Interface = pluginInterface ?? throw new ArgumentNullException(nameof(pluginInterface), "DalamudPluginInterface cannot be null");
            Configuration = pluginInterface.GetPluginConfig() as BluDexConfiguration ?? new BluDexConfiguration();
            AssemblyLocation ??= Assembly.GetExecutingAssembly().Location;

            Interface.CommandManager.AddHandler(Command, new CommandInfo(OnChatCommand)
            {
                HelpMessage = "Open a window to edit various settings.",
                ShowInHelp = true
            });

            Address = new PluginAddressResolver();
            Address.Setup(pluginInterface.TargetModuleScanner);

            try
            {
                LoadData();
            }
            catch (Exception ex)
            {
                PluginLog.Error(ex, "Boom");
            }

            PluginUi = new PluginUI(this);
        }

        public void Dispose()
        {
            Interface.CommandManager.RemoveHandler(Command);

            PluginUi.Dispose();
        }

        internal static byte[] ReadResource(string resourceName)
        {
            var assembly = Assembly.GetExecutingAssembly();
            using Stream stream = assembly.GetManifestResourceStream(resourceName);
            using BinaryReader reader = new(stream);
            return reader.ReadBytes((int)stream.Length);
        }

        internal void PrintMessage(string message) => Interface.Framework.Gui.Chat.Print($"[{Name}] {message}");

        internal void PrintError(string message) => Interface.Framework.Gui.Chat.PrintError($"[{Name}] {message}");

        internal void SaveConfiguration() => Interface.SavePluginConfig(Configuration);

        internal List<ActionData> ActionDataStorage = new();

        private void LoadData()
        {
            var aozActionSheet = Interface.Data.GetExcelSheet<AozAction>();
            var aozActionTransientSheet = Interface.Data.GetExcelSheet<AozActionTransient>();
            var actionTransientSheet = Interface.Data.GetExcelSheet<ActionTransient>();

            foreach (var row in aozActionSheet)
            {
                if (row.RowId == 0)
                    continue;

                var aozActionRow = row.Action.Value;
                var actionTransientRow = actionTransientSheet.GetRow(aozActionRow.RowId);
                var aozActionTransientRow = aozActionTransientSheet.GetRow(row.RowId);

                var statsParts = GetLuminaSeStringParts(aozActionTransientRow.Stats);

                var spellType = (SpellType)Enum.Parse(typeof(SpellType), statsParts[0]);

                var spellAspects = statsParts[1].Split('/')
                    .Select(str => (SpellAspect)Enum.Parse(typeof(SpellAspect), str))
                    .ToArray();

                var spellRank = (SpellRank)statsParts[2].Trim().Length - 1;

                var spellTarget = (SpellTarget)0;
                if (aozActionTransientRow.Unknown8)
                    spellTarget |= SpellTarget.Enemy;
                if (aozActionTransientRow.Unknown9)
                    spellTarget |= SpellTarget.SelfOrAlly;
                if (spellTarget == 0)
                    spellTarget = SpellTarget.Untargetable;

                var spellEffects = new object[] {
                    aozActionTransientRow.Unknown10 ? SpellEffect.Slow : null,
                    aozActionTransientRow.Unknown11 ? SpellEffect.PetrificationAndFreeze : null,
                    aozActionTransientRow.Unknown12 ? SpellEffect.Paralysis : null,
                    aozActionTransientRow.Unknown13 ? SpellEffect.Interruption : null,
                    aozActionTransientRow.Unknown14 ? SpellEffect.Blind : null,
                    aozActionTransientRow.Unknown15 ? SpellEffect.Stun : null,
                    aozActionTransientRow.Unknown16 ? SpellEffect.Sleep : null,
                    aozActionTransientRow.Unknown17 ? SpellEffect.Bind : null,
                    aozActionTransientRow.Unknown18 ? SpellEffect.Heavy : null,
                    aozActionTransientRow.Unknown19 ? SpellEffect.FlatDamageAndDeath : null,
                }.OfType<SpellEffect>().ToArray();

                var iconTex = Interface.Data.GetIcon(aozActionRow.Icon);

                ActionDataStorage.Add(new()
                {
                    ActionID = aozActionRow.RowId,
                    Number = aozActionTransientRow.Number,
                    Name = GetLuminaSeStringText(aozActionRow.Name),
                    Description = GetLuminaSeStringText(actionTransientRow.Description),
                    Fluff = GetLuminaSeStringText(aozActionTransientRow.Description),
                    Rank = spellRank,
                    Type = spellType,
                    Target = spellTarget,
                    Aspects = spellAspects,
                    Effects = spellEffects,
                    IconID = aozActionRow.Icon,
                    CastTime = (SpellCast)aozActionRow.Cast100ms,
                    RecastTime = (SpellRecast)aozActionRow.Recast100ms,
                }); ;
            }

            ActionDataStorage = ActionDataStorage.OrderBy(data => data.Number).ToList();
        }

        private string[] GetLuminaSeStringParts(Lumina.Text.SeString seString)
        {
            var bytes = Encoding.UTF8.GetBytes(seString.RawString);
            var parts = Interface.SeStringManager.Parse(bytes).Payloads
                .OfType<TextPayload>()
                .Where(payload => !payload.Text.StartsWith("\u0003"))
                .Select(payload => payload.Text)
                .ToArray();
            return parts;
        }

        private string GetLuminaSeStringText(Lumina.Text.SeString seString)
        {
            return string.Join("", GetLuminaSeStringParts(seString));
        }

        private void OnChatCommand(string command, string arguments)
        {
            PluginUi.Open();
        }
    }

    internal static class EnumEx
    {
        public static TAttribute GetAttribute<TAttribute>(this Enum value) where TAttribute : Attribute
        {
            var type = value.GetType();
            var name = Enum.GetName(type, value);
            return type.GetField(name)
                       .GetCustomAttributes(false)
                       .OfType<TAttribute>()
                       .SingleOrDefault();
        }
    }

    internal static class LuminaEx
    {
        public static bool TargetsEnemy(this AozActionTransient row) => row.Unknown8;
        public static bool TargetsSelfOrAlly(this AozActionTransient row) => row.Unknown9;
        public static bool CauseSlow(this AozActionTransient row) => row.Unknown10;
        public static bool CausePetrify(this AozActionTransient row) => row.Unknown11;
        public static bool CauseParalysis(this AozActionTransient row) => row.Unknown12;
        public static bool CauseInterrupt(this AozActionTransient row) => row.Unknown13;
        public static bool CauseBlind(this AozActionTransient row) => row.Unknown14;
        public static bool CauseStun(this AozActionTransient row) => row.Unknown15;
        public static bool CauseSleep(this AozActionTransient row) => row.Unknown16;
        public static bool CauseBind(this AozActionTransient row) => row.Unknown17;
        public static bool CauseHeavy(this AozActionTransient row) => row.Unknown18;
        public static bool CauseDeath(this AozActionTransient row) => row.Unknown19;
    }
}
