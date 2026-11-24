using ABI.CCK.Components;
using UnityEngine;
using UnityEngine.Rendering;

namespace VideoRemote
{
    // A head-locked quad that mirrors a texture straight into the local player's camera.
    // Used both for the "fullscreen" video view and as the display window for the Video Player Cam.
    // Purely local, never networked.
    internal sealed class FullscreenScreen : MonoBehaviour
    {
        internal CVRVideoPlayer VideoPlayer;        // used for validity + default texture
        internal RenderTexture OverrideTexture;     // if set, shown instead of the video texture
        internal float Distance = 0.5f;
        internal float SizeMultiplier = 1f;
        internal bool FillView = true;              // true: fill the FOV; false: use AbsoluteHeight
        internal float AbsoluteHeight = 0.4f;       // world-space height when FillView is false
        internal float OffsetX = 0f;                // head-locked horizontal shift (right is +)
        internal float OffsetY = 0f;                // head-locked vertical shift (up is +)

        private MeshRenderer _renderer;
        private Material _material;

        private void Awake()
        {
            // The primitive ships with a collider; drop it so the quad can't be grabbed or raycast.
            var col = GetComponent<Collider>();
            if (col != null) Destroy(col);

            _renderer = GetComponent<MeshRenderer>();
            if (_renderer != null)
            {
                _renderer.shadowCastingMode = ShadowCastingMode.Off;
                _renderer.receiveShadows = false;
                var shader = Shader.Find("Unlit/Texture");
                _material = shader != null ? new Material(shader) : _renderer.material;
                _renderer.material = _material;
            }
        }

        private void LateUpdate()
        {
            var cam = Camera.main;
            if (cam == null || VideoPlayer == null || VideoPlayer.Equals(null))
            {
                Destroy(gameObject);
                return;
            }

            var tex = OverrideTexture != null ? (Texture)OverrideTexture : VideoPlayer.ProjectionTexture;
            if (tex == null) return;

            if (_material != null && _material.mainTexture != tex)
                _material.mainTexture = tex;

            // Head-lock: sit a fixed distance in front of the camera, matching its rotation.
            // A Unity Quad's normal faces -Z, so aligning rotation with the camera makes the
            // textured face point back at the viewer, upright and un-mirrored.
            var camT = cam.transform;
            transform.position = camT.position + camT.forward * Distance
                                 + camT.right * OffsetX + camT.up * OffsetY;
            transform.rotation = camT.rotation;

            var aspect = tex.height > 0 ? (float)tex.width / tex.height : 16f / 9f;
            var height = FillView
                ? 2f * Distance * Mathf.Tan(cam.fieldOfView * 0.5f * Mathf.Deg2Rad) * SizeMultiplier
                : AbsoluteHeight * SizeMultiplier;
            transform.localScale = new Vector3(height * aspect, height, 1f);
        }

        private void OnDestroy()
        {
            if (_material != null) Destroy(_material);
        }
    }
}
