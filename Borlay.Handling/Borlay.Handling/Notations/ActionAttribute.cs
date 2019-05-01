using Borlay.Arrays;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;

namespace Borlay.Handling.Notations
{
    [AttributeUsage(AttributeTargets.Method)]
    public class ActionAttribute : Attribute, IGetId
    {
        public string MethodName { get; }

        public ActionAttribute([CallerMemberName] string methodName = "")
        {
            if (string.IsNullOrWhiteSpace(methodName))
                throw new ArgumentNullException(nameof(methodName));

            this.MethodName = methodName.ToLower();
        }

        public ActionAttribute(int id)
        {
            this.MethodName = id.ToString();
        }

        public string GetId()
        {
            return MethodName; //MethodName;
        }
    }

    public interface IGetId
    {
        string GetId();
    }
}
