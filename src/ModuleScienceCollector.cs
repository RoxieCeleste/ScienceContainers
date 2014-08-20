using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

using UnityEngine;

namespace ScienceContainer {
	public class ModuleScienceCollector : PartModule {

		[KSPEvent(name = "collectData", active = true, guiActive = true, guiName = "Collect Data")]
		public void collectData() {
			ModuleScienceContainer storage = part.FindModuleImplementing<ModuleScienceContainer>();

			if(storage == null) {
				ScreenMessages.PostScreenMessage("Science Collector has no storage.", 4f, ScreenMessageStyle.UPPER_LEFT);
				return;
			}

			List<IScienceDataContainer> containers = vessel.FindPartModulesImplementing<IScienceDataContainer>()
				.Where(container => !container.Equals(storage)).ToList();


			storage.StoreData(containers, false);
		}

		[KSPAction("Collect Data")]
		public void collectDataAction(KSPActionParam param) {
			collectData();
		}
	}
}