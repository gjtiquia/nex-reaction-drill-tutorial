#nullable enable

using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using TMPro;
using UnityEngine.EventSystems;


namespace Nex.Essentials.Examples.SlashSignal
{
    public class SlashIndicator : UIBehaviour
    {
        [SerializeField] private SlashDetector slashDetector = null!;
        [SerializeField] private Graphic arrowHead = null!;
        [SerializeField] private Graphic arrowBody = null!;
        [SerializeField] private TextMeshProUGUI speedLabel = null!;
        private RectTransform RectTransform { get; set; } = null!;

        private Coroutine? resetCoroutine;

        protected override void Awake()
        {
            base.Awake();
            RectTransform = (RectTransform)transform;
        }


        protected override void Start()
        {
            base.Start();
            arrowHead.color = Color.clear;
            arrowBody.color = Color.clear;
            speedLabel.text = "";
            slashDetector.OnSlashDetected += HandleSlashDetected;
        }

        void HandleSlashDetected(Vector2 slashVector)
        {
            arrowHead.color = Color.red;
            arrowBody.color = Color.red;
            // Update the speed label
            var slashSpeed = slashVector.magnitude;
            speedLabel.text = $"{slashSpeed:F1} in/s";
            
            // Rotate the arrow to point in the slash direction
            var slashAngle = Mathf.Atan2(slashVector.y, slashVector.x) * Mathf.Rad2Deg;
            RectTransform.rotation = Quaternion.Euler(0, 0, slashAngle);

            // Restart the reset color coroutine
            if (resetCoroutine != null)
            {
                StopCoroutine(resetCoroutine);
            }
            resetCoroutine = StartCoroutine(ResetColorAfterDelay(slashDetector.slashCooldown));

        }

        private IEnumerator ResetColorAfterDelay(float delay)
        {
            yield return new WaitForSeconds(delay);
            arrowHead.color = Color.clear;
            arrowBody.color = Color.clear;
            speedLabel.text = "";
            resetCoroutine = null;
        }
    }
}