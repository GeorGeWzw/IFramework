using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IFramework.Infrastructure
{
    public static class Parser
    {
        public static int TryParse(object s, int defaultValue)
        {
            int.TryParse(s?.ToString(), out defaultValue);
            return defaultValue;
        }

        public static double TryParse(object s, double defaultValue)
        {
            double.TryParse(s?.ToString(), out defaultValue);
            return defaultValue;
        }

        public static decimal TryParse(object s, decimal defaultValue)
        {
            decimal.TryParse(s?.ToString(), out defaultValue);
            return defaultValue;
        }

        public static float TryParse(object s, float defaultValue)
        {
            float.TryParse(s?.ToString(), out defaultValue);
            return defaultValue;
        }

        public static Guid TryParse(object s, Guid defaultValue)
        {
            Guid.TryParse(s?.ToString(), out defaultValue);
            return defaultValue;
        }
    }
}
