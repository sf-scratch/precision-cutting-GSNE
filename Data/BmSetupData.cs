using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using 精密切割系统.Helpers;

namespace 精密切割系统.Data
{
    internal class BmSetupData : JsonBase
    {
        private static readonly Lazy<BmSetupData> _lazy = new(() => new BmSetupData());

        public static BmSetupData Instance => _lazy.Value;

        private BmSetupData() : base("bmSetup.json")
        {
        }

        public int SpindleRev
        {
            get => GetValue<int>();
            set => UpdateAppSettings(value);
        }

        public int HeightMeasureTimes
        {
            get => GetValue<int>();
            set => UpdateAppSettings(value);
        }

        public bool IsAutomHeightMeasureBeforeCutting
        {
            get => GetValue();
            set => UpdateAppSettings(value);
        }

        public float ThetaMovementAngle
        {
            get => GetValue<float>();
            set => UpdateAppSettings(value);
        }

        public float ThetaStartingToMovePosition
        {
            get => GetValue<float>();
            set => UpdateAppSettings(value);
        }

        public float ThetaEndingToMovePosition
        {
            get => GetValue<float>();
            set => UpdateAppSettings(value);
        }

        public float ThetaCurrentLocation
        {
            get => GetValue<float>();
            set => UpdateAppSettings(value);
        }
    }
}