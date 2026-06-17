using Terraria.ModLoader;

namespace TrinyTokMod
{
    // The main class of the mod
    public class TrinyTokMod : Mod
    {
        public static TrinyTokMod Instance { get; private set; }

        public override void Load()
        {
            Instance = this;
            Logger.Info("TrinyTokMod loaded successfully.");
        }

        public override void Unload()
        {
            Instance = null;
        }
    }
}
