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

    [CalloutInfo("Burning Truck", CalloutProbability.Low)]

    class BurningTruck : Callout {

        private Random mrRandom = new Random();

        private Ped suspect;
        private Vehicle suspectVehicle;
        private Vector3 spawnPoint;
        private Blip locationBlip;
        private LHandle pursuit;
        private readonly string[] truckModels = new string[] { "mule", "pounder", "biff", "mixer", "mixer2", "rubble", "tiptruck",
                                                      "tiptruck2", "trash", "boxville", "benson", "barracks"};
        private bool willExplode = false;
        private int dialogueCount = 0;
        private bool suspectDialogueComplete = false;
        private bool pursuitCreated = false;
        private bool notificationShown = false;
        private bool otherNotificationShown = false;
        private bool madeTruckSmoke = false;

        private List<string[]> dialoguesSuspect = new List<string[]>() { new string[] { "~y~Suspect: ~w~Hello Officer, I am on my way to the workshop.",
                                                                                        "~y~You: ~w~That's good but it would be better if you had the truck towed.",
                                                                                        "~y~Suspect: ~w~Okay. Can you call a tow truck please? I have no phone with me."},
                                                                        new string[] { "~y~Suspect: ~w~Hello Officer.", "~y~You: ~w~Hello. Have you noticed that your truck is smoking under the hood?"},
                                                                        new string[] { "~y~Suspect: ~w~Oh yes, but that is no problem.", "~y~You: ~w~It sure is a problem. You can't drive with a smoking engine.",
                                                                                        "~y~Suspect: ~w~Okay okay. Then please call a tow truck for me."},
                                                                        new string[] { "~y~Suspect: ~w~It's all fine. There is no problem.", "The smoke sure is a problem. The truck needs to be towed.",
                                                                                       "~y|Suspect: ~w~Fine. I can't call a tow truck though." },
                                                                        new string[] { "~y~Suspect: ~w~I'm just on my way to the next workshop to fix that." } };

        int selector, dialogueEndChoice, dialoguePoint;

        public override bool OnBeforeCalloutDisplayed() {
            Game.LogTrivial("[FireyCallouts][Log] Initialising 'Burning Truck' callout.");

            spawnPoint = World.GetNextPositionOnStreet(Game.LocalPlayer.Character.Position.Around(350f));

            ShowCalloutAreaBlipBeforeAccepting(spawnPoint, 30f);
            AddMinimumDistanceCheck(40f, spawnPoint);

            CalloutMessage = "Burning Truck";
            CalloutPosition = spawnPoint;

            // Initialise ped and vehicle
            int decision = mrRandom.Next(0, truckModels.Length);
            suspectVehicle = new Vehicle(truckModels[decision], spawnPoint);
            suspectVehicle.IsPersistent = true;

            suspect = suspectVehicle.CreateRandomDriver();
            suspect.IsPersistent = true;
            suspect.BlockPermanentEvents = true;
            suspect.Tasks.CruiseWithVehicle(15f);

            decision = mrRandom.Next(0, 2);
            if (decision == 1) {
                willExplode = true;
            }

            selector = mrRandom.Next(0, 2);
            dialogueEndChoice = mrRandom.Next(2, 5);
            dialoguePoint = selector;

            Functions.PlayScannerAudioUsingPosition("ASSISTANCE_REQUIRED IN_OR_ON_POSITION", spawnPoint);
            Functions.PlayScannerAudio("UNITS_RESPOND_CODE_03");

            Game.DisplayNotification("web_lossantospolicedept",
                                     "web_lossantospolicedept",
                                     "~y~FireyCallouts",
                                     "~r~Truck smoking",
                                     "~w~Several callers report a truck driving around with smoke coming out of the engine. Respond ~r~Code 3");

            return base.OnBeforeCalloutDisplayed();
        }

        public override bool OnCalloutAccepted() {
            Game.LogTrivial("[FireyCallouts][Log] Accepted 'Burning Truck' callout.");

            locationBlip = suspectVehicle.AttachBlip();
            locationBlip.Color = Color.Yellow;
            locationBlip.EnableRoute(Color.Yellow);

            Game.DisplayHelp("Press " + Initialization.endKey.ToString() + " to end the callout at any time.");

            return base.OnCalloutAccepted();
        }

        public override void OnCalloutNotAccepted(){
            Game.LogTrivial("[FireyCallouts][Log] Not accepted 'Burning Truck' callout.");

            if (suspect.Exists()) suspect.Delete();
            if (suspectVehicle.Exists()) suspectVehicle.Delete();
            if (locationBlip.Exists()) locationBlip.Delete();

            base.OnCalloutNotAccepted();
            Game.LogTrivial("[FireyCallouts][Log] Cleaned up 'Burning Truck' callout.");
        }

        public override void Process() {

            base.Process();

            GameFiber.StartNew(delegate {
                if (suspect.Exists() && suspect.DistanceTo(Game.LocalPlayer.Character.GetOffsetPosition(Vector3.RelativeFront)) < 40f) {
                    
                    if (!notificationShown) {
                        suspect.KeepTasks = true;
                        Game.LogTrivial("[FireyCalouts][Debug-log] Truck burning: Pol mode notice");

                        Game.DisplayHelp("Press " + Initialization.dialogueKey.ToString() + " to show dialogues.");

                        notificationShown = true;
                    } else if (!notificationShown) {
                        suspect.Tasks.PerformDrivingManeuver(suspectVehicle, VehicleManeuver.GoForwardStraightBraking, 100);
                        Game.LogTrivial("[FireyCalouts][Debug-log] Truck burning: Fire mode notice 1");

                        notificationShown = true;
                    }

                    // Make the truck burn
                    if (suspectVehicle.Exists() && !madeTruckSmoke) {
                        suspectVehicle.EngineHealth = 0;
                        Game.LogTrivial("[FireyCalouts][Debug-log] Truck burning: Now smoking");
                        GameFiber.Wait(5000);

                        if (suspect.Exists() && !otherNotificationShown) {
                            otherNotificationShown = true;
                            Game.LogTrivial("[FireyCalouts][Debug-log] Truck burning: Fire mode notice 2");
                            suspect.Tasks.LeaveVehicle(LeaveVehicleFlags.BailOut);
                        }

                        if (suspectVehicle.Exists()) suspectVehicle.EngineHealth = 1;
                        GameFiber.Wait(15000);

                        if (willExplode && suspectVehicle.Exists()) {
                            suspectVehicle.Explode(true);
                            willExplode = false;
                            Game.LogTrivial("[FireyCalouts][Debug-log] Truck burning: Explode");
                        }

                        madeTruckSmoke = true;
                    }
                }

                if (!willExplode) {
                    if (suspect.Exists() && !suspectDialogueComplete && suspect.DistanceTo(Game.LocalPlayer.Character.GetOffsetPosition(Vector3.RelativeFront)) < 11f && suspectVehicle.Velocity == Vector3.Zero) {
                        if (Game.IsKeyDown(Initialization.dialogueKey)) {
                            // Story
                            Game.DisplaySubtitle(dialoguesSuspect[dialoguePoint][dialogueCount]);
                            dialogueCount++;
                            GameFiber.Wait(1000);

                            if (dialogueCount == dialoguesSuspect[dialoguePoint].Length) {
                                // Select ending
                                if (selector == 1 && dialoguePoint == selector) {
                                    dialogueCount = 0;
                                    dialoguePoint = dialogueEndChoice;
                                } else {
                                    suspectDialogueComplete = true;
                                }

                                // Create pursuit if none exists and storyline 4 is chosen
                                if (suspectDialogueComplete && !pursuitCreated && dialogueEndChoice == 4) {
                                    pursuit = Functions.CreatePursuit();
                                    Functions.AddPedToPursuit(pursuit, suspect);
                                    Functions.SetPursuitIsActiveForPlayer(pursuit, true);
                                    pursuitCreated = true;
                                    Game.LogTrivial("[FireyCalouts][Debug-log] Pursuit started");
                                }
                            }
                        }
                    }
                }

                if (Game.LocalPlayer.Character.IsDead) End();
                if (Game.IsKeyDown(Initialization.endKey)) End();
                if (suspect.Exists()) { if (suspect.IsDead) End(); }
                if (suspect.Exists()) { if (Functions.IsPedArrested(suspect)) End(); }
            }, "BurningTruck [FireyCallouts]");
        }

        public override void End() {

            if (suspect.Exists()) { suspect.Dismiss(); }
            if (suspectVehicle.Exists()) { suspectVehicle.Dismiss(); }
            if (locationBlip.Exists()) { locationBlip.Delete(); }

            Functions.PlayScannerAudio("WE_ARE_CODE_FOUR");

            base.End();
            Game.LogTrivial("[FireyCallouts][Log] Cleaned up 'Burning Truck' callout.");
        }
    }
}