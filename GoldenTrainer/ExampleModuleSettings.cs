using Microsoft.Xna.Framework.Input;
using YamlDotNet.Serialization;
using Celeste.Mod;

namespace GoldenTrainer
{
    public class ExampleModuleSettings : EverestModuleSettings
    {
        // SettingName also works on props, defaulting to
        // modoptions_[typename without settings]_[propname]

        // Example ON / OFF property with a default value.
        public bool ActivateMod { get; set; } = false;

        [SettingRange(1, 10)]
        public int NumberOfCompletions { get; set; } = 5;

        // TODO: Implement this
        //public bool SkipCutscencesAutomatically = true;

    }
}
