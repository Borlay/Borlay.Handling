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
        private readonly IResolver resolver;
        private readonly Type handlerType;
        private readonly MethodInfo method;
        private readonly PropertyInfo resultProperty;
        private readonly RoleAttribute[] classRoles;
        private readonly RoleAttribute[] methodRoles;
        private readonly ActionAttribute actionAttribute;

        public IActionMeta ActionMeta => actionAttribute;

        public MThreadHandler(IResolver resolver, Type handlerType, MethodInfo method, RoleAttribute[] classRoles, RoleAttribute[] methodRoles)
        {
            this.resolver = resolver;
            this.handlerType = handlerType;
            this.method = method;

            this.actionAttribute = method.GetCustomAttribute<ActionAttribute>() ?? throw new ArgumentNullException(nameof(actionAttribute));

            resultProperty = method.ReturnType.GetTypeInfo().GetProperty("Result");

            this.classRoles = classRoles;
            this.methodRoles = methodRoles;
        }

        

        public virtual async Task<object> HandleAsync(object request, CancellationToken cancellationToken)
        {
            return await HandleAsync(new Resolver(), request, cancellationToken);
        }

        public virtual async Task<object> HandleAsync(IResolver resolver, object request, CancellationToken cancellationToken)
        {
            if (request == null)
                throw new ArgumentNullException(nameof(request));

            var cResolver = new CombainedResolver(resolver, this.resolver);
            var argResolver = new Resolver(cResolver);
            argResolver.Register(request, true);
            argResolver.Register(cancellationToken);

            using (var session = argResolver.CreateSession())
            {
                if (classRoles.Length > 0 || methodRoles.Length > 0)
                {
                    if (!session.TryResolve<IRole>(out var role))
                        throw new UnauthorizedException(UnauthorizedReason.NoRoleProvider);

                    if (classRoles.Length > 0 && !classRoles.Any(ar => ar.Roles.All(r => role.Contains(r))))
                        throw new UnauthorizedException(UnauthorizedReason.ClassAccess);

                    if (methodRoles.Length > 0 && !methodRoles.Any(ar => ar.Roles.All(r => role.Contains(r))))
                        throw new UnauthorizedException(UnauthorizedReason.MethodAccess);
                }

                var instance = this.resolver.Resolve(handlerType);

                var arguments = method.GetParameters().Select(p => session.Resolve(p.ParameterType)).ToArray();

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
}
