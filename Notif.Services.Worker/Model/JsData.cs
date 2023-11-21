using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;

namespace DMS.Combank.Cons.Worker.Model
{
    [DataContract]
    public class JsData
    {
        [DataMember]
        public List<string> data { get; set; }
    }
}
