using Improbable.Unity;
using Improbable.Unity.Visualizer;
using Improbable.Ship;
using UnityEngine;

namespace Assets.Gamelogic.Pirates.Behaviours
{
    // Add this MonoBehaviour on client workers only
    [WorkerType(WorkerPlatform.UnityClient)]
    public class SinkingBehaviour : MonoBehaviour
    {
		// Enable this MonoBehaviour on workers with read access to the entity's Health component
		[Require] private Health.Reader HealthReader;

		[SerializeField]
        private Animation SinkingAnimation;

		private bool alreadySunk = false;

		private void OnEnable()
        {
			alreadySunk = false;
			InitializeSinkingAnimation();
			// Register callback for when components change
			HealthReader.CurrentHealthUpdated.Add(OnCurrentHealthUpdated);
		}

        private void OnDisable()
        {
			// Deregister callback for when components change
			HealthReader.CurrentHealthUpdated.Remove(OnCurrentHealthUpdated);
		}

		private void InitializeSinkingAnimation()
		{
			/*
			 * SinkingAnimation is triggered when the ship is first killed. But a worker which checks out
			 * the entity after this time (for example, a client connecting to the game later) 
			 * must not visualize the ship as still alive.
			 * 
			 * Therefore, on checkout, any sunk ships jump to the end of the sinking animation.
			 */

			if (HealthReader.Data.currentHealth <= 0)
			{
				foreach (AnimationState state in SinkingAnimation)
				{
					// Jump to end of the animation
					state.normalizedTime = 1;
				}
				VisualiseSinking();
				alreadySunk = true;
			}
		}

		private void OnCurrentHealthUpdated(int currentHealth)
		{
			if (!alreadySunk && currentHealth <= 0)
			{
				VisualiseSinking();
				alreadySunk = true;
			}
		}

		private void VisualiseSinking()
		{
			SinkingAnimation.Play();
		}
	}
}
