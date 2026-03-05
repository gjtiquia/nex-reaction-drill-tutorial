#nullable enable

using UnityEngine;
using UnityEngine.UI;

namespace Nex.Essentials.Examples.PointerInput
{
    public class JoystickManager : MonoBehaviour
    {
        [SerializeField] private JoystickProducer joystickProducer = null!;
        [SerializeField] private RectTransform joystickKnob = null!;

        [SerializeField] private float scale = 50;
        
        private Image joystickKnobImage = null!;

        private void Start()
        {
            joystickKnobImage = joystickKnob.GetComponent<Image>();   
        }

        private void Update()
        {
            // Set knob color to red when in dead zone
            var joystickOutput = joystickProducer.JoystickOutput.GetJoystickPosition();
            joystickKnobImage.color = joystickOutput == Vector2.zero ? Color.red : Color.green;
            
            // Set knob position to the raw position
            var rawJoystickOutput = joystickProducer.JoystickOutput.GetRawJoystickPosition();
            joystickKnob.anchoredPosition = rawJoystickOutput * scale;
        }
    }
}
