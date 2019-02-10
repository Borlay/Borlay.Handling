using System;
using System.Collections.Generic;
using System.Text;

namespace Borlay.Handling.Notations
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct | AttributeTargets.Parameter)]
    public class ArgumentAttribute : Attribute
    {
    }
}
