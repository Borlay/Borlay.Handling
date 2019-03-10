using System;
using System.Collections.Generic;
using System.Text;

namespace Borlay.Handling.Notations
{
    public class IdScopeAttribute : ScopeAttribute
    {
        public int ActionId { get; }

        public IdScopeAttribute(int action)
        {
            this.ActionId = action;
        }

        public override object GetScopeId()
        {
            return ActionId;
        }
    }
}
