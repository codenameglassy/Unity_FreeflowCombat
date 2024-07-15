using UnityEngine;


namespace FirstGearGames.SmoothCameraShaker
{



	public class ShakableBase : MonoBehaviour
	{
		#region Types.
		public enum ShakerTypes
		{
			CameraShaker = 0,
			ObjectShaker = 1
		}
		#endregion

		#region Serialized.
		/// <summary>
		/// 
		/// </summary>
		[Tooltip("Shaker type to use. CameraShaker will subscribe to your current or otherwise configured CameraShaker. ObjectShaker will subscribe to the first ObjectShaker found on or in parented objects.")]
		[SerializeField]
		private ShakerTypes _shakerType = ShakerTypes.CameraShaker;
		/// <summary>
		/// Shaker type to use. CameraShaker will subscribe to your current or otherwise configured CameraShaker. ObjectShaker will subscribe to the first ObjectShaker found on or in parented objects.s
		/// </summary>
		public ShakerTypes ShakerType { get { return _shakerType; } }		
		#endregion


	}


}