using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static 精密切割系统.Helpers.GTN.mc;
using static 精密切割系统.Helpers.GTN.mc_la;

namespace 精密切割系统.Helpers.GTN
{
    public class DiManager
    {
        private readonly short _core;
        private readonly int _bitsPerBank;

        private readonly Dictionary<int, DiPoint> _inputs = [];

        private CancellationTokenSource? _cts;

        public int PollInterval { get; set; } = 20;

        public event Action<DiPoint> StateChanged;

        public event Action<DiPoint> RisingEdge;

        public event Action<DiPoint> FallingEdge;

        public DiManager(short core = 1, int bitsPerBank = 16)
        {
            _core = core;
            _bitsPerBank = bitsPerBank;
        }

        public void Register(int globalIndex, string name)
        {
            int bankIndex = globalIndex / _bitsPerBank + 1;
            int bitIndex = globalIndex % _bitsPerBank;

            if (_inputs.ContainsKey(globalIndex))
                return;

            _inputs[globalIndex] = new DiPoint
            {
                GlobalIndex = globalIndex,
                BankIndex = bankIndex,
                BitIndex = bitIndex,
                Name = name,
                CurrentState = false,
                PreviousState = false,
                LastChangeTime = DateTime.MinValue
            };
        }

        public bool GetState(int globalIndex)
        {
            if (_inputs.TryGetValue(globalIndex, out var point))
                return point.CurrentState;

            return false;
        }

        public DiPoint? GetPoint(int globalIndex)
        {
            _inputs.TryGetValue(globalIndex, out var point);
            return point;
        }

        public bool TryGetPoint(int globalIndex, out DiPoint point)
        {
            if (_inputs.TryGetValue(globalIndex, out DiPoint? tempPoint))
            {
                point = tempPoint;
                return true;
            }
            point = new DiPoint();
            return false;
        }

        public void Start()
        {
            if (_cts != null)
                return;

            _cts = new CancellationTokenSource();

            Task.Run(async () =>
            {
                while (!_cts.Token.IsCancellationRequested)
                {
                    Poll();
                    await Task.Delay(PollInterval);
                }
            });
        }

        public void Stop()
        {
            _cts?.Cancel();
            _cts = null;
        }

        public void Poll()
        {
            if (_inputs.Count == 0)
                return;

            Dictionary<int, int> bankValues = new();

            foreach (var point in _inputs.Values)
            {
                if (!bankValues.ContainsKey(point.BankIndex))
                {
                    short rtn = GTN_GetExtDi(_core, (short)point.BankIndex, out int value);

                    if (rtn != 0)
                        continue;

                    bankValues[point.BankIndex] = value;
                }

                int bankValue = bankValues[point.BankIndex];

                bool newState = (bankValue & (1 << point.BitIndex)) != 0;

                if (newState == point.CurrentState)
                    continue;

                point.PreviousState = point.CurrentState;
                point.CurrentState = newState;
                point.LastChangeTime = DateTime.Now;

                StateChanged?.Invoke(point);

                if (point.RisingEdge)
                    RisingEdge?.Invoke(point);

                if (point.FallingEdge)
                    FallingEdge?.Invoke(point);
            }
        }
    }
}