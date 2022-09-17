using Celeste;
using Celeste.Mod;
using Microsoft.Xna.Framework;
using Mono.Cecil;
using Mono.Cecil.Cil;
using Monocle;
using MonoMod.Cil;
using MonoMod.RuntimeDetour;
using System;

namespace GoldenTrainer
{
    public class GoldenTrainerModule : EverestModule
    {
        public static GoldenTrainerModule Instance;

        public GoldenTrainerModule()
        {
            Instance = this;
        }
        
        public override Type SettingsType => typeof(GoldenTrainerSettings);
        public static GoldenTrainerSettings Settings => (GoldenTrainerSettings)Instance._Settings;

        private bool DeathCausedByMod { get; set; }
        
        private bool TransitionedAfterTransitionToCheck { get; set; }

        private int _completionCount;

        public int CompletionCount
        {
            get => _completionCount;
            set
            {
                _completionCount = value;
                if (value == 0)
                {
                    Display.SetDisplayText(_completionCount + "/" + Settings.NumberOfCompletions);
                }
                else
                {
                    Alarm.Set(Display, 1.5f, () => Display.SetDisplayText(_completionCount + "/" + Settings.NumberOfCompletions));
                }
            }
        }


        public CompletionDisplay Display;
        private Level _level;

        private Session.CoreModes _coreMode = Session.CoreModes.None;

        private int _latestSummitCheckpointTriggered = -1;

        private ILHook _dieGoldenHook;
        
        public override void Load()
        {
            Logger.SetLogLevel("GoldenTrainer", LogLevel.Verbose);
            Logger.Log(LogLevel.Info, "GoldenTrainer", "Loading GoldenTrainer Hooks");

            On.Celeste.Level.TransitionTo += RespawnAtEnd;
            Everest.Events.Player.OnDie += ResetUponDeath;
            On.Celeste.LevelLoader.LoadingThread += (orig, self) =>
            {
                orig(self);
                self.Level.Add(Display = new CompletionDisplay(self.Level));
                Display.SetDisplayText(CompletionCount + "/" + Settings.NumberOfCompletions);
                Display.Visible = Settings.ActivateMod;
                _level = self.Level;
            };
            On.Celeste.HeartGem.Collect += RespawnAtEndCrystal;
            On.Celeste.ChangeRespawnTrigger.OnEnter += RespawnAtEndTrigger;
            IL.Celeste.SummitCheckpoint.Update += SummitCheckpointHandler;
            On.Celeste.Session.Restart += OnSessionRestart;
            _dieGoldenHook = new ILHook(typeof(Player).GetMethod("orig_Die"), RespawnInRoomWithBerry);
            On.Celeste.Level.Update += AutoSkipCutscene;
        }


        // Optional, initialize anything after Celeste has initialized itself properly.
        public override void Initialize()
        {
        }

        // Unload the entirety of your mod's content. Free up any native resources.
        public override void Unload()
        {
            IL.Celeste.SummitCheckpoint.Update -= SummitCheckpointHandler;
            _dieGoldenHook?.Dispose();
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
                    Instance.TransitionedAfterTransitionToCheck = true;
                    p.Die(p.Position, true, false);
                }
                else
                {
                    CompletionCount = 0;
                    Audio.Play(SFX.game_07_checkpointconfetti);
                    _coreMode = _level.CoreMode;
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
                    _level.CoreMode = _coreMode;
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
            if (Settings.ActivateMod && self.Target != _level.Session.RespawnPoint)
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
                cursor.EmitDelegate<Func<Player, SummitCheckpoint, bool>>(SummitCheckpointUpdateHook); // Run SummitCheckpointUpdateHook and add Instance.CompletionCount < Settings.NumberOfCompletions && Instance.latestSummitCheckpointTriggered != self.Number && Settings.ActivateMod to the stack
                cursor.Emit(OpCodes.Brfalse, labelNext); // Jump past pop and Br if the returned bool is false
                cursor.Emit(OpCodes.Pop); // Pop the scene if we jump to end
                cursor.Emit(OpCodes.Br, labelEnd); // Jump to end if Instance.CompletionCount < Settings.NumberOfCompletions
                cursor.MarkLabel(labelNext); // Mark Next label to skip pop and Br
                cursor.GotoNext(MoveType.Before, instr => instr.MatchRet()).MarkLabel(labelEnd); // Jump to the end of the function to mark the label
                Logger.Log(LogLevel.Info, "GoldenTrainer", "Added IL hook for SummitCheckpoint.Update()");
            }
        }

        private static bool SummitCheckpointUpdateHook(Player p, SummitCheckpoint self)
        {
            var temp = false;
            if (Instance._latestSummitCheckpointTriggered != self.Number && Settings.ActivateMod && !Instance.TransitionedAfterTransitionToCheck) {
                Instance.CompletionCount++;
                if (Instance.CompletionCount < Settings.NumberOfCompletions)
                {
                    Instance.DeathCausedByMod = true;
                    p.Die(p.Position, true, false);
                }
                else
                {
                    Instance.CompletionCount = 0;
                    Instance._latestSummitCheckpointTriggered = self.Number; // Check because for some reason it triggers twice ???
                }
                temp = Instance.TransitionedAfterTransitionToCheck;
            }
            else if (Instance.TransitionedAfterTransitionToCheck)
            {
                temp = Instance.TransitionedAfterTransitionToCheck;
                Instance.TransitionedAfterTransitionToCheck = false;
            }
            return Instance.CompletionCount < Settings.NumberOfCompletions &&
                   Instance._latestSummitCheckpointTriggered != self.Number && Settings.ActivateMod &&
                   !temp;
        }

        private Session OnSessionRestart(On.Celeste.Session.orig_Restart orig, Session self, string intoLevel = null)
        {
            var session = orig(self, intoLevel);
            _latestSummitCheckpointTriggered = -1;
            _coreMode = Session.CoreModes.None;
            CompletionCount = 0;
            return session;
        }

        private void RespawnInRoomWithBerry(ILContext il)
        {
            ILCursor cursor = new ILCursor(il);
            if (cursor.TryGotoNext(MoveType.After, instr => instr.MatchLdfld(out FieldReference f) && f.Name == "goldenStrawb", instr => instr.OpCode == OpCodes.Brfalse_S))
            {
                Logger.Log(LogLevel.Info, "GoldenTrainer", "Adding IL hook for Player.orig_Die()");
                ILLabel labelEnd = cursor.DefineLabel();
                cursor.EmitDelegate<Func<bool>>(() => Settings.RespawnOnRoomOnGoldenBerryDeath);
                cursor.Emit(OpCodes.Brtrue, labelEnd);
                cursor.GotoNext(MoveType.Before, instr => instr.MatchLdarg(0), instr => instr.MatchCall<Entity>("get_Scene")).MarkLabel(labelEnd);
                Logger.Log(LogLevel.Info, "GoldenTrainer", "Added IL hook for Player.orig_Die()");
            }
        }

        private void AutoSkipCutscene(On.Celeste.Level.orig_Update orig, Level self)
        {
            orig(self);
            if (!self.InCutscene || self.SkippingCutscene || !Settings.SkipCutscenesAutomatically) return;
            self.SkipCutscene();
        }
    }
}