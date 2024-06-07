/*using UnityEngine;
using Mirror;

namespace Mirror.Examples.Common
{
    public class RandomColor : NetworkBehaviour
    {
        [SyncVar(hook = nameof(SetColor))]
        public Color32 color;

        public override void OnStartServer()
        {
            // Assign a random color when the object is created on the server
            color = new Color32(
                (byte)Random.Range(0, 256),
                (byte)Random.Range(0, 256),
                (byte)Random.Range(0, 256),
                255);
        }

        void SetColor(Color32 oldColor, Color32 newColor)
        {
            // Ensure the renderer is not null before setting the color
            if (TryGetComponent<Renderer>(out var renderer))
            {
                renderer.material.color = newColor;
            }
            else
            {
                Debug.LogError("Renderer component missing in RandomColor");
            }
        }

        public override void OnSerialize(NetworkWriter writer, bool initialState)
        {
            base.OnSerialize(writer, initialState);
            writer.WriteColor32(color);
        }

        public override void OnDeserialize(NetworkReader reader, bool initialState)
        {
            base.OnDeserialize(reader, initialState);
            color = reader.ReadColor32();
        }
    }
}
*/