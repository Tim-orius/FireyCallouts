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


namespace FireyCallouts.Callouts {

    [CalloutInfo("Illegal Firework", CalloutProbability.VeryHigh)]

    class IllegalFirework : Callout {

        Random mrRandom = new Random();

        private Ped suspect;
        private Ped dummy1, dummy2;
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

            suspect = new Ped(spawnPoint.Around(10f));
            suspect.IsPersistent = true;
            suspect.BlockPermanentEvents = true;
            suspect.Tasks.Wander();
            suspect.Inventory.GiveNewWeapon(new WeaponAsset("weapon_firework"), 10, true);

            dummy1 = new Ped(spawnPoint);
            dummy2 = new Ped(spawnPoint);

            dummy1.Tasks.Wander();
            dummy2.Tasks.Wander();

            decision = mrRandom.Next(0,2);

            Functions.PlayScannerAudioUsingPosition("ASSISTANCE_REQUIRED IN_OR_ON_POSITION", spawnPoint);

            return base.OnBeforeCalloutDisplayed();
        }

        public override bool OnCalloutAccepted() {
            Game.LogTrivial("[FireyCallouts][Log] Accepted 'Illegal Firework' callout.");

            area = spawnPoint.Around2D(1f, 2f);
            locationBlip = new Blip(area, 40f);
            locationBlip.Color = Color.Yellow;
            locationBlip.EnableRoute(Color.Yellow);

            return base.OnCalloutAccepted();
        }

        public override void OnCalloutNotAccepted(){
            Game.LogTrivial("[FireyCallouts][Log] Not accepted 'Illegal Firework' callout.");

            if(suspect.Exists()) suspect.Delete();
            if(locationBlip.Exists()) locationBlip.Delete();

            base.OnCalloutNotAccepted();
            Game.LogTrivial("[FireyCallouts][Log] Cleaned up 'Illegal Firework' callout.");
        }

        public override void Process() {

            base.Process();

            GameFiber.StartNew(delegate {
                if (suspect.Exists() && suspect.DistanceTo(Game.LocalPlayer.Character.GetOffsetPosition(Vector3.RelativeFront)) < 40f) {
                    suspect.KeepTasks = true;
                    if(locationBlip.Exists()) locationBlip.Delete();
                    GameFiber.Wait(2000);
                }

                if (suspect.Exists() && suspect.DistanceTo(Game.LocalPlayer.Character.GetOffsetPosition(Vector3.RelativeFront)) < 40f && !attacking){
                    if (decision == 1)
                    {
                        new RelationshipGroup("attacker");
                        new RelationshipGroup("victim");
                        suspect.RelationshipGroup = "attacker";
                        dummy1.RelationshipGroup = "victim";
                        dummy2.RelationshipGroup = "victim";

                        suspect.KeepTasks = true;
                        Game.SetRelationshipBetweenRelationshipGroups("attacker", "victim", Relationship.Hate);
                        suspect.Tasks.FightAgainstClosestHatedTarget(1000f);
                        GameFiber.Wait(1000);

                        suspect.Tasks.FightAgainst(Game.LocalPlayer.Character);
                        attacking = true;
                        GameFiber.Wait(600);
                    }
                    else {
                        if(!pursuitCreated && suspect.DistanceTo(Game.LocalPlayer.Character.GetOffsetPosition(Vector3.RelativeFront)) < 30f){
                            pursuit = Functions.CreatePursuit();
                            Functions.AddPedToPursuit(pursuit, suspect);
                            Functions.SetPursuitIsActiveForPlayer(pursuit, true);
                            pursuitCreated = true;
                        }
                    }
                }

                if (Game.LocalPlayer.Character.IsDead) End();
                if (suspect.IsDead) End();
                if (Game.IsKeyDown(System.Windows.Forms.Keys.Delete)) End();
                if (Functions.IsPedArrested(suspect)) End();
            }, "IllegalFirework [FireyCallouts]");
        }

        public override void End() {

            if (suspect.Exists()) { suspect.Dismiss(); }
            if (locationBlip.Exists()) { locationBlip.Delete(); }

            Functions.PlayScannerAudio("WE_ARE_CODE FOUR");

            base.End();
            Game.LogTrivial("[FireyCallouts][Log] Cleaned up 'Illegal Firework' callout.");
        }
    }
}
