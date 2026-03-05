using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using TMPro;

namespace Nex.Essentials.Examples.PointerInput
{
    public class ExamplesDropdown : MonoBehaviour
    {
        [SerializeField] private TMP_Dropdown dropdown;

        [System.Serializable]
        public struct DropdownObjects
        {
            public string name; // for easier recognition in the UI
            public List<GameObject> objects;
        }

        [SerializeField] private List<DropdownObjects> dropdownObjects;


        private int previousValue;

        // Start is called before the first frame update
        private void Start()
        {
            if (dropdown == null) return;

            dropdown.onValueChanged.AddListener(delegate { DropdownValueChanged(dropdown); });
        }

        private void DropdownValueChanged(TMP_Dropdown changedDropdown)
        {
            var newValue = changedDropdown.value;

            // disable previous objects
            foreach (var obj in dropdownObjects[previousValue].objects.Where(obj => obj != null))
            {
                obj.SetActive(false);
            }
            
            // enable new objects
            foreach (var obj in dropdownObjects[newValue].objects.Where(obj => obj != null))
            {
                obj.SetActive(true);
            }

            previousValue = newValue;
        }
    }
}
