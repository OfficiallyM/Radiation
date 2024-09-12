using UnityEngine;

namespace Radiation.Components
{
	internal class CameraEffect : MonoBehaviour
	{
		private Material _material;

        public Material GetMaterial() { 
            return _material; 
        }

        public void SetMaterial(Material material)
        {
            _material = material;
        }

		public void OnRenderImage(RenderTexture source, RenderTexture destination)
		{
			Graphics.Blit(source, destination, _material);
		}
	}
}
