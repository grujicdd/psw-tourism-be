using Explorer.BuildingBlocks.Core.Domain;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Explorer.Stakeholders.Core.Domain
{
    public class Interest : Entity
    {
        public string InterestName { get; set;}

        public Interest(string interestName)
        {
            InterestName = interestName;
        }

    }
}
