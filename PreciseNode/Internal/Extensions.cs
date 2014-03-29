using System;
using System.Collections.Generic;
using UnityEngine;
using KSP.IO;

/******************************************************************************
 * Copyright (c) 2013-2014, Justin Bengtson
 * All rights reserved.
 * 
 * Redistribution and use in source and binary forms, with or without
 * modification, are permitted provided that the following conditions are met: 
 * 
 * 1. Redistributions of source code must retain the above copyright notice,
 * this list of conditions and the following disclaimer.
 * 
 * 2. Redistributions in binary form must reproduce the above copyright notice,
 * this list of conditions and the following disclaimer in the documentation
 * and/or other materials provided with the distribution.
 * 
 * THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS "AS IS"
 * AND ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE
 * IMPLIED WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE
 * ARE DISCLAIMED. IN NO EVENT SHALL THE COPYRIGHT OWNER OR CONTRIBUTORS BE
 * LIABLE FOR ANY DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR
 * CONSEQUENTIAL DAMAGES (INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF
 * SUBSTITUTE GOODS OR SERVICES; LOSS OF USE, DATA, OR PROFITS; OR BUSINESS
 * INTERRUPTION) HOWEVER CAUSED AND ON ANY THEORY OF LIABILITY, WHETHER IN
 * CONTRACT, STRICT LIABILITY, OR TORT (INCLUDING NEGLIGENCE OR OTHERWISE)
 * ARISING IN ANY WAY OUT OF THE USE OF THIS SOFTWARE, EVEN IF ADVISED OF THE
 * POSSIBILITY OF SUCH DAMAGE.
 ******************************************************************************/

namespace RegexKSP {

	internal static class Extensions {
		/// <summary>
		/// Creates a new Meneuver Node Gizmo if needed
		/// </summary>
		internal static void CreateNodeGizmo(this ManeuverNode node) {
			if(node.attachedGizmo != null) { return; }
			node.AttachGizmo(MapView.ManeuverNodePrefab, FlightGlobals.ActiveVessel.patchedConicRenderer);
		}

		/// <summary>
		/// Converts the UT to human-readable Kerbal local time.
		/// </summary>
		/// <returns>The converted time.</returns>
		/// <param name="UT">Kerbal Space Program Universal Time.</param>
		internal static String convertUTtoHumanTime(this double UT) {
			long secs = (long)Math.Floor(UT % 60);
			long mins = (long)Math.Floor((UT / 60) % 60);
			long hour = (long)Math.Floor((UT / 3600) % 24);
			long day = (long)Math.Floor((UT / 86400) % 365) + 1;  // Ensure we don't get a "Day 0" here.
			long year = (long)Math.Floor(UT / (86400 * 365)) + 1; // Ensure we don't get a "Year 0" here.

			return "Year " + year + " Day " + day + " " + hour + ":" + (mins < 10 ? "0" : "") + mins + ":" + (secs < 10 ? "0" : "") + secs;
		}

		/// <summary>
		/// Converts the UT to human-readable duration.
		/// </summary>
		/// <returns>The converted time.</returns>
		/// <param name="UT">Kerbal Space Program Universal Time.</param>
		internal static String convertUTtoHumanDuration(this double UT) {
			double temp = Math.Floor(Math.Abs(UT % 60));
			string retval = (long)temp + " s";
			if(Math.Abs(UT / 60) > 1.0) {
				temp = Math.Floor(Math.Abs((UT / 60) % 60));
				retval = (long)temp + " m " + retval;
			}
			if(Math.Abs(UT / 3600) > 1.0) {
				temp = Math.Floor(Math.Abs((UT / 3600) % 24));
				retval = (long)temp + " h " + retval;
			}
			if(Math.Abs(UT / 86400) > 1.0) {
				temp = Math.Floor(Math.Abs((UT / 86400) % 365));
				retval = ((long)temp + 1) + " d " + retval;
			}
			if(Math.Abs(UT / (86400 * 365)) > 1.0) {
				temp = Math.Floor(Math.Abs(UT / (86400 * 365)));
				retval = ((long)temp + 1) + " y " + retval;
			}
			return retval;
		}

		/// <summary>
		/// Merges the given node into the next lowest node (n's index - 1).  If there is no lower node, does nothing.
		/// </summary>
		/// <param name="n">The ManeuverNode to merge down.</param>
		internal static void mergeNodeDown(this ManeuverNode n) {
			PatchedConicSolver p = NodeTools.getSolver();
			Orbit o = FlightGlobals.ActiveVessel.orbit;
			int nodes = p.maneuverNodes.Count;
			int idx = p.maneuverNodes.IndexOf(n);

			// if we're the last or only node, don't bother.
			if(idx == 0 || nodes < 2) { return; }
			ManeuverNode mergeInto = p.maneuverNodes[idx-1];

			Vector3d deltaV = mergeBurnVectors(mergeInto.UT, mergeInto, n.patch);

			mergeInto.OnGizmoUpdated(deltaV, mergeInto.UT);
			p.maneuverNodes.Remove(n);
		}

		// calculation function for mergeNodeDown
		private static Vector3d mergeBurnVectors(double UT, ManeuverNode first, Orbit projOrbit) {
			Orbit curOrbit = first.findPreviousOrbit();
			return difference(curOrbit.getOrbitalVelocityAtUT(UT), projOrbit.getOrbitalVelocityAtUT(UT));
		}

		// calculation function for mergeNodeDown
		private static Orbit findPreviousOrbit(this ManeuverNode n) {
			PatchedConicSolver p = NodeTools.getSolver();
			int idx = p.maneuverNodes.IndexOf(n);
			if(idx > 0) {
				return p.maneuverNodes[idx-1].patch;
			} else {
				return FlightGlobals.ActiveVessel.orbit;
			}
		}

		// calculation function for mergeNodeDown
		private static Vector3d difference(Vector3d initial, Vector3d final) {
			return new Vector3d(-(initial.x - final.x), -(initial.y - final.y), -(initial.z - final.z)).xzy;
		}

		/// <summary>
		/// Formats the given double into meters.
		/// </summary>
		/// <returns>The string format, in meters.</returns>
		/// <param name="d">The double to format</param>
		internal static string formatMeters(this double d) {
			if(Math.Abs(d / 1000000.0) > 1) {
				// format as kilometers.
				return (d/1000.0).ToString("0.##") + " km";
			} else {
				// use meters
				if(Math.Abs(d) > 100000.0) {
					return d.ToString("F0") + " m";
				} else {
					return d.ToString("0.##") + " m";
				}
			}
		}

		/// <summary>
		/// Gets the UT for the equatorial AN.
		/// </summary>
		/// <returns>The equatorial AN UT.</returns>
		/// <param name="o">The Orbit to calculate the UT from.</param>
		internal static double getEquatorialANUT(this Orbit o) {
            //TODO: Add safeguards for bad UTs, may need to be refactored to NodeManager
			return o.GetUTforTrueAnomaly(o.GetTrueAnomalyOfZupVector(o.GetANVector()), 2);
		}

		/// <summary>
		/// Gets the UT for the ascending node in reference to the target orbit.
		/// </summary>
		/// <returns>The UT for the ascending node in reference to the target orbit.</returns>
		/// <param name="a">The orbit to find the UT on.</param>
		/// <param name="b">The target orbit.</param>
		internal static double getTargetANUT(this Orbit a, Orbit b) {
            //TODO: Add safeguards for bad UTs, may need to be refactored to NodeManager
			Vector3d ANVector = Vector3d.Cross(b.h, a.GetOrbitNormal()).normalized;
			return a.GetUTforTrueAnomaly(a.GetTrueAnomalyOfZupVector(ANVector), 2);
		}

		/// <summary>
		/// Gets the UT for the equatorial DN.
		/// </summary>
		/// <returns>The equatorial DN UT.</returns>
		/// <param name="o">The Orbit to calculate the UT from.</param>
		internal static double getEquatorialDNUT(this Orbit o) {
            //TODO: Add safeguards for bad UTs, may need to be refactored to NodeManager
			Vector3d DNVector = QuaternionD.AngleAxis((o.LAN + 180).Angle360(), Planetarium.Zup.Z) * Planetarium.Zup.X;
			return o.GetUTforTrueAnomaly(o.GetTrueAnomalyOfZupVector(DNVector), 2);
		}

		/// <summary>
		/// Gets the UT for the descending node in reference to the target orbit.
		/// </summary>
		/// <returns>The UT for the descending node in reference to the target orbit.</returns>
		/// <param name="a">The orbit to find the UT on.</param>
		/// <param name="b">The target orbit.</param>
		internal static double getTargetDNUT(this Orbit a, Orbit b) {
            //TODO: Add safeguards for bad UTs, may need to be refactored to NodeManager
			Vector3d DNVector = Vector3d.Cross(a.GetOrbitNormal(), b.h).normalized;
			return a.GetUTforTrueAnomaly(a.GetTrueAnomalyOfZupVector(DNVector), 2);
		}

		/// <summary>
		/// Adjusts the specified angle to between 0 and 360 degrees.
		/// </summary>
		/// <param name="d">The specified angle to restrict.</param>
		internal static double Angle360(this double d) {
            d %= 360;
            if(d < 0) {
				return d + 360;
			}
			return d;
        }

		/// <summary>
		/// Gets the ejection angle of the current maneuver node.
		/// </summary>
		/// <returns>The ejection angle in degrees.  Positive results are the angle from prograde, negative results are the angle from retrograde.</returns>
		/// <param name="nodeUT">Kerbal Space Program Universal Time.</param>
		internal static double getEjectionAngle(this Orbit o, double nodeUT) {
			CelestialBody body = o.referenceBody;

			// Calculate the angle between the node's position and the reference body's velocity at nodeUT
			Vector3d prograde = body.orbit.getOrbitalVelocityAtUT(nodeUT);
			Vector3d position = o.getRelativePositionAtUT(nodeUT);
			double eangle = ((Math.Atan2(prograde.y, prograde.x) - Math.Atan2(position.y, position.x)) * 180.0 / Math.PI).Angle360();

			// Correct to angle from retrograde if needed.
			if(eangle > 180) {
				eangle = 180 - eangle;
			}

			return eangle;
		}

		internal static Orbit findNextEncounter(this ManeuverNode node) {
			System.Collections.ObjectModel.ReadOnlyCollection<Orbit> plan = node.solver.flightPlan.AsReadOnly();
			Orbit curOrbit = node.patch; // FlightGlobals.ActiveVessel.orbit;
			for(int k = plan.IndexOf(node.patch); k < plan.Count; k++) {
				Orbit o = plan[k];
				if(curOrbit.referenceBody.name != o.referenceBody.name && o.referenceBody.name != "Sun") {
					return o;
				}
			}
			return null;
		}

		internal static bool isClosed(this Orbit o) {
			return o.patchEndTransition == Orbit.PatchTransitionType.FINAL;
		}

		internal static bool hasAP(this Orbit o) {
			return o.isClosed();
		}

		internal static bool hasAN(this Orbit o, Orbit target) {
			double ut;
			if (target != null) {
				ut = o.getTargetANUT(target);
			} else {
				ut = o.getEquatorialANUT();
			}
			return o.isUTInsidePatch(ut);
		}

		internal static bool hasDN(this Orbit o, Orbit target) {
			double ut;
			if (target != null) {
				ut = o.getTargetDNUT(target);
			} else {
				ut = o.getEquatorialDNUT();
			}
			return o.isUTInsidePatch(ut);
		}

		internal static bool isUTInsidePatch(this Orbit o, double ut) {
			return (ut >= Planetarium.GetUniversalTime()) && (o.isClosed() || (ut <= o.EndUT));
		}
	}
}
