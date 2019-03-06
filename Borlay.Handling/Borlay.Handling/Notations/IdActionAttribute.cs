using Borlay.Arrays;
using System;
using System.Collections.Generic;
using System.Text;

namespace Borlay.Handling.Notations
{
    public class IdActionAttribute : ActionAttribute
    {
        public int ActionId { get; }

        public IdActionAttribute(int action)
        {
            this.ActionId = action;
        }

        public override object GetActionId()
        {
            return ActionId;
        }
    }
}
