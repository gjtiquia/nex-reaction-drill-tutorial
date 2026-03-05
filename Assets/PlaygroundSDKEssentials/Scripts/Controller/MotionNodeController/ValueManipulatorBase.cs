#nullable enable

using System;
using UnityEngine;

namespace Nex.Essentials.MotionNode
{
    [Serializable]
    public abstract class ValueManipulatorBase
    {
        [HideInInspector] public bool enabled = true;

        [Tooltip("Optional description to explain this transformation")]
        public string description;
        protected ValueManipulatorBase()
        {
            // Default description based on the concrete class name.
            description = GetType().Name;
        }
    }
}
