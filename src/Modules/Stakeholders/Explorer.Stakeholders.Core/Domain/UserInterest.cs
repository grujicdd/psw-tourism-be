using Explorer.BuildingBlocks.Core.Domain;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Explorer.Stakeholders.Core.Domain
{
    public class UserInterest : Entity
    {
        public long UserId { get; set; }
        public long InterestId {  get; set; }

        public UserInterest(long userId, long interestId)
        {
            UserId = userId;
            InterestId = interestId;
        }
    }
}
