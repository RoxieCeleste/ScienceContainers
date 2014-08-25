using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using ConfigurableScienceData;

namespace ScienceContainer {
	public class ScienceContainer : PartModule, IScienceDataContainer {

		protected List<ScienceData> storedData = new List<ScienceData>();

		/* Overriden PartModule Methods */
		public override void OnStart(StartState state) {
			base.OnStart(state);
		}

		public override void OnLoad(ConfigNode node) {
			base.OnLoad(node);
			foreach(ConfigNode dataNode in node.GetNodes("ScienceData")) {
				storedData.Add(new CfgScienceData(dataNode));
			}
			updateMenu();
		}

		public override void OnSave(ConfigNode node) {
			base.OnSave(node);
			node.RemoveNodes("ScienceData");
			foreach(CfgScienceData data in storedData) {
				data.Save((ConfigNode)node.AddNode("ScienceData"));
			}
		}

		public override void OnUpdate() {
			base.OnUpdate();
			int i = 0;
			if(FlightGlobals.ActiveVessel.FindPartModulesImplementing<KerbalEVA>().Count > 0) {
				foreach(ModuleScienceContainer c in FlightGlobals.ActiveVessel.FindPartModulesImplementing<ModuleScienceContainer>()) {
					i+=c.GetStoredDataCount();
				}
				Events["storeDataEVA"].active = i > 0;
				Events["storeDataEVA"].guiName = "Store Data (" + i + ")";
			}
		}

		/* IScienceDatacontainer Methods */
		public ScienceData[] GetData() {
			return storedData.ToArray();
		}

		public void DumpData(ScienceData data) {
			storedData.Remove((CfgScienceData)data);
			updateMenu();
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
				ModuleCfgScienceLab.GetBoostForVesselData(part.vessel, (CfgScienceData)data),
				false,
				"",
				false,
				data.labBoost < 1 && vessel.FindPartModulesImplementing<ModuleCfgScienceLab>().Count > 0 && ModuleScienceLab.IsLabData(data),
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

		/* Experiment Result Dialog Page Callbacks */
		public void onDiscardData(ScienceData data) {
			DumpData(data);
		}

		public void onKeepData(ScienceData data) {
		}

		public void onTransmitData(ScienceData data) {
			List<IScienceDataTransmitter> transList = vessel.FindPartModulesImplementing<IScienceDataTransmitter>();
			if(transList.Count > 0) {
				IScienceDataTransmitter transmitter = transList.First(t => t.CanTransmit());
				if(transmitter != null) {
					transmitter.TransmitData(new List<ScienceData> { data });
					storedData.Remove((CfgScienceData)data);
					updateMenu();
				}
				else {
					ScreenMessages.PostScreenMessage("No opperational transmitter on this vessel.", 4f, ScreenMessageStyle.UPPER_CENTER);
				}
			}
			else {
				ScreenMessages.PostScreenMessage("No transmitter on this vessel.", 4f, ScreenMessageStyle.UPPER_CENTER);
			}
		}

		public void onSendDataToLab(ScienceData data) {
			List<ModuleCfgScienceLab> labList = vessel.FindPartModulesImplementing<ModuleCfgScienceLab>();
			if(labList.Count > 0) {
				ModuleCfgScienceLab lab = labList.FirstOrDefault(l => l.IsOperational());
				if(lab != null) {
					lab.StartCoroutine(labList.FirstOrDefault().ProcessData((CfgScienceData)data, new Callback<ScienceData>(onLabComplete)));
				}
				else {
					ScreenMessages.PostScreenMessage("No opperational science lab on this vessel.", 4f, ScreenMessageStyle.UPPER_CENTER);
				}
			}
			else {
				ScreenMessages.PostScreenMessage("No science lab on this vessel.", 4f, ScreenMessageStyle.UPPER_CENTER);
			}
		}

		public void onLabComplete(ScienceData data) {
			ReviewDataItem(data);
		}

		/* Fields */
		[KSPField(guiActive = true, guiName = "Warning Prompt Suppressed", isPersistant = true)]
		protected bool suppressPrompt = false;

		/* Events */
		[KSPEvent(name = "collectDataManually", active = true, guiActive = true, guiName = "Collect Data")]
		public void collectDataManually() {
			cancelAutoCollect();
			collectData();
		}

		[KSPEvent(name = "reviewStoredData", active = true, guiActive = false, guiName = "Review Data")]
		public void reviewStoredData() {
			ReviewData();
			updateMenu();
		}

		[KSPEvent(name = "retrieveDataEVA", active = true, externalToEVAOnly = true, guiActiveUnfocused =  true, guiName = "Retrieve Data", unfocusedRange = 1.5f)]
		public void retrieveDataEVA() {
			List<ModuleScienceContainer> EVACont = FlightGlobals.ActiveVessel.FindPartModulesImplementing<ModuleScienceContainer>();
			if(EVACont.FirstOrDefault().StoreData(new List<IScienceDataContainer> { this }, false)) {
				foreach(ScienceData data in storedData) {
					DumpData(data);
				}
			}
			updateMenu();
		}

		[KSPEvent(name = "storeDataEVA", active = false, externalToEVAOnly = true, guiActiveUnfocused =  true, guiName = "Store Data", unfocusedRange = 1.5f)]
		public void storeDataEVA() {
			ScienceData lastData = null;
			int numberOfData = 0;
			List<ModuleScienceContainer> EVACont = FlightGlobals.ActiveVessel.FindPartModulesImplementing<ModuleScienceContainer>();
			foreach(ModuleScienceContainer c in EVACont) {
				ScienceData[] data = c.GetData();
				foreach(ScienceData d in data) {
					if(d != null) {
						lastData = d;
						storedData.Add((CfgScienceData)d);
						c.DumpData(d);
						numberOfData++;
					}
				}
			}
			showMessages(numberOfData, lastData);
			updateMenu();
		}

		[KSPEvent(name = "collectDataEVA", active = true, externalToEVAOnly = true, guiActiveUnfocused = true, guiName = "Collect Data", unfocusedRange = 1.5f)]
		public void collectDataEVA() {
			collectData();
		}

		[KSPEvent(name = "togglePrompt", active = true, guiActive = true, guiName = "Toggle Prompt Suppression")]
		public void togglePrompt() {
			suppressPrompt = !suppressPrompt;
		}

		/* Actions */
		[KSPAction("Collect Data")]
		public void collectDataAction(KSPActionParam param) {
			collectData();
		}

		/* Other Methods */
		protected void updateMenu() {
			Events["retrieveDataEVA"].active = storedData.Count > 0;
			Events["retrieveDataEVA"].guiName = "Retrieve Data (" + storedData.Count + ")";

			Events["reviewStoredData"].guiActive = storedData.Count > 0;
			Events["reviewStoredData"].guiName = "Review Data (" + storedData.Count + ")";
		}

		protected void cancelAutoCollect() {
			List<AutoCollectScienceContainer> containers = vessel.FindPartModulesImplementing<AutoCollectScienceContainer>().ToList();
			foreach(AutoCollectScienceContainer c in containers) {
				if(c.isAutoCollectEnabled()) {
					c.stopAutoCollect();
				}
			}
		}

		protected void collectData() {
			List<IScienceDataContainer> containers = vessel.FindPartModulesImplementing<IScienceDataContainer>();
			bool prompt = false;

			if(!suppressPrompt) {
				foreach(IScienceDataContainer c in containers) {
					if(!c.IsRerunnable() && c.GetData().Count()>0) {
						prompt = true;
					}
				}
			}

			if(prompt) {
				promptForCollect(containers);
			}
			else {
				onTransferNonrerunnable(containers);
			}
		}

		protected void promptForCollect(List<IScienceDataContainer> containers) {
			DialogOption[] dialog = new DialogOption[2];
			dialog[0] = new DialogOption<List<IScienceDataContainer>>("Transfer All Science", new Callback<List<IScienceDataContainer>>(onTransferNonrerunnable), containers);
			dialog[1] = new DialogOption<List<IScienceDataContainer>>("Transfer Rerunnable Science Only", new Callback<List<IScienceDataContainer>>(onTransferRerunnable), containers);
			PopupDialog.SpawnPopupDialog(new MultiOptionDialog("Transfering science from nonrerunnable parts will cause them to be inoperable.", "Warning", HighLogic.Skin, dialog), false, HighLogic.Skin);
		}

		protected void onTransferNonrerunnable(List<IScienceDataContainer> containers) {
			int numberOfData = 0;
			ScienceData lastData = null;
			foreach(IScienceDataContainer c in containers) {
				if(c != (IScienceDataContainer)this) {
					ScienceData[] data = c.GetData();
					foreach(ScienceData d in data) {
						if(d != null) {
							lastData = d;
							storedData.Add(new CfgScienceData(d));
							numberOfData++;
							c.DumpData(d);
						}
					}
				}
			}
			showMessages(numberOfData, lastData);
		}
		protected void onTransferRerunnable(List<IScienceDataContainer> containers) {
			int numberOfData = 0;
			ScienceData lastData = null;
			foreach(IScienceDataContainer c in containers) {
				if(c != (IScienceDataContainer)this) {
					ScienceData[] data = c.GetData();
					if(c.IsRerunnable()) {
						foreach(ScienceData d in data) {
							if(d != null) {
								lastData = d;
								storedData.Add(new CfgScienceData(d));
								numberOfData++;
								c.DumpData(d);
							}
						}
					}
				}
			}
			showMessages(numberOfData, lastData);
		}

		protected void showMessages(int numberOfData, ScienceData lastData) {
			if(numberOfData == 1) {
				ScreenMessages.PostScreenMessage(lastData.title + " transferred to " + base.part.partInfo.title + ".", 4f, ScreenMessageStyle.UPPER_LEFT);
			}
			else if(numberOfData > 1) {
				ScreenMessages.PostScreenMessage(numberOfData + " science reports transferred to " + base.part.partInfo.title + ".", 4f, ScreenMessageStyle.UPPER_LEFT);
			}
			updateMenu();
		}
	}
}