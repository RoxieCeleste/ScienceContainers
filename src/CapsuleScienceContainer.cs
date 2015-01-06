using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace ScienceContainers {
	class CapsuleScienceContainer : ScienceContainer {

		#region Fields

		ModuleScienceContainer scienceContainer = null;

		#endregion

		#region PartModule Methods

		public override void OnStart(PartModule.StartState state) {
			scienceContainer = part.FindModuleImplementing<ModuleScienceContainer>();
			
			storedDataCount = scienceContainer.GetData().Count();

			base.OnStart(state);

			print("T1");

			scienceContainer.Events["StoreDataExternalEvent"].guiActiveUnfocused = false;

			print("T2");

			scienceContainer.Events["CollectDataExternalEvent"].guiActiveUnfocused = false;
		}

		public override void OnLoad(ConfigNode node) {
			base.OnLoad(node);
		}

		public override void OnUpdate() {
			dataToCollect = 0;
			foreach(IScienceDataContainer contianer in vessel.FindPartModulesImplementing<IScienceDataContainer>().Where(c => !(c is ModuleScienceContainer && (ModuleScienceContainer)c == scienceContainer))) {
				dataToCollect += contianer.GetData().Count();
			}
			
			storedDataCount = scienceContainer.GetData().Count();

			base.OnUpdate();

			scienceContainer.Events["StoreDataExternalEvent"].guiActiveUnfocused = false;
			scienceContainer.Events["CollectDataExternalEvent"].guiActiveUnfocused = false;
		}

		#endregion

		#region Other methods

		protected override IEnumerator collectDataCoroutine(List<IScienceDataContainer> containers) {
			if(!collecting) {
				collecting = true;

				foreach(IScienceDataContainer c in containers) {
					foreach(ScienceData d in c.GetData()) {
						if(d != null) {
							scienceContainer.AddData(d);
							c.DumpData(d);
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

			List<IScienceDataContainer> contianers = vessel.FindPartModulesImplementing<IScienceDataContainer>().Where(c => !(c is ModuleScienceContainer && (ModuleScienceContainer)c == scienceContainer) && (collectAll ? true : c.IsRerunnable())).ToList();
			StartCoroutine(collectDataCoroutine(contianers));
		}

		protected override void takeData() {
			List<ModuleScienceContainer> EVACont = FlightGlobals.ActiveVessel.FindPartModulesImplementing<ModuleScienceContainer>();
			if(EVACont.FirstOrDefault().StoreData(new List<IScienceDataContainer> { scienceContainer }, false)) {
				foreach(ScienceData data in scienceContainer.GetData()) {
					scienceContainer.DumpData(data);
				}
			}
		}

		protected override void storeData() {
			foreach(ModuleScienceContainer c in FlightGlobals.ActiveVessel.FindPartModulesImplementing<ModuleScienceContainer>()) {
				foreach(ScienceData d in c.GetData()) {
					if(d != null) {
						scienceContainer.AddData(d);
						c.DumpData(d);
					}
				}
			}

			foreach(IScienceDataContainer c in FlightGlobals.ActiveVessel.FindPartModulesImplementing<IScienceDataContainer>()) {
				foreach(ScienceData d in c.GetData()) {
					if(d != null) {
						scienceContainer.AddData(d);
						c.DumpData(d);
					}
				}
			}
		}

		#endregion
	}
}
