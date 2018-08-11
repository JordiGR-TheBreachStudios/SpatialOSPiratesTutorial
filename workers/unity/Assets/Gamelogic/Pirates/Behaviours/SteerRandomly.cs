using UnityEngine;
using Improbable.Unity;
using Improbable.Unity.Visualizer;
using Improbable.Ship;


namespace Assets.Gamelogic.Pirates.Behaviours
{
	// Add this MonoBehaviour on UnityWorker (server-side) workers only
	[WorkerType(WorkerPlatform.UnityWorker)]
	public class SteerRandomly : MonoBehaviour
	{
		// This MonoBehaviour will only be enabled for the single UnityWorker
		// which has write access to this entity's ShipControls component.
		[Require] private ShipControls.Writer ShipControlsWriter;

		private void RandomizeSteering()
		{
			ShipControlsWriter.Send(new ShipControls.Update()
				.SetTargetSpeed(Random.value)
				.SetTargetSteering((Random.value * 30.0f) - 15.0f));
		}

		private void OnEnable()
		{
			// Change steering decisions every five seconds
			InvokeRepeating("RandomizeSteering", 0, 5.0f);
		}

		private void OnDisable()
		{
			CancelInvoke("RandomizeSteering");
		}
	}
}
