using UnityEngine;
using Improbable.Ship;
using Improbable.Unity.Visualizer;

namespace Assets.Gamelogic.Pirates.Cannons
{
    // This MonoBehaviour will be enabled on both client and server-side workers
    public class CannonFirer : MonoBehaviour
    {
		[Require] private ShipControls.Reader ShipControlsReader;

		private Cannon cannon;

        private void Start()
        {
            // Cache entity's cannon gameobject
            cannon = gameObject.GetComponent<Cannon>();
        }

		private void OnEnable()
		{
			ShipControlsReader.FireLeftTriggered.Add(OnFireLeft);
			ShipControlsReader.FireRightTriggered.Add(OnFireRight);
		}

		private void OnDisable()
		{
			ShipControlsReader.FireLeftTriggered.Remove(OnFireLeft);
			ShipControlsReader.FireRightTriggered.Remove(OnFireRight);
		}

		public void AttemptToFireCannons(Vector3 direction)
        {
            if (cannon != null)
            {
                cannon.Fire(direction);
            }
        }

		private void OnFireLeft(FireLeft fireLeft)
		{
			// Respond to FireLeft event
			AttemptToFireCannons(-transform.right);
		}

		private void OnFireRight(FireRight fireRight)
		{
			// Respond to FireRight event
			AttemptToFireCannons(transform.right);
		}
	}
}