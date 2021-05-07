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

    [CalloutInfo("Lost Freight", CalloutProbability.Low)]

    class LostFreight : Callout {

        private Random mrRandom = new Random();

        private Ped suspect;
        private Ped witness;
        private Vehicle suspectVehicle;
        private Vehicle lostVehicle;
        private Vector3 spawnPoint;
        private Blip suspectBlip, witnessBlip, lostVehicleBlip;
        private LHandle pursuit;

        private bool pursuitCreated = false;

        // Dialogue texts
        private List<string[]> dialoguesSuspect = new List<string[]>() { new string[] { "~y~Suspect: ~w~Hello Officer.", "~y~You: ~w~Good day. Have you been driving this truck?",
                                                                                        "~y~Suspect: ~w~Yes, I am the driver. A belt has come loose and the tractor fell on the street.",
                                                                                        "~y~You: ~w~That's not good. You should have secured the load better. I will clear this situation."},
                                                                        new string[] { "~y~Suspect: ~w~Hello Officer.", "~y~You: ~w~Hello. Have you been driving this truck?",
                                                                                        "~y~Suspect: ~w~Yes, I am the driver. I don't know what happened.", "~y~You: ~w~It looks like you didn't secure the load properly.",
                                                                                        "~y~Suspect: ~w~I am sure it was secured when I set off.", "~y~You: ~w~As you can see it fell off. I will clear this situation."},
                                                                        new string[] { "~y~Suspect: ~w~Hello Officer.", "~y~You: ~w~Hello. You are the driver?",
                                                                                        "~y~Suspect: ~w~Yes, I am the driver. I am picking up this tractor. It was involved in an accident",
                                                                                        "~y~You: ~w~I've been informed that a truck has lost its freight.", "~y~Suspect: ~w~There must have been a misunderstanding then.",
                                                                                        "~y~You: ~w~I will check this and clear the situation."},
                                                                        new string[] {  "~y~Suspect: ~w~Hello Officer. Anything wrong?", "~y~You: ~w~Yes, you have lost your load on the way.",
                                                                                        "~y~Suspect: ~w~I did not notice that. I'm sorry.", "~y~You: ~w~You need to secure your load better next time."},
                                                                        new string[] {  "~y~Suspect: ~w~Hello Officer.", "~y~You: ~w~Hello. You have lost your load a few blocks away.",
                                                                                        "~y~Suspect: ~w~I have lost my load? No, I didn't have anything loaded yet.",
                                                                                        "~y~You: ~w~We have information that the truck you are driving has lost a tractor.",
                                                                                        "~y~Suspect: ~w~I can assure you that you have the wrong truck."} };

        private List<string[]> dialoguesWitness = new List<string[]>() { new string[] { "~y~Witness: ~w~Hello Officer. I have seen what happened.", "~y~You: ~w~Thats good. Can you explain it to me, please?",
                                                                                        "~y~Witness: ~w~Of course. The truck was driving really strange and suddenly swerved a lot.",
                                                                                        "~y~You: ~w~Is there anything else you noticed?", "~y~Witness: ~w~Yes, I think I saw the driver holding a phone.",
                                                                                        "~y~You: ~w~Thank you very much for this information. You are good to leave the scene now."},
                                                                         new string[] { "~y~Witness: ~w~Hello Officer.", "~y~You: ~w~Hello. Have you witness what happened here?",
                                                                                        "~y~Witness: ~w~I heard a loud squeak. When I turned around I saw that the truck had lost the tractor.",
                                                                                        "~y~You: ~w~Is there anything else you noticed?", "~y~Witness: ~w~No, thats all I can tell you.",
                                                                                        "~y~You: ~w~Thank you for the information. You can leave the scene now."},
                                                                         new string[] { "~y~Witness: ~w~Hello Officer. I have witnessed what happened", "~y~You: ~w~Please tell me what happened.",
                                                                                        "~y~Witness: ~w~The truck was driving down the road when suddenly a rope tore.",
                                                                                        "~y~You: ~w~Did you notice something else?", "~y~Witness: ~w~No, that is all I saw. I hope it helps.",
                                                                                        "~y~You: ~w~Thanks for the information. You are free to leave the scene."},
                                                                         new string[] { "~y~Witness: ~w~Hello Officer. I have seen what happened.", "~y~You: ~w~Thats good. Can you explain it to me, please?",
                                                                                        "~y~Witness: ~w~Of course. A truck was driving really strange and suddenly swerved a lot.",
                                                                                        "~y~You: ~w~Is there anything else you noticed?", "- placeholder -",
                                                                                        "~y~You: ~w~Thank you very much for this information. You are good to leave the scene now."} };

        private List<string[]> notificationTexts = new List<string[]> { new string[] { "A witness has informed us that the driver took off. The witness is on scene and has some information.",
                                                                                       "A witness has informed us that the driver took off. We will send the information we received shortly."},
                                                                        new string[] { "The driver took of. We have no information where they are now. Clear the road if necessary.",
                                                                                       "We have received information that the truck is driving away from the scene. Information will be passed shortly."},
                                                                        new string[] { "A witness has informed us that the driver took off. The witness is on scene and has some information.",
                                                                                       "We've been informed that the driver has left the scene. A witness has given use information about the suspect."}};

        private int decision, selector, storyDecisionSuspect, storyRunaway, storyDecisionWitness, runawayBehavior;
        private int dialogueCountSuspect = 0;
        private int dialogueCountWitness = 0;
        private bool witnessThere = false;
        private bool suspectDialogueComplete = false;
        private bool witnessDialogueComplete = false;
        private bool notificationShown = false;
        private bool witnessInfo = false;
        private bool updateLocations = false;
        private bool timeBuffer = false;
        private bool lostVehicleBlipped = false;
        

        public override bool OnBeforeCalloutDisplayed() {
            Game.LogTrivial("[FireyCallouts][Log] Initialising 'Lost Freight' callout.");

            spawnPoint = World.GetNextPositionOnStreet(Game.LocalPlayer.Character.Position.Around(350f));

            ShowCalloutAreaBlipBeforeAccepting(spawnPoint, 30f);
            AddMinimumDistanceCheck(40f, spawnPoint);

            CalloutMessage = "Lost Freight";
            CalloutPosition = spawnPoint;

            // Initialise vehicle(s) and ped
            suspectVehicle = new Vehicle("FLATBED", spawnPoint);
            suspectVehicle.IsPersistent = true;

            lostVehicle = new Vehicle("Tractor", spawnPoint.Around(10f));
            lostVehicle.IsPersistent = true;

            /*
            suspectVehicle.TowVehicle(lostVehicle, true);
            suspectVehicle.DetachTowedVehicle();
            */

            suspect = suspectVehicle.CreateRandomDriver();
            suspect.IsPersistent = true;
            suspect.BlockPermanentEvents = true;

            decision = mrRandom.Next(0, 6);

            if (decision < 2) {
                suspect.Tasks.CruiseWithVehicle(suspectVehicle, 50f, VehicleDrivingFlags.Emergency);
            } else if (decision < 4) {
                suspect.Tasks.CruiseWithVehicle(suspectVehicle, 20f, VehicleDrivingFlags.None);
            } else {
                suspect.Tasks.LeaveVehicle(suspectVehicle, LeaveVehicleFlags.LeaveDoorOpen);
            }

            if (decision % 2 == 0) {
                witnessThere = true;
                witness = new Ped(lostVehicle.Position.Around(5f));
            }

            selector = mrRandom.Next(0, 2);
            storyDecisionSuspect = mrRandom.Next(0, 3);
            storyRunaway = mrRandom.Next(3, 5);
            storyDecisionWitness = mrRandom.Next(0, 3);
            runawayBehavior = mrRandom.Next(0, 2);

            Game.LogTrivial("[FireyCallouts][Debug-Log] Selected suspect story: " + dialoguesSuspect[storyDecisionSuspect]);
            Game.LogTrivial("[FireyCallouts][Debug-Log] Selected witness story: " + dialoguesWitness[storyDecisionWitness]);

            Functions.PlayScannerAudioUsingPosition("ASSISTANCE_REQUIRED IN_OR_ON_POSITION", spawnPoint);
            Functions.PlayScannerAudio("UNITS_RESPOND_CODE_03");

            Game.DisplayNotification("web_lossantospolicedept",
                                     "web_lossantospolicedept",
                                     "~y~FireyCallouts",
                                     "~r~Lost freight",
                                     "~w~A tow truck has lost its freight. Respond ~r~Code 3");

            return base.OnBeforeCalloutDisplayed();
        }

        public override bool OnCalloutAccepted() {
            Game.LogTrivial("[FireyCallouts][Log] Accepted 'Lost Freight' callout.");

            // Show route for player
            suspectBlip = new Blip(spawnPoint) {
                IsFriendly = true,
                Color = Color.Yellow
            };
            suspectBlip.EnableRoute(Color.Yellow);

            Game.DisplayHelp("Press " + Initialization.endKey.ToString() + " to end the callout at any time.");

            return base.OnCalloutAccepted();
        }

        public override void OnCalloutNotAccepted(){
            Game.LogTrivial("[FireyCallouts][Log] Not accepted 'Lost Freight' callout.");

            // Clean up if not accepted
            if (suspect.Exists()) suspect.Delete();
            if (witness.Exists()) witness.Delete();
            if (suspectVehicle.Exists()) suspectVehicle.Delete();
            if (lostVehicle.Exists()) lostVehicle.Delete();
            if (suspectBlip.Exists()) suspectBlip.Delete();
            if (witnessBlip.Exists()) witnessBlip.Delete();
            if (lostVehicleBlip.Exists()) { lostVehicleBlip.Delete(); }

            base.OnCalloutNotAccepted();
            Game.LogTrivial("[FireyCallouts][Log] Cleaned up 'Lost Freight' callout.");
        }

        public override void Process() {

            base.Process();

            GameFiber.StartNew(delegate {
                if (suspect.Exists() && spawnPoint.DistanceTo(Game.LocalPlayer.Character.GetOffsetPosition(Vector3.RelativeFront)) < 40f) {
                    suspect.KeepTasks = true;
                    if (suspectBlip.Exists() && !notificationShown) suspectBlip.Delete();
                    GameFiber.Wait(2000);

                    if (!lostVehicleBlipped) {
                        lostVehicleBlip = lostVehicle.AttachBlip();
                        lostVehicleBlip.Color = Color.Green;
                        lostVehicleBlipped = true;
                    }

                    if (!notificationShown) {
                        Game.DisplayHelp("Press " + Initialization.dialogueKey.ToString() + " to show dialogues.");
                    }

                    if (decision < 4 && witnessThere && !notificationShown) {
                        notificationShown = true;

                        Game.DisplayNotification("web_lossantospolicedept",
                                        "web_lossantospolicedept",
                                        "~y~FireyCallouts",
                                        "~b~Dispatch",
                                        "~w~" + notificationTexts[decision][selector]);
                        GameFiber.Wait(2000);

                        if (selector == 0) {
                            // Witness has information
                            witnessBlip = witness.AttachBlip();
                            witnessBlip.Color = Color.Green;
                            witnessBlip.EnableRoute(Color.Green);
                            storyDecisionWitness = 3;
                            dialoguesWitness[storyDecisionWitness][4] = "I've noticed the license plate of the vehicle. It is ~g~" + suspectVehicle.LicensePlate + "~w~.";
                            witnessInfo = true;

                        } else {
                            Game.DisplayNotification("web_lossantospolicedept",
                                            "web_lossantospolicedept",
                                            "~y~FireyCallouts",
                                            "~b~Dispatch",
                                            "~w~We are looking for a truck with license plate ~g~" + suspectVehicle.LicensePlate + "~w~. We will update you with the latest known positions.");

                            witnessBlip = witness.AttachBlip();
                            witnessBlip.Color = Color.Green;

                            updateLocations = true;
                        }

                        notificationShown = true;

                    } else if (decision < 4 && !witnessThere && !notificationShown) {
                        notificationShown = true;

                        Game.DisplayNotification("web_lossantospolicedept",
                                        "web_lossantospolicedept",
                                        "~y~FireyCallouts",
                                        "~b~Dispatch",
                                        "~w~" + notificationTexts[1][selector]);
                        GameFiber.Wait(2000);

                        if (selector == 1) {
                            Game.DisplayNotification("web_lossantospolicedept",
                                        "web_lossantospolicedept",
                                        "~y~FireyCallouts",
                                        "~b~Dispatch",
                                        "~w~We are looking for a truck with license plate ~g~" + suspectVehicle.LicensePlate + "~w~. We will update you with the latest known positions.");

                            updateLocations = true;
                        }

                        notificationShown = true;
                    } else {
                        notificationShown = true;
                    }

                }

                if (suspect.Exists() && suspect.DistanceTo(Game.LocalPlayer.Character.GetOffsetPosition(Vector3.RelativeFront)) < 8f && !suspectDialogueComplete && 
                    (!witness.Exists() || witness.Exists() && witness.DistanceTo(Game.LocalPlayer.Character.GetOffsetPosition(Vector3.RelativeFront)) 
                                         > suspect.DistanceTo(Game.LocalPlayer.Character.GetOffsetPosition(Vector3.RelativeFront)))) {
                    // Story driver (suspect)
                    if (Game.IsKeyDown(Initialization.dialogueKey) && !suspectDialogueComplete && decision >= 2) {
                        if (decision < 4) {
                            Game.DisplaySubtitle(dialoguesSuspect[storyRunaway][dialogueCountSuspect]);
                            dialogueCountSuspect++;
                            GameFiber.Wait(1000);

                            if (dialogueCountSuspect >= dialoguesSuspect[storyRunaway].Length) {
                                suspectDialogueComplete = true;
                            }

                        } else {

                            Game.DisplaySubtitle(dialoguesSuspect[storyDecisionSuspect][dialogueCountSuspect]);
                            dialogueCountSuspect++;
                            GameFiber.Wait(1000);

                            if (dialogueCountSuspect >= dialoguesSuspect[storyDecisionSuspect].Length) {
                                suspectDialogueComplete = true;
                            }
                        }
                    } else {
                        if (decision < 2) {
                            suspectDialogueComplete = true;
                        }
                    }

                } else if (witness.Exists() && witness.DistanceTo(Game.LocalPlayer.Character.GetOffsetPosition(Vector3.RelativeFront)) < 8f && !witnessDialogueComplete && notificationShown && 
                          (!suspect.Exists() || suspect.Exists() && witness.DistanceTo(Game.LocalPlayer.Character.GetOffsetPosition(Vector3.RelativeFront))
                                                < suspect.DistanceTo(Game.LocalPlayer.Character.GetOffsetPosition(Vector3.RelativeFront)))) {
                    // Story witness
                    if (Game.IsKeyDown(Initialization.dialogueKey) && !witnessDialogueComplete) {
                        Game.DisplaySubtitle(dialoguesWitness[storyDecisionWitness][dialogueCountWitness]);
                        dialogueCountWitness++;
                        Game.LogTrivial("[FireyCallouts][Debug-log] Dialogue Counter: " + dialogueCountWitness.ToString() + " Dialogue: " + dialoguesWitness[storyDecisionWitness][dialogueCountWitness]);
                        GameFiber.Wait(1000);

                        if (dialogueCountWitness >= dialoguesWitness[storyDecisionWitness].Length) {
                            witnessDialogueComplete = true;
                            if (witness.Exists()) witness.Dismiss();
                            if (witnessBlip.Exists()) witnessBlip.Delete();

                            GameFiber.Wait(3000);

                            if (witnessInfo) {
                                Game.DisplayNotification("web_lossantospolicedept",
                                        "web_lossantospolicedept",
                                        "~y~FireyCallouts",
                                        "~b~Dispatch",
                                        "~w~We have located the vehicle. Stop them.");

                                updateLocations = true;
                                GameFiber.Wait(1500);
                            }
                        }
                    }
                }

                if (suspect.Exists() && updateLocations && !pursuitCreated && suspect.DistanceTo(Game.LocalPlayer.Character.GetOffsetPosition(Vector3.RelativeFront)) > 40f && !timeBuffer) {
                    timeBuffer = true;
                    if (suspectBlip.Exists()) { suspectBlip.Delete(); }
                    suspectBlip = new Blip(suspect.Position, 60f) {
                        Color = Color.Yellow
                    };
                    suspectBlip.EnableRoute(Color.Yellow);

                    if (decision >= 2 && decision < 4) {
                        GameFiber.Wait(10000);
                    }
                    GameFiber.Wait(5000);
                    timeBuffer = false;
                }

                if (suspect.Exists() && suspect.DistanceTo(Game.LocalPlayer.Character.GetOffsetPosition(Vector3.RelativeFront)) < 20f && decision < 2 && notificationShown && !pursuitCreated) {
                    pursuit = Functions.CreatePursuit();
                    Functions.AddPedToPursuit(pursuit, suspect);
                    Functions.SetPursuitIsActiveForPlayer(pursuit, true);
                    pursuitCreated = true;
                    Game.LogTrivial("[FireyCalouts][Debug-log] Pursuit started");

                    updateLocations = false;
                    if (suspectBlip.Exists()) { suspectBlip.Delete(); }

                } else if (suspect.Exists() && suspect.DistanceTo(Game.LocalPlayer.Character.GetOffsetPosition(Vector3.RelativeFront)) < 20f && notificationShown) {
                    updateLocations = false;
                    if (suspectBlip.Exists()) { suspectBlip.Delete(); }
                }

                if (Game.LocalPlayer.Character.IsDead) End();
                if (Game.IsKeyDown(Initialization.endKey)) End();
                if (suspect.Exists()) { if (suspect.IsDead) End(); }
                if (suspect.Exists()) { if (Functions.IsPedArrested(suspect)) End(); }
            }, "LostFreight [FireyCallouts]");
        }

        public override void End() {

            if (suspect.Exists()) { suspect.Dismiss(); }
            if (witness.Exists()) witness.Dismiss();
            if (suspectVehicle.Exists()) { suspectVehicle.Dismiss(); }
            if (lostVehicle.Exists()) { lostVehicle.Dismiss(); }
            if (suspectBlip.Exists()) { suspectBlip.Delete(); }
            if (witnessBlip.Exists()) witnessBlip.Delete();
            if (lostVehicleBlip.Exists()) { lostVehicleBlip.Delete(); }

            Functions.PlayScannerAudio("WE_ARE_CODE_4");

            base.End();
            Game.LogTrivial("[FireyCallouts][Log] Cleaned up 'Lost Freight' callout.");
        }
    }
}
