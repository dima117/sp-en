using System;
using System.Text;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using VRageMath;
using VRage.Game;
using VRage.Game.GUI.TextPanel;
using Sandbox.ModAPI.Interfaces;
using Sandbox.ModAPI.Ingame;
using Sandbox.Game.EntityComponents;
using VRage.Game.Components;
using VRage.Collections;
using VRage.Game.ObjectBuilders.Definitions;
using VRage.Game.ModAPI.Ingame;
using SpaceEngineers.Game.ModAPI.Ingame;
using Sandbox.Game.Entities;

namespace Examples
{
    public sealed class Program : MyGridProgram
    {
        //////////////////////////////////
        //Gravity Drive Script  v8.2//
        //by Misha.Malanyuk(FreeUA)//
        //////////////////////////////////
        static Vector2I AxesTransform(Vector3D p)
        {
            return new Vector2I(-(int)p.X, -(int)p.Z);
        }

        int ŧā = 0;
        List<IMyShipConnector> connectors = new List<IMyShipConnector>();
        List<IMyLightingBlock> ĊĂ = new List<IMyLightingBlock>();
        List<IMyLightingBlock> ćĂ = new List<IMyLightingBlock>();
        List<IMyLightingBlock> ĈĂ = new List<IMyLightingBlock>();
        List<IMyLightingBlock> ĆĂ = new List<IMyLightingBlock>();
        List<IMyLightingBlock> āĂ = new List<IMyLightingBlock>();
        List<IMyLightingBlock> ĉĂ = new List<IMyLightingBlock>();

        int Ļā = 0;
        long Ń = 60;
        IMyBlockGroup ŇĂ = null;
        List<IMyRemoteControl> remotes = new List<IMyRemoteControl>();
        List<IMyFlightMovementBlock> aiFlight = new List<IMyFlightMovementBlock>();

        IMyBeacon beacon = null;
        void řā(long uf = 36000, bool First = false)
        {
            Ń = ŏ(20);
            var allMass = new List<IMyVirtualMass>();
            var recGen = new List<IMyGravityGenerator>();
            var sphereGenerators = new List<IMyGravityGeneratorSphere>();
            var thrusters = new List<IMyThrust>();
            var gyros = new List<IMyGyro>();

            var recLCD = new List<IMyTextPanel>();

            var ļā = new List<IMyTextPanel>();
            bool Ĥ = false;

            switch (Ļā)
            {
                case (0):
                    {
                        if (Ďā != Me.CustomData.GetHashCode()) if (Me.CustomData.Trim() == "") ž();
                            else
                            {
                                Ďā = Me.CustomData.GetHashCode(); ųā.Clear();
                                foreach (var Setting in Me.CustomData.Split('\n'))
                                { var Line = Setting.Trim(); var l = Line.IndexOf("//"); if (l >= 0) Line = Line.Substring(0, l); if (Line.ToLower() == "settings.end!") break; Łā(Line); }
                                if (ūă != "" && Űă != "") Źă = Źă ?? new List<ŭā>(); else Źă = null;
                            }
                        ŗ = Ř * Ř; ř = Ś * Ś; ŭĂ = ŮĂ * ŮĂ; Ļā++;
                        ġā();
                    }
                    break;
                case (1): { if (ņĂ != "") { ŇĂ = GridTerminalSystem.GetBlockGroupWithName(ņĂ); if (ŇĂ != null) Ļā = 20; else { Ćă = "Group no found!\n\"" + trimString(ņĂ, 12) + "\""; Ń = ŏ(600); } } else { ŇĂ = null; Ļā = 2; } } break;
                case (2):
                    {
                        GridTerminalSystem.GetBlocksOfType(Ħă, b => (b.CubeGrid == Me.CubeGrid && IsCockpit(b.DefinitionDisplayNameText)));
                        Ġā(); if (shipController == null) { Ćă = "No Cockpit Found!\n"; Ļā = 0; Ń = ŏ(600); } else { ŌĂ = Ňā(shipController.WorldMatrix, shipController.CenterOfMass - shipController.CubeGrid.GetPosition()); Ļā++; }
                    }
                    break;
                case (3):
                    {
                        Ĥ = true; GridTerminalSystem.GetBlocksOfType(recLCD, b => (b.CubeGrid == Me.CubeGrid && b is IMyTextSurface && b.CustomName.Contains(ĒĂ)));
                        GridTerminalSystem.GetBlocksOfType(ļā, b => (b.CubeGrid == Me.CubeGrid && b is IMyTextSurface && b.CustomName.Contains(Ĉ))); Ļā++;
                    }
                    break;
                case (4):
                    {
                        GridTerminalSystem.GetBlocksOfType(allMass, b => (b.CubeGrid == Me.CubeGrid));
                        if (shipController != null) āā(allMass, shipController.CenterOfMass);

                        GridTerminalSystem.GetBlocksOfType(ĊĂ, b => (b.CubeGrid == Me.CubeGrid && b.CustomName.Contains(ąĂ))); Ļā++;
                    }
                    break;
                case (5):
                    {
                        GridTerminalSystem.GetBlocksOfType(recGen, b => (b.CubeGrid == Me.CubeGrid)); GridTerminalSystem.GetBlocksOfType(ćĂ, b => (b.CubeGrid == Me.CubeGrid && b.CustomName.Contains(ĂĂ)));
                        Ļā++;
                    }
                    break;
                case (6):
                    {
                        GridTerminalSystem.GetBlocksOfType(sphereGenerators, b => (b.CubeGrid == Me.CubeGrid)); if (shipController != null) āā(sphereGenerators, shipController.CenterOfMass); GridTerminalSystem.GetBlocksOfType(ĈĂ, b => (b.CubeGrid == Me.CubeGrid && b.CustomName.Contains(ăĂ))); GridTerminalSystem.GetBlocksOfType(āĂ, b => (b.CubeGrid == Me.CubeGrid && b.CustomName.Contains(LightMarkerSMax)));
                        Ļā++;
                    }
                    break;
                case (7): { GridTerminalSystem.GetBlocksOfType(thrusters, b => (b.CubeGrid == Me.CubeGrid)); Ļā++; } break;
                case (8):
                    {
                        GridTerminalSystem.GetBlocksOfType(gyros, b => (b.CubeGrid == Me.CubeGrid)); GridTerminalSystem.GetBlocksOfType(ĆĂ, b => (b.CubeGrid == Me.CubeGrid && b.CustomName.Contains(LightMarkerGJ)));
                        GridTerminalSystem.GetBlocksOfType(ĉĂ, b => (b.CubeGrid == Me.CubeGrid && b.CustomName.Contains(ĄĂ))); Ļā++;
                    }
                    break;
                case (9):
                    {
                        GridTerminalSystem.GetBlocksOfType(remotes, b => (b.CubeGrid == Me.CubeGrid)); GridTerminalSystem.GetBlocksOfType(aiFlight, b => (b.CubeGrid == Me.CubeGrid)); Ļā++;
                    }
                    break;
                case (10): { if (!ŵă) GridTerminalSystem.GetBlocksOfType(connectors, b => (b.CubeGrid == Me.CubeGrid)); Ļā = 99; } break;
                case (20):
                    {
                        ŇĂ.GetBlocksOfType(Ħă, b => (IsCockpit(b.DefinitionDisplayNameText))); Ġā(); if (shipController == null)
                        {
                            Ćă = "No Cockpit In the Group!\n\"" + trimString(ņĂ, 25) + "\"";
                            Ļā = 0; Ń = ŏ(600);
                        }
                        else { ŌĂ = Ňā(shipController.WorldMatrix, shipController.CenterOfMass - shipController.CubeGrid.GetPosition()); Ļā++; }
                    }
                    break;
                case (21):
                    {
                        Ĥ = true; ŇĂ.GetBlocksOfType(recLCD, b => (b is IMyTextSurface && b.CustomName.Contains(ĒĂ))); ŇĂ.GetBlocksOfType(ļā, b => (b is IMyTextSurface && b.CustomName.Contains(Ĉ)));
                        Ļā++;
                    }
                    break;
                case (22): { ŇĂ.GetBlocksOfType(allMass); if (shipController != null) āā(allMass, shipController.CenterOfMass); ŇĂ.GetBlocksOfType(ĊĂ, b => (b.CustomName.Contains(ąĂ))); Ļā++; } break;
                case (23):
                    {
                        ŇĂ.GetBlocksOfType(recGen); ŇĂ.GetBlocksOfType(ćĂ, b => (b.CustomName.Contains(ĂĂ))); ŇĂ.GetBlocksOfType(āĂ, b => (b.CustomName.Contains(LightMarkerSMax)));
                        Ļā++;
                    }
                    break;
                case (24): { ŇĂ.GetBlocksOfType(sphereGenerators); if (shipController != null) āā(sphereGenerators, shipController.CenterOfMass); ŇĂ.GetBlocksOfType(ĈĂ, b => (b.CustomName.Contains(ăĂ))); Ļā++; } break;
                case (25): { ŇĂ.GetBlocksOfType(thrusters); Ļā++; } break;
                case (26):
                    {
                        ŇĂ.GetBlocksOfType(gyros); ŇĂ.GetBlocksOfType(ĆĂ, b => (b.CustomName.Contains(LightMarkerGJ)));
                        ŇĂ.GetBlocksOfType(ĉĂ, b => (b.CustomName.Contains(ĄĂ))); Ļā++;
                    }
                    break;
                case (27): { ŇĂ.GetBlocksOfType(remotes); ŇĂ.GetBlocksOfType(aiFlight); Ļā++; } break;
                case (28): { if (!ŵă) ŇĂ.GetBlocksOfType(connectors); Ļā = 99; } break;
                case (99): { SortListId(connectors); Ļā++; } break;
                case (100):
                    {
                        Ĝā();
                        Ļā++;
                    }
                    break;
                case (101): { Č(); Ļā++; } break;
                case (102): { ż(); ĝā(ĆĂ, ŃĂ); Ļā++; } break;
                default: { Ļā = 0; Ń = ŏ((ňă()) ? uf : 600); } break;
            }
            if (shipController != null)
            {
                foreach (var b in allMass) if (śā(b)) Ď.Add(new BlockWrapper(b, (b is IMySpaceBall) ? 0 : 1, 0)); if (recGen.Count > 0)
                {
                    foreach (var b in recGen)
                        if (śā(b)) { if (b.CustomName.Contains(ťĂ)) ŽĂ = b as IMyGravityGenerator; ŤĂ.Add(new BlockWrapper(b, 4, 0, true), Base6Directions.Direction.Up); }
                    ŦĂ = ŤĂ.ćĄ; Ī = 0; Ĺ = ŏ(300);
                }
                if (thrusters.Count > 0)
                {
                    foreach (var b in thrusters) if (śā(b)) Ŗ.Add(new BlockWrapper(b, 3, 0, true), Base6Directions.Direction.Backward);
                    ś = Ŗ.ćĄ; ħ = 0; Ĺ = ŏ(300);
                }
                if (gyros.Count > 0) { foreach (var b in gyros) if (śā(b)) ļĂ.Add(new ķĂ(shipController.Orientation, b)); ň = 0; Ĩ = 0; Ĺ = ŏ(300); }
                if (sphereGenerators.Count > 0) { foreach (var b in sphereGenerators) if (śā(b)) ŢĂ.Add(new ťā(b)); ĩ = 0; Ĺ = ŏ(300); }
            }
            if (Ĥ)
            {
                ĕĂ.Clear(); ĖĂ.Clear(); foreach (var ėĂ in recLCD)
                    ĕĂ.Add(ėĂ as IMyTextSurface); foreach (var ėĂ in ļā) ĖĂ.Add(new ć(ėĂ)); if (Ġ >= 0) foreach (var c in Ħă) if (c is IMyTextSurfaceProvider) { var p = c as IMyTextSurfaceProvider; if (Ġ < p.SurfaceCount) ĕĂ.Add(p.GetSurface(Ġ)); }
                İĂ = ıĂ = true;
            }
            if (Ćă != null)
            {
                ĵā = "";
                ħā = ŷā; SetScriptSpeed(UpdateFrequency.Update10, 10);
            }
            else { if (First) { SetScriptSpeed(UpdateFrequency.Update1, 1); Ń = ŏ(3600); CalculateMassInfo(); ĥ(); ěĂ = shipController; if (MyLCD != null) { ňā(MyLCD, Color.Green, TextAlignment.CENTER, 1f); MyLCD.WriteText(MyLCDTitle + ĵā); } } }
        }
        const string ăă = "#Generator.FieldSize=";
        void Ęā(BoundingBox śă, List<BlockWrapper> GG)
        {
            foreach (var Űā in GG)
            {
                var b = Űā.AsGravityGen; if (b != ŽĂ && b != null)
                {
                    var bb = śă; Vector3I p = b.Position; bb.Min -= p; bb.Max -= p; var ĉĄ = new Vector3D(Math.Abs(bb.Min.X), Math.Abs(bb.Min.Y), Math.Abs(bb.Min.Z)); var ĊĄ = new Vector3D(Math.Abs(bb.Max.X), Math.Abs(bb.Max.Y), Math.Abs(bb.Max.Z));
                    var Ū = new Vector3D(Math.Max(ĉĄ.X, ĊĄ.X), Math.Max(ĉĄ.Y, ĊĄ.Y), Math.Max(ĉĄ.Z, ĊĄ.Z)) * 5 + 2; var m = new Matrix(); b.Orientation.GetMatrix(out m); var NewFieldSize = new Vector3D(Math.Abs(m.Left.Dot(Ū)), Math.Abs(m.Down.Dot(Ū)), Math.Abs(m.Forward.Dot(Ū)));
                    int Ōā = b.CustomData.IndexOf(ăă); if (Ōā < 0) b.CustomData = ăă + b.FieldSize.ToString("f2") + "\n" + b.CustomData; b.FieldSize = NewFieldSize;
                }
            }
        }
        void ĳā(List<BlockWrapper> blocks)
        {
            foreach (var b in blocks)
            {
                var generator = b.AsGravityGen; 

                if (generator != ŽĂ && generator != null)
                {
                    int Ōā = generator.CustomData.IndexOf(ăă);
                    if (Ōā < 0) continue;
                    int Ŋā = Ōā + ăă.Length; int e = (Ŋā < generator.CustomData.Length) ? generator.CustomData.IndexOf("\n", Ŋā) : -1; if (e < 0) e = generator.CustomData.Length; var Ģā = generator.CustomData.Substring(Ŋā, e - Ŋā); var FieldSize = Vector3D.Zero; if (ľā(ref FieldSize, Ģā)) generator.FieldSize = FieldSize;
                    if (e < generator.CustomData.Length) e++; generator.CustomData = generator.CustomData.Substring(0, Ōā) + generator.CustomData.Substring(e);
                }
            }
        }
        void Ĝā()
        {
            if (ęā) { var śă = ŞĂ(Ď); Ęā(śă, ŤĂ.L); Ęā(śă, ŤĂ.R); Ęā(śă, ŤĂ.U); Ęā(śă, ŤĂ.D); Ęā(śă, ŤĂ.B); Ęā(śă, ŤĂ.F); }
            else
            {
                ĳā(ŤĂ.L); ĳā(ŤĂ.R); ĳā(ŤĂ.U); ĳā(ŤĂ.D);
                ĳā(ŤĂ.B); ĳā(ŤĂ.F);
            }
        }
        string ŨĂ = ""; int ŦĂ = 0; int ź = 0; int ś = 0; int ĻĂ = 0; bool ňă()
        {
            Ġā(); ŧĂ = (č.Count > 0) && (ŦĂ > 0 || ŢĂ.Count > 0); ŨĂ = ""; if (č.Count == 0) ŨĂ += "Virtual Mass unfuntional!\n"; if (ŦĂ == 0 && ŢĂ.Count == 0) ŨĂ += "Generators unfuntional!\n";
            if (ś == 0) ŨĂ += "Thrusters unfuntional!\n"; if (ĻĂ == 0) ŨĂ += "Gyros unfuntional!\n"; if (shipController == null) Ćă = "Cockpit lost!"; else Ćă = null; return Ćă == null;
        }
        Ũā ŤĂ = new Ũā(); Ũā Ŗ = new Ũā(); IMyThrust[] Ŝ = new IMyThrust[6]; IMyGravityGenerator ŽĂ = null; bool ŧĂ = false;
        bool ūĂ = true; double ŰĂ = 1.0; Vector3D ŲĂ = new Vector3D(1, 1, 1); bool Ź = false; bool ĄĄ = false; bool Đ = false; bool ď = false; bool Ÿ = false; bool ŵ = false; bool DampenersOverride = false; bool ŕ = true; bool œ = false; bool Ŕ = true; bool Œ = false; long Ļ = 0;
        Vector3D? ş = null; Vector3D Ş = Vector3D.Zero; Vector3D Š = new Vector3D(1, 1, 1); float Ŵ = 0; Vector3D ŝ = Vector3D.Zero; Vector3D źĂ = Vector3D.Zero; Vector3D Ŭā = Vector3D.Zero; Vector3D ōĂ = Vector3D.Zero; Vector3D ė = Vector3D.Zero; Vector3D Velocity = Vector3D.Zero;
        double ŎĂ = 0; double Ě = 0; long ĸ = 180; int ħ = 0; int Ħ = 0; int Ī = 0; int ĩ = 0; int Ĩ = 0; long ń = long.MinValue; long ņ = long.MinValue; Vector3D ęĂ = Vector3D.Zero; Vector3D ĚĂ = Vector3D.Zero; Vector3D JumpLookAt = Vector3D.Zero; Vector3D I = Vector3D.Zero; Vector3D Last = Vector3D.Zero;
        bool šā = false; bool Ţā = false; bool ŵă = false; bool Ă = false; bool Šā = false; bool ThrUsed = false; string ũĂ = null; string ĞĂ = null; List<őĂ> ĠĂ = new List<őĂ>(); List<őĂ> ųā = new List<őĂ>(); int ğĂ = 0; int MarkedUpdateIndex = 0; const long Ąā = 72000; long ŉ = 0;
        long ļ = Ąā; long ľ = 0; long ō = 320; long Ŏ = 0; long Ĺ = long.MaxValue; long ęă = 240; long ĺ = long.MaxValue; long Ŋ = long.MaxValue; bool ŝă = false; Vector3D DodgeVector = new Vector3D(100, 0, 10); void Ėă()
        {
            long ŸĂ = (ĥĂ(ļ)) ? 30 : ģā; var dt = 60 / ŸĂ; if (ŵă && ŸĂ > 3) ŸĂ = 3;
            ŉ = ŏ(ŸĂ); bool Śā = ĥĂ(ľ); var Matrix = shipController.WorldMatrix; var ŁĂ = Matrix; IMyTerminalBlock ăĄ = null; ş = null; if (ĥĂ(Ĺ))
            {
                Ĺ = ŏ(60); int Žā = 100, ĭ = 0; ĭ = 0; bool ĕă = false; int it = ħ; if (ČĂ)
                {
                    if (it < Ŗ.L.Count) ĭ = Ģ(Ŗ.L, it, Žā);
                    else
                    {
                        it -= Ŗ.L.Count; if (it < Ŗ.R.Count)
                            ĭ = Ģ(Ŗ.R, it, Žā);
                        else
                        {
                            it -= Ŗ.R.Count; if (it < Ŗ.U.Count) ĭ = Ģ(Ŗ.U, it, Žā);
                            else
                            {
                                it -= Ŗ.U.Count; if (it < Ŗ.D.Count) ĭ = Ģ(Ŗ.D, it, Žā);
                                else
                                {
                                    it -= Ŗ.D.Count; if (it < Ŗ.B.Count) ĭ = Ģ(Ŗ.B, it, Žā); else { it -= Ŗ.B.Count; if (it < Ŗ.F.Count) ĭ = Ģ(Ŗ.F, it, Žā); else { ĕă = true; ħ = 0; } }
                                }
                            }
                        }
                    }
                    ħ += ĭ; Žā -= ĭ; ĭ = Ģ(ļĂ, Ĩ, Žā); Ĩ += ĭ; Žā -= ĭ; ĕă &= ĭ < Žā;
                }
                else ĕă = true; if (ĕă)
                {
                    ĕă = false; it = Ī; if (it < ŤĂ.L.Count) ĭ = Ģ(ŤĂ.L, it, Žā);
                    else
                    {
                        it -= ŤĂ.L.Count; if (it < ŤĂ.R.Count) ĭ = Ģ(ŤĂ.R, it, Žā);
                        else
                        {
                            it -= ŤĂ.R.Count; if (it < ŤĂ.U.Count) ĭ = Ģ(ŤĂ.U, it, Žā);
                            else { it -= ŤĂ.U.Count; if (it < ŤĂ.D.Count) ĭ = Ģ(ŤĂ.D, it, Žā); else { it -= ŤĂ.D.Count; if (it < ŤĂ.B.Count) ĭ = Ģ(ŤĂ.B, it, Žā); else { it -= ŤĂ.B.Count; if (it < ŤĂ.F.Count) ĭ = Ģ(ŤĂ.F, it, Žā); else { ĕă = true; Ī = 0; } } } }
                        }
                    }
                    Ī += ĭ; Žā -= ĭ;
                }
                ĭ = Ģ(Ď, Ħ, Žā); ĕă &= ĭ == Ď.Count;
                Ħ += ĭ; Žā -= ĭ; ĭ = Ģ(ŢĂ, ĩ, Žā); ĩ += ĭ; Žā -= ĭ; ĕă &= ĭ < Žā; if (ĕă) Ĺ = long.MaxValue;
            }
            var ģă = shipController.GetPosition(); var ıā = řĂ(shipController); if (ıā.IsZero()) ņ = ŏ(60); var Şă = false; if (Śā)
            {
                ĄĄ = false; foreach (var b in aiFlight) if (b.IsAutoPilotEnabled) { ĄĄ = true; break; }
                if (!ĄĄ) foreach (var b in remotes) if (b.IsAutoPilotEnabled) { ĄĄ = true; break; }
                var ĢĂ = (ęĂ - ģă).LengthSquared(); var ĥă = ģă + Matrix.Forward * łĂ; if (1e+6 < ĢĂ && ĢĂ < 1e+8)
                {
                    ń = long.MinValue; if (!ĽĂ && ĤĂ(ĚĂ - ĥă + ģă - ęĂ, 1)) ĞĂ = "!Linear!";
                    else
                    {
                        ń = ŏ(600);
                        JumpLookAt = ĚĂ; ļ = ŏ(Ąā); ĞĂ = "Active"; Şă = true; if (ĠĂ.Count > ġĂ) ĠĂ.RemoveAt(0); ĠĂ.Add(new őĂ("jumped", ęĂ, Color.Black));
                    }
                }
                ęĂ = ģă; ĚĂ = ĥă; if (ĥĂ(ń)) ĞĂ = (ŃĂ) ? "Waiting" : "Off";
            }
            ė = shipController.GetShipVelocities().LinearVelocity; Ě = ė.LengthSquared(); Velocity = Ňā(Matrix, ė);
            if (ĥĂ(ĸ) || (PhysicalMass == 0 && Śā)) { ĸ = ŏ(600); ōĂ = shipController.GetNaturalGravity(); ŎĂ = ōĂ.LengthSquared(); CalculateMassInfo(); }
            DampenersOverride = shipController.DampenersOverride || Żă; if (ĥĂ(ĺ)) { ĺ = ŏ(120); if ((ĜĂ - ŌĂ).LengthSquared() > 4) { Ń = ŏ(60); Č(); } }
            if (!şă && !ĄĄ && ĥĂ(Ļ))
            {
                Ļ = ŏ(10);
                ŝ = ŚĂ() * G;
            }
            Đ = false; var ŻĂ = Vector3D.Zero; Ŭā = (shipController != null && shipController.IsUnderControl) ? (Vector3D)shipController.MoveIndicator : Vector3D.Zero; var Žă = 0.0; Vector3D? Ť = null, ť = null; ũĂ = null; ŵă &= ŧā < connectors.Count && Źă != null && Ŭă < Źă.Count; if (ŃĂ && !ĥĂ(ń) && !ŵă)
            { ũĂ = "Jump Detected!"; Ť = Ňā(Matrix, JumpLookAt - ģă); Žă = 0.1; }
            if (ŵă)
            {
                ũĂ = "Parking"; Şă = true; Ŏ = 0; ěā(1); ŲĂ = new Vector3D(1, 1, 1); Ť = null; ť = null; var Īă = connectors[ŧā]; var ĩă = Īă.GetPosition(); var ųă = Źă[Ŭă]; if (Śā && ĄĄ) { ĝā(aiFlight, false); ĝā(remotes, false); }
                bool ĝĂ = !Ŭā.IsZero(); bool Ūă = Īă.Status == MyShipConnectorStatus.Connected || Īă.Enabled == false || shipController.DampenersOverride || ĝĂ; if (ĝĂ) shipController.DampenersOverride = true; if (Īă.Status == MyShipConnectorStatus.Connectable) Īă.Connect(); if (Ūă) { ŵă = false; ş = Vector3D.Zero; }
                else
                {
                    Đ = true; var ğă = ųă.Ĥă - ĩă; var Ĝă = ğă.LengthSquared(); var Ę = ğă + ųă.Forward * 50; var ę = 100.0; if (Ĝă < 44000) { ăĄ = Īă; ŁĂ = ăĄ.WorldMatrix; Ť = Ňā(ŁĂ, ųă.Position - ĩă); Ť = Ňā(ŁĂ, -ųă.Forward); if (Ŵă) ť = ųă.Up; } else Ť = Ňā(ŁĂ, Ę); if (Ĝă < 12100)
                    {
                        var īă = ğă + ųă.Forward * 2.6; Ĝă = īă.LengthSquared(); var ķā = īă - (īă.Dot(ųă.Forward) * ųă.Forward); var tvN = safeNormalize(Ť.Value); var AlignErr = new Vector2D(tvN.X, tvN.Y).LengthSquared(); bool īā = (ķā.LengthSquared() < 16) && (AlignErr < 0.001) && (tvN.Z >= 0);
                        ę = (Ĝă > 25) ? 7.5 : 3; if (īā) { Ę = īă; } else { Ę += ķā; ę = Math.Min(ũă[0], 20); }
                        Ť = Ňā(ŁĂ, -ųă.Forward);
                    }
                    else ę = (Ĝă < 44000) ? 20 : ((Ĝă < 1e+6) ? ũă[2] : ((Ĝă < 4e+6) ? ũă[1] : ũă[0])); ş = ŻĂ = (Velocity - Ňā(Matrix, ųă.Velocity) - safeNormalize(Ňā(Matrix, Ę), ę)) / ŸĂ;
                    ŻĂ /= MassRatio; if (ūĂ) ş = null;
                }
                ļ = ŏ(Ąā);
            }
            else if (şă)
            {
                ũĂ = "Attach to Thrusters!"; Şă = true; if (Śā)
                {
                    Ŝ[0] = ŖĂ(Ŗ.R) as IMyThrust; Ŝ[1] = ŖĂ(Ŗ.L) as IMyThrust; Ŝ[2] = ŖĂ(Ŗ.U) as IMyThrust; Ŝ[3] = ŖĂ(Ŗ.D) as IMyThrust; Ŝ[4] = ŖĂ(Ŗ.B) as IMyThrust; Ŝ[5] = ŖĂ(Ŗ.F) as IMyThrust;
                    Ă = false; foreach (var t in Ŝ) if (t == null) { Ă = true; break; }
                }
                ŻĂ.X = ŜĂ(Ŝ[0]) - ŜĂ(Ŝ[1]); ŻĂ.Y = ŜĂ(Ŝ[2]) - ŜĂ(Ŝ[3]); ŻĂ.Z = ŜĂ(Ŝ[4]) - ŜĂ(Ŝ[5]); ŻĂ *= G; if (!ĤĂ(ŻĂ, 0.001)) ļ = ŏ(Ąā); Đ = true;
            }
            else if (ĄĄ)
            {
                ũĂ = "AI AutoPilot"; Şă = true; ŻĂ = Vector3D.Zero;
                ļ = ŏ(Ąā);
            }
            else if (Ŋ < long.MaxValue)
            {
                ũĂ = "Alwayse Dodge"; Şă = true; Đ = ŎĂ < ŭĂ; if (ĥĂ(Ŋ)) { Ŋ = ŏ(ęă); var tmp = DodgeVector.X; DodgeVector.X = -DodgeVector.Y; DodgeVector.Y = tmp; }
                ŻĂ = DodgeVector / MassRatio; Đ &= !Ŭā.IsZero() || !ĤĂ(ŻĂ, 0.01); ŻĂ = ě(ŝ, ě(Ŭā * G, ŻĂ / ŸĂ));
                if (!ĤĂ(Ŭā, 0.001)) ļ = ŏ(Ąā);
            }
            else { Đ = ŎĂ < ŭĂ; if (DampenersOverride) ŻĂ = Velocity / MassRatio; Đ &= !Ŭā.IsZero() || !ĤĂ(ŻĂ, 0.01); ŻĂ = ě(ŝ, ě(Ŭā * G, ŻĂ / ŸĂ)); if (!ĤĂ(Ŭā, 0.001)) ļ = ŏ(Ąā); }
            bool ĪĂ = ŝ.X == 0 && Ŭā.X == 0; bool IsFreeY = ŝ.Y == 0 && Ŭā.Y == 0; bool ĨĂ = ŝ.Z == 0 && Ŭā.Z == 0;
            if (űĂ != null) ŻĂ = safeNormalize(ŻĂ, űĂ.Value); ŲĂ.X = (ĪĂ) ? Şā(ŲĂ.X, Math.Sign(ŻĂ.X) != Math.Sign(źĂ.X), Ăă, ŰĂ) : ŰĂ; ŲĂ.Y = (IsFreeY) ? Şā(ŲĂ.Y, Math.Sign(ŻĂ.Y) != Math.Sign(źĂ.Y), Ăă, ŰĂ) : ŰĂ; ŲĂ.Z = (ĨĂ) ? Şā(ŲĂ.Z, Math.Sign(ŻĂ.Z) != Math.Sign(źĂ.Z), Ăă, ŰĂ) : ŰĂ;
            źĂ = ŻĂ; ŻĂ *= ŲĂ; Đ &= ūĂ && !ĤĂ(ŻĂ, 0.001); Ÿ = Đ; bool ī = false; if (Đ) { ţĂ(ŻĂ); if (Ź) { œĂ = (Ŵ >= 0) ? "Pull" : "Push"; var f = (Ŵ >= 0) ? G : -G; ėā(šĂ, f); ėā(ŠĂ, f); } else { ī = true; Ż(ŻĂ, ċā); } } else { ī = true; ŔĂ(); }
            if (ī) œĂ = (Ŵ == 0) ? "Floor" : (Ŵ >= 0) ? "Pull" : "Push";
            if (ď != Đ) { ŤĂ.Enabled = ď = Đ; Ğā(č, Đ); }
            if (ŵ != Ÿ) { ŵ = Ÿ; Ğā(šĂ, Ÿ); Ğā(ŠĂ, !Đ); }
            Šā = !ŧĂ || !ūĂ || şă || (ŎĂ > ŗ) || (Ě < ř) || (Ŭā.IsZero() && DampenersOverride) || ş != null; if (ş != null)
            {
                ThrUsed = true; var tf = ş.Value; Š.X = (ĪĂ) ? Şā(Š.X, Math.Sign(tf.X) != Math.Sign(Ş.X), Ăă, 1) : 1;
                Š.Y = (IsFreeY) ? Şā(Š.Y, Math.Sign(tf.Y) != Math.Sign(Ş.Y), Ăă, 1) : 1; Š.Z = (ĨĂ) ? Şā(Š.Z, Math.Sign(tf.Z) != Math.Sign(Ş.Z), Ăă, 1) : 1; Ş = tf; Đā(tf * Š * PhysicalMass);
            }
            else { if (ThrUsed) Đā(Vector3D.Zero); Ş = Vector3D.Zero; Š = new Vector3D(1, 1, 1); ThrUsed = false; }
            if (Šā) { if (Œ) { Ğā(Ŗ.B, Ŕ); Œ = false; } else Ŕ = ĦĂ(Ŗ.B); } else { if (Ŕ) Œ = true; Ğā(Ŗ.B, false); }
            if (Šā) { if (œ) { Ŗ.Enabled = ŕ; œ = false; } else ŕ = Ŗ.ĬĂ(); } else { if (ŕ) œ = true; Ŗ.Enabled = false; }
            var Ĳā = ŁĂ.Right; Ţā = false; var ĀĄ = Vector3D.Zero; if (ľĂ && Ť == null && ť == null && ŎĂ > 0)
            {
                Ţā = true; var Z = Ŭā.Z; var X = Ŭā.X; if (DampenersOverride) { if (X == 0) X += Velocity.X * Velocity.X * Velocity.X / ŸĂ; if (Z == 0) Z += Velocity.Z * Velocity.Z * Velocity.Z / ŸĂ; }
                X = āă(X, -ĿĂ.X, ĿĂ.X); Z = āă(Z, -ĿĂ.Y, ĿĂ.Y); var vCtrl = ŁĂ.Backward * (1 - Z) + ŁĂ.Up * Z;
                Ĳā = ŁĂ.Right * (1 - X) + ŁĂ.Up * X; ĀĄ.X = ıā.X * (1 - Z * Z) * (1 - X * X); ĀĄ.Y = vCtrl.Dot(safeNormalize(ōĂ)); ť = -ōĂ;
            }
            bool ĂĄ = false; if (Ť != null)
            {
                Ţā = true; var tv = Ť.Value; if (tv.Z < 0) { ĀĄ = new Vector3D((tv.X > 0) ? -1 : 1, 0, 0); ť = null; I = Last = Vector3D.Zero; }
                else
                {
                    tv = safeNormalize(tv); var żă = new Vector2D(tv.X, tv.Y); ĀĄ.X = -Math.Asin(żă.X); ĀĄ.Y = Math.Asin(żă.Y); var āĄ = ĀĄ.LengthSquared(); var D = (ĀĄ - Last) * dt; D.X = -D.X; Last = ĀĄ; I = (āĄ < 0.0005) ? (I + ĀĄ / ŸĂ) : Vector3D.Zero; ĀĄ += I * 0.001 - new Vector3D(ŁĂ.Down.Dot(D), ŁĂ.Left.Dot(D), 0);
                    if (āĄ > 0.25) ť = null; ĂĄ = (āĄ < Žă);
                }
            }
            else I = Last = Vector3D.Zero; if (ť != null) { Ţā = true; ĀĄ.Z = Ĳā.Dot(safeNormalize(ť.Value)); ĂĄ &= (ĀĄ.Z > Žă); }
            if (ĂĄ) { ń = long.MinValue; }
            if (ŀĂ && Ť == null && ť == null && ŧĂ && ūĂ) { Ţā = true; ĀĄ = ıā / ŸĂ / 2; }
            if (Ţā)
            { ĕā(ŅĂ, ăĄ ?? shipController); Ėā(ŅĂ, ĀĄ); }
            if (Şă != ŝă) { ŝă = Şă; ĝā(ĉĂ, Şă); }
            if (šā != Ţā) { Ĕā(ŅĂ, Ţā); šā = Ţā; }
            if (ūĂ) Ēā(ŻĂ.Z < -0.01, ŻĂ.Z > 0.01); else { ũĂ = "GDrive Off"; œĂ = "Off"; }
            if (ĥĂ(ľ)) ľ = ŏ(60); if (ĥĂ(Ł)) { Ł = long.MaxValue; űĂ = null; ěā(1); }
            if (ĥĂ(ō))
            { ō = ŏ(300); for (int i = 0; i < connectors.Count; i++) { var c = connectors[i]; c.ShowOnHUD = i == ŧā && !ĥĂ(Ŏ); } }
        }
        string œĂ = ""; void ŔĂ()
        {
            if ((šĂ.Count > 0 || ŠĂ.Count > 0) && (Ŵ != 0 || Ź) && ūĂ) { var f = (Ŵ >= 0) ? G : -G; ėā(šĂ, f); ėā(ŠĂ, f); if (ŽĂ != null) ŽĂ.Enabled = false; Ÿ = true; return; }
            if (ŽĂ != null) { ėā(šĂ, 0); ėā(ŠĂ, 0); ŽĂ.Enabled = true; ŽĂ.GravityAcceleration = ġ; Ÿ = false; }
        }
        void Ż(Vector3D f, bool ğ = false) { foreach (var sp in šĂ) { var a = (float)sp.Ĝ.Dot(f); sp.AsGravityGenBase.GravityAcceleration = (ğ) ? SignG(a) : a; } foreach (var sp in ŠĂ) sp.AsGravityGenBase.GravityAcceleration = 0; }
        void ţĂ(Vector3D f) { var x = (float)f.X; ėā(ŤĂ.R, -x); ėā(ŤĂ.L, x); var y = (float)f.Y; ėā(ŤĂ.U, -y); ėā(ŤĂ.D, y); var z = (float)f.Z; ėā(ŤĂ.B, -z); ėā(ŤĂ.F, z); }
        static IMyThrust ŖĂ<T>(List<T> list) { IMyThrust res = null; foreach (var i in list) { var t = (i as BlockWrapper).AsThrust; if (t != null && t.IsWorking) { res = t; if (t.ThrustOverride > 0) break; } } return res; }
        static IMyTerminalBlock ŗĂ<T>(List<T> list) { foreach (var i in list) { var t = (i as BlockWrapper).block; if (t != null && t.IsWorking) return t; } return null; }
        Vector3D ŚĂ() { return new Vector3D(ŘĂ(Ŗ.R) - ŘĂ(Ŗ.L), ŘĂ(Ŗ.U) - ŘĂ(Ŗ.D), ŘĂ(Ŗ.B) - ŘĂ(Ŗ.F)); }
        float ŘĂ(List<BlockWrapper> direct)
        { float s = 0; int c = 0; foreach (var b in direct) { var t = b.AsThrust; if (t != null && t.ThrustOverridePercentage > 0) { s += t.ThrustOverridePercentage; c++; } } return (c > 0) ? (s / c) : 0; }
        void Đā(Vector3D f)
        {
            var x = (float)f.X; ďā(Ŗ.R, x); ďā(Ŗ.L, -x); var y = (float)f.Y; ďā(Ŗ.U, y); ďā(Ŗ.D, -y); var z = (float)f.Z; ďā(Ŗ.B, z); ďā(Ŗ.F, -z);
        }
        void ďā(List<BlockWrapper> l, double f) { foreach (var Űā in l) { var thr = Űā.AsThrust; if (thr != null) { var mf = thr.MaxEffectiveThrust; var tf = (f > mf) ? mf : f; thr.ThrustOverride = (float)tf; f -= tf; } } }
        double Şā(double f, bool ĈĄ, double c, double m) { return (ĈĄ) ? f * c + 0.001 : Math.Min(f * (1f + c / 5f), m); }
        string Ĵă = "_on"; string ĵă = "_off"; bool? ŝĂ(ref string ļă) { if (ļă.StartsWith(Ĵă)) { ļă = ļă.Substring(Ĵă.Length).TrimStart(); return true; } else if (ļă.StartsWith(ĵă)) { ļă = ļă.Substring(Ĵă.Length).TrimStart(); return false; } return null; }
        void Ēā(bool Œă, bool Ŝă)
        { ĝā(ćĂ, ūĂ && Œă); ĝā(ĈĂ, ūĂ && Ŝă); ĝā(ĊĂ, ūĂ); }
        double? űĂ = null; const string Żā = "Power"; string Čā = Żā + "=100.0%"; void ěā(double v, long Ő = long.MaxValue) { ŰĂ = āă(v, 0.001, 1); űĂ = null; Čā = Żā + "=" + (ŰĂ * 100).ToString("f1") + "%"; Ł = Ő; }
        void Ěā(double v, long Ő = long.MaxValue) { ŰĂ = 1; űĂ = v; Čā = "GD.Limit=" + v.ToString("f2") + "g"; Ł = Ő; }
        long Ł = 0; string Ķă = "maxsphere"; string ĸă = "gmode"; string Ĺă = "enable"; string ıă = "gdpower"; string ĳă = "myconnector"; string Ĳă = "onoff"; string ĺă = "aps";
        void Ěă(string ļă)
        {
            var ķă = ļă.ToLower(); var Blocks = new List<IMyTerminalBlock>(); if (ķă.StartsWith(Ķă))
            {
                ķă = ķă.Substring(Ķă.Length).TrimStart(); bool? Need = ŝĂ(ref ķă); var S_ = Ŵ; if (ķă.StartsWith("pull")) Ŵ = G; else if (ķă.StartsWith("push")) Ŵ = -G;
                Ź = (Ŵ == S_) ? (Need ?? !Ź) : (Need != false); ĝā(āĂ, Ź);
            }
            else if (ķă.StartsWith(ĸă))
            {
                ķă = ķă.Substring(ĸă.Length).TrimStart(); float? ţā = null; if (ķă.StartsWith("floor")) { ķă = ķă.Substring(5).TrimStart(); ţā = 0; } else if (ķă.StartsWith("pull")) { ķă = ķă.Substring(4).TrimStart(); ţā = G; } else if (ķă.StartsWith("push")) { ķă = ķă.Substring(4).TrimStart(); ţā = -G; }
                if (ţā != null) { Ŵ = ţā.Value; if (ţā == 0) Ź = false; } else { if (Ŵ > 0) Ŵ = -G; else if (Ŵ == 0) Ŵ = G; else Ŵ = (Ź || ŽĂ == null) ? G : 0; }
            }
            else if (ķă.StartsWith(Ĺă))
            {
                ķă = ķă.Substring(Ĺă.Length).TrimStart(); ļă = ļă.Substring(Ĺă.Length).TrimStart(); var Need = ŝĂ(ref ķă); ļă = ļă.Substring(ļă.Length - ķă.Length); if (ņĂ != "") { if (ŇĂ != null) ŇĂ.GetBlocksOfType(Blocks, b => (b.CustomName.Contains(ļă))); }
                else GridTerminalSystem.GetBlocksOfType(Blocks, b => (b.CubeGrid == Me.CubeGrid && b.CustomName.Contains(ļă))); bool on = Need ?? !īĂ(Blocks); ĝā(Blocks, on);
            }
            else if (ķă.StartsWith(ıă))
            {
                ķă = ķă.Substring(ıă.Length).TrimStart(); if (ķă.StartsWith("_off"))
                {
                    ķă = ķă.Substring(4).TrimStart(); if (ŰĂ < 1) { ěā(1); Ł = long.MaxValue; return; }
                }
                string Ų = ķă, sTime = ""; int sp = ķă.IndexOf(' '); if (sp >= 0) { Ų = ķă.Substring(0, sp).TrimEnd(); sTime = ķă.Substring(sp).TrimStart(); }
                bool Œā = Ų.EndsWith("%"); if (Œā) Ų = Ų.Substring(0, Ų.Length - 1); float p, t; if (!float.TryParse(Ų, out p)) return;
                if (Œā) ěā(p / 100); else Ěā(p); Ł = ((ŰĂ < 1 || űĂ != null) && float.TryParse(sTime, out t) && t > 0) ? ŏ((long)(t * 3600)) : long.MaxValue;
            }
            else if (ķă.StartsWith(ĺă))
            {
                ķă = ķă.Substring(ĺă.Length).TrimStart(); ŵă = ŝĂ(ref ķă) ?? !ŵă; if (shipController != null) shipController.DampenersOverride = true;
                if (ŵă) { shipController.DampenersOverride = false; ŲĂ = new Vector3D(ŰĂ, ŰĂ, ŰĂ); }
                Đā(Vector3D.Zero);
            }
            else if (ķă.StartsWith(ĳă))
            {
                if (ņĂ != "") { if (ŇĂ != null) ŇĂ.GetBlocksOfType(connectors); } else GridTerminalSystem.GetBlocksOfType(connectors, b => (b.CubeGrid == Me.CubeGrid)); if (connectors.Count > 0 && !ŵă)
                {
                    var N = connectors.Count; ķă = ķă.Substring(ĳă.Length).TrimStart(); Ŏ = ŏ(1200); ō = 0; if (ķă == "++") ŧā = (ŧā + 1) % N; else if (ķă == "--") ŧā = (ŧā + N - 1) % N; else if (ķă != "") for (int i = 0; i < N; i++) if (connectors[i].CustomName.ToLower().Contains(ķă)) ŧā = i;
                }
            }
            else if (ķă.StartsWith(Ĳă)) { ķă = ķă.Substring(Ĳă.Length).TrimStart(); ūĂ = ŝĂ(ref ķă) ?? !ūĂ; Ēā(false, false); } else if (Łā(ļă, true)) ž();
        }
        List<ķĂ> ļĂ = new List<ķĂ>(); List<ķĂ> ŅĂ = new List<ķĂ>(); long ň = 300; void ėă()
        {
            ň = ŏ(600); ĻĂ = 0; ŅĂ.Clear(); foreach (var b in ļĂ)
            { var g = b.AsGyro; bool ĆĄ = false; if (g != null) { if (g.IsFunctional) { ĻĂ++; ĆĄ = ŅĂ.Count < ńĂ; } g.GyroOverride = Ţā && ĆĄ; } b.ĈĄ = ĆĄ; if (ĆĄ) ŅĂ.Add(b); }
        }
        List<BlockWrapper> Ď = new List<BlockWrapper>(); List<BlockWrapper> č = new List<BlockWrapper>(); string sgps(Vector3D p) { return "{" + p.X.ToString("f3") + "," + p.Y.ToString("f3") + "," + p.Z.ToString("f3") + "}"; }
        const string ŷā = "--No Function--"; string ħā = ŷā; string Ĩā = ""; int ċ = 0; int Ŧă = 0, Ž = 0; Vector3D ĜĂ = Vector3D.Zero; Vector3D ŵĂ = Vector3D.Zero; void Č()
        {
            if (shipController == null) return; ĜĂ = ŌĂ; ħā = ŷā; č.Clear(); var V = Vector3D.Zero; VirtualMass = 0; ŵĂ = Vector3D.Zero; ċ = 0; var C = shipController.CenterOfMass; PhysicalMass = shipController.CalculateShipMass().PhysicalMass; foreach (var vb in Ď)
            { vb.ĈĄ = (vb.block != null) && (vb.block.IsFunctional); if (vb.ĈĄ) { V += (vb.block.GetPosition() - C) * vb.AsMass.VirtualMass; VirtualMass += vb.AsMass.VirtualMass; č.Add(vb); } }
            if (VirtualMass <= 0) return; ŵĂ = Ňā(shipController.WorldMatrix, V / VirtualMass); MassRatio = Math.Max((PhysicalMass > 0) ? VirtualMass / PhysicalMass : 1, 0.0001); var ŴĂ = ŵĂ - ĶĂ;
            ħā = "X:" + ŵĂ.X.ToString("f2") + " Y:" + ŵĂ.Y.ToString("f2") + " Z:" + ŵĂ.Z.ToString("f2"); if (ĳĂ > 0 && !ĤĂ(ŴĂ, ĴĂ))
            {
                ħā = "ERROR:AUTO-BALANCE IS ON"; var ĝ = ŴĂ.LengthSquared(); int Žā = Math.Min(č.Count - 1, ĳĂ); while (ċ < ĳĂ)
                {
                    var œă = Vector3D.Zero; var Ŕă = double.MaxValue;
                    int Ŗă = int.MaxValue; var ŕă = double.MaxValue; for (int i = 0; i < č.Count; i++)
                    {
                        var b = č[i].AsMass; var BlockV = b.GetPosition() - C; var Vi = V - BlockV * b.VirtualMass; var Ųā = VirtualMass - b.VirtualMass; var ċĂ = (Vi / Ųā - ĶĂ).LengthSquared(); if (ċĂ < ŕă)
                        { Ŗă = i; œă = Vi; Ŕă = Ųā; ŕă = ċĂ; }
                    }
                    if (Ŗă >= č.Count || ĝ < ŕă) break; ċ++; č[Ŗă].ĈĄ = false; č.RemoveAt(Ŗă); var şā = œă / Ŕă; var Pr = ŵĂ.Dot(şā); ŵĂ = şā; ŴĂ = ŵĂ - ĶĂ; VirtualMass = Ŕă; V = œă; ĝ = ŕă;
                }
            }
            Ğā(Ď, ď = false); Ĩā = ŵĂ.ToString("f2"); ĺ = (ċ > 0) ? ŏ(300) : long.MaxValue; Ŧă = Ž = 0; foreach (var vb in Ď)
                if (vb.block != null) { if (vb.block is IMyArtificialMassBlock) Ŧă++; if (vb.block is IMySpaceBall) Ž++; }
            var m = shipController.WorldMatrix; m.Translation = C + ŵĂ; foreach (var s in ŢĂ) s.čā(m);
        }
        List<ťā> ŢĂ = new List<ťā>(); List<ťā> šĂ = new List<ťā>(); List<ťā> ŠĂ = new List<ťā>(); Vector3D ųĂ = Vector3D.Zero;
        string sBalanceSp = ""; void ż()
        {
            if (shipController == null) return;
            var V = Vector3D.Zero; ųĂ = Vector3D.Zero; šĂ.Clear(); ŠĂ.Clear(); var C = shipController.CenterOfMass + ņā(shipController.WorldMatrix, ųĂ); foreach (var vb in ŢĂ) { vb.ĈĄ = (vb.block != null) && (vb.block.IsFunctional); if (vb.ĈĄ) { V += (vb.block.GetPosition() - C); šĂ.Add(vb); } }
            if (šĂ.Count > 0)
            {
                ųĂ = Ňā(shipController.WorldMatrix, V / šĂ.Count); var ŴĂ = ųĂ - ŷ; if (ĵĂ > 0 && !ĤĂ(ŴĂ, Ŷ))
                {
                    var ĝ = ŴĂ.LengthSquared(); int Žā = Math.Min(šĂ.Count - 1, ĵĂ); while (ŠĂ.Count < ĵĂ)
                    {
                        var œă = Vector3D.Zero; int Ŗă = int.MaxValue; var ŕă = double.MaxValue; int n1 = šĂ.Count - 1;
                        for (int i = 0; i < šĂ.Count; i++) { var b = šĂ[i].block; var BlockV = b.GetPosition() - C; var Vi = V - BlockV; var ċĂ = (Vi / n1 - ŷ).LengthSquared(); if (ċĂ < ŕă) { Ŗă = i; œă = Vi; ŕă = ċĂ; } }
                        if (Ŗă >= šĂ.Count || ĝ < ŕă) break; var g = šĂ[Ŗă]; g.ĈĄ = false; ŠĂ.Add(g); šĂ.RemoveAt(Ŗă);
                        var şā = œă / n1; var Pr = ųĂ.Dot(şā); ųĂ = şā; ŴĂ = ųĂ - ŷ; V = œă; ĝ = ŕă;
                    }
                }
            }
            Ğā(ŢĂ, ŵ = Ÿ = false); sBalanceSp = ųĂ.ToString("f2"); ź = šĂ.Count + ŠĂ.Count;
        }
        double VirtualMass = 0;
        double PhysicalMass = 0;
        double MassRatio = 1;
        Vector3D ŌĂ = Vector3D.Zero;

        void CalculateMassInfo()
        {
            var m = shipController.CalculateShipMass();
            var worldMatrix = shipController.WorldMatrix;
            ŌĂ = Ňā(worldMatrix, shipController.CenterOfMass - shipController.CubeGrid.GetPosition());
            PhysicalMass = m.PhysicalMass;
            VirtualMass = 0;
            foreach (var v in č)
                VirtualMass += v.AsMass.VirtualMass;

            MassRatio = Math.Max((PhysicalMass > 0) ? VirtualMass / PhysicalMass : 1, 0.0001);
        }
        class BlockWrapper
        {
            public BlockWrapper(IMyTerminalBlock b, int d, float angle, bool a = false)
            {
                block = b;
                ĈĄ = a;
                Angle = angle;
                Ĕă = d;
                isLarge = b.DefinitionDisplayNameText.Contains("Large");
            }

            public IMyVirtualMass AsMass
            {
                get { return block as IMyVirtualMass; }
            }
            public IMyGravityGeneratorBase AsGravityGenBase
            {
                get { return block as IMyGravityGeneratorBase; }
            }
            public IMyGravityGenerator AsGravityGen
            {
                get { return block as IMyGravityGenerator; }
            }
            public IMyThrust AsThrust
            {
                get { return block as IMyThrust; }
            }
            public IMyGyro AsGyro
            {
                get { return block as IMyGyro; }
            }
            public bool Enabled
            {
                get
                {
                    var f = block as IMyFunctionalBlock;
                    return f != null && f.Enabled;
                }
                set
                {
                    var f = block as IMyFunctionalBlock;
                    if (f != null) f.Enabled = value;
                }
            }
            public IMyTerminalBlock block = null;
            public bool ĈĄ = false;
            public void Ĭ()
            {
                Matrix orientation;
                shipController.Orientation.GetMatrix(out orientation);
                if (block != null)
                    Position = AxesTransform(Ňā(orientation, block.Position));
            }

            public int Ĕă = 1;
            public bool isLarge = false;
            public float Angle = 0;
            public Vector2I? Position = null;
        }
        static int şĂ(List<BlockWrapper> l) { int c = 0; foreach (var b in l) if (b.ĈĄ && b.block != null && b.block.IsFunctional) c++; return c; }
        static bool ĦĂ<T>(List<T> l) { foreach (var b in l) { var f = (b as BlockWrapper).block as IMyFunctionalBlock; if (f != null) return f.Enabled; } return false; }
        static bool ĭĂ<T>(List<T> l) { foreach (var b in l) { var f = (b as BlockWrapper).block as IMyFunctionalBlock; if (f != null && f.Enabled) return true; } return false; }
        static void Ğā<T>(List<T> l, bool v) { foreach (var b in l) { var f = (b as BlockWrapper).block as IMyFunctionalBlock; if (f != null) f.Enabled = v; } }
        static bool īĂ<T>(List<T> l) { foreach (var b in l) { var f = b as IMyFunctionalBlock; if (f != null && f.Enabled) return true; } return false; }
        static void ĝā<T>(List<T> l, bool v) { foreach (var b in l) { var f = b as IMyFunctionalBlock; if (f != null) f.Enabled = v; } }
        static void ėā<T>(List<T> l, float v) { foreach (var b in l) { var g = (b as BlockWrapper).AsGravityGenBase; if (g != null) g.GravityAcceleration = v; } }
        class ťā : BlockWrapper { public ťā(IMyTerminalBlock b, bool a = false) : base(b, 5, 0, a) { } public void čā(MatrixD O) { if (block != null) Ĝ = Ňā(O, O.Translation - block.GetPosition()); } public Vector3D Ĝ = Vector3D.Zero; }
        class Ũā
        {
            public Ũā() { }
            public void Add(BlockWrapper b, Base6Directions.Direction direction)
            {
                switch (śĂ(shipController.Orientation, b.block, direction))
                {
                    case (Base6Directions.Direction.Up): U.Add(b); break;
                    case (Base6Directions.Direction.Down): D.Add(b); break;
                    case (Base6Directions.Direction.Left): L.Add(b); break;
                    case (Base6Directions.Direction.Right): R.Add(b); break;
                    case (Base6Directions.Direction.Forward): F.Add(b); break;
                    case (Base6Directions.Direction.Backward): B.Add(b); break;
                }
            }
            public bool ĬĂ() { return ĦĂ(L) || ĦĂ(R) || ĦĂ(U) || ĦĂ(D) || ĦĂ(F); }
            public bool Enabled { get { return ĭĂ(R) || ĭĂ(L) || ĭĂ(U) || ĭĂ(D) || ĭĂ(F) || ĭĂ(B); } set { Ğā(R, value); Ğā(L, value); Ğā(U, value); Ğā(D, value); Ğā(B, value); Ğā(F, value); } }
            public int Count { get { return R.Count + L.Count + U.Count + D.Count + B.Count + F.Count; } }
            public int ćĄ { get { return şĂ(U) + şĂ(D) + şĂ(L) + şĂ(R) + şĂ(F) + şĂ(B); } }
            public List<BlockWrapper> R = new List<BlockWrapper>(); public List<BlockWrapper> L = new List<BlockWrapper>(); public List<BlockWrapper> U = new List<BlockWrapper>(); public List<BlockWrapper> D = new List<BlockWrapper>(); public List<BlockWrapper> B = new List<BlockWrapper>(); public List<BlockWrapper> F = new List<BlockWrapper>();
        }
        int Ģ<T>(List<T> l, int ĲĂ = 0, int ĀĂ = 30) { int c = 0; while (ĀĂ > 0 && ĲĂ < l.Count) { var Űā = l[ĲĂ] as BlockWrapper; if (Űā != null && Űā.Position == null) Űā.Ĭ(); c++; ĀĂ--; ĲĂ++; } return c; }
        string ņĂ = ""; long ģā = 10; int Ŧā = 0; string ĒĂ = "[GDRIVE]"; string Ĉ = "[GDVR]";
        string ąĂ = "[GDRIVE]"; string ĂĂ = "[GD-FORWARD]"; string ăĂ = "[GD-BACKWARD]"; string ūă = "APS.Receive"; string Űă = "APS.Sender"; string LightMarkerGJ = "[GyroJump]"; string LightMarkerSMax = "[GD-MAXSPHERE]"; string ĄĂ = "[GD-AUTOPILOT]";
        bool Ŵă = true; double[] ũă = { 100, 80, 60 }; int Ġ = 2; int ĐĂ = 8; bool ČĂ = true; bool ĎĂ = true; bool ďĂ = true; bool čĂ = true; Vector2I đĂ = Vector2I.Zero; long ĔĂ = 30; bool ęā = true; int ĳĂ = 0; double ĴĂ = 0.25; Vector3D ĶĂ = Vector3D.Zero; int ĵĂ = 0; double Ŷ = 0.25;
        Vector3D ŷ = Vector3D.Zero; string ťĂ = "[Floor]"; float ġ = 0.4f * G; float ŮĂ = 0.62f * G; double Ăă = 0.4; float ůĂ = 99.5f; float ŬĂ = 0.02f; float Ř = 0.1f * G; float Ś = 99.5f; bool ċā = false; bool şă = false; string įă = "[Main]"; double Ŕā = 0.25; double łĂ = 2000;
        bool ŀĂ = false; bool ľĂ = false; Vector2D ĿĂ = new Vector2D(0.3, 0.6); bool ćā = false; bool Żă = false; bool ŃĂ = false; bool ĽĂ = true; int ńĂ = 10; int ġĂ = 3; void ž()
        {
            Me.CustomData = "//===SCRIPT SETTIGNS===" + "\n" + "\nGroupName=" + ņĂ + "\nRuntime.Frequency=" + ģā
        + "\nRuntime.Limit = " + formatDouble(Ŕā + 0.05) + "\nGDrive.AlwaysDodge = " + ((Ŋ < long.MaxValue) ? "Yes" : "No") + "\nGDrive.DodgeFrequency = " + formatDouble(ęă / 60.0) + "\nGDrive.DodgeProjection = " + formatDouble(DodgeVector.Z) + "\nGDrive.AlwaysDamper = " + ((Żă) ? "Yes" : "No") + "\nGDrive.AutoFieldSize = " + ((ęā) ? "Yes" : "No")
        + "\nGDrive.Floor = " + formatDouble(ġ / G) + "\nGDrive.Overpowerfule = " + formatDouble(Ăă) + "\nGDrive.AttachToThruster = " + ((şă) ? "Yes" : "No") + "\nGDrive.Off.Dampernens = " + formatDouble(ůĂ) + "\nGDrive.Off.OnGravity = " + formatDouble(ŮĂ / G) + "\nGDrive.Off.MinSpeed = " + formatDouble(ŬĂ) + "\nGDrive.Off.Thrusters = " + formatDouble(Ś, "f1")
        + "\nGDrive.Thrusters.OnGravity = " + formatDouble(Ř / G) + "\nGDrive.Imbalance = " + ĳĂ + "\nGDrive.Imbalance.Tolerance = " + formatDouble(ĴĂ) + "\nGDrive.Imbalance.Vector = " + formatDouble(ĶĂ.X) + "," + formatDouble(ĶĂ.Y) + "," + formatDouble(ĶĂ.Z) + "\nGDrive.Spherical.Imbalance = " + ĵĂ + "\nGDrive.Spherical.Tolerance = " + formatDouble(Ŷ)
        + "\nGDrive.Spherical.Vector = " + formatDouble(ŷ.X) + "," + formatDouble(ŷ.Y) + "," + formatDouble(ŷ.Z) + "\nGDrive.Spherical.Projection = " + ((ċā) ? "Yes" : "No") + "\nGDrive.ShowBalance = " + ((ćā) ? "Yes" : "No") + "\nMarker.Cockpit = " + įă + "\nMarker.LCD = " + ĒĂ + "\nMarker.LCD_VR = " + Ĉ + "\nMarker.Light = " + ąĂ
        + "\nMarker.Forward = " + ĂĂ + "\nMarker.GyroJump = " + LightMarkerGJ + "\nMarker.MaxSphere = " + LightMarkerSMax + "\nMarker.Backward = " + ăĂ + "\nMarker.AutoPilot = " + ĄĂ + "\nMarker.Floor = " + ťĂ + "\nGyro.ActiveLimit = " + ńĂ + "\nGyro.AfterJump = " + ((ŃĂ) ? "Yes" : "No")
        + "\nGyro.AfterJumpLook = " + formatDouble(łĂ) + "\nGyro.AfterJumpLinear = " + ((ĽĂ) ? "Yes" : "No") + "\nGyro.Balancer = " + ((ŀĂ) ? "Yes" : "No") + "\nGyro.Horizontal = " + ((ľĂ) ? "Yes" : "No") + "\nGyro.HorizontalFly = " + (formatDouble(Math.Atan(ĿĂ.X) * Ķ) + "," + formatDouble(Math.Atan(ĿĂ.Y) * Ķ))
        + "\nAPS.Send = " + ūă + "\nAPS.Receive = " + Űă + "\nAPS.AlignUp = " + ((Ŵă) ? "Yes" : "No") + "\nAPS.Velocity = " + (ũă[0].ToString("f0") + "," + ũă[1].ToString("f0") + "," + ũă[2].ToString("f0")) + "\nLCD.My = " + Ŧā + "\nLCD.Cockpit = " + Ġ + "\nLCD.ShowThrusters = " + ((ČĂ) ? "Yes" : "No")
        + "\nLCD.ShowGenerators = " + ((ĎĂ) ? "Yes" : "No") + "\nLCD.ShowSpherical = " + ((čĂ) ? "Yes" : "No") + "\nLCD.ShowBalance = " + ((ďĂ) ? "Yes" : "No") + "\nLCD.ShowLastJump = " + (ġĂ) + "\nLCD.Scale = " + ĐĂ + "\nLCD.Offset = " + formatDouble(-đĂ.X, "f0") + "," + formatDouble(đĂ.Y, "f0")
        + "\nLCD.Frequency = " + ĔĂ + "\n\nSETTINGS.END!" + "\n\n" + "\n//===COMMANDS== = " + "\n\"" + Ĳă + "\"-Turn On/Off G-Drive" + "\n\"" + Ĺă + "\"-Enable/Disable Block" + "\n\"" + ıă + "\"-Set Lower GDrive Power" + "\n\"" + ĳă + "++\"-Select Conector for APS" + "\n\"" + ĺă + "\"-APS Active"
        + "\n\"" + ĸă + "\"-change spherical mode" + "\n\"" + Ķă + "\"-turn on/off SphereOnMax" + "\n\n\n"; Ďā = Me.CustomData.GetHashCode();
        }
        double ŗ = 0; double ř = 0; double ŭĂ = 0; long Ďā = 0; bool Łā(string Ļă, bool ŉă = false)
        {
            var Data = Ļă.Split('='); if (Data.Length >= 1)
            {
                string Ŭ = (Data.Length >= 2) ? Data[1].Trim() : "", ū = Ŭ.ToLower(); switch (Data[0].Trim().ToLower())
                {
                    case ("groupname"): ņĂ = Ŭ; return true;
                    case ("runtime.frequency"): if (ŀā(ū)) ģā = (long)āă(ĭā, 1, 20); return true;
                    case ("runtime.limit"): if (ŀā(Ŭ)) Ŕā = āă(ĭā - 0.05, 0.1, 1); return true;
                    case ("gdrive.alwaysdodge"): { var AlwaysDodge = Ŋ < long.MaxValue; ńā(ref AlwaysDodge, ū, ŉă); Ŋ = (AlwaysDodge) ? 0 : long.MaxValue; } return true;
                    case ("gdrive.dodgefrequency"): { if (ŀā(Ŭ)) ęă = (long)āă(ĭā * 60, 20, 3600); } return true;
                    case ("gdrive.dodgeprojection"): { if (ŀā(Ŭ)) DodgeVector.Z = ĭā; } return true;
                    case ("gdrive.alwaysdamper"): ńā(ref Żă, ū, ŉă); return true;
                    case ("gdrive.autofieldsize"): ńā(ref ęā, ū, ŉă); return true;
                    case ("gdrive.floor"): if (ŀā(ū)) ġ = (float)ĭā * G; return true;
                    case ("gdrive.overpowerfule"): if (ŀā(ū)) Ăă = ĭā; return true;
                    case ("gdrive.off.dampernens"): if (ŀā(ū)) ůĂ = (float)ĭā; return true;
                    case ("gdrive.off.ongravity"): if (ŀā(ū)) ŮĂ = (float)ĭā * G; return true;
                    case ("gdrive.off.minspeed"): if (ŀā(ū)) ŬĂ = (float)ĭā; return true;
                    case ("gdrive.off.thrusters"): if (ŀā(ū)) Ś = (float)ĭā; return true;
                    case ("gdrive.attachtothruster"): ńā(ref şă, ū, ŉă); return true;
                    case ("gdrive.thrusters.ongravity"): if (ŀā(ū)) Ř = (float)ĭā * G; return true;
                    case ("gdrive.imbalance"): if (ŀā(ū)) { ĳĂ = (int)ĭā; if (ŉă) Č(); } return true;
                    case ("gdrive.imbalance.tolerance"): if (ŀā(ū)) { ĴĂ = ĭā; if (ŉă) Č(); } return true;
                    case ("gdrive.imbalance.vector"): { ľā(ref ĶĂ, ū); if (ŉă) Č(); } return true;
                    case ("gdrive.spherical.imbalance"): if (ŀā(ū)) { ĵĂ = (int)ĭā; if (ŉă) ż(); } return true;
                    case ("gdrive.spherical.tolerance"): if (ŀā(ū)) { Ŷ = ĭā; if (ŉă) ż(); } return true;
                    case ("gdrive.spherical.vector"): if (ľā(ref ŷ, ū)) if (ŉă) ż(); return true;
                    case ("gdrive.spherical.projection"): ńā(ref ċā, ū, ŉă); return true;
                    case ("gdrive.showbalance"): ńā(ref ćā, ū, ŉă); return true;
                    case ("marker.cockpit"): įă = Ŭ; return true;
                    case ("marker.lcd"): ĒĂ = Ŭ; return true;
                    case ("marker.lcd_vr"): Ĉ = Ŭ; return true;
                    case ("marker.light"): ąĂ = Ŭ; return true;
                    case ("marker.forward"): ĂĂ = Ŭ; return true;
                    case ("marker.backward"): ăĂ = Ŭ; return true;
                    case ("marker.gyrojump"): LightMarkerGJ = Ŭ; return true;
                    case ("marker.maxsphere"): LightMarkerSMax = Ŭ; return true;
                    case ("marker.autopilot"): ĄĂ = Ŭ; return true;
                    case ("marker.floor"): ťĂ = Ŭ; return true;
                    case ("gyro.activelimit"): if (ŀā(ū)) ńĂ = (int)āă(ĭā, 1, 20); return true;
                    case ("gyro.afterjump"): { ńā(ref ŃĂ, ū, ŉă); if (ŉă && !ĥĂ(ń)) ń = ŏ(600); ĝā(ĆĂ, ŃĂ); } return true;
                    case ("gyro.afterjumplook"): if (ŀā(ū)) łĂ = āă(ĭā, 0, 10000); return true;
                    case ("gyro.afterjumplinear"): ńā(ref ĽĂ, ū, ŉă); return true;
                    case ("gyro.balancer"): ńā(ref ŀĂ, ū, ŉă); return true;
                    case ("gyro.horizontal"): ńā(ref ľĂ, ū, ŉă); return true;
                    case ("gyro.horizontalfly"): { var v = Ŀā(ū); if (v != null) { ĿĂ = new Vector2D(Math.Tan(āă(v.Value.X, 0, 60) / Ķ), Math.Tan(āă(v.Value.Y, 0, 60) / Ķ)); } } return true;
                    case ("aps.send"): ūă = Ŭ; return true;
                    case ("aps.receive"): Űă = Ŭ; return true;
                    case ("aps.alignup"): ńā(ref Ŵă, ū, ŉă); return true;
                    case ("aps.velocity"): { var ŭ = Ŭ.Split(','); if (ŭ.Length > 2 && ŀā(ŭ[2])) ũă[2] = Math.Max(ĭā, 10); if (ŭ.Length > 1 && ŀā(ŭ[1])) ũă[1] = Math.Max(ĭā, ũă[2]); if (ŭ.Length > 0 && ŀā(ŭ[0])) ũă[0] = Math.Max(ĭā, ũă[1]); } return true;
                    case ("lcd.my"): if (ŀā(ū)) Ŧā = (int)ĭā; return true;
                    case ("lcd.showthrusters"): { ńā(ref ČĂ, ū, ŉă); Ĺ = ŏ(60); } return true;
                    case ("lcd.showgenerators"): ńā(ref ĎĂ, ū, ŉă); return true;
                    case ("lcd.showbalance"): ńā(ref ďĂ, ū, ŉă); return true;
                    case ("lcd.showspherical"): ńā(ref čĂ, ū, ŉă); return true;
                    case ("lcd.showlastjump"): if (ŀā(ū)) { ġĂ = (int)āă(ĭā, 0, 50); while (ĠĂ.Count > ġĂ) ĠĂ.RemoveAt(0); } return true;
                    case ("lcd.cockpit"): if (ŀā(ū)) Ġ = (int)ĭā; return true;
                    case ("lcd.scale"): if (ŀā(ū)) ĐĂ = Math.Max((int)ĭā, 1); return true;
                    case ("lcd.offset"): { var v = Ŀā(ū); if (v != null) { đĂ = (Vector2I)v; đĂ.X = -đĂ.X; } } return true;
                    case ("lcd.frequency"): if (ŀā(ū)) ĔĂ = (long)āă(ĭā, 10, 120); return true;
                    case ("vr.gps"): { var s = ""; var ņă = Color.Black; var v = łā(Ŭ, out s, out ņă); if (v != null) ųā.Add(new őĂ(s, v.Value, ņă)); } break;
                }
            }
            return false;
        }
        static bool ľā(ref Vector3D v, string s, char sp = ',') { bool b = false; if (s.ToLower().Trim() == "no") { v = Vector3D.Zero; b = true; } else { double x, y, z; var ű = s.Split(sp); if (ű.Length >= 3 && double.TryParse(ű[0], out x) && double.TryParse(ű[1], out y) && double.TryParse(ű[2], out z)) { v = new Vector3D(x, y, z); b = true; } } return b; }
        static Vector2D? Ŀā(string s) { double x, y; var ű = s.Split(','); if (ű.Length >= 2 && double.TryParse(ű[0], out x) && double.TryParse(ű[1], out y)) return new Vector2D(x, y); return null; }
        static double ĭā = 0; static bool ŀā(string s) { if (s.ToLower().Trim() == "no") { ĭā = 0; return true; }; return double.TryParse(s, out ĭā); }
        static void ńā(ref bool v, string s, bool ŉă) { if ((s == "switch" || s == "") && ŉă) v = !v; else if (s == "yes" || s == "y") v = true; else if (s == "no" || s == "n") v = false; }
        string źā = "Error:No Connector!"; string Źā = "No Signal!"; long Ņ = 0; long ķ = 0; long ł = 300; long ŋ = 0; Vector2I Ŋă = Vector2I.Zero; string Īā = ""; IMyTextSurface MyLCD = null; List<IMyTextSurface> ĕĂ = new List<IMyTextSurface>(); List<ć> ĖĂ = new List<ć>(); int Ēă = 0; bool ıĂ = true; string Ŷā = "Imbalance-"; string Ÿā = "Sp.Imbalance-";
        void Đă()
        {
            Ņ = ŏ(ĔĂ); bool Ĵā = ĥĂ(ł); if (ıĂ) { foreach (var ėĂ in ĕĂ) ŉā(ėĂ); ıĂ = false; }
            if (ĥĂ(ŋ)) { ŋ = ŏ(120); Ŋă = AxesTransform(Ňā(shipController.WorldMatrix, shipController.CenterOfMass - shipController.CubeGrid.GetPosition()) / 2.5); Ŋă += đĂ; }
            if (Ēă >= ĕĂ.Count) Ēă = 0; var ų = (Ŵ < 0) ? 0f : (float)Őā;
            for (int ēĂ = 0; ēĂ < 10; ēĂ++)
            {
                if (Ēă >= ĕĂ.Count) break; var ėĂ = ĕĂ[Ēă++]; var ŹĂ = ėĂ.DrawFrame(); var ĵ = Vector2I.Zero; var Ē = ((Vector2I)(ėĂ.TextureSize - ėĂ.SurfaceSize)) >> 1; var đ = (Vector2I)ėĂ.SurfaceSize; var Ĕ = đ >> 1; var ĕ = Ē + Ĕ; var ŶĂ = đ.X / 512.0f;
                var ŷĂ = đ.Y / 512.0f; var š = (int)(60 * ŷĂ); var żā = đ >> 6; var Āā = ĕ; var ąă = ėĂ.ScriptForegroundColor; var Śă = ėĂ.ScriptBackgroundColor; var řă = Color.Lerp(Color.Darken(Śă, 0.125f), Color.Gray, 0.125f); var Ŀă = Color.Lerp(Color.Lighten(ąă, 0.125f), Color.Yellow, 0.5f);
                if (Ĵā) Čă(ŹĂ, Śă, "SquareSimple", Ē, đ);
                else if (!ĥĂ(Ŏ) && Źă != null)
                {
                    Āā.Y = Ē.Y + 10; var Ŵā = 20; ĵ.Y = Ē.Y + 50; ĵ.X = ĕ.X; čă(ŹĂ, ąă, "APS", ĵ, 1.5f * ŷĂ, TextAlignment.CENTER); var Ĥā = źā; var ņă = Color.Red; if (ŧā < connectors.Count) { var c = connectors[ŧā]; Ĥā = c.CustomName; if (c.Status == MyShipConnectorStatus.Unconnected) ņă = ąă; }
                    ĵ.X = Ē.X + Ŵā; ĵ.Y = ĕ.Y - š; čă(ŹĂ, ņă, Ĥā, ĵ, 1.35f * ŶĂ); Ĥā = Źā; ņă = Color.Red; if (Ŭă < Źă.Count) { var c = Źă[Ŭă]; Ĥā = c.Name; ņă = ąă; }
                    ĵ.X = Ē.X + đ.X - Ŵā; ĵ.Y = ĕ.Y; čă(ŹĂ, ņă, Ĥā, ĵ, 1.35f * ŶĂ, TextAlignment.RIGHT); ĵ.Y += (int)(1.35 * š); čă(ŹĂ, ņă, Īā, ĵ, ŶĂ, TextAlignment.RIGHT); ċă(ŹĂ, ąă, "SquareSimple", ĕ, new Vector2I(đ.X, żā.Y));
                    ċă(ŹĂ, ąă, "SquareSimple", ĕ, new Vector2I(đ.X, żā.Y));
                }
                else
                {
                    var ġă = Color.Lerp(ąă, Color.Yellow, 0.37f); var Ņă = Color.Lerp(ąă, Color.Green, 0.10f); var ċĄ = (čĂ) ? new Vector2I((đ.X * 45) >> 7, (Ĕ.Y * 83) >> 7) : Vector2I.Zero; var ČĄ = new Vector2I(đ.X - ċĄ.X + Ē.X, Ē.Y);
                    var ěĄ = (ďĂ) ? new Vector2I(đ.X, (đ.Y * 40) >> 7) : Vector2I.Zero; var ĜĄ = new Vector2I(Ē.X, Ē.Y + đ.Y - ěĄ.Y); if (čĂ)
                    {
                        var đĄ = ČĄ; var čĄ = ċĄ; var ėĄ = čĄ >> 1; var ĘĄ = đĄ + ėĄ; var ĒĄ = (čĄ.X * 5) >> 7; var ĕĄ = đĄ.X + ĒĄ; var ďĄ = đĄ.X + čĄ.X - ĒĄ; var ũ = (čĄ * 13) >> 7;
                        var Ĵ = ĘĄ.Y - ((ũ.Y * 444) >> 7); if (Đ && !Ź) čă(ŹĂ, ąă, "Move", new Vector2I(ĘĄ.X, Ĵ), ŶĂ, TextAlignment.CENTER); if (Ŵ == 0 && !Ź) { ũ.X = ėĄ.X >> 1; var ĔĄ = ĘĄ; ĔĄ.X -= ũ.X; Čă(ŹĂ, ąă, "Triangle", ĔĄ, ũ, ų); Čă(ŹĂ, ąă, "Triangle", ĘĄ, ũ, ų); }
                        else
                        {
                            var ĔĄ = new Vector2I(ĕĄ + ũ.X, Ĵ);
                            var ĎĄ = new Vector2I(ďĄ - ũ.X, Ĵ); if (Ź) čă(ŹĂ, ąă, "!!!MAX!!!", new Vector2I(ĘĄ.X, Ĵ), ŶĂ, TextAlignment.CENTER); for (int i = 0; i < 3; i++) { Čă(ŹĂ, ąă, "Triangle", ĔĄ, ũ, ų); Čă(ŹĂ, ąă, "Triangle", ĎĄ, ũ, ų); Ĵ += ũ.Y; ĔĄ.Y = ĎĄ.Y = Ĵ; }
                        }
                        ĵ = ĘĄ; ĵ.Y += (int)(20 * ŷĂ);
                        čă(ŹĂ, ąă, œĂ, ĵ, 0.75f * ŶĂ, TextAlignment.CENTER);
                    }
                    if (ďĂ)
                    {
                        var đĄ = ĜĄ; var čĄ = ěĄ; var ėĄ = čĄ >> 1; var ĘĄ = đĄ + ėĄ; ĵ = ĘĄ; čă(ŹĂ, ąă, sBalanceSp, ĵ, ŶĂ, TextAlignment.CENTER); ĵ.Y -= š; čă(ŹĂ, ąă, Ĩā, ĵ, ŶĂ, TextAlignment.CENTER); var ĒĄ = (čĄ.X * 6) >> 7;
                        var ęĄ = new Vector2I((čĄ.X * 8) >> 7, čĄ.Y - (ĒĄ << 1)); var ĖĄ = đĄ; ĖĄ.X += ĒĄ; var ĐĄ = đĄ; ĐĄ.X += čĄ.X - ĒĄ - ęĄ.X; ĖĄ.Y = ĐĄ.Y = đĄ.Y + ĒĄ; Čă(ŹĂ, ąă, "DecorativeBracketLeft", ĖĄ, ęĄ); Čă(ŹĂ, ąă, "DecorativeBracketRight", ĐĄ, ęĄ);
                    }
                    if (čĂ)
                    {
                        var đĄ = ČĄ; đĄ.Y += ċĄ.Y;
                        var čĄ = new Vector2I(ċĄ.X, đ.Y - ċĄ.Y - ěĄ.Y); var ėĄ = čĄ >> 1; var ĘĄ = đĄ + ėĄ; var ĒĄ = (čĄ.X * 10) >> 7; var ĚĄ = new Vector2I((čĄ.X * 12) >> 7, (čĄ.Y * 70) >> 7); var ŏă = new Vector2I(đĄ.X + ĒĄ, ĘĄ.Y); if (ź <= 0)
                        {
                            ĵ = ĘĄ; ĵ.Y -= š >> 1; čă(ŹĂ, ąă, "No Sphere\nGenerators", ĵ, ŶĂ, TextAlignment.CENTER);
                        }
                        else
                        {
                            int Žā = 8; foreach (var Űā in šĂ) if (Űā.block != null) { if (Žā < 0) break; Žā--; var b = Űā.AsGravityGenBase; var F = (b.IsWorking) ? ((int)(ĚĄ.Y * b.GravityAcceleration * 10)) >> 7 : 0; Čă(ŹĂ, ąă, "SquareSimple", new Vector2I(ŏă.X, ŏă.Y - F), new Vector2I(ĚĄ.X, F)); ŏă.X += ĚĄ.X + 2; }
                            foreach (var Űā in ŠĂ) if (Űā.block != null) { if (Žā < 0) break; Žā--; var b = Űā.AsGravityGenBase; var F = (b.IsWorking) ? ((int)(ĚĄ.Y * b.GravityAcceleration * 10)) >> 7 : 0; Čă(ŹĂ, ġă, "SquareSimple", new Vector2I(ŏă.X, ŏă.Y - F), new Vector2I(ĚĄ.X, F)); ŏă.X += ĚĄ.X + 2; }
                        }
                    }
                    {
                        var đĄ = Ē; var čĄ = đ - new Vector2I(ċĄ.X, ěĄ.Y); var ĒĄ = (čĄ.X * 20) >> 7; var ēĄ = new Rectangle(đĄ.X, đĄ.Y, čĄ.X, čĄ.Y); var ĚĄ = new Vector2I((čĄ.X * 12) >> 7, (čĄ.Y * 70) >> 7); var ŋĂ = new Vector2I((čĄ.X * ĐĂ) >> 7, (čĄ.Y * ĐĂ) >> 7); if (ŋĂ.X < ŋĂ.Y) ŋĂ.Y = ŋĂ.X; else ŋĂ.X = ŋĂ.Y;
                        ēĄ.Inflate(-ŋĂ.X, -ŋĂ.Y); var ľă = ąă; ľă.A = 64; var ńă = Color.Orange; ńă.A = 160; var ŀă = Color.Red; ŀă.A = 255; var Ńă = Color.Yellow; Ńă.A = 127; đă(ŹĂ, Ď, ēĄ, ŋĂ, ľă, ńă, ŀă, Ńă); if (ĎĂ)
                        {
                            đă(ŹĂ, ŤĂ.L, ēĄ, ŋĂ, ľă, ńă, ŀă, Ńă); đă(ŹĂ, ŤĂ.R, ēĄ, ŋĂ, ľă, ńă, ŀă, Ńă);
                            đă(ŹĂ, ŤĂ.U, ēĄ, ŋĂ, ľă, ńă, ŀă, Ńă); đă(ŹĂ, ŤĂ.D, ēĄ, ŋĂ, ľă, ńă, ŀă, Ńă); đă(ŹĂ, ŤĂ.F, ēĄ, ŋĂ, ľă, ńă, ŀă, Ńă); đă(ŹĂ, ŤĂ.B, ēĄ, ŋĂ, ľă, ńă, ŀă, Ńă); đă(ŹĂ, ŢĂ, ēĄ, ŋĂ, ľă, ńă, ŀă, Ńă);
                        }
                        if (ČĂ)
                        {
                            var Ľă = (Šā) ? ľă : Ńă; var łă = ľă; if (Ţā) { łă = Color.LightGreen; łă.A = 64; }
                            đă(ŹĂ, Ŗ.L, ēĄ, ŋĂ, Ľă, ńă, ŀă, Ľă); đă(ŹĂ, Ŗ.R, ēĄ, ŋĂ, Ľă, ńă, ŀă, Ľă); đă(ŹĂ, Ŗ.U, ēĄ, ŋĂ, Ľă, ńă, ŀă, Ľă); đă(ŹĂ, Ŗ.D, ēĄ, ŋĂ, Ľă, ńă, ŀă, Ľă); đă(ŹĂ, Ŗ.F, ēĄ, ŋĂ, Ľă, ńă, ŀă, Ľă); đă(ŹĂ, Ŗ.B, ēĄ, ŋĂ, Ľă, ńă, ŀă, Ľă); đă(ŹĂ, ļĂ, ēĄ, ŋĂ, łă, ńă, ŀă, ľă);
                        }
                        var ėĄ = čĄ >> 1; var ĘĄ = đĄ + ėĄ; if (ŧĂ)
                        {
                            ĵ = đĄ; ĵ.Y += čĄ.Y - š; var ņă = (ŰĂ != 1 || űĂ != null) ? Color.Yellow : ąă; var s = Čā; if (Ł < long.MaxValue) s += " [ " + ((Ł - ő) / 60) + " sec]"; čă(ŹĂ, ņă, s, ĵ, 1f * ŶĂ); ĵ.Y -= š; if (ĵĂ > 0) { if (ŠĂ.Count == 0) čă(ŹĂ, ąă, Ÿā + ĵĂ, ĵ, 1f * ŶĂ); else čă(ŹĂ, Color.Yellow, Ÿā + ŠĂ.Count + "/" + ĵĂ, ĵ, 1f * ŶĂ); }
                            ĵ.Y -= (int)(40 * ŷĂ); if (ĳĂ > 0) { if (ċ == 0) čă(ŹĂ, ąă, Ŷā + ĳĂ, ĵ, 1f * ŶĂ); else čă(ŹĂ, Color.Yellow, Ŷā + ċ + "/" + ĳĂ, ĵ, 1f * ŶĂ); }
                        }
                        if (DampenersOverride) čă(ŹĂ, ąă, "Damperned", new Vector2I(ĘĄ.X, đĄ.Y), ŶĂ, TextAlignment.CENTER); else Čă(ŹĂ, Color.Red, "Danger", new Vector2I(ĘĄ.X - (š >> 1), đĄ.Y), new Vector2I(š, š), 0f);
                    }
                    Čă(ŹĂ, ąă, "SquareSimple", new Vector2I(ČĄ.X, Ē.Y), new Vector2I(żā.X, đ.Y - ěĄ.Y)); Čă(ŹĂ, ąă, "SquareSimple", new Vector2I(ČĄ.X + żā.X, Ē.Y + ċĄ.Y), new Vector2I(ċĄ.X, żā.Y)); Čă(ŹĂ, ąă, "SquareSimple", new Vector2I(Ē.X, Ē.Y + đ.Y - ěĄ.Y), new Vector2I(đ.X, żā.Y));
                }
                if (ũĂ != null) { var ĳ = 2 * ŷĂ; var İ = (Vector2I)ėĂ.MeasureStringInPixels(new StringBuilder(ũĂ), ėĂ.Font, ĳ); var Ū = new Vector2I(đ.X, (İ.Y * 3) >> 1); ċă(ŹĂ, řă, "SquareSimple", Āā, Ū); Āā.Y -= İ.Y >> 1; čă(ŹĂ, Ŀă, ũĂ, Āā, ĳ, TextAlignment.CENTER); } else ċă(ŹĂ, Ŀă, "SquareSimple", Āā, Vector2I.Zero);
                ŹĂ.Dispose();
            }
            if (Ĵā) { ł = ŏ(3600); Ņ = ŏ(11); }
        }
        void đă<T>(MySpriteDrawFrame ŹĂ, List<T> ēă, Rectangle View, Vector2I ŋĂ, Color ľă, Color ńă, Color ŀă, Color Ńă)
        {
            var đ = new Vector2I(View.Width, View.Height); var ē = đ >> 1; var ĕ = new Vector2I(View.Left, View.Top) + ē; var ŊĂ = ŋĂ; ŊĂ -= 1; var ňĂ = ŋĂ * 3; ňĂ -= 1; var ŉĂ = ŊĂ; ŉĂ.Y = (ŉĂ.Y << 1) - 1; foreach (var ģĂ in ēă)
            {
                var Űā = ģĂ as BlockWrapper; if (Űā != null && Űā.Position != null)
                {
                    var Őă = Űā.Position.Value - Ŋă; var Ōā = new Vector2I((int)(Őă.X * ŋĂ.X + ĕ.X), (int)(Őă.Y * ŋĂ.Y + ĕ.Y)); if (View.Contains(Ōā.X, Ōā.Y))
                    {
                        var ņă = ľă; if (Űā.block == null) ņă = ŀă; else if (!Űā.block.IsFunctional) ņă = ńă; else if (!Űā.ĈĄ) ņă = Ńă; var ĉā = "SquareSimple"; switch (Űā.Ĕă)
                        { case (1): ĉā = "SquareSimple"; break; case (2): Čă(ŹĂ, ņă, "SemiCircle", Ōā, ŉĂ, Űā.Angle); continue; case (3): ĉā = "Triangle"; break; case (4): case (5): ĉā = "Circle"; break; }
                        var Ū = ŊĂ; if (Űā.isLarge) { Ōā -= ŋĂ; Ū = ňĂ; }
                        Čă(ŹĂ, ņă, ĉā, Ōā, Ū, Űā.Angle);
                    }
                }
            }
        }
        int ĉ = 0; int Ć = 0; int Ċă = 0; bool İĂ = true; long ŀ = 3600; void ďă()
        {
            bool Ĵā = ĥĂ(ŀ); if (İĂ)
            {
                foreach (var Ċ in ĖĂ)
                {
                    var ėĂ = Ċ.Ţă; ŉā(ėĂ); var Ē = ((Vector2I)(ėĂ.TextureSize - ėĂ.SurfaceSize)) >> 1; var đ = ((Vector2I)ėĂ.SurfaceSize) >> 1; Ċ.ĕ = Ē + đ; đ.X = -đ.Y; Ċ.đ = đ;
                }
                İĂ = false;
            }
            var Łă = Color.Yellow; Łă.A = 32; var Ũă = Źă != null; var ŷă = (Ũă && Ŭă < Źă.Count) ? Źă[Ŭă].ĹĂ : 0; var ų = (Ŵ < 0) ? 0f : (float)Őā; if (Ċă >= ĖĂ.Count) Ċă = 0; for (int ēĂ = 0; ēĂ < 4; ēĂ++)
            {
                if (Ċă >= ĖĂ.Count) break; var Ċ = ĖĂ[Ċă++]; var ėĂ = Ċ.Ţă; var ŹĂ = ėĂ.DrawFrame(); var ĵ = Vector2I.Zero; var Ē = ((Vector2I)(ėĂ.TextureSize - ėĂ.SurfaceSize)) >> 1; var đ = (Vector2I)ėĂ.SurfaceSize; var Ĕ = đ >> 1; var ĕ = Ē + Ĕ; Čă(ŹĂ, Color.Black, "SquareSimple", Ē, đ);
                var ąă = ėĂ.ScriptForegroundColor; var Ąă = ąă; Ąă.A = 140; var Śă = ėĂ.ScriptBackgroundColor; var ŶĂ = đ.X / 512.0f; var ŷĂ = đ.Y / 512.0f; var š = (int)(60 * ŷĂ); var ŐĂ = (int)(15 * ŷĂ); var Āā = ĕ; var řă = Color.Lerp(Color.Darken(Śă, 0.125f), Color.Gray, 0.125f); var Ŀă = Color.Lerp(Color.Lighten(ąă, 0.125f), Color.Yellow, 0.5f);
                var űă = Ē.Y + đ.Y - (š << 1); var żā = (đ) >> 7; foreach (var ŒĂ in Ċ.ōā) { var ą = Ċ.ōă(ŒĂ.Position); if (ą != null) { var ņă = ŒĂ.ĭă; ĵ = ą.Value; ċă(ŹĂ, ņă, "SquareSimple", ĵ, new Vector2I(3, 3)); ĵ = ą.Value; ĵ.Y += ŐĂ << 1; čă(ŹĂ, ņă, ŒĂ.ı, ĵ, 0.4f * ŷĂ, TextAlignment.CENTER); ĵ = ą.Value; ĵ.Y += ŐĂ; čă(ŹĂ, ņă, ŒĂ.Name, ĵ, 0.4f * ŷĂ, TextAlignment.CENTER); } }
                foreach (var ŒĂ in ĠĂ) { var ą = Ċ.ōă(ŒĂ.Position); if (ą != null) { ĵ = ą.Value; ċă(ŹĂ, Łă, "SquareSimple", ĵ, new Vector2I(3, 3)); ĵ = ą.Value; ĵ.Y += ŐĂ << 1; čă(ŹĂ, Łă, ŒĂ.ı, ĵ, 0.4f * ŷĂ, TextAlignment.CENTER); } }
                foreach (var ŒĂ in ųā)
                {
                    var ą = Ċ.ōă(ŒĂ.Position); if (ą != null)
                    { var ņă = ŒĂ.ĭă; ĵ = ą.Value; ċă(ŹĂ, ņă, "SquareSimple", ĵ, new Vector2I(3, 3)); ĵ = ą.Value; ĵ.Y += ŐĂ << 1; čă(ŹĂ, ņă, ŒĂ.ı, ĵ, 0.4f * ŷĂ, TextAlignment.CENTER); ĵ = ą.Value; ĵ.Y += ŐĂ; čă(ŹĂ, ņă, ŒĂ.Name, ĵ, 0.4f * ŷĂ, TextAlignment.CENTER); }
                }
                if (Ũă && !ŵă) foreach (var źă in Źă)
                    { var ŋā = źă.Ĥă; var ą = Ċ.ōă(ŋā); if (ą != null && źă.ĹĂ != ŷă) ċă(ŹĂ, Ąă, "SquareSimple", ą.Value, new Vector2I(3, 3)); }
                if (Ũă && Ŭă < Źă.Count)
                {
                    var źă = Źă[Ŭă]; var ą = Ċ.ōă(źă.Ĥă); if (ą != null)
                    {
                        ĵ = ą.Value; ċă(ŹĂ, ąă, "SquareSimple", ĵ, new Vector2I(5, 5)); ċă(ŹĂ, Color.Black, "SquareSimple", ĵ, new Vector2I(3, 3));
                        ĵ = ą.Value; ĵ.Y += ŐĂ << 1; čă(ŹĂ, ąă, źă.ı, ĵ, 0.4f * ŷĂ, TextAlignment.CENTER); ĵ = ą.Value; ĵ.Y += ŐĂ; čă(ŹĂ, ąă, źă.Name, ĵ, 0.4f * ŷĂ, TextAlignment.CENTER);
                    }
                }
                var ċĄ = (čĂ) ? new Vector2I((đ.X * 30) >> 7, (Ĕ.Y * 63) >> 7) : Vector2I.Zero; var ČĄ = Ē + đ - ċĄ; ČĄ.Y = űă - ċĄ.Y;
                if (čĂ)
                {
                    var đĄ = ČĄ; var čĄ = ċĄ; var ėĄ = čĄ >> 1; var ĘĄ = đĄ + ėĄ; var ĒĄ = (čĄ.X * 5) >> 7; var ĕĄ = đĄ.X + ĒĄ; var ďĄ = đĄ.X + čĄ.X - ĒĄ; var ũ = (čĄ * 13) >> 7; var Ũ = ũ.X >> 1; var Ĵ = ĘĄ.Y - ((ũ.Y * 444) >> 7); if (Đ && !Ź) čă(ŹĂ, ąă, "Move", new Vector2I(ĘĄ.X + Ũ, Ĵ), ŶĂ, TextAlignment.CENTER);
                    if (Ŵ == 0 && !Ź) { ũ.X = ėĄ.X >> 1; var ĔĄ = ĘĄ; ĔĄ.X -= ũ.X; Čă(ŹĂ, ąă, "Triangle", ĔĄ, ũ, ų); Čă(ŹĂ, ąă, "Triangle", ĘĄ, ũ, ų); }
                    else
                    {
                        var ĔĄ = new Vector2I(ĕĄ + ũ.X, Ĵ); var ĎĄ = new Vector2I(ďĄ - ũ.X, Ĵ); if (Ź) čă(ŹĂ, ąă, "!!!MAX!!!", new Vector2I(ĘĄ.X + Ũ, Ĵ), 0.5f * ŶĂ, TextAlignment.CENTER);
                        for (int i = 0; i < 3; i++) { Čă(ŹĂ, ąă, "Triangle", ĔĄ, ũ, ų); Čă(ŹĂ, ąă, "Triangle", ĎĄ, ũ, ų); Ĵ += ũ.Y; ĔĄ.Y = ĎĄ.Y = Ĵ; }
                    }
                    ĵ = ĘĄ; ĵ.Y += (int)(20 * ŷĂ); čă(ŹĂ, ąă, œĂ, ĵ, 0.75f * ŶĂ, TextAlignment.CENTER);
                }
                foreach (var t in Ċ.Ţ) čă(ŹĂ, t.Ĭă, t.Ţ, t.Position, t.Size);
                ĵ.X = Ē.X + 10; ĵ.Y = űă; if (!Ũă) čă(ŹĂ, Color.Red, "APS-OFF", ĵ, 0.75f * ŷĂ);
                else
                {
                    var ņă = Color.Red; var ĥā = źā; if (ŧā < connectors.Count) { var c = connectors[ŧā]; ĥā = c.CustomName; ņă = (c.Status == MyShipConnectorStatus.Connected) ? Color.Red : ąă; }
                    čă(ŹĂ, ņă, "APS:" + ĥā, ĵ, 0.75f * ŷĂ);
                    ĥā = Źā; ņă = Color.Red; if (Ŭă < Źă.Count) { var c = Źă[Ŭă]; ĥā = c.Name; ņă = ąă; }
                    ĵ.X = Ē.X + đ.X - 10; ĵ.Y += š; čă(ŹĂ, ņă, ĥā, ĵ, 0.75f * ŷĂ, TextAlignment.RIGHT); ĵ.Y -= š >> 1; čă(ŹĂ, ņă, Īā, ĵ, 0.5f * ŷĂ, TextAlignment.RIGHT);
                }
                if (ũĂ != null)
                {
                    var ĳ = 2 * ŷĂ; var İ = (Vector2I)ėĂ.MeasureStringInPixels(new StringBuilder(ũĂ), ėĂ.Font, ĳ);
                    var Ū = new Vector2I(đ.X, (İ.Y * 3) >> 1); ċă(ŹĂ, řă, "SquareSimple", Āā, Ū); Āā.Y -= İ.Y >> 1; čă(ŹĂ, Ŀă, ũĂ, Āā, ĳ, TextAlignment.CENTER);
                }
                else ċă(ŹĂ, Ŀă, "SquareSimple", Āā, Vector2I.Zero); ċă(ŹĂ, Śă, "SquareSimple", new Vector2I(ĕ.X, űă), new Vector2I(đ.X, żā.Y)); if (čĂ)
                { var ŵā = new Vector2I(Ē.X + đ.X, űă) - ċĄ - (żā >> 1); Čă(ŹĂ, Śă, "SquareSimple", ŵā, new Vector2I(żā.X, ċĄ.Y)); Čă(ŹĂ, Śă, "SquareSimple", ŵā, new Vector2I(ċĄ.X, żā.Y)); }
                ŹĂ.Dispose();
            }
            if (Ĵā) ŀ = ŏ(3600);
        }
        class Ą
        {
            public Ą() { }
            public bool Ŗā(string s)
            {
                var ű = s.Split(':'); var r = true; Ĭă = Color.White; Position = Vector2I.Zero; Size = 1f; Ţ = ""; float f; int x, y; uint clHex = 0; if (ű.Length > 0 && uint.TryParse(ű[0], System.Globalization.NumberStyles.HexNumber, null, out clHex)) Ĭă = new Color(clHex); else r = false; if (ű.Length > 2 && int.TryParse(ű[1], out x) && int.TryParse(ű[2], out y)) Position = new Vector2I(x, y); else r = false;
                if (ű.Length > 3 && float.TryParse(ű[3], out f)) Size = f; else r = false; if (ű.Length > 4) Ţ = ű[4].Replace("\\n", "\n"); else r = false; return r;
            }
            public Color Ĭă = Color.White; public Vector2I Position = Vector2I.Zero; public float Size = 1f; public string Ţ = "";
        }
        static void ňā(IMyTextSurface ėĂ, Color C, TextAlignment A = TextAlignment.LEFT, float FS = 1.5f, float ĵ = 0.0f, string żĂ = "DEBUG")
        {
            ėĂ.ContentType = ContentType.TEXT_AND_IMAGE; ėĂ.BackgroundColor = new Color(0, 8, 16); ėĂ.FontSize = FS; ėĂ.Font = żĂ; ėĂ.FontColor = C; ėĂ.TextPadding = ĵ; ėĂ.Alignment = A; ėĂ.ClearImagesFromSelection();
        }
        static void ŉā(IMyTextSurface l) { l.ContentType = ContentType.SCRIPT; l.Script = ""; }
        static void Čă(MySpriteDrawFrame ŹĂ, Color Įă, string Ċā, Vector2I p, Vector2I size, float rotation = 0f) { p.Y += size.Y >> 1; ŹĂ.Add(new MySprite() { Type = SpriteType.TEXTURE, Data = Ċā, Position = p, Size = size, RotationOrScale = rotation, Color = Įă }); }
        static void ċă(MySpriteDrawFrame ŹĂ, Color Įă, string Ċā, Vector2I p, Vector2I size, float rotation = 0f) { p.X -= size.X >> 1; ŹĂ.Add(new MySprite() { Type = SpriteType.TEXTURE, Data = Ċā, Position = p, Size = size, RotationOrScale = rotation, Color = Įă }); }
        static void čă(MySpriteDrawFrame ŹĂ, Color Įă, string ţ, Vector2I position, float scale = 0.75f, TextAlignment alignment = TextAlignment.LEFT) { ŹĂ.Add(new MySprite() { Type = SpriteType.TEXT, Data = ţ, Position = position, RotationOrScale = scale, Color = Įă, Alignment = alignment, FontId = "White" }); }
        static void Ďă(MySpriteDrawFrame ŹĂ, Color c1, Color c2, Vector2I Ōā, Vector2I Ū, double v) { var sz_ = Ū; sz_.X = (int)(Ū.X * āă(v)); Čă(ŹĂ, c1, "SquareSimple", Ōā, sz_); Ōā.X += sz_.X; sz_.X = Ū.X - sz_.X; Čă(ŹĂ, c2, "SquareSimple", Ōā, sz_); }
        void ġā() { if (Ŧā >= 0 && Me is IMyTextSurfaceProvider) { var Provider = (Me as IMyTextSurfaceProvider); if (Provider != null) MyLCD = Provider.GetSurface(Ŧā); } }
        string ĵā = ""; string ąā = ""; void ĥ()
        {
            ĵā = "\nVMassActive=" + č.Count + "/" + (Ŧă + Ž) + "\nGenerators=" + ŦĂ + "\nSp.Generators=" + šĂ.Count + "/" + ź + "\nThrusters=" + ś + "/" + Ŗ.Count + "\nGyros=" + ŅĂ.Count + "/" + ļĂ.Count + "\nLight=" + ĊĂ.Count + "+" + ćĂ.Count + "+" + ĈĂ.Count +
        "\nLCD=" + ĕĂ.Count + "+" + ĖĂ.Count; ąā = "Potential=" + formatDouble(MassRatio) + "\nBalance:\n " + Ĩā + "\n " + sBalanceSp;
        }
        void Ęă()
        {
            var Ĳ = Ĉă("--==GD-Controller v8.2==--", "00A0FF") + "\nWorkingTime=" + Ľ(ő) + "\nRuntime.Ms=" + œā.ToString("f2") + "\nRuntime.IPS=" + (įĂ * 100 / (Runtime.MaxInstructionCount + 1)).ToString() + "%\n";
            {
                Ĳ += Ĉă("-GD-Drive-\n", "00A0FF") + ąā + "\n"; if (ũĂ != null) Ĳ += "Status:" + Ĉă(ũĂ, "FFFF00") + "\n"; if (ĞĂ != null) Ĳ += "GyroVsJump:" + Ĉă(ĞĂ, "FFFF00") + "\n"; if (Ćă != null) Ĳ += Ĉă(Ćă, "FF0000") + "\n"; if (ŨĂ != "") Ĳ += Ĉă(ŨĂ, "FFFF00") + "\n"; if (ŰĂ != 1 || űĂ != null) Ĳ += Čā + "\n"; if (Ă) Ĳ += Ĉă("WARNING:need thrusters in all direction!\n", "FFFF00");
            }
            Echo(Ĳ);
        }
        string MyLCDTitle = "--GDrive Script--\n"; string Ćă = "Initialize..."; Program() { SetScriptSpeed(UpdateFrequency.Update10, 10); }
        void Main(string ŧă, UpdateType ut)
        {
            ŕā += Runtime.LastRunTimeMs; if ((ut & Įā) == 0) { if (Ćă == null) foreach (var ļă in ŧă.Trim().Split(';')) Ěă(ļă.Trim()); Ęă(); }
            else
            {
                ő += őā; Ōă(); bool Śā = ĥĂ(Ŀ); if (Ćă == null) { if (ĥĂ(Ń)) { řā(); if (Ćă != null) return; } if (ĥĂ(ň)) ėă(); if (Śā && !ňă()) { Ń = 60; Ļā = 0; } else if (ĥĂ(ŉ)) Ėă(); } else { if (ĥĂ(Ń)) řā(600, true); else if (ĥĂ(Ņ)) { Ņ = ŏ(60); string Ĳ = Ćă + "\n[Refresh " + Ľ(Ń - ő) + "]"; if (MyLCD != null) { ňā(MyLCD, Color.Red, TextAlignment.CENTER, 1.5f); MyLCD.WriteText(MyLCDTitle + Ĳ); } } }
                Ÿă(); if (Śā)
                {
                    if (Ďā != Me.CustomData.GetHashCode()) if (Me.CustomData.Trim() == "") ž(); else { Ń = ŏ(60); Ļā = 0; }
                    for (int i = 0; i < 10; i++) if (ĉ < ĖĂ.Count) ĖĂ[ĉ++].Ńā(); else { ĉ = 0; break; }
                    if (Ćă == null && Ć < ĖĂ.Count) { var Ċ = ĖĂ[Ć++]; foreach (var Ņā in Ċ.ōā) Ņā.ı = formatDistance((Ņā.Position - shipController.GetPosition()).Length()); } else Ć = 0; if (Ćă == null)
                    {
                        if (ğĂ < ĠĂ.Count) { var ŒĂ = ĠĂ[ğĂ++]; ŒĂ.ı = formatDistance((ŒĂ.Position - shipController.GetPosition()).Length()); } else ğĂ = 0; if (MarkedUpdateIndex < ųā.Count) { var ŒĂ = ųā[MarkedUpdateIndex++]; ŒĂ.ı = formatDistance((ŒĂ.Position - shipController.GetPosition()).Length()); } else MarkedUpdateIndex = 0;
                        if (Źă != null)
                        {
                            for (var i = Źă.Count; i > 0;) if (!Źă[--i].ĮĂ) Źă.RemoveAt(i); if (!ŵă) { if (Ŭă >= Źă.Count) Ŭă = 0; else if (ŭă < Źă.Count) CheckSelect(ŭă++); else ŭă = 0; }
                            Īā = "0.00"; if (Ŭă < Źă.Count) { var Ůă = Źă[Ŭă]; Ůă.ı = formatDistance((Ůă.Ĥă - shipController.GetPosition()).Length()); Īā = Ůă.Velocity.Length().ToString("f2"); }
                            Īā += " m/s";
                        }
                    }
                    if (beacon != null)
                    {
                        string ŗă = "Beacon.HudText="; if (ćā) { if (beacon.CustomData == "") beacon.CustomData = ŗă + beacon.HudText; beacon.HudText = ħā; if (Ćă != null && ĳĂ <= 0) Č(); } else if (beacon.CustomData.StartsWith(ŗă)) { beacon.HudText = beacon.CustomData.Substring(ŗă.Length); beacon.CustomData = ""; }
                    }
                    Ęă(); Ŀ = ŏ(60);
                }
                if (Ćă == null) { if (ĥĂ(Ņ)) Đă(); if (ĥĂ(ķ)) ďă(); }
            }
            Ňă += Runtime.CurrentInstructionCount; if (ĥĂ(Ň)) { Ň = ŏ(60); įĂ = Ňă; Ňă = 0; }
        }
        bool CheckSelect(int i)
        {
            var źă = Źă[i]; if (i != Ŭă)
            {
                var Ůă = Źă[Ŭă]; var m = shipController.WorldMatrix; var ShipPos = m.Translation; var ĝă = Ňā(m, Ůă.Ĥă - ShipPos); ĝă.Z = 0;
                var Ğă = Ňā(m, źă.Ĥă - ShipPos); Ğă.Z = 0; if (ĝă.LengthSquared() > Ğă.LengthSquared()) { Ŭă = i; return true; }
            }
            return false;
        }
        static IMyShipController shipController = null; List<IMyShipController> Ħă = new List<IMyShipController>(); void Ġā()
        {
            shipController = null; var ā = shipController; foreach (var c in Ħă) if (c.IsFunctional) { ā = c; if (c.IsMainCockpit) { shipController = c; break; } if (c.IsUnderControl) shipController = c; }
            shipController = shipController ?? ā;
        }
        long ĘĂ = 0; double ŕā = 0; double œā = 0; string Ăā = "00:00:00"; string Ű = "0.00"; bool Ćā = false; bool Ŏă = false; long Ō = 120; long Ŀ = 60; long Ňă = 0; long įĂ = 0; long Ň = 0; void Ōă()
        {
            if (ĥĂ(Ŀ))
            {
                var ĉă = DateTime.Now; long ŝā = ĉă.ToFileTime(); Ăā = ĉă.ToString("HH:mm:ss");
                float ğā = 10000001.0f / (ŝā - ĘĂ + 1); if (ğā > 1.0f) ğā = 1.0f; Ű = ğā.ToString("f2"); Ćā = (ğā < 0.7); ĘĂ = ŝā; œā = ŕā / 100.0; ŕā = 0;
            }
            Ŏă = ŕā < Ŕā; if (!Ŏă) Ō = ŏ(120);
        }
        const float G = 9.81f; const double Ķ = 57.295779513082320876798; const double Ŏā = 1.5707963267948966192313; const double Őā = 3.1415926535897932384626;
        const double ŏā = 6.2831853071795864769253; const UpdateType Įā = UpdateType.Update1 | UpdateType.Update10 | UpdateType.Update100; static long ő = 0; static long őā = 0; static bool ĥĂ(long Ő) { return (ő >= Ő); }
        static long ŏ(long p) { return ő + p; }
        static string Ľ(long Ő) { return (Math.Max(0, Ő) / 60) + " sec"; }
        static List<long> ĸĂ = new List<long>(); static bool śā(IMyTerminalBlock b) { long n = b.EntityId; int i = ĸĂ.BinarySearch(n); var r = i < 0; if (r) ĸĂ.Insert(~i, n); return r; }
        void SetScriptSpeed(UpdateFrequency f, long p) { Runtime.UpdateFrequency = f; őā = p; }
        static string Ĉă(string s, string c) { return "[Color=#FF" + c + "]" + s + "[/Color]"; }
        static float āă(double x, double a = 0, double b = 1) { return (float)((x < a) ? a : ((x > b) ? b : x)); }
        static bool ĤĂ(Vector3D v, double ćă = 0.01) { return (-ćă < v.X && v.X < ćă) && (-ćă < v.Y && v.Y < ćă) && (-ćă < v.Z && v.Z < ćă); }
        static bool In(double v, double Ůā, double max) { return Ůā < v && v < max; }
        static Vector3D safeNormalize(Vector3D x)
        {
            return (x.IsZero()) ? Vector3D.Zero : Vector3D.Normalize(x);
        }
        static Vector2D safeNormalize(Vector2D x)
        {
            return (x.X == 0 && x.Y == 0) ? Vector2D.Zero : Vector2D.Normalize(x);
        }
        static Vector3D safeNormalize(Vector3D x, double l)
        {
            return (x.LengthSquared() > l * l) ? Vector3D.Normalize(x) * l : x;
        }
        static Vector3D ě(Vector3D a, Vector3D b) { return new Vector3D((a.X == 0) ? b.X : a.X, (a.Y == 0) ? b.Y : a.Y, (a.Z == 0) ? b.Z : a.Z); }
        static string formatDistance(double l)
        {
            var m = ""; if (l > 500) { l /= 1000; m = "km"; }
            return l.ToString("f2") + m;
        }
        static string formatDouble(double d, string f = "f2")
        {
            return d.ToString(f);
        }
        static string trimString(string s, int l = 20)
        {
            return (s.Length > l) ? s.Substring(0, l - 3) + "..." : s;
        }

        static string ēā(string s, int l = 2, char c = ' ') { if (s.Length < l) s = new string(c, l - s.Length) + s; return s; }
        static bool IsCockpit(string blockName)
        {
            return !blockName.Contains("Bed") && !blockName.Contains("Passenger") && !blockName.Contains("Toilet") && !blockName.Contains("Bathroom");
        }
        static Base6Directions.Direction śĂ(MyBlockOrientation O, IMyTerminalBlock b, Base6Directions.Direction D) { return O.TransformDirectionInverse(b.Orientation.TransformDirection(D)); }
        static Vector3D Ňā(MatrixD m, Vector3D v) { return new Vector3D(m.Left.Dot(v), m.Down.Dot(v), m.Forward.Dot(v)); }
        static Vector3D ņā(MatrixD m, Vector3D v) { return (m.Left * v.X) + (m.Down * v.Y) + (m.Forward * v.Z); }
        static double ŜĂ(IMyThrust t) { return (t != null) ? t.CurrentThrustPercentage : 0; }
        static float SignG(double x, float ćă = G * 0.4f) { return (x < -ćă) ? -G : ((ćă < x) ? G : (float)x); }
        static Vector3D řĂ(IMyShipController b) { var rotation = b.RotationIndicator; return new Vector3D(rotation.Y, rotation.X, b.RollIndicator); }
        static void SortListId<T>(List<T> l) { l.Sort(delegate (T v1, T v2) { long ŧ = (v1 as IMyTerminalBlock).EntityId, Ŧ = (v2 as IMyTerminalBlock).EntityId; return (ŧ < Ŧ) ? -1 : (ŧ > Ŧ) ? 1 : 0; }); }
        static void āā<T>(List<T> l, Vector3D C) { l.Sort(delegate (T v1, T v2) { var ŧ = ((v1 as IMyTerminalBlock).GetPosition() - C).LengthSquared(); var Ŧ = ((v2 as IMyTerminalBlock).GetPosition() - C).LengthSquared(); return (ŧ < Ŧ) ? -1 : (ŧ > Ŧ) ? 1 : 0; }); }

        enum RDirection { Yaw, Pitch, Roll, MinusRoll, MinusPitch, MinusYaw };

        static void SetRotation(IMyGyro g, RDirection d, float v)
        {
            switch (d)
            {
                case (RDirection.Yaw): g.Yaw = v; break;
                case (RDirection.MinusYaw): g.Yaw = -v; break;
                case (RDirection.Pitch): g.Pitch = v; break;
                case (RDirection.MinusPitch): g.Pitch = -v; break;
                case (RDirection.Roll): g.Roll = v; break;
                case (RDirection.MinusRoll): g.Roll = -v; break;
            }
        }
        static BoundingBox ŞĂ<T>(List<T> list)
        {
            Vector3 Ůā = Vector3.Zero, max = Ůā;
            foreach (var ģĂ in list) { var Űā = ģĂ as BlockWrapper; if (Űā != null && Űā.block != null) { var Ōā = Űā.block.Position; Ůā = Vector3.Min(Ůā, Ōā); max = Vector3.Max(max, Ōā); } }
            return new BoundingBox(Ůā, max);
        }
        class ķĂ : BlockWrapper
        {
            public ķĂ(MyBlockOrientation O, IMyGyro g) : base(g, 2, 0, true) { đā(O); }
            static int[] ŕĂ = { 0, 0, 5, 6, 7, 4, 0, 0, 0, 3, 1, 2, 2, 7, 0, 0, 3, 6, 1, 4, 0, 0, 5, 0, 0, 6, 4, 2, 0, 0, 3, 5, 1, 7 }; public void đā(MyBlockOrientation O)
            {
                var g = AsGyro; var u = śĂ(O, g, Base6Directions.Direction.Up); var l = śĂ(O, g, Base6Directions.Direction.Left); var f = śĂ(O, g, Base6Directions.Direction.Forward); ąĄ(u, RDirection.Yaw); ąĄ(l, RDirection.Pitch); ąĄ(f, RDirection.Roll); int m = ((int)(u) * 6) + (int)(f); if ((ŕĂ[m] & 1) > 0) Y = (RDirection)(5 - (int)Y); if ((ŕĂ[m] & 2) > 0) P = (RDirection)(5 - (int)P);
                if ((ŕĂ[m] & 4) > 0) R = (RDirection)(5 - (int)R);
            }
            void ąĄ(Base6Directions.Direction d, RDirection v)
            {
                switch (d)
                {
                    case (Base6Directions.Direction.Up): case (Base6Directions.Direction.Down): Y = v; break;
                    case (Base6Directions.Direction.Right): case (Base6Directions.Direction.Left): P = v; break;
                    case (Base6Directions.Direction.Backward): case (Base6Directions.Direction.Forward): R = v; break;
                }
            }
            public void ŻĂ(Vector3D v) { SetRotation(AsGyro, Y, (float)v.X); SetRotation(AsGyro, P, (float)v.Y); SetRotation(AsGyro, R, (float)v.Z); }
            RDirection Y = RDirection.Yaw; RDirection P = RDirection.Pitch;
            RDirection R = RDirection.Roll;
        }
        static void Ėā(List<ķĂ> l, Vector3D v) { foreach (var b in l) { var g = b.AsGyro; if (g != null && g.GyroOverride) b.ŻĂ(v); } }
        static void Ĕā(List<ķĂ> l, bool v) { foreach (var b in l) { var g = b.AsGyro; if (g != null) g.GyroOverride = v; } }
        static IMyTerminalBlock ěĂ = null; static void ĕā(List<ķĂ> l, IMyTerminalBlock ăĄ) { if (ěĂ != ăĄ) { ěĂ = ăĄ; foreach (var b in l) b.đā(ăĄ.Orientation); } }
        int Ŭă = 0; int ŭă = 0; List<ŭā> Źă = null; IMyMessageProvider Ųă = null; void Ŷă(string Ūā) { if (Źă != null) IGC.SendBroadcastMessage(ūă, Ūā, TransmissionDistance.TransmissionDistanceMax); }
        void Ÿă()
        {
            if (Źă != null)
            {
                Ųă = Ųă ?? IGC.RegisterBroadcastListener(Űă); for (int l = 0; l < 20; l++)
                {
                    var ůā = Ųă.AcceptMessage().Data; if (ůā == null) break;
                    else
                    {
                        var ūā = ůā.ToString(); if (ūā.StartsWith("#CMD:"))
                        {
                            var ŧă = ūā.Split(':'); if (shipController != null && ŧă.Length > 2 && shipController.CubeGrid.CustomName.Contains(ŧă[1].Trim())) foreach (var ļă in ŧă[2].Trim().Split(';')) Ěă(ļă.Trim());
                        }
                        else { var Ŝā = new ŭā(); if (Ŝā.Ŗā(ūā)) { int p = ůă(Ŝā.ĹĂ); if (p >= 0) Źă[p].Į(Ŝā); else Źă.Insert(~p, Ŝā); } }
                    }
                }
            }
        }
        public int ůă(long ĹĂ, int p = 0)
        {
            var l = Źă; if (l.Count == 0) return ~0; int L = 0, R = l.Count - 1; var İă = l[L].ĹĂ - ĹĂ; if (İă == 0) return L; if (İă > 0) return ~L;
            İă = l[R].ĹĂ - ĹĂ; if (İă == 0) return R; if (İă < 0) return ~(R + 1); while (L != R) { int M = (L + R - 1) >> 1; İă = l[M].ĹĂ - ĹĂ; if (İă == 0) return M; if (İă > 0) R = M; else L = M + 1; }
            return ~L;
        }
        class ŭā
        {
            public ŭā() { }
            public bool Ŗā(string Ģă)
            {
                var ű = Ģă.Split('\n');
                if (ű.Length < 3 || ű[0] != "#OBJ" || !ŀā(ű[2]) || ĭā < 1) { return false; }
                Name = ű[1].Replace("\\n", "\n"); ĹĂ = (long)ĭā; ľā(ref Position, ű[3]); Velocity = Vector3D.Zero; Forward = Vector3D.Forward; Up = Vector3D.Up; if (ű.Length > 4) ľā(ref Velocity, ű[4]); if (ű.Length > 5) ľā(ref Forward, ű[5]);
                if (ű.Length > 6) ľā(ref Up, ű[6]); TimeStamp = ő; return true;
            }
            public void Į(ŭā Ŝā) { Name = Ŝā.Name; Position = Ŝā.Position; Velocity = Ŝā.Velocity; Forward = Ŝā.Forward; Up = Ŝā.Up; TimeStamp = Ŝā.TimeStamp; }
            public Vector3D Ĥă { get { return Position + Velocity * ((ő - TimeStamp + 1) / 60.0); } }
            public bool ĮĂ { get { return ő < TimeStamp + 600; } }
            public long ĹĂ = 0; public string Name = null; public string ı = ""; public Vector3D Position = Vector3D.Zero; public Vector3D Velocity = Vector3D.Zero; public Vector3D Forward = Vector3D.Up; public Vector3D Up = Vector3D.Up;
            public long TimeStamp = 0;
        }
        static string Ğ(Vector3D v) { return v.X.ToString("f2") + "," + v.Y.ToString("f2") + "," + v.Z.ToString("f2"); }
        static string ŏĂ(string Ťā, Vector3D v, string Įă = "#FF0000FF") { return "GPS:" + Ťā + ":" + v.X.ToString("f2") + ":" + v.Y.ToString("f2") + ":" + v.Z.ToString("f2") + ":" + Įă + ":"; }
        static Vector3D? łā(string s, out string Ťā, out Color ņă)
        {
            Vector3D r; var ű = s.Split(':'); Ťā = (ű.Length > 1) ? ű[1].Replace("\\n", "\n") : ""; ņă = Color.LightBlue; if (ű.Length > 5 && ű[0].ToLower() == "gps")
            {
                var Ħā = ű[5].Trim(); if (Ħā.StartsWith("#")) Ħā = Ħā.Substring(1);
                if (Ħā.Length > 8) Ħā = Ħā.Substring(Ħā.Length - 8); else Ħā = ēā(Ħā, 8, 'F'); uint clHex = 0; if (uint.TryParse(Ħā, System.Globalization.NumberStyles.HexNumber, null, out clHex)) ņă = new Color(clHex); if (double.TryParse(ű[2], out r.X) && double.TryParse(ű[3], out r.Y) && double.TryParse(ű[4], out r.Z)) return r;
            }
            return null;
        }
        class őĂ { public őĂ(string n, Vector3D p, Color ņă) { Name = n; Position = p; ĭă = ņă; } public string Name = ""; public Color ĭă = Color.Black; public string ı = ""; public Vector3D Position = Vector3D.Zero; }
        class ć
        {
            public ć(IMyTerminalBlock p) { Řā = p; Ńā(); }
            public IMyTextSurface Ţă { get { return (Řā as IMyTextSurfaceProvider).GetSurface(0); } }
            public MatrixD Matrix { get { var M = Řā.WorldMatrix; if (Řā is IMyEmotionControllerBlock) { M.Forward = Řā.WorldMatrix.Left; M.Right = Řā.WorldMatrix.Forward; } return M; } }
            public MySpriteDrawFrame DrawFrame() { return Ţă.DrawFrame(); }
            public Vector2I? ōă(Vector3D P, double l = 0.8)
            {
                var M = Matrix; var p = Ňā(M, P - (M.Translation + M.Backward * ŗā.Y)); if (p.Z <= ŗā.Y) return null; var k = (double)ŗā.Y / Math.Abs(p.Z); p.X *= k; p.Y *= k; p.Y -= ŗā.X;
                if (p.X < -l || p.X > l) return null; return ĕ + new Vector2I((int)(đ.X * p.X), (int)(đ.Y * p.Y));
            }
            public void Ńā()
            {
                if (Řā != null)
                {
                    var ŋă = Řā.CustomData.GetHashCode(); if (ŋă != ĺĂ)
                    {
                        ōā.Clear(); Ţ.Clear(); foreach (var ăā in Řā.CustomData.Split('\n'))
                        {
                            var Ģā = ăā.Split('='); if (Ģā.Length >= 2) switch (Ģā[0].Trim().ToLower())
                                {
                                    case ("vr.param"): { var v = Ŀā(Ģā[1]); if (v != null) ŗā = v.Value; } break;
                                    case ("vr.gps"): { var s = ""; var ņă = Color.Black; var v = łā(Ģā[1], out s, out ņă); if (v != null) ōā.Add(new őĂ(s, v.Value, ņă)); } break;
                                    case ("vr.text"): { var ă = new Ą(); if (ă.Ŗā(Ģā[1])) Ţ.Add(ă); } break;
                                }
                        }
                        ĺĂ = ŋă;
                    }
                }
            }
            public Vector2I ĕ = Vector2I.Zero; public Vector2I đ = Vector2I.Zero; public List<Ą> Ţ = new List<Ą>(); public List<őĂ> ōā = new List<őĂ>(); long ĺĂ = 0; public IMyTerminalBlock Řā = null; public Vector2D ŗā = new Vector2D(0, 3.25);
        }
    }
}
