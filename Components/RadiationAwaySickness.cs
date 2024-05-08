using UnityEngine;

namespace Radiation.Components
{
	[DisallowMultipleComponent]
	internal class RadiationAwaySickness : MonoBehaviour
	{
		internal Material material;

		public void Awake()
		{
			material = new Material(Radiation.RadiationAwaySicknessShader);
		}

		public void OnRenderImage(RenderTexture source, RenderTexture destination)
		{
			Graphics.Blit(source, destination, material);
		}
	}
}
