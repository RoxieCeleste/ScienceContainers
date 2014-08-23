using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace ScienceContainer {
	public class AutoCollectScienceContainer : ScienceContainer {

		/* Overriden PartModule Methods */
		public override void OnStart(StartState state) {
			base.OnStart(state);
			Events["togglePrompt"].active = false;
		}

		public override void OnLoad(ConfigNode node) {
			base.OnLoad(node);
		}

		public override void OnSave(ConfigNode node) {
			base.OnSave(node);
		}

		public override void OnUpdate() {
			base.OnUpdate();
			if(autoCollectEnabled) {
				autoCollectData();
			}
		}

		/* IScienceDatacontainer Methods */

		/* Experiment Result Dialog Page Callbacks */

		/* Fields */
		[KSPField(guiActive = false, isPersistant = true)]
		protected bool autoCollectEnabled = false;

		[KSPField(guiActive = true, isPersistant = true, guiName = "Collect Nonrerunnable")]
		protected bool autoCollectNonrerunnable = false;

		/* Events */
		[KSPEvent(name = "toggleCollectNonrerunnable", active = true, guiActive = true, guiName = "Collect Nonrerunnable?")]
		public void toggleCollectNonrerunnable() {
			autoCollectNonrerunnable = !autoCollectNonrerunnable;
		}

		[KSPEvent(name = "startAutoCollect", active = true, guiActive = true, guiName = "Start Auto-collect")]
		public void startAutoCollect() {
			base.cancelAutoCollect();
			autoCollectEnabled = true;
			Events["stopAutoCollect"].guiActive = true;
			Events["startAutoCollect"].guiActive = false;
		}

		[KSPEvent(name = "stopAutoCollect", active = true, guiActive = false, guiName = "Stop Auto-collect")]
		public void stopAutoCollect() {
			autoCollectEnabled = false;
			Events["stopAutoCollect"].guiActive = false;
			Events["startAutoCollect"].guiActive = true;
		}

		/* Actions */
		[KSPAction("Toggle Auto-collect")]
		public void toggleAutoCollect() {
			autoCollectEnabled = !autoCollectEnabled;
		}

		[KSPAction("Start Auto-collect")]
		public void startAutoCollectAction() {
			startAutoCollect();
		}

		[KSPAction("Stop Auto-collect")]
		public void stopAutoCollectAction() {
			stopAutoCollect();
		}

		/* Other Methods */
		public bool isAutoCollectEnabled() {
			return autoCollectEnabled;
		}

		protected void autoCollectData() {
			List<IScienceDataContainer> containers = vessel.FindPartModulesImplementing<IScienceDataContainer>();
			if(autoCollectNonrerunnable) {
				onTransferNonrerunnable(containers);
			}
			else {
				onTransferRerunnable(containers);
			}
		}
	}
}