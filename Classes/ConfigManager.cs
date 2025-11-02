using BepInEx.Configuration;

namespace GorillaMedia
{
    public class ConfigManager
    {
        public static ConfigEntry<int> BackgroundIndex { get; private set; }
        public static ConfigEntry<string> HandChoice { get; private set; }

        public static void LoadConfig(ConfigFile Config)
        {
            BackgroundIndex = Config.Bind<int>("GorillaMedia", "BackgroundColor", 0,
@"The background color of the media UI. This does not affect usage.
0: Default
1: YouTube
2: iTunes
3: VLC Media Player");

            HandChoice = Config.Bind<string>("GorillaMedia", "Hand", "Left",
@"The hand to use for the media UI.
Options: Left, Right
Default: Left");
        }
    }
}