using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace 精密切割系统.Model.cut
{
    public class ServicePauseResult
    {
        public ServicePauseResultType Type { get; set; }

        public CancellationToken? Token { get; set; }

        private ServicePauseResult()
        {
            Type = ServicePauseResultType.None;
            Token = null;
        }

        public static ServicePauseResult Continue(CancellationToken token)
        {
            return new ServicePauseResult { Type = ServicePauseResultType.Continue, Token = token };
        }

        public static ServicePauseResult ContinueAndResetCutY(CancellationToken token)
        {
            return new ServicePauseResult { Type = ServicePauseResultType.ContinueAndResetCutY, Token = token };
        }

        public static ServicePauseResult Stop
        {
            get
            {
                return new ServicePauseResult { Type = ServicePauseResultType.Stop };
            }
        }

        public static ServicePauseResult BladeScrap
        {
            get
            {
                return new ServicePauseResult { Type = ServicePauseResultType.BladeScrap };
            }
        }

        public enum ServicePauseResultType
        {
            None,
            Continue,
            ContinueAndResetCutY,
            Stop,
            BladeScrap
        }
    }
}
