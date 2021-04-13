using Dalamud.Game.Command;
using Dalamud.Game.Text.SeStringHandling.Payloads;
using Dalamud.Plugin;
using Lumina.Excel.GeneratedSheets;
using System;
using System.Text;
using System.Linq;
using System.Collections.Generic;
using System.Reflection;
using System.IO;
using System.Runtime.InteropServices;

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

        internal List<ActionData> ActionDataStorage = new();

        private IsActionUnlockedDelegate IsActionUnlocked;

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

            IsActionUnlocked = Marshal.GetDelegateForFunctionPointer<IsActionUnlockedDelegate>(Address.IsActionUnlockedAddress);

            LoadData();

            if (Interface.ClientState.LocalPlayer != null)
                RefreshUnlockedSpells();

            Interface.ClientState.OnLogin += RefreshUnlockedSpells_OnLogin;
            Interface.ClientState.TerritoryChanged += RefreshUnlockedSpells_OnTerritoryChanged;

            PluginUi = new PluginUI(this);
        }

        public void Dispose()
        {
            Interface.CommandManager.RemoveHandler(Command);

            Interface.ClientState.TerritoryChanged -= RefreshUnlockedSpells_OnTerritoryChanged;
            Interface.ClientState.OnLogin -= RefreshUnlockedSpells_OnLogin;

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

        internal void RefreshUnlockedSpells_OnLogin(object sender, EventArgs e) => RefreshUnlockedSpells();

        internal void RefreshUnlockedSpells_OnTerritoryChanged(object sender, ushort territoryID) => RefreshUnlockedSpells();

        internal void RefreshUnlockedSpells()
        {
            foreach (var data in ActionDataStorage)
                data.IsUnlocked = IsActionUnlocked(Address.ClientGameUiHotbarAddress, data.UnlockLink);
        }

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

                var spellAspect = statsParts[1].Split('/')
                    .Select(str => (SpellAspect)Enum.Parse(typeof(SpellAspect), str))
                    .Aggregate((a, b) => a | b);

                var spellRank = (SpellRank)statsParts[2].Trim().Length - 1;

                var spellTarget = (SpellTarget)0;
                if (aozActionTransientRow.TargetsEnemy)
                    spellTarget |= SpellTarget.Enemy;
                if (aozActionTransientRow.TargetsSelfOrAlly)
                    spellTarget |= SpellTarget.SelfOrAlly;
                if (spellTarget == 0)
                    spellTarget = SpellTarget.Untargetable;

                var spellEffects = new object[] {
                    aozActionTransientRow.CauseSlow ? SpellEffect.Slow : null,
                    aozActionTransientRow.CausePetrify ? SpellEffect.PetrificationAndFreeze : null,
                    aozActionTransientRow.CauseParalysis ? SpellEffect.Paralysis : null,
                    aozActionTransientRow.CauseInterrupt ? SpellEffect.Interruption : null,
                    aozActionTransientRow.CauseBlind? SpellEffect.Blind : null,
                    aozActionTransientRow.CauseStun ? SpellEffect.Stun : null,
                    aozActionTransientRow.CauseSleep ? SpellEffect.Sleep : null,
                    aozActionTransientRow.CauseBind ? SpellEffect.Bind : null,
                    aozActionTransientRow.CauseHeavy ? SpellEffect.Heavy : null,
                    aozActionTransientRow.CauseDeath ? SpellEffect.FlatDamageAndDeath : null,
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
                    Aspect = spellAspect,
                    Effects = spellEffects,
                    IconID = aozActionRow.Icon,
                    CastTime = (SpellCast)aozActionRow.Cast100ms,
                    RecastTime = (SpellRecast)aozActionRow.Recast100ms,
                    UnlockLink = aozActionRow.UnlockLink,
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
}
