using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace ScienceContainers {
	class ProbeScienceContainer : ScienceContainer, IScienceDataContainer {

		#region Fields

		List<ScienceData> storedData = new List<ScienceData>();

		#endregion

		#region KSPEvents

		[KSPEvent(guiName = "Review Stored Data", guiActive = true, name = "reviewStoredData")]
		public void reviewStoredData() {
			ReviewData();
		}

		#endregion

		#region PartModule Methods

		public override void OnStart(PartModule.StartState state) {
			storedDataCount = storedData.Count;

			base.OnStart(state);

			Events["reviewStoredData"].guiName = "Reviewed Stored Data (" + storedData.Count + ")";
			Events["reviewStoredData"].guiActive = storedData.Count > 0;
		}

		public override void OnLoad(ConfigNode node) {
			base.OnLoad(node);

			foreach(ConfigNode dataNode in node.GetNodes("ScienceData")) {
				storedData.Add(new ScienceData(dataNode));
			}

			updateMenu();
		}

		public override void OnSave(ConfigNode node) {
			base.OnSave(node);

			node.RemoveNodes("ScienceData");
			foreach(ScienceData data in storedData) {
				data.Save((ConfigNode)node.AddNode("ScienceData"));
			}
		}

		public override void OnUpdate() {
			dataToCollect = 0;
			foreach(IScienceDataContainer contianer in vessel.FindPartModulesImplementing<IScienceDataContainer>().Where(c => !(c is ProbeScienceContainer && (ProbeScienceContainer)c == this))) {
				dataToCollect += contianer.GetData().Count();
			}

			storedDataCount = storedData.Count;
			
			base.OnUpdate();

			Events["reviewStoredData"].guiName = "Reviewed Stored Data (" + storedData.Count + ")";
			Events["reviewStoredData"].guiActive = storedData.Count > 0;
		}

		#endregion

		#region IScienceDataContainer Methods

		public ScienceData[] GetData() {
			return storedData.ToArray();
		}

		public void DumpData(ScienceData data) {
			storedData.Remove(data);
			updateMenu();
			ScreenMessages.PostScreenMessage("<color=#ff9900ff>[" + part.partInfo.title + "]: " + data.title + " Removed</color>", 5f, ScreenMessageStyle.UPPER_LEFT);
		}

		public void ReviewData() {
			foreach(ScienceData data in storedData) {
				ReviewDataItem(data);
			}
		}

		public void ReviewDataItem(ScienceData data) {
			ExperimentResultDialogPage page = new ExperimentResultDialogPage(
				part,
				data,
				data.transmitValue,
				ModuleScienceLab.GetBoostForVesselData(part.vessel, data),
				false,
				"",
				false,
				data.labBoost < 1 && vessel.FindPartModulesImplementing<ModuleScienceLab>().Count > 0 && ModuleScienceLab.IsLabData(data),
				new Callback<ScienceData>(onDiscardData),
				new Callback<ScienceData>(onKeepData),
				new Callback<ScienceData>(onTransmitData),
				new Callback<ScienceData>(onSendDataToLab));
			ExperimentsResultDialog.DisplayResult(page);
		}

		public int GetScienceCount() {
			return storedData.Count;
		}

		public bool IsRerunnable() {
			return true;
		}

		#endregion

		#region ExperimentresultDialogPage Callbacks

		public void onDiscardData(ScienceData data) {
			DumpData(data);
		}

		public void onKeepData(ScienceData data) {
		}

		public void onTransmitData(ScienceData data) {
			List<IScienceDataTransmitter> transList = vessel.FindPartModulesImplementing<IScienceDataTransmitter>();
			if(transList.Count > 0) {
				IScienceDataTransmitter trans = transList.OrderBy(t => ScienceUtil.GetTransmitterScore(t)).First(t => t.CanTransmit());
				if(trans != null) {
					trans.TransmitData(new List<ScienceData> { data });
					DumpData(data);
				}
				else {
					ScreenMessages.PostScreenMessage("<color=#ff9900ff>No opperational transmitter on this vessel.</color>", 4f, ScreenMessageStyle.UPPER_CENTER);
				}

			}
			else {
				ScreenMessages.PostScreenMessage("<color=#ff9900ff>No transmitter on this vessel.</color>", 4f, ScreenMessageStyle.UPPER_CENTER);
			}
		}

		public void onSendDataToLab(ScienceData data) {
			List<ModuleScienceLab> labList = vessel.FindPartModulesImplementing<ModuleScienceLab>();
			if(labList.Count > 0) {
				ModuleScienceLab lab = labList.OrderBy(l => ScienceUtil.GetLabScore(l)).First(l => l.IsOperational());
				if(lab != null) {
					lab.StartCoroutine(lab.ProcessData(data, new Callback<ScienceData>(onLabComplete)));
				}
				else {
					ScreenMessages.PostScreenMessage("<color=#ff9900ff>No opperational science lab on this vessel.</color>", 4f, ScreenMessageStyle.UPPER_CENTER);
				}
			}
			else {
				ScreenMessages.PostScreenMessage("<color=#ff9900ff>No science lab on this vessel.</color>", 4f, ScreenMessageStyle.UPPER_CENTER);
			}
		}

		public void onLabComplete(ScienceData data) {
			ReviewDataItem(data);
		}

		#endregion

		#region Other methods

		protected override IEnumerator collectDataCoroutine(List<IScienceDataContainer> containers) {
			if(!collecting) {
				collecting = true;

				foreach(IScienceDataContainer c in containers) {
					foreach(ScienceData d in c.GetData()) {
						if(d != null) {
							storedData.Add(d);
							c.DumpData(d);
							updateMenu();
							ScreenMessages.PostScreenMessage("<color=#99ff00ff>[" + part.partInfo.title + "]: <i>" + d.title + " </i> Added</color>", 5f, ScreenMessageStyle.UPPER_LEFT);
						}
					}
				}

				yield return new WaitForSeconds(1);

				collecting = false;
			}
		}

		protected override void collectData(bool collectAll) {
			foreach(ScienceContainer collector in vessel.FindPartModulesImplementing<ScienceContainer>().Where(c => c != this)) {
				collector.autoCollect = false;
			}

			List<IScienceDataContainer> contianers = vessel.FindPartModulesImplementing<IScienceDataContainer>().Where(c => !(c is ProbeScienceContainer && (ProbeScienceContainer)c == this) && (collectAll ? true : c.IsRerunnable())).ToList();
			StartCoroutine(collectDataCoroutine(contianers));
		}

		protected override void takeData() {
			List<ModuleScienceContainer> EVACont = FlightGlobals.ActiveVessel.FindPartModulesImplementing<ModuleScienceContainer>();
			if(EVACont.FirstOrDefault().StoreData(new List<IScienceDataContainer> { this }, false)) {
				foreach(ScienceData data in storedData) {
					DumpData(data);
				}
			}
		}

		protected override void storeData() {
			foreach(ModuleScienceContainer c in FlightGlobals.ActiveVessel.FindPartModulesImplementing<ModuleScienceContainer>()) {
				foreach(ScienceData d in c.GetData()) {
					if(d != null) {
						storedData.Add(d);
						c.DumpData(d);
						ScreenMessages.PostScreenMessage("<color=#99ff00ff>[" + part.partInfo.title + "]: <i>" + d.title + " </i> Added</color>", 5f, ScreenMessageStyle.UPPER_LEFT);
					}
				}
			}
		}

		protected void updateMenu() {
			
		}

		#endregion

	}
}
