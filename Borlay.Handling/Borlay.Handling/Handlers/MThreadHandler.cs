using Borlay.Handling.Notations;
using Borlay.Injection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Borlay.Handling
{
    public class MThreadHandler : IHandler
    {
        private readonly Type handlerType;
        private readonly MethodInfo method;
        private readonly PropertyInfo resultProperty;
        private readonly RoleAttribute[] classRoles;
        private readonly RoleAttribute[] methodRoles;
        private readonly ActionAttribute actionAttribute;
        //private readonly Type[] parameterTypes;


        public IActionMeta ActionMeta => actionAttribute;
        //public Type[] ParameterTypes => parameterTypes;

        public MThreadHandler(Type handlerType, MethodInfo method, RoleAttribute[] classRoles, RoleAttribute[] methodRoles)
        {
            this.handlerType = handlerType;
            this.method = method;
            //this.parameterTypes = parameterTypes;

            this.actionAttribute = method.GetCustomAttribute<ActionAttribute>() ?? throw new ArgumentNullException(nameof(actionAttribute));

            resultProperty = method.ReturnType.GetTypeInfo().GetProperty("Result");

            this.classRoles = classRoles;
            this.methodRoles = methodRoles;
        }



        //public virtual async Task<object> HandleAsync(object[] requests, CancellationToken cancellationToken)
        //{
        //    return await HandleAsync(new Resolver(), requests, cancellationToken);
        //}

        public virtual async Task<object> HandleAsync(IResolverSession session, object[] requests, CancellationToken cancellationToken)
        {
            if (requests == null)
                throw new ArgumentNullException(nameof(requests));

            if (classRoles.Length > 0 || methodRoles.Length > 0)
            {
                if (!session.TryResolve<IRole>(out var role))
                    throw new UnauthorizedException(UnauthorizedReason.NoRoleProvider);

                if (classRoles.Length > 0 && !classRoles.All(ar => ar.AnyOfRoles.Any(r => role.Contains(r))))
                    throw new UnauthorizedException(UnauthorizedReason.ClassAccess);

                if (methodRoles.Length > 0 && !methodRoles.All(ar => ar.AnyOfRoles.Any(r => role.Contains(r))))
                    throw new UnauthorizedException(UnauthorizedReason.MethodAccess);
            }

            var instance = session.Resolve(handlerType);

            var parameters = method.GetParameters();
            var arguments = new object[parameters.Length];
            var argumentIndex = 0;

            for (int i = 0; i < arguments.Length; i++)
            {
                if (session.TryResolve(parameters[i].ParameterType, out var arg))
                    arguments[i] = arg;
                else
                {
                    if (parameters[i].ParameterType.GetTypeInfo().IsAssignableFrom(typeof(CancellationToken)))
                    {
                        arguments[i] = cancellationToken;
                    }
                    else if (parameters[i].ParameterType.GetTypeInfo().IsAssignableFrom(typeof(IResolverSession)))
                    {
                        arguments[i] = session;
                    }
                    else
                    {
                        if (requests.Length > argumentIndex)
                        {
                            var req = requests[argumentIndex];
                            if (parameters[i].ParameterType.GetTypeInfo().IsAssignableFrom(req.GetType()))
                            {
                                arguments[i] = req;
                                argumentIndex++;
                                continue;
                            }
                        }
                        arguments[i] = null;
                    }
                }
            }

            var result = method.Invoke(instance, arguments);
            if (result == null) return null;

            if (result is Task)
            {
                var task = (Task)result;
                await task;

                if (resultProperty != null)
                    return resultProperty.GetValue(result);

                return null;
            }

            return result;
        }
    }
}
