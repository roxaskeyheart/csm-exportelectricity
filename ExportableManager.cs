using ColossalFramework.UI;
using ICities;
using System;
using System.Collections.Generic;
using System.IO;

namespace Exportable
{
    public class ExportableManager
    {
        private SortedDictionary<string, Exportable> exportables = new SortedDictionary<string, Exportable>();
        public const String CONF = "ExportElectricityModConfig.txt";
        private float multiplier;
        private int realtimeinterval = 2;
        private const double interval = 1.0;
        private double waited = 0.0;
        private UISlider textbox;

        public ExportableManager()
        {
            multiplier = 1.0f;

            //new ExportableCremation (this);
            new ExportableElementary(this);
            new ExportableHighSchool(this);
            new ExportableUniversity(this);
            new ExportableElectricity(this);
            //new ExportableIncineration (this);
            new ExportableHealth(this);
            new ExportableHeat(this);
            new ExportableJail(this);
            new ExportableSewage(this);
            new ExportableWater(this);

            LoadSettings();
        }

        public void Log(string msg)
        {
            ExportElectricityMod.Debugger.Write(msg);
        }

        public void AddExportable(Exportable exp)
        {
            if (!exportables.ContainsKey(exp.Id))
                exportables.Add(exp.Id, exp);
        }

        public SortedDictionary<string, Exportable> GetExportables()
        {
            return exportables;
        }

        public void LoadSettings()
        {
            Log("Load Settings");
            try
            {
                using (var file = new StreamReader(CONF, true))
                {
                    var s = file.ReadLine();
                    var sections = s.Split(new char[1] { '|' });

                    var ids = sections[0].Split(new char[1] { ',' });
                    if (sections.Length == 3)
                    {
                        multiplier = float.Parse(sections[1]);
                        realtimeinterval = int.Parse(sections[2]);
                    }
                    else if (sections.Length == 2)
                    {
                        multiplier = float.Parse(sections[1]);
                        realtimeinterval = 2;
                    }
                    else
                    {
                        multiplier = 1.0f;
                        realtimeinterval = 2;
                    }


                    foreach (var id in ids)
                    {
                        if (exportables.ContainsKey(id))
                        {
                            exportables[id].SetEnabled(true, false);
                        }
                    }
                }
            }
            catch (Exception e)
            {
                // no file? use defaults
                Log("Using Defaults: " + e.ToString());
                exportables[Ids.ELECTRICITY].SetEnabled(true);
            }
        }

        public void ClearExportables()
        {
            foreach (var exportable in exportables)
            {
                var exp = exportable.Value;
                exp.LastWeeklyEarning = 0;

                if (exportables.ContainsKey(exportable.Key))
                    exportables[exportable.Key] = exp;
            }
        }

        public void StoreSettings()
        {
            Log("Store Settings");
            try
            {
                using (var file = new FileStream(CONF, FileMode.Create))
                {
                    var enabled_ids = new List<string>();
                    using (var sw = new StreamWriter(file))
                    {
                        foreach (var pair in exportables)
                        {
                            if (pair.Value.GetEnabled())
                            {
                                enabled_ids.Add(pair.Key);
                            }
                        }
                        var cs = string.Join(",", enabled_ids.ToArray()) + "|" + multiplier.ToString() + "|" + realtimeinterval.ToString();
                        Log("Storing settings - enabled: " + cs);
                        sw.WriteLine(cs);
                        sw.Flush();
                    }
                }
            }
            catch (Exception e)
            {
                Log("Error storing settings: " + e.ToString());
            }
        }

        public double CalculateIncome(District d, string id, double weekPortion)
        {
            double income = 0.0;

            if (exportables.ContainsKey(id))
            {
                Exportable exp = exportables[id];
                if (exp.GetEnabled())
                {
                    Log("Calculating Income for " + id);
                    income = exp.CalculateIncome(d, weekPortion);
                }
            }

            return income;
        }

        public double CalculateIncome(District d, double weekPortion)
        {
            double total = 0.0;

            waited += weekPortion;
            if (waited < interval)
            {
                return 0;
            }

            Log("Calculating Income");

            foreach (var id in exportables.Keys)
            {
                total += CalculateIncome(d, id, waited);
            }

            waited = 0.0;

            return total * multiplier;
        }

        public void AddOptions(UIHelperBase group)
        {
            LoadSettings();
            foreach (var id in exportables.Keys)
            {
                Exportable exp = exportables[id];
                group.AddCheckbox(exp.Description, exp.GetEnabled(), exp.SetEnabled);
            }

            textbox = group.AddSlider($"Multiplier", 0.0f, 2.0f, 0.05f, multiplier, MultiplierSliderChanged) as UISlider;

            var dropdown = group.AddDropdown("Real Time Mod Interval", new string[] { "3 Hours", "6 Hours", "12 Hours", "1 Day", "1 Week" }, 2, RealTimeModInterval) as UIDropDown;
            dropdown.tooltip = @"When using Real Time Mod, the payout cycle will be affected by the interval selected here. Default: 6 hours.";

            group.AddCheckbox("Debug Mode", ExportElectricityMod.Debugger.enabled, SetDebug);
        }

        private void MultiplierSliderChanged(float val)
        {
            if (textbox != null)
            {
                textbox.tooltip = $"{multiplier}";
            }

            multiplier = val;
            StoreSettings();
        }

        private void RealTimeModInterval(int index)
        {
            realtimeinterval = index;

            StoreSettings();
        }

        public void SetDebug(bool b)
        {
            ExportElectricityMod.Debugger.enabled = b;
        }

        public int GetRealTimeInterval()
        {
            return realtimeinterval;
        }
    }
}

