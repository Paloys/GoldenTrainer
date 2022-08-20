using Celeste.Mod;
using Microsoft.Xna.Framework.Input;

namespace GoldenTrainer
{
    public class GoldenTrainerSettings : EverestModuleSettings
    {

        public bool ActivateMod { get; set; } = false;

        private int _numberOfCompletions = 5;

        [SettingRange(2, 10)]
        public int NumberOfCompletions
        {
            get { return _numberOfCompletions; }
            set
            {
                _numberOfCompletions = value;
                GoldenTrainerModule.Instance.display?.SetDisplayText(GoldenTrainerModule.Instance.CompletionCount + "/" + _numberOfCompletions);
            }
        }

        [DefaultButtonBinding(0, Keys.M)]
        public ButtonBinding ActivateButton { get; set; } = new ButtonBinding(0, Keys.M);

        [DefaultButtonBinding(0, Keys.RightControl)]
        public ButtonBinding DecrementButton { get; set; } = new ButtonBinding(0, Keys.RightControl);

        [DefaultButtonBinding(0, Keys.RightShift)]
        public ButtonBinding IncrementButton { get; set; } = new ButtonBinding(0, Keys.RightShift);

        [SettingSubText("Activating this will respawn you in the room you died if you carried a Golden Berry\nUseful for immediatly praticing a room that made you fail the Golden!\nNote: This setting still works if Activate Mod is turned off (else it'd be pretty useless)")]
        public bool RespawnOnRoomOnGoldenBerryDeath { get; set; } = false;

        public bool SkipCutscenesAutomatically { get; set; } = false;

    }
}
