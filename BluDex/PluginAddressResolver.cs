using Dalamud.Game;
using Dalamud.Game.Internal;
using Dalamud.Plugin;
using System;

namespace BluDex
{
    internal delegate IntPtr OnSetupDelegate(IntPtr addon, uint a2, IntPtr dataPtr);

    internal class PluginAddressResolver : BaseAddressResolver
    {
        public IntPtr ThingAddress { get; private set; }

        private const string ThingSignature = "";

        protected override void Setup64Bit(SigScanner scanner)
        {
            // ThingAddress = scanner.ScanText(ThingSignature);

            PluginLog.Verbose("===== BLU DEX =====");
            // PluginLog.Verbose($"{nameof(ThingAddress)} {ThingAddress.ToInt64():X}");
        }
    }

}
