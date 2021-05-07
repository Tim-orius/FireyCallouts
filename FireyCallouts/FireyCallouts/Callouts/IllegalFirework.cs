using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Rage;
using Rage.Native;
using LSPD_First_Response.Mod.API;
using LSPD_First_Response.Mod.Callouts;
using LSPD_First_Response.Engine.Scripting.Entities;
using FireyCallouts.Utilitys;


namespace FireyCallouts.Callouts {

    [CalloutInfo("Illegal Firework", CalloutProbability.Low)]

    class IllegalFirework : Callout {

        private Random mrRandom = new Random();

        private Ped suspect;
        private Ped witness1, witness2;
        private Vector3 spawnPoint;
        private Vector3 area;
        private Blip locationBlip;
        private LHandle pursuit;
        private bool pursuitCreated = false;
        private bool attacking = false;
        private int decision;

        public override bool OnBeforeCalloutDisplayed() {
            Game.LogTrivial("[FireyCallouts][Log] Initialising 'Illegal Firework' callout.");

            spawnPoint = World.GetNextPositionOnStreet(Game.LocalPlayer.Character.Position.Around(350f));

            ShowCalloutAreaBlipBeforeAccepting(spawnPoint, 30f);
            AddMinimumDistanceCheck(40f, spawnPoint);

            CalloutMessage = "Illegal Firework";
            CalloutPosition = spawnPoint;

            // Initialise peds
            suspect = new Ped(spawnPoint) {
                IsPersistent = true,
                BlockPermanentEvents = true
            };
            suspect.Tasks.Wander();
            // Give weapon to suspect
            suspect.Inventory.GiveNewWeapon(new WeaponAsset("weapon_firework"), 10, true);
            Game.LogTrivial("[FireyCalouts][Debug-log] Firework: Suspect spawned at position " + suspect.Position.ToString());

            witness1 = new Ped(spawnPoint.Around2D(10f));
            witness2 = new Ped(spawnPoint.Around2D(10f));

            witness1.Tasks.Wander();
            witness2.Tasks.Wander();

            decision = mrRandom.Next(0,2);

            Functions.PlayScannerAudioUsingPosition("ASSISTANCE_REQUIRED IN_OR_ON_POSITION", spawnPoint);
            Functions.PlayScannerAudio("UNITS_RESPOND_CODE_03");

            Game.DisplayNotification("web_lossantospolicedept",
                                     "web_lossantospolicedept",
                                     "~y~FireyCallouts",
                                     "~r~Illegal fireworks",
                                     "~w~A caller reported a person shooting with a firework launcher. Respond ~r~Code 3");

            return base.OnBeforeCalloutDisplayed();
        }

        public override bool OnCalloutAccepted() {
            Game.LogTrivial("[FireyCallouts][Log] Accepted 'Illegal Firework' callout.");

            area = spawnPoint.Around2D(1f, 2f);
            locationBlip = new Blip(area, 40f) {
                Color = Color.Yellow
            };
            locationBlip.EnableRoute(Color.Yellow);

            Game.DisplayHelp("Press " + Initialization.endKey.ToString() + " to end the callout at any time.");

            return base.OnCalloutAccepted();
        }

        public override void OnCalloutNotAccepted(){
            Game.LogTrivial("[FireyCallouts][Log] Not accepted 'Illegal Firework' callout.");

            if (suspect.Exists()) suspect.Delete();
            if (witness1.Exists()) witness1.Delete();
            if (witness2.Exists()) witness2.Delete();
            if (locationBlip.Exists()) locationBlip.Delete();

            base.OnCalloutNotAccepted();
            Game.LogTrivial("[FireyCallouts][Log] Cleaned up 'Illegal Firework' callout.");
        }

        public override void Process() {

            GameFiber.StartNew(delegate {
                if (suspect.Exists() && suspect.DistanceTo(Game.LocalPlayer.Character.GetOffsetPosition(Vector3.RelativeFront)) < 40f) {
                    suspect.KeepTasks = true;
                    if(locationBlip.Exists()) locationBlip.Delete();
                    GameFiber.Wait(2000);
                }

                if (suspect.Exists() && suspect.DistanceTo(Game.LocalPlayer.Character.GetOffsetPosition(Vector3.RelativeFront)) < 40f && !attacking){
                    if (decision == 1) {
                        // Set relationships for peds
                        new RelationshipGroup("attacker");
                        new RelationshipGroup("victim");
                        suspect.RelationshipGroup = "attacker";
                        witness1.RelationshipGroup = "victim";
                        witness2.RelationshipGroup = "victim";

                        suspect.KeepTasks = true;

                        // Make suspect shoot with weapon
                        Game.SetRelationshipBetweenRelationshipGroups("attacker", "victim", Relationship.Hate);
                        suspect.Tasks.FightAgainstClosestHatedTarget(1000f);

                        Game.LogTrivial("[FireyCalouts][Debug-log] Firework: suspect attacking");
                        GameFiber.Wait(2000);

                        // Make suspect attack player
                        suspect.Tasks.FightAgainst(Game.LocalPlayer.Character);
                        attacking = true;

                        Game.LogTrivial("[FireyCalouts][Debug-log] Firework: dec-1 finish");
                        GameFiber.Wait(1000);
                    } else {

                        // Create pursuit (if none exists yet)
                        if(!pursuitCreated && suspect.DistanceTo(Game.LocalPlayer.Character.GetOffsetPosition(Vector3.RelativeFront)) < 30f){
                            pursuit = Functions.CreatePursuit();
                            Functions.AddPedToPursuit(pursuit, suspect);
                            Functions.SetPursuitIsActiveForPlayer(pursuit, true);
                            pursuitCreated = true;
                            Game.LogTrivial("[FireyCalouts][Debug-log] Pursuit started");
                        }
                    }
                }

                if (Game.LocalPlayer.Character.IsDead) { End(); }
                if (suspect.Exists()) { if (suspect.IsDead) End(); }
                if (Game.IsKeyDown(Initialization.endKey)) { End(); }
                if (suspect.Exists()) { if (Functions.IsPedArrested(suspect)) End(); }
            }, "IllegalFirework [FireyCallouts]");

            base.Process();
        }

        public override void End() {

            if (suspect.Exists()) { suspect.Dismiss(); }
            if (witness1.Exists()) witness1.Dismiss();
            if (witness2.Exists()) witness2.Dismiss();
            if (locationBlip.Exists()) { locationBlip.Delete(); }

            Functions.PlayScannerAudio("WE_ARE_CODE_FOUR");

            base.End();
            Game.LogTrivial("[FireyCallouts][Log] Cleaned up 'Illegal Firework' callout.");
        }
    }
}
