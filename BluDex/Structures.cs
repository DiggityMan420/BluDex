using System;

namespace BluDex
{
    [Flags]
    internal enum SpellTarget
    {
        [UiData(15336, "Untargetable")] Untargetable = 1 << 0,
        [UiData(15338, "Targets Self or Ally")] SelfOrAlly = 1 << 1,
        [UiData(15339, "Targets Enemy")] Enemy = 1 << 2,
        [UiData(-1, "Targets Self, Ally, or Enemy")] SelfAllyOrEnemy = SelfOrAlly | Enemy,
    }

    internal enum SpellType
    {
        [UiData(15050)] Physical,
        [UiData(15054, "Magical")] Magic,
    }

    internal enum SpellAspect
    {
        [UiData(16018, "Unaspected")] None,
        [UiData(15535)] Blunt,
        [UiData(15536)] Piercing,
        [UiData(15537)] Slashing,
        [UiData(15100)] Fire,
        [UiData(15101)] Ice,
        [UiData(15102)] Wind,
        [UiData(15103)] Earth,
        [UiData(15104)] Lightning,
        [UiData(15105)] Water,
    }

    internal enum SpellEffect
    {
        [UiData(72461)] Slow,
        [UiData(72462, "Petrification/Freeze")] PetrificationAndFreeze,
        [UiData(72463)] Paralysis,
        [UiData(72464)] Interruption,
        [UiData(72465)] Blind,
        [UiData(72466)] Stun,
        [UiData(72467)] Sleep,
        [UiData(72468)] Bind,
        [UiData(72469)] Heavy,
        [UiData(72470, "Flat Damage/Death")] FlatDamageAndDeath,
    }

    internal enum SpellRank
    {
        [UiData(19381, "★")] One,
        [UiData(19382, "★★")] Two,
        [UiData(19383, "★★★")] Three,
        [UiData(19384, "★★★★")] Four,
        [UiData(19385, "★★★★★")] Five
    }

    internal enum SpellCast
    {
        [UiData("0s")] s0 = 0,
        [UiData("1s")] s1 = 10,
        [UiData("1.5s")] s1_5 = 15,
        [UiData("2s")] s2 = 20,
        [UiData("3s")] s3 = 30,
        [UiData("6s")] s6 = 60,
        [UiData("10s")] s10 = 100,
    }

    internal enum SpellRecast
    {
        [UiData("2.5s")] s2_5 = 25,
        [UiData("30s")] s30 = 300,
        [UiData("60s")] s60 = 600,
        [UiData("90s")] s90 = 900,
        [UiData("120s")] s120 = 1200,
        [UiData("180s")] s180 = 1800,
        [UiData("300s")] s300 = 3000,
    }

    internal class UiDataAttribute : Attribute
    {
        public int IconID { get; private set; }

        public string Text { get; private set; }

        public UiDataAttribute(int iconID) : this(iconID, null) { }

        public UiDataAttribute(string text) : this(0, text) { }

        public UiDataAttribute(int iconID, string text)
        {
            IconID = iconID;
            Text = text;
        }
    }

    internal struct ActionData
    {
        public uint ActionID;
        public uint Number;
        public int IconID;
        public string Name;
        public string Description;
        public string Fluff;
        public SpellRank Rank;
        public SpellTarget Target;
        public SpellType Type;
        public SpellAspect[] Aspects;
        public SpellEffect[] Effects;
        public SpellCast CastTime;
        public SpellRecast RecastTime;
    }
}
