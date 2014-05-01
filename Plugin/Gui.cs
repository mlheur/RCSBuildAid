/* Copyright © 2013-2014, Elián Hanisch <lambdae2@gmail.com>
 *
 * This program is free software: you can redistribute it and/or modify
 * it under the terms of the GNU Lesser General Public License as published by
 * the Free Software Foundation, either version 3 of the License, or
 * (at your option) any later version.
 *
 * This program is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 * GNU Lesser General Public License for more details.
 *
 * You should have received a copy of the GNU Lesser General Public License
 * along with this program.  If not, see <http://www.gnu.org/licenses/>.
 */

using System;
using System.Linq;
using System.Collections.Generic;
using System.Diagnostics;
using UnityEngine;

namespace RCSBuildAid
{
    public class Window : MonoBehaviour
    {
        int winID;
        Rect winRect;
        bool markerMenu = false;
        bool modeSelect = false;
        bool debugMenu = false;
        bool softLock = false;
        bool minimized = false;
        string title = "RCS Build Aid v0.5-dev";
        int winX = 270, winY = 50;
        int minWidth = 184;
        int maxWidth = 184;
        int minHeight = 102;
        int maxHeight = 102;
        int minimizedWidth = 184;
        int minimizedHeight = 26;

        static bool setupStyleDone = false;
        static Texture2D blankTexture;
        static GUIStyle centerText;
        static GUIStyle labelButton;
        static GUIStyle mainButton;
        static GUIStyle activeButton;
        static GUIStyle sizeLabel;
        static GUIStyle clickLabel;
        static GUIStyle tableLabel;
        static GUIStyle clickTableLabel;
        static GUIStyle toggleLabel;

        Dictionary<PluginMode, string> menuTitles = new Dictionary<PluginMode, string> () {
            { PluginMode.Attitude, "Attitude"    },
            { PluginMode.RCS     , "Translation" },
            { PluginMode.Engine  , "Engines"     },
        };

        void Awake ()
        {
            winID = gameObject.GetInstanceID ();
            winRect = new Rect (winX, winY, minWidth, minHeight);
            Load ();

        }

        void Start ()
        {
        }

        void OnDestroy ()
        {
            Save ();
            Settings.SaveConfig();
        }

        void Load ()
        {
            winRect.x = Settings.GetValue ("window_x", winX);
            winRect.y = Settings.GetValue ("window_y", winY);

            /* check if within screen */
            winRect.x = Mathf.Clamp (winRect.x, 0, Screen.width - maxWidth);
            winRect.y = Mathf.Clamp (winRect.y, 0, Screen.height - maxHeight);
        }

        void Save ()
        {
            Settings.SetValue ("window_x", (int)winRect.x);
            Settings.SetValue ("window_y", (int)winRect.y);
        }

        void setupStyle ()
        {
            /* need a blank texture for the hover effect or it won't work */
            blankTexture = new Texture2D (1, 1, TextureFormat.Alpha8, false);
            blankTexture.SetPixel (0, 0, Color.clear);
            blankTexture.Apply ();

            GUI.skin.label.padding = new RectOffset ();
            GUI.skin.label.wordWrap = false;
            GUI.skin.toggle.padding = new RectOffset (15, 0, 0, 0);
            GUI.skin.toggle.overflow = new RectOffset (0, 0, -1, 0);

            centerText = new GUIStyle (GUI.skin.label);
            centerText.alignment = TextAnchor.MiddleCenter;
            centerText.wordWrap = true;

            labelButton = new GUIStyle (GUI.skin.button);
            labelButton.clipping = TextClipping.Overflow;
            labelButton.fixedHeight = GUI.skin.label.lineHeight;

            mainButton = new GUIStyle (GUI.skin.button);
            mainButton.clipping = TextClipping.Overflow;
            mainButton.fixedHeight = GUI.skin.button.lineHeight;

            activeButton = new GUIStyle(mainButton);
            activeButton.normal = mainButton.onNormal;

            Vector2 size = GUI.skin.label.CalcSize (new GUIContent ("Size"));
            sizeLabel = new GUIStyle (GUI.skin.label);
            sizeLabel.fixedWidth = size.x;
            sizeLabel.normal.textColor = GUI.skin.box.normal.textColor;

            tableLabel = new GUIStyle (GUI.skin.label);
            tableLabel.normal.textColor = GUI.skin.box.normal.textColor;
            tableLabel.padding = GUI.skin.toggle.padding;

            clickLabel = new GUIStyle (GUI.skin.button);
            clickLabel.alignment = TextAnchor.LowerLeft;
            clickLabel.padding = new RectOffset ();
            clickLabel.normal = GUI.skin.label.normal;
            clickLabel.hover.background = blankTexture;
            clickLabel.hover.textColor = Color.yellow;
            clickLabel.active = clickLabel.hover;
            clickLabel.fixedHeight = GUI.skin.label.lineHeight;
            clickLabel.clipping = TextClipping.Overflow;

            clickTableLabel = new GUIStyle(clickLabel);
            clickTableLabel.normal.textColor = GUI.skin.box.normal.textColor;

            toggleLabel = new GUIStyle(GUI.skin.label);
            toggleLabel.padding = GUI.skin.toggle.padding;
        }

        void OnGUI ()
        {
            switch (HighLogic.LoadedScene) {
            case GameScenes.EDITOR:
            case GameScenes.SPH:
                break;
            default:
                /* don't show window during scene changes */
                return;
            }

            /* style */
            if (!setupStyleDone) {
                setupStyle ();
                setupStyleDone = true;
            }

            if (RCSBuildAid.Enabled) {
                if (minimized) {
                    GUI.skin.window.clipping = TextClipping.Overflow;
                    winRect.height = minimizedHeight;
                    winRect.width = minimizedWidth;
                    winRect = GUI.Window (winID, winRect, drawWindowMinimized, title);
                } else {
                    GUI.skin.window.clipping = TextClipping.Clip;
                    if (Event.current.type == EventType.Layout) {
                        winRect.height = minHeight;
                        winRect.width = minWidth;
                    }
                    winRect = GUILayout.Window (winID, winRect, drawWindow, title);
                }
            }
            if (Event.current.type == EventType.Repaint) {
                setEditorLock ();
            }

            debug ();
        }

        void drawWindowMinimized (int ID)
        {
            minimizeButton();
            GUI.DragWindow ();
        }

        void drawWindow (int ID)
        {
            if (minimizeButton () && minimized) {
                return;
            }

            GUILayout.BeginVertical ();
            {
                if (modeSelect || (RCSBuildAid.mode == PluginMode.none)) {
                    if (GUILayout.Button ("Select mode", mainButton)) {
                        modeSelect = !modeSelect;
                    }
                } else {
                    if (GUILayout.Button (menuTitles [RCSBuildAid.mode], activeButton)) {
                        modeSelect = !modeSelect;
                    }
                }
                if (!modeSelect) { 
                    switch (RCSBuildAid.mode) {
                    case PluginMode.Attitude:
                        drawAttitudeMenu ();
                        break;
                    case PluginMode.RCS:
                        drawRCSMenu ();
                        break;
                    case PluginMode.Engine:
                        drawEngineMenu ();
                        break;
                    }
                } else {
                    GUILayout.BeginVertical (GUI.skin.box);
                    {
                        for (int i = 1; i < 4; i++) {
                            if (GUILayout.Button (menuTitles[(PluginMode)i], clickLabel)) {
                                modeSelect = false;
                                RCSBuildAid.SetMode ((PluginMode)i);
                            }
                        }
                        if (GUILayout.Button ("None", clickLabel)) {
                            modeSelect = false;
                            RCSBuildAid.SetMode (PluginMode.none);
                        }
                    }
                    GUILayout.EndVertical ();
                }
                Settings.menu_vessel_mass = GUILayout.Toggle (Settings.menu_vessel_mass, 
                                                              "Vessel mass",
                                                              mainButton);
                if (Settings.menu_vessel_mass) {
                    drawMassMenu ();
                }
                Settings.menu_res_mass = GUILayout.Toggle (Settings.menu_res_mass, 
                                                           "Resources",
                                                           mainButton);
                if (Settings.menu_res_mass) {
                    drawResourcesMenu ();
                }
                markerMenu = GUILayout.Toggle (markerMenu, "Markers", mainButton);
                if (markerMenu) {
                    drawMarkerMenu ();
                }
#if DEBUG
                debugMenu = GUILayout.Toggle (debugMenu, "DEBUG", mainButton);
                if (debugMenu) {
                    drawDebugMenu ();
                }
#endif
            }
            GUILayout.EndVertical ();

            GUI.DragWindow ();
        }

        bool minimizeButton ()
        {
            if (GUI.Button (new Rect (winRect.width - 15, 3, 12, 12), "")) {
                minimized = !minimized;
                minimizedWidth = (int)winRect.width;
                return true;
            }
            return false;
        }

        void drawRCSMenu ()
        {
            MarkerForces comv = RCSBuildAid.VesselForces;
            GUILayout.BeginHorizontal ();
            {
                if (RCSBuildAid.RCSlist.Count != 0) {
                    GUILayout.BeginVertical (); 
                    {
                        GUILayout.Label ("Reference");
                        GUILayout.Label ("Direction");
                        GUILayout.Label ("Torque");
                        GUILayout.Label ("Thrust");
                        if (DeltaV.sanity) {
                            GUILayout.Label ("Delta V");
                            GUILayout.Label ("Burn time");
                        }
                    }
                    GUILayout.EndVertical();
                    GUILayout.BeginVertical ();
                    {
                        referenceButton ();
                        directionButton ();
                        GUILayout.Label (String.Format ("{0:0.## kNm}", comv.Torque().magnitude));
                        GUILayout.Label (String.Format ("{0:0.## kN}", comv.Thrust().magnitude));
                        if (DeltaV.sanity) {
                            GUILayout.Label(String.Format ("{0:0.# m/s}", DeltaV.dV));
                            GUILayout.Label(timeFormat(DeltaV.burnTime));
                        }
                    }
                    GUILayout.EndVertical();
                } else {
                    GUILayout.Label("No RCS thrusters attached", centerText);
                }
            }
            GUILayout.EndHorizontal();
        }

        void drawAttitudeMenu ()
        {
            MarkerForces comv = RCSBuildAid.VesselForces;
            GUILayout.BeginHorizontal ();
            {
                if (hasAttitudeControl ()) {
                    GUILayout.BeginVertical (); 
                    {
                        GUILayout.Label ("Reference");
                        GUILayout.Label ("Direction");
                        GUILayout.Label ("Torque");
                        GUILayout.Label ("Thrust");
                    }
                    GUILayout.EndVertical ();
                    GUILayout.BeginVertical ();
                    {
                        referenceButton ();
                        directionButton ();
                        GUILayout.Label (String.Format ("{0:0.## kNm}", comv.Torque().magnitude));
                        GUILayout.Label (String.Format ("{0:0.## kN}", comv.Thrust().magnitude));
                    }
                    GUILayout.EndVertical ();
                } else {
                    GUILayout.Label ("No attitude control elements attached", centerText);
                }
            }
            GUILayout.EndHorizontal ();
            Settings.include_wheels = GUILayout.Toggle (Settings.include_wheels, "Reaction wheels");
            Settings.include_rcs = GUILayout.Toggle (Settings.include_rcs, "RCS thrusters");
        }

        bool hasAttitudeControl ()
        {
            bool noRcs = false, noWheels = false;
            if (Settings.include_rcs && RCSBuildAid.RCSlist.Count == 0) {
                noRcs = true;
            }
            if (Settings.include_wheels && RCSBuildAid.WheelList.Count == 0) {
                noWheels = true;
            }
            return !(noWheels && noRcs);
        }

        void directionButton()
        {
            if (GUILayout.Button (RCSBuildAid.Direction.ToString (), labelButton)) {
                int i = (int)RCSBuildAid.Direction;
                if (Event.current.button == 0) {
                    i += 1;
                    if (i > 6) {
                        i = 1;
                    }
                } else if (Event.current.button == 1) {
                    i -= 1;
                    if (i < 1) {
                        i = 6;
                    }
                }
                RCSBuildAid.Direction = (RCSBuildAid.Directions)i;
            }
        }

        void drawEngineMenu ()
        {
            MarkerForces comv = RCSBuildAid.VesselForces;
            MassEditorMarker comm = RCSBuildAid.ReferenceMarker.GetComponent<MassEditorMarker> ();
            GUILayout.BeginHorizontal ();
            {
                if (RCSBuildAid.EngineList.Count != 0) {
                    GUILayout.BeginVertical ();
                    {
                        GUILayout.Label ("Reference");
                        GUILayout.Label ("Torque");
                        GUILayout.Label ("Thrust");
                        GUILayout.Label ("TWR");
                    }
                    GUILayout.EndVertical ();
                    GUILayout.BeginVertical ();
                    {
                        referenceButton ();
                        GUILayout.Label (String.Format ("{0:0.## kNm}", comv.Torque().magnitude));
                        GUILayout.Label (String.Format ("{0:0.## kN}", comv.Thrust().magnitude));
                        GUILayout.Label (String.Format ("{0:0.##}", comv.Thrust().magnitude / (comm.mass * 9.81)));
                    }
                    GUILayout.EndVertical ();
                } else {
                    GUILayout.Label("No engines attached", centerText);
                }
            }
            GUILayout.EndHorizontal();
        }

        void drawMassMenu ()
        {
            Vector3 offset = RCSBuildAid.CoM.transform.position
                - RCSBuildAid.DCoM.transform.position;

            /* Vessel stats */
            GUILayout.BeginHorizontal ();
            {
                GUILayout.BeginVertical ();
                {
                    GUILayout.Label ("Wet mass");
                    if (GUILayout.Button (Settings.show_dry_mass ? "Dry mass" : "Fuel mass", clickLabel)) {
                        Settings.show_dry_mass = !Settings.show_dry_mass;
                    }
                    GUILayout.Label ("DCoM offset");
                }
                GUILayout.EndVertical ();
                GUILayout.BeginVertical ();
                {
                    float mass;
                    if (Settings.show_dry_mass) {
                        mass = DCoM_Marker.Mass;
                    } else {
                        mass = CoM_Marker.Mass - DCoM_Marker.Mass;
                    }
                    GUILayout.Label (String.Format ("{0:0.###} t", CoM_Marker.Mass));
                    GUILayout.Label (String.Format ("{0:0.### t}", mass));
                    GUILayout.Label (String.Format ("{0:0.##} m", offset.magnitude));
                }
                GUILayout.EndVertical ();
            }
            GUILayout.EndHorizontal ();
        }

        void drawResourcesMenu ()
        {
            /* resources */
            if (DCoM_Marker.Resource.Count != 0) {
                var Resources = DCoM_Marker.Resource.Values.OrderByDescending (o => o.mass).ToList ();
                GUILayout.BeginVertical ();
                {
                    GUILayout.BeginHorizontal ();
                    {
                        GUILayout.BeginVertical ();
                        {
                            GUILayout.Label ("Name", tableLabel);
                            foreach (DCoMResource resource in Resources) {
                                string name = resource.name;
                                if (!resource.isMassless ()) {
                                    Settings.resource_cfg [name] = 
                                        GUILayout.Toggle (Settings.resource_cfg [name], name);
                                } else {
                                    GUILayout.Label (name, toggleLabel);
                                }
                            }
                        }
                        GUILayout.EndVertical ();
                        GUILayout.BeginVertical ();
                        {
                            if (GUILayout.Button (Settings.resource_amount ? "Amnt" : "Mass", clickTableLabel)) {
                                Settings.resource_amount = !Settings.resource_amount;
                            }
                            foreach (DCoMResource resource in Resources) {
                                string s = String.Empty;
                                if (Settings.resource_amount) {
                                    s = String.Format ("{0:F0}", resource.amount);
                                } else {
                                    if (!resource.isMassless ()) {
                                        s = String.Format ("{0:0.## t}", resource.mass);
                                    }
                                }
                                GUILayout.Label (s);
                            }
                        }
                        GUILayout.EndVertical ();
                    }
                    GUILayout.EndHorizontal ();
                }
                GUILayout.EndVertical ();
            }
        }

        void drawMarkerMenu ()
        {
            /* markers toggles */
            GUILayout.BeginVertical ();
            {
                GUILayout.BeginHorizontal ();
                {
                    for (int i = 0; i < 3; i++) {
                        MarkerType marker = (MarkerType)i;
                        bool visibleBefore = RCSBuildAid.isMarkerVisible(marker);
                        bool visibleAfter = GUILayout.Toggle (visibleBefore, marker.ToString());
                        if (visibleBefore != visibleAfter) {
                            RCSBuildAid.setMarkerVisibility(marker, visibleAfter);
                        }
                    }
                }
                GUILayout.EndHorizontal ();
                GUILayout.BeginHorizontal ();
                {
                    GUILayout.Label ("Size", sizeLabel);
                    Settings.marker_scale = GUILayout.HorizontalSlider (Settings.marker_scale, 0, 1);
                }
                GUILayout.EndHorizontal ();
            }
            GUILayout.EndVertical ();
        }

        void referenceButton ()
        {
            if (GUILayout.Button (RCSBuildAid.referenceMarker.ToString(), labelButton)) {
                int i = (int)RCSBuildAid.referenceMarker;
                if (Event.current.button == 0) {
                    i += 1;
                    if (i > 2) {
                        i = 0;
                    }
                } else if (Event.current.button == 1) {
                    i -= 1;
                    if (i < 0) {
                        i = 2;
                    }
                }
                RCSBuildAid.SetReferenceMarker((MarkerType)i);
            }
        }

        string timeFormat (float seconds)
        {
            int min = (int)seconds / 60;
            int sec = (int)seconds % 60;
            return String.Format("{0:D}m {1:D}s", min, sec);
        }

        bool isMouseOver ()
        {
            Vector2 position = new Vector2(Input.mousePosition.x,
                                           Screen.height - Input.mousePosition.y);
            return winRect.Contains(position);
        }

        /* Whenever we mouseover our window, we need to lock the editor so we don't pick up
         * parts while dragging the window around */
        void setEditorLock ()
        {
            if (RCSBuildAid.Enabled) {
                bool mouseOver = isMouseOver ();
                if (mouseOver && !softLock) {
                    softLock = true;
                    ControlTypes controlTypes =   ControlTypes.CAMERACONTROLS 
                                                | ControlTypes.EDITOR_ICON_HOVER 
                                                | ControlTypes.EDITOR_ICON_PICK 
                                                | ControlTypes.EDITOR_PAD_PICK_PLACE 
                                                | ControlTypes.EDITOR_PAD_PICK_COPY 
                                                | ControlTypes.EDITOR_EDIT_STAGES 
                                                | ControlTypes.EDITOR_ROTATE_PARTS 
                                                | ControlTypes.EDITOR_OVERLAYS;

                    InputLockManager.SetControlLock (controlTypes, "RCSBuildAidLock");
                } else if (!mouseOver && softLock) {
                    softLock = false;
                    InputLockManager.RemoveControlLock("RCSBuildAidLock");
                }
            } else if (softLock) {
                softLock = false;
                InputLockManager.RemoveControlLock("RCSBuildAidLock");
            }
        }

        /*
         * Debug stuff
         */

        [Conditional("DEBUG")]
        void debug ()
        {
            if (Input.GetKeyDown(KeyCode.Space)) {
                print (winRect.ToString ());
            }
        }

        [Conditional("DEBUG")]
        void drawDebugMenu ()
        {
            MarkerForces comv = RCSBuildAid.VesselForces;
            MomentOfInertia moi = comv.MoI;
            GUILayout.BeginHorizontal (GUI.skin.box);
            {
                GUILayout.BeginVertical (); 
                {
                    GUILayout.Label ("MoI:");
                    GUILayout.Label ("Ang Acc:");
                    GUILayout.Label ("Ang Acc:");
                }
                GUILayout.EndVertical ();
                GUILayout.BeginVertical ();
                {
                    GUILayout.Label (String.Format ("{0:0.## tm²}", moi.value));
                    float angAcc = comv.Torque().magnitude / moi.value;
                    GUILayout.Label (String.Format ("{0:0.## r/s²}", angAcc));
                    GUILayout.Label (String.Format ("{0:0.## °/s²}", angAcc * Mathf.Rad2Deg));
                }
                GUILayout.EndVertical ();
            }
            GUILayout.EndHorizontal ();
            DebugSettings.labelMagnitudes = 
                GUILayout.Toggle(DebugSettings.labelMagnitudes, "Show vector magnitudes");
            DebugSettings.inFlightAngularInfo = 
                GUILayout.Toggle(DebugSettings.inFlightAngularInfo, "In flight angular data");
            DebugSettings.startInOrbit = 
                GUILayout.Toggle(DebugSettings.startInOrbit, "Start in orbit");
        }
    }
}
