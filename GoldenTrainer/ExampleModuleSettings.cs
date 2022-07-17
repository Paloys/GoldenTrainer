using Celeste.Mod;
using Microsoft.Xna.Framework.Input;

namespace GoldenTrainer
{
    public class ExampleModuleSettings : EverestModuleSettings
    {
        // SettingName also works on props, defaulting to
        // modoptions_[typename without settings]_[propname]

        // Example ON / OFF property with a default value.
        public bool ActivateMod { get; set; } = false;

        private int _numberOfCompletions = 5;

        [SettingRange(2, 10)]
        public int NumberOfCompletions
        {
            get { return _numberOfCompletions; }
            set
            {
                _numberOfCompletions = value;
                GoldenTrainerModule.Instance.display.SetDisplayText(GoldenTrainerModule.Instance.CompletionCount + "/" + _numberOfCompletions);
            }
        }

        [DefaultButtonBinding(0, Keys.M)]
        public ButtonBinding ActivateButton { get; set; } = new ButtonBinding(0, Keys.M);

        [DefaultButtonBinding(0, Keys.RightControl)]
        public ButtonBinding DecrementButton { get; set; } = new ButtonBinding(0, Keys.RightControl);

        [DefaultButtonBinding(0, Keys.RightShift)]
        public ButtonBinding IncrementButton { get; set; } = new ButtonBinding(0, Keys.RightShift);

        // TODO: Implement this
        //public bool SkipCutscencesAutomatically = true;

    }
}
