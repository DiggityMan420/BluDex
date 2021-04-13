using Dalamud.Game;
using Dalamud.Game.Internal;
using Dalamud.Plugin;
using System;

namespace BluDex
{
    internal delegate bool IsActionUnlockedDelegate(IntPtr hotbarModule, uint unlockLink);

    internal class PluginAddressResolver : BaseAddressResolver
    {
        public IntPtr ActiveSpellsAddress { get; private set; }
        public IntPtr ClientGameUiHotbarAddress { get; private set; }
        public IntPtr IsActionUnlockedAddress { get; private set; }

        private const string ActionManagerSignature = // g_ActionManager
            "48 8D 0D ?? ?? ?? ?? 48 8B D5 E8 ?? ?? ?? ?? 84 C0";
        private const string ClientGameUiHotbarSignature = // Client::Game::UI::Hotbar
            "48 8D 0D ?? ?? ?? ?? E8 ?? ?? ?? ?? 84 C0 0F 84 ?? ?? ?? ?? 49 8B CD E8 ?? ?? ?? ??";
        private const string IsActionUnlockedSignature = // Client::Game::UI::Hotbar.IsActionUnlocked
            "4C 8B C9 8B C2 48 C1 E8 03";

        protected override void Setup64Bit(SigScanner scanner)
        {
            ActiveSpellsAddress = scanner.GetStaticAddressFromSig(ActionManagerSignature, 316);
            ClientGameUiHotbarAddress = scanner.GetStaticAddressFromSig(ClientGameUiHotbarSignature, 1);
            IsActionUnlockedAddress = scanner.ScanText(IsActionUnlockedSignature);
            
            PluginLog.Verbose("===== BLU DEX =====");
            PluginLog.Verbose($"{nameof(ClientGameUiHotbarAddress)} {ClientGameUiHotbarAddress.ToInt64():X}");
            PluginLog.Verbose($"{nameof(IsActionUnlockedAddress)} {IsActionUnlockedAddress.ToInt64():X}");
        }
    }
}
