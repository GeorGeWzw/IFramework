using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace IFramework.Domain
{
    public class Entity : IEntity
    {
        [JsonIgnore]
        public object DomainContext { get; set; }
    }
}
