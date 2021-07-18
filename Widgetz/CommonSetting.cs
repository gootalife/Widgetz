using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Widgetz {
    public record CommonSetting {
        public string WidgetName { get; init; }
        public int PosX { get; set; }
        public int PosY { get; set; }
        public bool AutoBoot { get; set; }
    }
}
