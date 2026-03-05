using TMPro;
using UnityEngine;

namespace Nex.Essentials.Examples.PointerInput
{
    public class JoystickDebug : MonoBehaviour
    {
        [SerializeField] private JoystickProducer joystickProducer = null!;
        [SerializeField] private TMP_Text debugText = null!;

        private void Update()
        {
            var pos = joystickProducer.JoystickOutput.GetJoystickPosition();
            var raw = joystickProducer.JoystickOutput.GetRawJoystickPosition();
            var four = joystickProducer.JoystickOutput.GetFourDirection();
            var fourVec = joystickProducer.JoystickOutput.GetFourDirectionVector();
            var eight = joystickProducer.JoystickOutput.GetEightDirection();
            var eightVec = joystickProducer.JoystickOutput.GetEightDirectionVector();
            var horizontal = joystickProducer.JoystickOutput.GetHorizontalAxis();
            var vertical = joystickProducer.JoystickOutput.GetVerticalAxis();

            debugText.text =
                $"position: {raw} {pos} \nfour direction: {four} {fourVec} \neight direction: {eight} {eightVec} \nhorizontal only: {horizontal} \nvertical only: {vertical}";
        }
    }
}
