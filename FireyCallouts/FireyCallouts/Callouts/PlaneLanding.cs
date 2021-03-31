﻿using System;
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

    [CalloutInfo("Plane Landing", CalloutProbability.Low)]

    class PlaneLanding : Callout {

        private Random mrRandom = new Random();

        private Ped suspect;
        private Vehicle suspectVehicle;
        private Vector3 spawnPoint;
        private Vector3 landPoint;
        private Blip suspectBlip;
        private List<Vector3> landLocations = new List<Vector3>() { new Vector3(1772.492f, 2088.625f, 66.50783f), // Near prison
                                                                    new Vector3(77.06483f, -1230.351f, 37.81571f)}; // Highway-bridge right south of simeons

        private List<Vector3> spawnLocations = new List<Vector3>() { new Vector3(1891.882f, 3130.54f, 308.6497f), // Straight line north of landing point
                                                                     new Vector3(-948.3423f, -1253.808546f, 382.55003f)}; // Straight line west of landing point

        private readonly float flySpeed = 80f;
        private int seat = 1;
        private int landingSituation;

        private string[] planeModels = new string[] { "velum", "velum2", "vestra", "dodo", "duster", "mammatus" };
        private bool willCrash = false;

        private List<Vehicle> emergencyVehicles = new List<Vehicle>();
        private List<Ped[]> emergencyPeds = new List<Ped[]>();

        public override bool OnBeforeCalloutDisplayed() {
            Game.LogTrivial("[FireyCallouts][Log] Initialising 'Plane Landing' callout.");

            // Check distance of player to landing locations
            
            List<Vector3> possibleLandLocations = new List<Vector3>();
            List<Vector3> possibleSpawnLocations = new List<Vector3>();
            Vector3 l;
            for (int zz = 0; zz < landLocations.Count; zz++) {
                l = landLocations[zz];
                if (l.DistanceTo(Game.LocalPlayer.Character.GetOffsetPosition(Vector3.RelativeFront)) < 800f) {
                    possibleLandLocations.Add(l);
                    possibleSpawnLocations.Add(spawnLocations[zz]);
                }
            }

            if (possibleLandLocations.Count < 1) {
                Game.LogTrivial("[FireyCallouts][Log] Distance to callout scene point too far.");
                OnCalloutNotAccepted();
                return false;
            }
            

            int locationDecision = mrRandom.Next(0, landLocations.Count);
            int decision;

            landPoint = possibleLandLocations[locationDecision];
            spawnPoint = possibleSpawnLocations[locationDecision];

            Game.LogTrivial("[FireyCallouts][Debug] Locations set.");

            ShowCalloutAreaBlipBeforeAccepting(landPoint, 30f);
            AddMinimumDistanceCheck(40f, landPoint);

            CalloutMessage = "Plane doing emergency landing";
            CalloutPosition = landPoint;

            // Initialise vehicle
            decision = mrRandom.Next(0, planeModels.Length);
            suspectVehicle = new Vehicle(planeModels[decision], spawnPoint);
            suspectVehicle.IsPersistent = true;
            suspectVehicle.Face(landPoint);
            suspectVehicle.IsEngineOn = true;

            // Initialise ped
            suspect = suspectVehicle.CreateRandomDriver();
            suspect.IsPersistent = true;

            suspect.Tasks.LandPlane(suspectVehicle, spawnPoint, landPoint);
            landingSituation = 0;

            Game.LogTrivial("[FireyCallouts][Debug] Vehicle & pilot set.");

            // Decide wether the plane will crash (explode)
            decision = mrRandom.Next(0, 2);
            if (decision == 1) {
                willCrash = true;
            }

            Functions.PlayScannerAudioUsingPosition("ASSISTANCE_REQUIRED IN_OR_ON_POSITION", spawnPoint);
            Functions.PlayScannerAudio("UNITS_RESPOND_CODE_03");

            return base.OnBeforeCalloutDisplayed();
        }

        public override bool OnCalloutAccepted() {
            Game.LogTrivial("[FireyCallouts][Log] Accepted 'Plane Landing' callout.");

            // Show route for player
            suspectBlip = suspectVehicle.AttachBlip();
            suspectBlip.Color = Color.Yellow;
            suspectBlip.EnableRoute(Color.Yellow);

            return base.OnCalloutAccepted();
        }

        public override void OnCalloutNotAccepted() {
            Game.LogTrivial("[FireyCallouts][Log] Not accepted 'Plane Landing' callout.");

            // Clean up if not accepted
            if (suspect.Exists()) suspect.Delete();
            if (suspectVehicle.Exists()) suspectVehicle.Delete();
            if (suspectBlip.Exists()) suspectBlip.Delete();

            // Remove emergency services
            foreach (Vehicle ev in emergencyVehicles) {
                if (ev.Exists()) { ev.Delete(); }
            }
            foreach (Ped[] epl in emergencyPeds) {
                if (epl.Length > 0) {
                    foreach (Ped ep in epl) {
                        if (ep.Exists()) { ep.Delete(); }
                    }
                }
            }

            base.OnCalloutNotAccepted();
            Game.LogTrivial("[FireyCallouts][Log] Cleaned up 'Plane Landing' callout.");
        }

        public override void Process() {

            base.Process();

            GameFiber.StartNew(delegate {

                // Distance plane - player is < 40
                if (suspect.Exists() && suspect.DistanceTo(Game.LocalPlayer.Character.GetOffsetPosition(Vector3.RelativeFront)) < 40f) {
                    suspect.KeepTasks = true;
                    //if (suspectBlip.Exists()) suspectBlip.Delete();
                    GameFiber.Wait(2000);
                }

                // Plane touchdown
                if (suspectVehicle.Exists() && suspect.Exists() && suspectVehicle.DistanceTo(landPoint) < 5f && (landingSituation == 0)) {
                    // Explode if the plane is dedicated to crash
                    if(willCrash) {
                        suspect.Tasks.CruiseWithVehicle(10f);
                        suspectVehicle.Explode();
                    }
                    landingSituation = 1;
                    Game.LogTrivial("[FireyCallouts][Debug] Landed.");
                    GameFiber.Wait(3000);
                }

                if (Game.LocalPlayer.Character.IsDead) End();
                if (Game.IsKeyDown(Initialization.endKey)) End();
                if (suspect.Exists()) { if (suspect.IsDead) End(); }
                if (suspect.Exists()) { if (Functions.IsPedArrested(suspect)) End(); }
            }, "PlaneLanding [FireyCallouts]");
        }

        public override void End() {

            if (suspect.Exists()) { suspect.Dismiss(); }
            if (suspectVehicle.Exists()) { suspectVehicle.Dismiss(); }
            if (suspectBlip.Exists()) { suspectBlip.Delete(); }

            foreach (Vehicle ev in emergencyVehicles) {
                if (ev.Exists()) { ev.Dismiss(); }
            }
            foreach (Ped[] epl in emergencyPeds) {
                if (epl.Length > 0) {
                    foreach (Ped ep in epl) {
                        if (ep.Exists()) { ep.Dismiss(); }
                    }
                }
            }

            Functions.PlayScannerAudio("WE_ARE_CODE_4");

            base.End();
            Game.LogTrivial("[FireyCallouts][Log] Cleaned up 'Plane Landing' callout.");
        }
    }
}
