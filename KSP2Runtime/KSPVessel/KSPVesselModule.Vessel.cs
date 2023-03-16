﻿using System;
using System.Linq;
using System.Xml.Schema;
using KontrolSystem.KSP.Runtime.KSPControl;
using KontrolSystem.KSP.Runtime.KSPMath;
using KontrolSystem.KSP.Runtime.KSPOrbit;
using KontrolSystem.TO2.Binding;
using KontrolSystem.TO2.Runtime;
using KSP.Game;
using KSP.Messages;
using KSP.Modules;
using KSP.Sim.DeltaV;
using KSP.Sim.impl;

namespace KontrolSystem.KSP.Runtime.KSPVessel {
    public partial class KSPVesselModule {
        [KSClass("Vessel",
            Description =
                "Represents an in-game vessel, which might be a rocket, plane, rover ... or actually just a Kerbal in a spacesuite.")]
        public class VesselAdapter : IKSPTargetable {
            internal readonly IKSPContext context;
            internal readonly VesselComponent vessel;
            private readonly ManeuverAdapter maneuver;
            private readonly ActionGroupsAdapter actions;
            private readonly AutopilotAdapter autopilot;
            private readonly VesselDeltaVAdapter deltaV;
            private readonly StagingAdapter staging;

            public static Option<VesselAdapter> NullSafe(IKSPContext context, VesselComponent vessel) => vessel != null
                ? new Option<VesselAdapter>(new VesselAdapter(context, vessel))
                : new Option<VesselAdapter>();

            internal VesselAdapter(IKSPContext context, VesselComponent vessel) {
                this.context = context;
                this.vessel = vessel;
                maneuver = new ManeuverAdapter(this);
                actions = new ActionGroupsAdapter(this.vessel);
                autopilot = new AutopilotAdapter(this);
                deltaV = new VesselDeltaVAdapter(this);
                staging = new StagingAdapter(this);
            }

            [KSField(Description = "The name of the vessel.")]
            public string Name => vessel.Name;

            [KSField] public bool IsActive => vessel.SimulationObject.IsActiveVessel;

            [KSField]
            public string ControlStatus => vessel.ControlStatus.ToString();

            [KSField] public ManeuverAdapter Maneuver => maneuver;

            [KSField] public ActionGroupsAdapter Actions => actions;

            [KSField] public AutopilotAdapter Autopilot => autopilot;

            [KSField] public VesselDeltaVAdapter DeltaV => deltaV;

            [KSField] public StagingAdapter Staging => staging;

            [KSField] public KSPOrbitModule.IBody MainBody => new BodyWrapper(context, vessel.mainBody);

            [KSField] public KSPOrbitModule.IOrbit Orbit => new OrbitWrapper(context, vessel.Orbit);

            [KSField] public Vector3d OrbitalVelocity => vessel.mainBody.coordinateSystem.ToLocalVector(vessel.OrbitalVelocity);

            [KSField] public Vector3d SurfaceVelocity => vessel.mainBody.coordinateSystem.ToLocalVector(vessel.SurfaceVelocity);

            [KSField] public double Mass => vessel.totalMass;

            [KSField("CoM")] public Vector3d CoM => vessel.mainBody.coordinateSystem.ToLocalPosition(vessel.CenterOfMass);

            [KSField] public double OffsetGround => vessel.OffsetToGround;

            [KSField] public double AtmosphereDensity => vessel.AtmDensity;

            [KSField] public double Heading => vessel.Heading;

            [KSField] public double HorizontalSurfaceSpeed => vessel.HorizontalSrfSpeed;

            [KSField] public double VerticalSurfaceSpeed => vessel.VerticalSrfSpeed;

            [KSField] public double AltitudeTerrain => vessel.AltitudeFromTerrain;

            [KSField] public double AltitudeSealevel => vessel.AltitudeFromSeaLevel;

            [KSField] public double AltitudeScenery => vessel.AltitudeFromScenery;

            [KSField] public Vector3d AngularMomentum => vessel.mainBody.coordinateSystem.ToLocalVector(vessel.angularMomentum.relativeAngularVelocity);

            [KSField] public Vector3d AngularVelocity => vessel.mainBody.coordinateSystem.ToLocalVector(vessel.AngularVelocity.relativeAngularVelocity);

            [KSField] public double VerticalSpeed => vessel.VerticalSrfSpeed;
            
            [KSField]
            public KSPOrbitModule.GeoCoordinates GeoCoordinates => new KSPOrbitModule.GeoCoordinates(MainBody, vessel.Latitude, vessel.Longitude);

            [KSField]
            public Vector3d Up => vessel.mainBody.coordinateSystem
                .ToLocalVector(vessel.SimulationObject.Telemetry.HorizonUp);

            [KSField] public Vector3d North => vessel.mainBody.coordinateSystem.ToLocalVector(vessel.SimulationObject.Telemetry.HorizonNorth);

            [KSField] public Vector3d East => vessel.mainBody.coordinateSystem.ToLocalVector(vessel.SimulationObject.Telemetry.HorizonEast);

            [KSField] public string Situation => vessel.Situation.ToString();

            [KSMethod]
            public Direction HeadingDirection(double degreesFromNorth, double pitchAboveHorizon, double roll) {
                QuaternionD q = QuaternionD.LookRotation(North, Up);
                q *= QuaternionD.Euler(-pitchAboveHorizon, degreesFromNorth, 0);
                q *= QuaternionD.Euler(0, 0, roll);
                return new Direction(q);
            }

            [KSField]
            public Direction Facing {
                get {
                    QuaternionD vesselRotation = vessel.mainBody.coordinateSystem
                        .ToLocalRotation(vessel.ControlTransform.bodyFrame, QuaternionD.identity);
                    QuaternionD vesselFacing = QuaternionD.Inverse(QuaternionD.Euler(90, 0, 0) *
                                                                   QuaternionD.Inverse(vesselRotation));
                    return new Direction(vesselFacing);
                }
            }

            [KSField] public PartAdapter[] Parts => vessel.SimulationObject.PartOwner.Parts.Select(part => new PartAdapter(this, part)).ToArray();

            [KSField]
            public ModuleAirIntakeAdapter[] AirIntakes => vessel.SimulationObject.PartOwner.Parts.Select(part => {
                if (part.IsPartAirIntake(out Data_ResourceIntake data)) {
                    return new ModuleAirIntakeAdapter(part, data);
                }
                return null;
            }).Where(intake => intake != null).ToArray();
            
            [KSField]
            public ModuleEngineAdapter[] Engines => vessel.SimulationObject.PartOwner.Parts.Select(part => {
                if (part.IsPartEngine(out Data_Engine data)) {
                    return new ModuleEngineAdapter(part, data);
                }
                return null;
            }).Where(engine => engine != null).ToArray();

            [KSField]
            public ModuleDockingNodeAdapter[] DockingNodes => vessel.SimulationObject.PartOwner.Parts.Select(part => {
                if (part.IsPartDockingPort(out Data_DockingNode data)) {
                    return new ModuleDockingNodeAdapter(part, data);
                }
                return null;
            }).Where(node => node != null).ToArray();

            [KSField]
            public ModuleSolarPanelAdapter[] SolarPanels => vessel.SimulationObject.PartOwner.Parts.Select(part => {
                if (part.IsPartSolarPanel(out Data_SolarPanel data)) {
                    return new ModuleSolarPanelAdapter(part, data);
                }
                return null;
            }).Where(panel => panel != null).ToArray();

            [KSField]
            public Option<IKSPTargetable> Target {
                get {
                    SimulationObjectModel target = vessel.TargetObject;
                    if (target != null) {
                        VesselComponent vessel = target.Vessel;
                        CelestialBodyComponent body = target.CelestialBody;

                        if (vessel != null) return new Option<IKSPTargetable>(new VesselAdapter(context, vessel));
                        if (body != null) return new Option<IKSPTargetable>(new BodyWrapper(context, body));
                    }
                    return new Option<IKSPTargetable>();
                }
                set {
                    if (value.defined) {
                        vessel.SetTargetByID(value.value.UnderlyingId);
                    } else {
                        vessel.ClearTarget();
                    }
                }
            }

            [KSField]
            public double AvailableThrust {
                get {
                    double thrust = 0.0;

                    foreach (PartComponent part in vessel.SimulationObject.PartOwner.Parts) {
                        if (part.TryGetModuleData<PartComponentModule_Engine, Data_Engine>(out var engine) && engine.staged) {
                            thrust += engine.MaxThrustOutputVac(true);
                        }
                    }

                    return thrust;
                }
            }

            [KSField]
            public double RollHorizonRelative => vessel.Roll_HorizonRelative;

            [KSField]
            public double PitchHorizonRelative => vessel.Pitch_HorizonRelative;

            [KSMethod]
            public KSPControlModule.SteeringManager SetSteering(Vector3d pitchYawRoll) =>
                new KSPControlModule.SteeringManager(context, vessel, _ => pitchYawRoll);

            [KSMethod]
            public KSPControlModule.SteeringManager ManageSteering(Func<double, Vector3d> pitchYawRollProvider) =>
                new KSPControlModule.SteeringManager(context, vessel, pitchYawRollProvider);

            [KSMethod]
            public KSPControlModule.ThrottleManager SetThrottle(double throttle) =>
                new KSPControlModule.ThrottleManager(context, vessel, _ => throttle);

            [KSMethod]
            public KSPControlModule.ThrottleManager ManageThrottle(Func<double, double> throttleProvider) =>
                new KSPControlModule.ThrottleManager(context, vessel, throttleProvider);

            [KSMethod]
            public KSPControlModule.RCSTranslateManager SetRcsTranslate(Vector3d translate) =>
                new KSPControlModule.RCSTranslateManager(context, vessel, _ => translate);

            [KSMethod]
            public KSPControlModule.RCSTranslateManager ManageRcsTranslate(Func<double, Vector3d> translateProvider) =>
                new KSPControlModule.RCSTranslateManager(context, vessel, translateProvider);

            [KSMethod]
            public KSPControlModule.WheelSteeringManager SetWheelSteering(double wheelSteering) =>
                new KSPControlModule.WheelSteeringManager(context, vessel, _ => wheelSteering);

            [KSMethod]
            public KSPControlModule.WheelSteeringManager ManageWheelSteering(Func<double, double> wheelSteeringProvider) =>
                new KSPControlModule.WheelSteeringManager(context, vessel, wheelSteeringProvider);

            [KSMethod]
            public KSPControlModule.WheelThrottleManager SetWheelThrottle(double wheelThrottle) =>
                new KSPControlModule.WheelThrottleManager(context, vessel, _ => wheelThrottle);

            [KSMethod]
            public KSPControlModule.WheelThrottleManager ManageWheelThrottle(Func<double, double> wheelThrottleProvider) =>
                new KSPControlModule.WheelThrottleManager(context, vessel, wheelThrottleProvider);

            [KSMethod]
            public void ReleaseControl() => context.UnhookAllAutopilots(vessel);

            [KSMethod]
            public void OverrideInputPitch(double value) => FlightInputHandler.OverrideInputPitch((float)value);

            [KSMethod]
            public void OverrideInputRoll(double value) => FlightInputHandler.OverrideInputRoll((float)value);

            [KSMethod]
            public void OverrideInputYaw(double value) => FlightInputHandler.OverrideInputYaw((float)value);

            [KSMethod]
            public void OverrideInputTranslateX(double value) => FlightInputHandler.OverrideInputTranslateX((float)value);

            [KSMethod]
            public void OverrideInputTranslateY(double value) => FlightInputHandler.OverrideInputTranslateY((float)value);

            [KSMethod]
            public void OverrideInputTranslateZ(double value) => FlightInputHandler.OverrideInputTranslateZ((float)value);

            public IGGuid UnderlyingId => vessel.SimulationObject.GlobalId;
        }
    }
}
