using Celeste;
using Celeste.Mod;
using Microsoft.Xna.Framework;
using Mono.Cecil.Cil;
using Monocle;
using MonoMod.Cil;
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

        public Session.CoreModes coreMode = Session.CoreModes.None;

        private int latestSummitCheckpointTriggered = -1;

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
            On.Celeste.Level.TransitionTo += RespawnAtEnd;
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
            On.Celeste.ChangeRespawnTrigger.OnEnter += RespawnAtEndTrigger;
            IL.Celeste.SummitCheckpoint.Update += SummitCheckpointHandler;
        }


        // Optional, initialize anything after Celeste has initialized itself properly.
        public override void Initialize()
        {
        }

        // Unload the entirety of your mod's content. Free up any native resources.
        public override void Unload()
        {
            IL.Celeste.SummitCheckpoint.Update -= SummitCheckpointHandler;
        }

        private void RespawnAtEnd(On.Celeste.Level.orig_TransitionTo orig, Level self, LevelData next, Vector2 direction)
        {
            if (Settings.ActivateMod)
            {
                CompletionCount++;
                if (CompletionCount < Settings.NumberOfCompletions)
                {
                    Player p = self.Tracker.GetEntity<Player>();
                    Instance.DeathCausedByMod = true;
                    p.Die(p.Position, true, false);
                }
                else
                {
                    CompletionCount = 0;
                    Audio.Play(SFX.game_07_checkpointconfetti);
                    coreMode = level.CoreMode;
                    orig(self, next, direction);
                }
            }
            else
            {
                orig(self, next, direction);
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
                else
                {
                    level.CoreMode = coreMode;
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
                    DeathCausedByMod = true;
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
        
        private void RespawnAtEndTrigger(On.Celeste.ChangeRespawnTrigger.orig_OnEnter orig, ChangeRespawnTrigger self, Player p)
        {
            if (Settings.ActivateMod && self.Target != level.Session.RespawnPoint)
            {
                CompletionCount++;
                if (CompletionCount < Settings.NumberOfCompletions)
                {
                    DeathCausedByMod = true;
                    p.Die(p.Position, true, false);
                }
                else
                {
                    CompletionCount = 0;
                    Audio.Play(SFX.game_07_checkpointconfetti);
                    orig(self, p);
                }
            }
            else
            {
                orig(self, p);
            }
        }

        private void SummitCheckpointHandler(ILContext il)
        {
            ILCursor cursor = new ILCursor(il);
            if (cursor.TryGotoNext(MoveType.After, instr => instr.MatchCall<Entity>("get_Scene"), instr => instr.MatchIsinst<Level>()))
            {
                ILLabel labelEnd = cursor.DefineLabel();
                ILLabel labelNext = cursor.DefineLabel();
                Logger.Log(LogLevel.Info, "GoldenTrainer", "Adding IL hook for SummitCheckpoint.Update()");
                cursor.Emit(OpCodes.Ldloc_0); // Load Player into stack
                cursor.Emit(OpCodes.Ldarg_0); // Load self into stack
                cursor.EmitDelegate<Func<Player, SummitCheckpoint, bool>>(SummitCheckpointUpdateHook); // Run SummitCheckpointUpdateHook and Add Instance.CompletionCount < Settings.NumberOfCompletions to the stack
                cursor.Emit(OpCodes.Brfalse, labelNext); // Jump past pop and Br
                cursor.Emit(OpCodes.Pop); // Pop the scene if we jump to end
                cursor.Emit(OpCodes.Br, labelEnd); // Jump to end if Instance.CompletionCount < Settings.NumberOfCompletions
                cursor.MarkLabel(labelNext); // Mark Next label to skip pop and Br
                cursor.GotoNext(MoveType.Before, instr => instr.MatchRet()).MarkLabel(labelEnd); // Jump to the end of the function to mark the label
                Logger.Log(LogLevel.Info, "GoldenTrainer", "Added IL hook for SummitCheckpoint.Update()");
            }
        }

        private static bool SummitCheckpointUpdateHook(Player p, SummitCheckpoint self)
        {
            if (Instance.latestSummitCheckpointTriggered != self.Number) {
                Instance.CompletionCount++;
                if (Instance.CompletionCount < Settings.NumberOfCompletions)
                {
                    Instance.DeathCausedByMod = true;
                    p.Die(p.Position, true, false);
                }
                else
                {
                    Instance.CompletionCount = 0;
                    Instance.latestSummitCheckpointTriggered = self.Number; // Check because for some reason it triggers twice ???
                }
            }
            return Instance.CompletionCount < Settings.NumberOfCompletions && Instance.latestSummitCheckpointTriggered != self.Number;
        }
    }
}
