using Celeste;
using Celeste.Mod;
using Microsoft.Xna.Framework;
using Monocle;
using System;


namespace GoldenTrainer
{
    public class GoldenTrainerModule : EverestModule
    {
        // Only one alive module instance can exist at any given time.
        public static GoldenTrainerModule Instance;

        public GoldenTrainerModule()
        {
            Instance = this;
        }

        // If you need to store settings:
        public override Type SettingsType => typeof(ExampleModuleSettings);
        public static ExampleModuleSettings Settings => (ExampleModuleSettings)Instance._Settings;

        private bool DeathCausedByMod { get; set; } = false;

        private int _completionCount = 0;

        public int CompletionCount
        {
            get { return _completionCount; }
            set
            {
                _completionCount = value;
                display.SetDisplayText(_completionCount + "/" + Settings.NumberOfCompletions);
            }
        }

        public CompletionDisplay display = null;
        public Level level = null;

        // Initialized in LoadContent, after graphics and other assets have been loaded.
        public SpriteBank ExampleSpriteBank;

        // Set up any hooks, event handlers and your mod in general here.
        // Load runs before Celeste itself has initialized properly.
        public override void Load()
        {
            // SetLogLevel will set the *minimum* log level that will be written for logs that use the given prefix string.
            Logger.SetLogLevel("GoldenTrainer", LogLevel.Verbose);
            Logger.Log(LogLevel.Info, "GoldenTrainer", "Loading GoldenTrainer Hooks");
            // The default LogLevel when using Logger.Log is Verbose.
            Logger.Log(LogLevel.Verbose, "GoldenTrainer", "This line would not be logged with SetLogLevel LogLevel.Info");
            Everest.Events.Level.OnTransitionTo += RespawnAtEnd;
            Everest.Events.Player.OnDie += ResetUponDeath;
            On.Celeste.LevelLoader.LoadingThread += (orig, self) =>
            {
                orig(self);
                self.Level.Add(display = new CompletionDisplay(self.Level));
                display.SetDisplayText(CompletionCount + "/" + Settings.NumberOfCompletions);
                display.Visible = Settings.ActivateMod;
                level = self.Level;
            };
            On.Celeste.HeartGem.Collect += RespawnAtEndCrystal;
        }


        // Optional, initialize anything after Celeste has initialized itself properly.
        public override void Initialize()
        {
        }

        // Unload the entirety of your mod's content. Free up any native resources.
        public override void Unload()
        {

        }

        private void RespawnAtEnd(Level level, LevelData next, Vector2 direction)
        {
            if (Settings.ActivateMod)
            {
                CompletionCount++;
                if (CompletionCount < Settings.NumberOfCompletions)
                {
                    Player p = level.Tracker.GetEntity<Player>();
                    Instance.DeathCausedByMod = true;
                    p.Die(p.Position, true, false);
                }
                else
                {
                    CompletionCount = 0;
                    Audio.Play(SFX.game_07_checkpointconfetti);
                }
            }
        }

        private void ResetUponDeath(Player player)
        {
            if (Settings.ActivateMod)
            {
                if (!DeathCausedByMod)
                {
                    CompletionCount = 0;
                }
                DeathCausedByMod = false;
            }
        }

        private void RespawnAtEndCrystal(On.Celeste.HeartGem.orig_Collect orig, HeartGem self, Player player)
        {
            if (Settings.ActivateMod && !self.IsFake)
            {
                CompletionCount++;
                if (CompletionCount < Settings.NumberOfCompletions)
                {
                    Instance.DeathCausedByMod = true;
                    player.Die(player.Position, true, false);
                }
                else
                {
                    CompletionCount = 0;
                    Audio.Play(SFX.game_07_checkpointconfetti);
                }
            }
            orig(self, player);
        }
    }
}
