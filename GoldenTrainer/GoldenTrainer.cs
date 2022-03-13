using Microsoft.Xna.Framework;
using Monocle;
using System;


namespace Celeste.Mod.Example
{
    public class GoldenTrainer : EverestModule
    {
        // Only one alive module instance can exist at any given time.
        public static GoldenTrainer Instance;

        public GoldenTrainer()
        {
            Instance = this;
        }

        // If you need to store settings:
        public override Type SettingsType => typeof(ExampleModuleSettings);
        public static ExampleModuleSettings Settings => (ExampleModuleSettings)Instance._Settings;

        public int CompletionCount { get; set; } = 0;


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
            if (Settings.ModActivate)
            {
                Instance.CompletionCount++;
                if (Instance.CompletionCount < Settings.NumberOfCompletions)
                {
                    Player p = level.Tracker.GetEntity<Player>();
                    p.Die(p.Position, true, false);
                }
                else if (Instance.CompletionCount == Settings.NumberOfCompletions)
                {
                    Instance.CompletionCount = 0;
                }
            }
        }
            
    }
}