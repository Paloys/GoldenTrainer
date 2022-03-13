using Microsoft.Xna.Framework.Input;
using YamlDotNet.Serialization;

namespace Celeste.Mod.Example
{
    public class ExampleModuleSettings : EverestModuleSettings
    {
        // SettingName also works on props, defaulting to
        // modoptions_[typename without settings]_[propname]

        // Example ON / OFF property with a default value.
        public bool ModActivate { get; set; } = false;

        [SettingRange(1, 10)]
        public int NumberOfCompletions { get; set; } = 5;


    }
}
