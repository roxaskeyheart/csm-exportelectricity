using ColossalFramework.UI;
using ICities;
using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace Exportable
{
	public class ExportableManager
	{
		private SortedDictionary<string, Exportable> exportables = new SortedDictionary<string, Exportable>();
		public const String CONF = "ExportElectricityModConfig.txt";
		private float multiplier;
        private const double interval = 1.0;
        private double waited = 0.0;
		private UISlider textbox;

		public ExportableManager ()
		{
			multiplier = 1.0f;

			//new ExportableCremation (this);
			new ExportableElementary (this);
			new ExportableHighSchool (this);
			new ExportableUniversity (this);
			new ExportableElectricity (this);
			//new ExportableIncineration (this);
			new ExportableHealth (this);
			new ExportableHeat (this);
			new ExportableJail (this);
			new ExportableSewage (this);
			new ExportableWater (this);

			LoadSettings();
		}

		public void Log (String msg)
		{
			ExportElectricityMod.Debugger.Write(msg);
		}

		public void AddExportable (Exportable exp)
		{
			if (!exportables.ContainsKey(exp.Id))
				exportables.Add (exp.Id, exp);
		}

        public SortedDictionary<string, Exportable> GetExportables()
        {
            return exportables;
        }

		public void LoadSettings ()
		{
			Log ("Load Settings");
			try {
				using (System.IO.StreamReader file = 
					new System.IO.StreamReader(CONF, true))
				{
					String s = file.ReadLine ();
					String [] sections = s.Split(new char[1]{'|'});
					String [] ids;

					ids = sections[0].Split(new char[1]{','});
					if (sections.Length >= 2) {
						multiplier = float.Parse(sections[1]);
					} else {
						multiplier = 1.0f;
					}

					foreach (var id in ids) {
						if (exportables.ContainsKey(id)) {
							exportables[id].SetEnabled(true, false);
						}
					}
				}
			} catch (Exception e) {
				// no file? use defaults
				Log ("Using defaults: " + e.ToString());
				exportables[Ids.ELECTRICITY].SetEnabled(true);
			}
		}

		public void StoreSettings()
		{
			Log ("Store Settings");
			try {
				using (System.IO.FileStream file =
					new System.IO.FileStream(CONF, FileMode.Create))
				{
					List<String>enabled_ids = new List<String>();
					StreamWriter sw = new StreamWriter(file);
					foreach (var pair in exportables) {
						if (pair.Value.GetEnabled()) {
							enabled_ids.Add(pair.Key);
						}
					}
					String cs = String.Join(",", enabled_ids.ToArray()) + "|" + multiplier.ToString();
					Log ("Storing settings - enabled: " + cs);
					sw.WriteLine(cs);
					sw.Flush();
				}
			} catch (Exception e) {
				Log ("Error storing settings: " + e.ToString());
			}
		}

		public double CalculateIncome (District d, String id, double weekPortion)
		{
			double income = 0.0;

			if (exportables.ContainsKey (id)) {
				Exportable exp = exportables [id];
				if (exp.GetEnabled()) {
					Log ("Calculating Income for " + id);
					income = exp.CalculateIncome (d, weekPortion);
				}
			}

			return income;
		}

		public double CalculateIncome (District d, double weekPortion)
		{
			double total = 0.0;
			/*
			if (!updated) {
				return 0;
			}
			*/

			waited += weekPortion;
            if (waited < interval)
            {
                return 0;
            }

            Log ("Calculating Income");

			foreach (var id in exportables.Keys) {
				total += CalculateIncome (d, id, waited);
			}

            waited = 0.0;

			return total * multiplier;
		}

		public void AddOptions (UIHelperBase group)
		{
			LoadSettings ();
			foreach (var id in exportables.Keys) {
				Exportable exp = exportables [id];
				group.AddCheckbox(exp.Description, exp.GetEnabled(), exp.SetEnabled);
			}

			textbox = group.AddSlider($"Multiplier", 0.0f, 2.0f, 0.05f, multiplier, MultiplierSliderChanged) as UISlider;
			group.AddCheckbox ("Debug Mode", ExportElectricityMod.Debugger.enabled, SetDebug);
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

		public void SetDebug (bool b)
		{
			ExportElectricityMod.Debugger.enabled = b;
		}
	}
}

