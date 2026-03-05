#nullable enable

using System;
using UnityEngine;

namespace Nex.Essentials
{
    [Obsolete("This script has been moved to avoid asmdef limitations. Please use Nex.Essentials.PoseSmoothEngine instead.", true)]
    public sealed class PoseSmoothEngineDeprecated
    {
        // Leave empty on purpose to overwrite the old PoseSmoothEngine that was placed inside Util/Smoothing
        // The new version is moved to Controller/PoseSmoothEngine.cs instead
    }
}
