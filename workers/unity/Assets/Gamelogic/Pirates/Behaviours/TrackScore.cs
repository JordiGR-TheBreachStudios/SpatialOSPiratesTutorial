using Improbable.Entity.Component;
using Improbable.Ship;
using Improbable.Unity;
using Improbable.Unity.Visualizer;
using UnityEngine;

namespace Assets.Gamelogic.Pirates.Behaviours
{
	// Add this MonoBehaviour on UnityWorker (server-side) workers only
	[WorkerType(WorkerPlatform.UnityWorker)]
	public class TrackScore : MonoBehaviour
	{
		/*
         * An entity with this MonoBehaviour will only be enabled for the single UnityWorker
         * which has write access for its Score component.
         */
		[Require] private Score.Writer ScoreWriter;

		void OnEnable()
		{
			// Register command callback
			ScoreWriter.CommandReceiver.OnAwardPoints.RegisterResponse(OnAwardPoints);
		}

		private void OnDisable()
		{
			// Deregister command callbacks
			ScoreWriter.CommandReceiver.OnAwardPoints.DeregisterResponse();
		}

		// Command callback for handling points awarded by other entities when they sink
		private AwardResponse OnAwardPoints(AwardPoints request, ICommandCallerInfo callerInfo)
		{
			int newScore = ScoreWriter.Data.numberOfPoints + (int)request.amount;
			ScoreWriter.Send(new Score.Update().SetNumberOfPoints(newScore));
			// Acknowledge command receipt
			return new AwardResponse(request.amount);
		}
	}
}