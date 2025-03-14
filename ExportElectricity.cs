﻿using ColossalFramework;
using ColossalFramework.UI;
using csm_exportelectricity;
using ICities;
using System;
using System.IO;
using UnityEngine;

namespace ExportElectricityMod
{
    public static class ExpmHolder
    {
        // because c# doesn't let you have bare variables in a namespace
        private static Exportable.ExportableManager expm = null;
        public static UIComponent IncomePanel;
        public static float buttonX;
        public static float buttonY;
        public static UIView view;

        public static Exportable.ExportableManager get()
        {
            if (expm == null)
            {
                expm = new Exportable.ExportableManager();
            }
            return expm;
        }
    }

    public static class Debugger
    {
        // Debugger.Write appends to a text file.  This is here because Debug.Log wasn't having any effect
        // when called from OnUpdateMoneyAmount.  Maybe a Unity thing that event handlers can't log?  I dunno.
        public static bool enabled = false; // don't commit
        public static void Write(string s)
        {
            if (!enabled)
            {
                return;
            }

            using (var file = new FileStream("ExportElectricityModDebug.txt", FileMode.Append))
            {
                using (var sw = new StreamWriter(file))
                {
                    sw.WriteLine(s);
                    sw.Flush();
                }
            }
        }
    }

    public class ExportElectricity : IUserMod
    {
        public string Name
        {
            get { return "Export Electricity Revisited [2.2.0]"; }
        }

        public string Description
        {
            get { return "Earn money for unused electricity and (optionally) other production."; }
        }

        public void OnSettingsUI(UIHelperBase helper)
        {
            UIHelperBase group = helper.AddGroup("Check to enable income from excess capacity");
            ExpmHolder.get().AddOptions(group);
        }
    }

    public class EconomyExtension : EconomyExtensionBase
    {
        private const DayOfWeek startDay = DayOfWeek.Sunday;
        private bool updated;
        private bool nextWeekSet;
        private bool IsRealTimeDetected;
        private bool showDate;
        private DateTime prevDate;
        private DateTime nextWeek;
        private DateTime EstDate;
        private int changeInterval;

        public override long OnUpdateMoneyAmount(long internalMoneyAmount)
        {
            try
            { 
                DistrictManager DMinstance = Singleton<DistrictManager>.instance;
                Array8<District> dm_array = DMinstance.m_districts;
                District d;

                //New Week Calculation
                Debugger.Write($"\r\n Now: {this.managers.threading.simulationTime}//{nextWeek}");
                if (!nextWeekSet)
                {
                    //Calculate week
                    Debugger.Write("First Run");
                    nextWeek = this.managers.threading.simulationTime.ClosestWeekDay(startDay, false, true);
                    Debugger.Write($"\r\n Set Next Week to: {nextWeek}");
                    nextWeekSet = true;

                    //Check if RealTime Mode Enabled
                    if (UIUtils.CheckRealTimeEnabled())
                    {
                        IsRealTimeDetected = true;
                        Debugger.Write($"\r\n Real Time Mod Detected!");
                    }
                    else
                    {
                        IsRealTimeDetected = false;
                    }
                }
                else
                {
                    //Check for week change
                    if (this.managers.threading.simulationTime >= nextWeek)
                    {
                        //Update Week
                        Debugger.Write($"\r\n NEW WEEK!");
                        nextWeek = this.managers.threading.simulationTime.ClosestWeekDay(startDay, false, true);
                        Debugger.Write($"\r\n Set Next Week to: {nextWeek}");
                        //updated = true;

                    }

                }


                Debugger.Write("\r\n== OnUpdateMoneyAmount ==");

                //double sec_per_day = 75600.0; // for some reason
                double sec_per_week;
                double week_proportion = 0.0;
                int export_earnings = 0;
                int earnings_shown = 0;

                if (dm_array == null)
                {
                    Debugger.Write("early return, dm_array is null");
                    return internalMoneyAmount;
                }

                d = dm_array.m_buffer[0];

                if (!updated)
                {

                    updated = true;
                    prevDate = this.managers.threading.simulationTime;
                    Debugger.Write("first run");

                }
                else
                {
                    var newDate = this.managers.threading.simulationTime;
                    var timeDiff = newDate.Subtract(prevDate);

                    if (IsRealTimeDetected)
                    {
                        //Real Time settings
                        var rtinterval = 12;
                        switch(ExpmHolder.get().GetRealTimeInterval())
                        {
                            //3 hours
                            case 0:
                                rtinterval = 3;
                                break;
                            //6 hours
                            case 1:
                                rtinterval = 6;
                                break;
                            //12 hours
                            case 2:
                                rtinterval = 12;
                                break;
                            //1 day
                            case 3:
                                rtinterval = 24;
                                break;
                            //1 week
                            case 4:
                                rtinterval = 168;
                                break;
                        }

                        if (rtinterval > 24)
                        {
                            
                            showDate = true;
                        }
                        else
                        {
                            showDate = false;
                        }
                                                
                        sec_per_week = (newDate.AddHours(rtinterval) - newDate).TotalSeconds;

                    }
                    else
                    {
                        //Vanilla settings - 1 week cycle
                        sec_per_week = (nextWeek - newDate.StartOfWeek(startDay)).TotalSeconds;
                        showDate = true;
                    }

                    if (newDate > EstDate || ExpmHolder.get().GetRealTimeInterval() != changeInterval)
                    {
                        Debugger.Write($"Set Next Payout to: {EstDate}");
                        EstDate = newDate.AddSeconds(sec_per_week - 75600);
                        changeInterval = ExpmHolder.get().GetRealTimeInterval();
                        UIUtils.SetNextPayout(EstDate, showDate);
                    }
                    
                    week_proportion = ((double)timeDiff.TotalSeconds) / sec_per_week;

                    if (week_proportion > 0.0 && week_proportion <= 1.0)
                    {
                        Debugger.Write("Proportion: " + week_proportion.ToString("F20"));
                        EconomyManager EM = Singleton<EconomyManager>.instance;

                        if (EM != null)
                        {
                            // add income							
                            export_earnings = (int)ExpmHolder.get().CalculateIncome(d, week_proportion);
                            earnings_shown = export_earnings / 100;
                            Debugger.Write("Total earnings: " + earnings_shown.ToString());
                            EM.AddResource(EconomyManager.Resource.PublicIncome,
                                    export_earnings,
                                    ItemClass.Service.None,
                                    ItemClass.SubService.None,
                                    ItemClass.Level.None);
                        }
                    }
                    else
                    {
                        Debugger.Write("week_proportion zero");
                    }
                    prevDate = newDate;
                }

            }
            catch (Exception ex)
            {
                // shouldn't happen, but if it does, start logging
                Debugger.Write("Exception " + ex.Message);
            }
            return internalMoneyAmount;
        }
    }

    public class ExportLoading : LoadingExtensionBase
    {
        private GameObject ExportUIObj;

        public override void OnLevelLoaded(LoadMode mode)
        {
            ExpmHolder.get().ClearExportables();
            UIUtils.ResetPayout();
            Debugger.Write("Clearing Tables");

            if (ExportUIObj == null)
            {
                if (mode == LoadMode.NewGame || mode == LoadMode.LoadGame)
                {
                    ExportUIObj = new GameObject();
                    ExportUIObj.AddComponent<ExportUI>();
                }
            }

            var view = UIView.GetAView();
            ExpmHolder.view = view;
            var c = view.FindUIComponent("IncomePanel");
            ExpmHolder.IncomePanel = c;

            var pos = c.absolutePosition;
            ExpmHolder.buttonX = (pos.x + c.width) * view.inputScale - 2;
            ExpmHolder.buttonY = (pos.y) * view.inputScale;

        }

        public override void OnLevelUnloading()
        {
            if (ExportUIObj != null)
            {
                GameObject.Destroy(ExportUIObj);
                ExportUIObj = null;
            }
        }
    }

    public class ExportUI : MonoBehaviour
    {
        private Rect windowRect = new Rect(Screen.width - 300, Screen.height - 450, 300, 300);
        private bool showingWindow = false;
        private bool uiSetup = false;
        private UIUtils.ImageButton button;
        private UIComponent tb;


        private void SetupUI()
        {
            uiSetup = true;
            var policies = ExpmHolder.view.FindUIComponent("Policies");
            tb = ExpmHolder.view.FindUIComponent("MainToolstrip"); // TSBar/MainToolstrip
            string[] imgtypes = new string[] { "normalBg", "disabledBg", "hoveredBg", "pressedBg", "focusedBg", "normalFg", "pressedFg" };

            button = tb.AddUIComponent<UIUtils.ImageButton>();

            button.SetDetail("expinc", "exporticon.png", "Exports Income", 32, 42, imgtypes);

            button.eventClick += new MouseEventHandler(buttonClick);

        }

        private void buttonClick(UIComponent sender, UIMouseEventParameter e)
        {
            showingWindow = !showingWindow;
        }

        private void OnGUI()
        {
            if (ExpmHolder.view.enabled)
            {
                if (!uiSetup)
                {
                    SetupUI();
                }

                if (showingWindow)
                {
                    if (UIUtils.CheckRealTimeEnabled())
                    {
                        var title = @"";
                        switch(ExpmHolder.get().GetRealTimeInterval())
                        {
                            //3 hours
                            case 0:
                                title = @"Three-Hourly Income from Exports";
                                break;
                            //6 hours
                            case 1:
                                title = @"Six-Hourly Income from Exports";
                                break;
                            //12 hours
                            case 2:
                                title = @"Bi-Daily Income from Exports";
                                break;
                            //1 day
                            case 3:
                                title = @"Daily Income from Exports";
                                break;
                            //1 week
                            case 4:
                                title = @"Weekly Income from Exports";
                                break;
                            default:
                                title = @"Bi-Daily Income from Exports";
                                return;
                        }

                        windowRect = GUILayout.Window(314, windowRect, ShowExportIncomeWindow, title);
                    }
                    else
                    {
                        windowRect = GUILayout.Window(314, windowRect, ShowExportIncomeWindow, "Weekly Income from Exports");
                    }

                }
            }
        }

        void ShowExportIncomeWindow(int windowID)
        {
            var em = ExpmHolder.get();
            var exportables = em.GetExportables();
            int totalEarned = 0;

            foreach (var exportable in exportables)
            {
                var c = exportable.Value;
                if (c.GetEnabled())
                {
                    int earned = (int)(c.LastWeeklyEarning / 100.0);
                    totalEarned += earned;

                    GUILayout.BeginHorizontal();
                    GUILayout.Label(exportable.Value.Description);
                    GUILayout.FlexibleSpace();
                    GUI.contentColor = Color.white;
                    GUILayout.Label($"₡{string.Format("{0:n0}", earned)}");
                    GUILayout.EndHorizontal();
                }
            }

            //Blank Space
            GUILayout.BeginHorizontal();
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Label("Total");
            GUILayout.FlexibleSpace();
            GUILayout.Label($"₡{string.Format("{0:n0}", totalEarned)}");
            GUILayout.EndHorizontal();
            
            GUILayout.BeginHorizontal();
            GUILayout.Label("Next Payout");
            GUILayout.FlexibleSpace();
            GUILayout.Label(UIUtils.GetNextPayoutDate());
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Close"))
            {
                button.state = UIButton.ButtonState.Normal;
                showingWindow = false;
            }
            GUILayout.EndHorizontal();

            GUI.DragWindow();
        }

    }
}