using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace ScienceContainers {
	public class ScienceContainer : PartModule {

		#region Fields

		protected bool collecting = false;
		protected int dataToCollect = 0;
		protected int storedDataCount = 0;

		[KSPField(guiName = "Auto-Collect", guiActive = true, isPersistant = true)]
		public bool autoCollect = false;

		[KSPField(guiName = "Collect", guiActive = true, isPersistant = true), UI_Toggle(scene = UI_Scene.Flight, enabledText = "All Data", disabledText = "Rerunnable")]
		public bool collectNonrerunnable = false;

		#endregion

		#region KSPEvents

		[KSPEvent(guiName = "Toggle Auto Collect", guiActive = true, name = "tglAutoColectEvent")]
		public void tglAutoCollectEvent() {
			tglAutoCollect();
		}

		[KSPEvent(guiName = "Collect Data", guiActive = true, guiActiveUnfocused = true, name = "collectDataEvent")]
		public void collectDataEvent() {
			initCollect();
		}

		[KSPEvent(guiName = "Take Data", guiActive = false, guiActiveUnfocused = true, name = "takeDataEvent")]
		public void takeDataEvent() {
			takeData();
		}

		[KSPEvent(guiName = "Store Data", guiActive = false, guiActiveUnfocused = true, name = "storeDataEvent")]
		public void storeDataEvent() {
			storeData();
		}

		#endregion

		#region KSPActions

		[KSPAction("CollectData")]
		public void collectDataAction(KSPActionParam param) {
			initCollect();
		}

		[KSPAction("Start Auto-Collect")]
		public void startAutoCollectAction(KSPActionParam param) {
			startAutoCollect();
		}

		[KSPAction("Stop Auto-Collect")]
		public void stopAutoCollectAction(KSPActionParam param) {
			stopAutoCollect();
		}

		[KSPAction("Toggle Auto-collect")]
		public void tglAutoCollectAction(KSPActionParam param) {
			tglAutoCollect();
		}

		#endregion

		#region PartModule Methods

		public override void OnStart(PartModule.StartState state) {
			base.OnStart(state);

			Events["collectDataEvent"].guiName = "Collect Data (" + dataToCollect + ")";
			Events["collectDataEvent"].guiActive = !autoCollect && !collecting && dataToCollect > 0;
			Events["collectDataEvent"].guiActiveUnfocused = !autoCollect && !collecting && dataToCollect > 0;

			Events["takeDataEvent"].guiName = "Take Data (" + storedDataCount + ")";
			Events["takeDataEvent"].guiActiveUnfocused = storedDataCount > 0;
		}

		public override void OnUpdate() {
			base.OnUpdate();

			Events["collectDataEvent"].guiName = "Collect Data (" + dataToCollect + ")";
			Events["collectDataEvent"].guiActive = !autoCollect && !collecting && dataToCollect > 0;
			Events["collectDataEvent"].guiActiveUnfocused = !autoCollect && !collecting && dataToCollect > 0;

			if(FlightGlobals.ActiveVessel.FindPartModulesImplementing<KerbalEVA>().Count > 0) {
				int i = 0;
				foreach(ModuleScienceContainer c in FlightGlobals.ActiveVessel.FindPartModulesImplementing<ModuleScienceContainer>()) {
					i += c.GetStoredDataCount();
				}
				Events["storeDataEvent"].guiActiveUnfocused = i > 0;
				Events["storeDataEvent"].guiName = "Store Data (" + i + ")";
			}

			Events["takeDataEvent"].guiName = "Take Data (" + storedDataCount + ")";
			Events["takeDataEvent"].guiActiveUnfocused = storedDataCount > 0;
		}

		public void FixedUpdate() {
			if(autoCollect && !collecting) {
				collectData(collectNonrerunnable);
			}
		}

		#endregion

		#region Other Methods

		protected void cancelOtherAutoCollect() {
			foreach(ScienceContainer collector in vessel.FindPartModulesImplementing<ScienceContainer>().Where(c => c != this)) {
				collector.autoCollect = false;
			}
		}

		protected void startAutoCollect() {
			cancelOtherAutoCollect();
			autoCollect = true;
		}

		protected void stopAutoCollect() {
			autoCollect = false;
		}

		protected void tglAutoCollect() {
			if(autoCollect) {
				stopAutoCollect();
			}
			else {
				startAutoCollect();
			}
		}

		protected void initCollect() {
			cancelOtherAutoCollect();

			if(!collectNonrerunnable && vessel.FindPartModulesImplementing<IScienceDataContainer>().Any(c => !c.IsRerunnable() && c.GetData().Count() > 0)) {
				DialogOption<bool>[] dialogOptions = new DialogOption<bool>[2];
				dialogOptions[0] = new DialogOption<bool>("Transfer All Science", new Callback<bool>(collectData), true);
				dialogOptions[1] = new DialogOption<bool>("Transfer Rerunnable Science Data Only", new Callback<bool>(collectData), false);
				PopupDialog.SpawnPopupDialog(new MultiOptionDialog("Transfering science from nonrerunnable experiments will cause them to become inoperable.", "Warning", HighLogic.Skin, dialogOptions), false, HighLogic.Skin);
			}
			else {
				collectData(true);
			}
		}

		protected virtual void collectData(bool collectAll) {}

		protected virtual IEnumerator collectDataCoroutine(List<IScienceDataContainer> containers){
			yield return new WaitForSeconds(0);
		}

		protected virtual void takeData() {}

		protected virtual void storeData() {}

		#endregion

	}
}