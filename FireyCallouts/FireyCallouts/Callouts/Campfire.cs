using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Drawing;
using Rage;
using Rage.Native;
using LSPD_First_Response.Mod.API;
using LSPD_First_Response.Mod.Callouts;
using LSPD_First_Response.Engine.Scripting.Entities;
using FireyCallouts.Utilitys;


namespace FireyCallouts.Callouts {
    [CalloutInfo("Campfire", CalloutProbability.Medium)]

    class Campfire : Callout {

        private Random mrRandom = new Random();

        // (First in array is center position)
        private List<Vector3> locations = new List<Vector3>() { new Vector3(1088.714f, -681.0089f, 56.54359f), // Mirror Park
                                                                new Vector3(-1384.228f, -1384.739f, 3.151442f), // Beach
                                                                new Vector3(-1154.693f, 929.415f, 198.1925f), // Vinewood Hills (south side)
                                                                new Vector3(1897.169f, 438.3262f, 164.0482f), // Lake above dam (at facility)
                                                                new Vector3(-439.5568f, 1587.239f, 357.8765f), // Vinewood Hills (north side)
                                                                new Vector3(2635.188f, 3662.036f, 101.9726f), // Sandy Shores
                                                                new Vector3(-1042.187f, 4374.532f, 11.534f), // Cassidy Creek
                                                                new Vector3(-1011.776f, 5070.507f, 173.8113f) // Paleto Forest
                                                              };

        private List<string[]> dialogues = new List<string[]>() { new string[] { "~y~Suspect: ~w~Hello, do you want to chill with us?.",
                                                                                 "~y~You: ~w~No, I am here to check what you are doing.", "~y~Suspect: ~w~Oh we are just chilling and enjoying the nature."},
                                                                  new string[] { "~y~Suspect: ~w~Hello Officer.", "~y~You: ~w~Hello, I am checking on what you are doing here.",
                                                                                 "~y~Suspect: ~w~We are just grilling here. I hope that's not a problem."},
                                                                };

        private string[] investigations = new string[] { "The fire is burning. There must have been people here. Maybe I can find them.", "The campfire is burning so there might be people around.",
                                                         "The wood looks burned. It was defenitely used for a fire. It is also still warm.", "The wood still emits heat. There must be people nearby who lit it.",
                                                         "The wood has no burn marks nor does it emit heat. This is probably nothing."};

        private string[] weaponList = new string[] { "weapon_flaregun", "weapon_molotov", "weapon_petrolcan" };

        private Vector3 spawnPoint;
        private Vector3 logSpawn;
        private Vector3 area;
        private Blip locationBlip;
        private List<Ped> suspects = new List<Ped>();
        private Rage.Object fireWood;
        private LHandle pursuit;

        private uint fire;
        private List<uint> fireList = new List<uint>();
        private bool endKeyPressed = false;

        private int decision;
        private bool nobodyThere = false;
        private bool fireBurning = false;

        private bool notificationShown = false;
        private bool nobodyNotificationShown = false;
        private bool pursuitCreated = false;

        private int dialogueCount = 0;
        private bool suspectDialogueComplete = false;

        int suspectsDead, suspectsArrested, dialogueChoice, investigationChoiceBurning, investigationChoiceNotBurning, displayInvestigationDecision;


        public override bool OnBeforeCalloutDisplayed() {
            Game.LogTrivial("[FireyCallouts][Log] Initialising 'Campfire' callout.");

            int fireAmplify;
            float offsetx, offsety, offsetz;

            decision = mrRandom.Next(0, 8);
            int makeFire = mrRandom.Next(0, 10);
            int hasWeapons = mrRandom.Next(0, 4);

            // Check locations around 800f to the player
            List<Vector3> possibleLocations = new List<Vector3>();
            
            foreach (Vector3 l in locations) {
                if (l.DistanceTo(Game.LocalPlayer.Character.GetOffsetPosition(Vector3.RelativeFront)) < Initialization.maxCalloutDistance) {
                    possibleLocations.Add(l);
                }
            }

            if (possibleLocations.Count < 1) {
                return AbortCallout();
            }

            // Random location for the fire
            int chosenLocation = mrRandom.Next(0, possibleLocations.Count);
            spawnPoint = possibleLocations[chosenLocation];

            ShowCalloutAreaBlipBeforeAccepting(spawnPoint, 30f);
            AddMinimumDistanceCheck(40f, spawnPoint);

            CalloutMessage = "Campfire";
            CalloutPosition = spawnPoint;

            // Lower spawn point due to the wood spawning in mid air
            spawnPoint.Z -= 0.5f;
            logSpawn = spawnPoint;
            logSpawn.Z -= 1.5f;

            fireWood = new Rage.Object("prop_fncwood_16g", logSpawn);
            fireWood.MakePersistent();

            if (decision < 4) {
                // No one there
                nobodyThere = true;

            } else {
                // People there - spawn 2 to 4 people
                int maxPeds = mrRandom.Next(2, 5);
                int suspectWander = mrRandom.Next(0, 9);
                int weaponsGiven = 0;

                for (int ii = 0; ii < maxPeds; ii++) {
                    suspects.Add(new Ped(spawnPoint.Around2D(5f)) { IsPersistent = true, BlockPermanentEvents = true });

                    if (weaponsGiven < hasWeapons) {
                        suspects[ii].Inventory.GiveNewWeapon(new WeaponAsset(weaponList[mrRandom.Next(0, weaponList.Length)]), 8, true);
                        weaponsGiven++;
                    }
                    if (suspectWander > 6) {
                        suspects[ii].Tasks.Wander();
                        nobodyThere = true;
                    }
                }
            }

            if (makeFire > 2) {
                fireBurning = true;
                int maxFires = 7;
                if (nobodyThere && mrRandom.Next(0,10) < 7) {
                    maxFires = mrRandom.Next(20, 40);
                }

                // Create Fire
                for (int f = 1; f < maxFires; f++) {
                    // Spawn several fires with random offset positions to generate a bigger fire
                    fireAmplify = mrRandom.Next(0, 4);
                    offsetx = fireAmplify * (f / 50);
                    fireAmplify = mrRandom.Next(0, 3);
                    offsety = fireAmplify * (f / 50);
                    fireAmplify = mrRandom.Next(0, 2);
                    offsetz = fireAmplify * (1 / 50);

                    fireAmplify = mrRandom.Next(0, 2);
                    if (fireAmplify == 0) {
                        offsetx = -offsetx;
                    }
                    fireAmplify = mrRandom.Next(0, 2);
                    if (fireAmplify == 0) {
                        offsety = -offsety;
                    }

                    // These fires do not extinguish by themselves.
                    fire = NativeFunction.Natives.StartScriptFire<uint>(spawnPoint.X + offsetx, spawnPoint.Y + offsety, spawnPoint.Z + offsetz, 25, true);

                    fireList.Add(fire);
                }
            }

            suspectsDead = 0;
            suspectsArrested = 0;

            dialogueChoice = mrRandom.Next(0, dialogues.Count);
            investigationChoiceBurning = mrRandom.Next(0, 2);
            investigationChoiceNotBurning = mrRandom.Next(2, 4);

            Functions.PlayScannerAudioUsingPosition("ASSISTANCE_REQUIRED IN_OR_ON_POSITION", spawnPoint);
            Functions.PlayScannerAudio("UNITS_RESPOND_CODE_03");

            Game.DisplayNotification("web_lossantospolicedept",
                                     "web_lossantospolicedept",
                                     "~y~FireyCallouts",
                                     "~r~Campfire",
                                     "~w~Someone called because of a campfire. Investigate the situation. Respond ~r~Code 2");

            return base.OnBeforeCalloutDisplayed();
        }

        public bool AbortCallout() {
            Game.LogTrivial("[FireyCallouts][Log] Abort 'Campfire' callout. Locations too far away (> "+Initialization.maxCalloutDistance.ToString()+").");

            // Clean up if not accepted
            if (locationBlip.Exists()) locationBlip.Delete();
            if (fireWood.Exists()) fireWood.Delete();

            foreach (uint f in fireList) {
                NativeFunction.Natives.RemoveScriptFire(f);
            }
            foreach (Ped p in suspects) {
                if (p.Exists()) p.Delete();
            }

            Game.LogTrivial("[FireyCallouts][Log] Cleaned up 'Campfire' callout.");
            return false;
        }

        public override bool OnCalloutAccepted() {
            Game.LogTrivial("[FireyCallouts][Log] Accepted 'Campfire' callout.");

            area = spawnPoint.Around2D(1f, 2f);
            locationBlip = new Blip(area, 40f) {
                Color = Color.Yellow
            };
            locationBlip.EnableRoute(Color.Yellow);

            Game.DisplayHelp("Press " + Initialization.endKey.ToString() + " to end the callout at any time.");

            return base.OnCalloutAccepted();
        }

        public override void OnCalloutNotAccepted() {
            Game.LogTrivial("[FireyCallouts][Log] Not accepted 'Campfire' callout.");

            // Clean up if not accepted
            foreach (Ped p in suspects) {
                if (p.Exists()) p.Delete();
            }
            if (locationBlip.Exists()) locationBlip.Delete();
            if (fireWood.Exists()) fireWood.Delete();

            foreach (uint f in fireList) {
                NativeFunction.Natives.RemoveScriptFire(f);
            }

            base.OnCalloutNotAccepted();
            Game.LogTrivial("[FireyCallouts][Log] Cleaned up 'Campfire' callout.");
        }

        public override void Process() {
            base.Process();

            suspectsDead = 0;
            suspectsArrested = 0;

            if (fireBurning) {
                displayInvestigationDecision = investigationChoiceBurning;
            } else {
                if (decision < 4) {
                    displayInvestigationDecision = 4;
                } else {
                    displayInvestigationDecision = investigationChoiceNotBurning;
                }
            }

            GameFiber.StartNew(delegate {


                if (locationBlip.Exists() && locationBlip.DistanceTo(Game.LocalPlayer.Character.GetOffsetPosition(Vector3.RelativeFront)) < 40f) {
                    if (locationBlip.Exists()) locationBlip.Delete();

                    if (decision > 6 && !pursuitCreated) {
                        // Pursuit
                        pursuit = Functions.CreatePursuit();
                        foreach (Ped sus in suspects) {
                            Functions.AddPedToPursuit(pursuit, sus);
                        }
                        Functions.SetPursuitIsActiveForPlayer(pursuit, true);
                        pursuitCreated = true;
                        Game.LogTrivial("[FireyCalouts][Debug-log] Pursuit started");

                    } else if (decision <= 6 && !nobodyThere) {

                        if (!notificationShown) {
                            Game.DisplayHelp("Press " + Initialization.dialogueKey.ToString() + " to show dialogues.");

                            notificationShown = true;
                        }
                    }

                    if (nobodyThere) {
                        // Nobody there -- investigations

                        if (!notificationShown) {
                            Game.DisplayHelp("Investigate the area. Press " + Initialization.dialogueKey.ToString() + " to show monologues.");

                            notificationShown = true;
                        }

                    }

                    GameFiber.Wait(2000);
                }

                // Story
                if (suspects.Count > 0 && suspects[0].Exists() && !suspectDialogueComplete && !nobodyThere && suspects[0].DistanceTo(Game.LocalPlayer.Character.GetOffsetPosition(Vector3.RelativeFront)) < 11f) {
                    if (Game.IsKeyDown(Initialization.dialogueKey)) {
                        // Story
                        Game.DisplaySubtitle(dialogues[dialogueChoice][dialogueCount]);
                        dialogueCount++;
                        GameFiber.Wait(1000);

                        if (dialogueCount == dialogues[dialogueChoice].Length) {
                            suspectDialogueComplete = true;
                        }
                    }
                } else if (!suspectDialogueComplete && nobodyThere && spawnPoint.DistanceTo(Game.LocalPlayer.Character.GetOffsetPosition(Vector3.RelativeFront)) < 4f) {

                    if (Game.IsKeyDown(Initialization.dialogueKey)) {
                        // Investigation
                        Game.DisplaySubtitle(investigations[displayInvestigationDecision]);
                        GameFiber.Wait(1000);
                        suspectDialogueComplete = true;
                    }
                }


                if (Game.LocalPlayer.Character.IsDead) End();
                if (Game.IsKeyDown(Initialization.endKey)) { endKeyPressed = true; End(); }

                foreach (Ped s in suspects) {
                    if (s.Exists() && s.IsDead) {
                        suspectsDead++;
                    } else if (s.Exists() && Functions.IsPedArrested(s)) {
                        suspectsArrested++;
                    }
                }
                if (suspectsDead == suspects.Count || suspectsArrested == suspects.Count || suspectsArrested + suspectsDead >= suspects.Count) {
                    End();
                }

            }, "Campfire [FireyCallouts]");
        }

        public override void End() {

            foreach (Ped p in suspects) {
                if (p.Exists()) p.Dismiss();
            }
            if (locationBlip.Exists()) locationBlip.Delete();
            if (fireWood.Exists()) fireWood.Delete();

            // Check if ended by pressing end and delete fires; Otherwise keep them
            // Warning: ending the callout without deleting the fires is causing the fires to burn indefinitely
            if (endKeyPressed) {
                foreach (uint f in fireList) {
                    NativeFunction.Natives.RemoveScriptFire(f);
                }
            }

            Functions.PlayScannerAudio("WE_ARE_CODE_4");

            base.End();
            Game.LogTrivial("[FireyCallouts][Log] Cleaned up 'Campfire' callout.");
        }

    }
}
