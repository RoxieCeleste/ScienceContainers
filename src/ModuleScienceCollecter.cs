using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

using UnityEngine;

namespace ScienceContainer {
	public class ModuleScienceCollecter : PartModule {

		public override void OnUpdate() {
			Events["collectData"].active = (part.FindModulesImplementing<ModuleScienceContainer>().Count() > 1);
		}

		[KSPEvent(name = "collectData", active = true, guiActive = true, guiName = "Collect Data")]
		public void collectData() {
			int i = 0;
			ScienceData lastData = null;

			ModuleScienceContainer storage = part.FindModuleImplementing<ModuleScienceContainer>();

			if(storage == null) {
				ScreenMessages.PostScreenMessage("Science Collector has no storage.", 4f, ScreenMessageStyle.UPPER_LEFT);
				return;
			}

			List<IScienceDataContainer> containers = vessel.FindPartModulesImplementing<IScienceDataContainer>();

			foreach(IScienceDataContainer c in containers) {
				if((PartModule)c != (PartModule)storage)
					c.GetData().ToList().ForEach(delegate (ScienceData d) {
						lastData = d;
						storage.AddData(d);
						c.DumpData(d);
						i++;
					});
			}
		}

		[KSPAction("Collect Data")]
		public void collectDataAction(KSPActionParam param) {
			collectData();
		}
	}
}