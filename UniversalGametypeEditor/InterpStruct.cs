using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UniversalGametypeEditor
{
    public class InterpStruct
    {
        public string name { get; set; }
        public string blockname { get; set; }
        public List<s> blockparams { get; set; }

        public struct s
        {
            public string name { get; set; }
            public string type { get; set; }
            public string refe { get; set; }
        }

        // type -> ref
        // not sure why i did it this way, honestly could not think of a better method
    }
}
