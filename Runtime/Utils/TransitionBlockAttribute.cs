using System;
using System.Linq;
using RiseOn.Utils;

namespace RiseOn.Transitions {
    /// <summary>
    /// Lets the picker of a modifier or source field build generic blocks, and narrows it to blocks that can run on the field's value type:<br/>
    /// a type parameter marked <see cref="AxesAttribute"/> takes only structs with registered axes,<br/>
    /// and a block whose <see cref="RequiresOperationAttribute"/> has no implementation for the value type is grayed out.
    /// </summary>
    [AttributeUsage(AttributeTargets.Interface)]
    internal sealed class TransitionBlockAttribute : AllowGenericsAttribute {
        public override bool Accepts(Type parameter, Type argument) {
            return !parameter.IsDefined(typeof(AxesAttribute), false) || HasAxes(argument);
        }

        public override string GetDisabledReason(Type type) {
            if (type.IsGenericType) {
                var parameters = type.GetGenericTypeDefinition().GetGenericArguments();
                var arguments = type.GetGenericArguments();
                for (var i = 0; i < parameters.Length; i++) {
                    if (parameters[i].IsDefined(typeof(AxesAttribute), false) && !HasAxes(arguments[i])) return $"{arguments[i].Name} has no registered axes";
                }
            }

            var value = type.GetInterfaces()
                .Where(contract => contract.IsGenericType)
                .Where(contract => contract.GetGenericTypeDefinition() == typeof(ITransitionModifier<,>) || contract.GetGenericTypeDefinition() == typeof(ITransitionSource<,>))
                .Select(contract => contract.GetGenericArguments()[1])
                .FirstOrDefault();
            if (value == null) return null;

            foreach (RequiresOperationAttribute requirement in type.GetCustomAttributes(typeof(RequiresOperationAttribute), true)) {
                if (OperationRegistry.Resolve(requirement.Operation.MakeGenericType(value)) == null) return $"needs {requirement.Operation.Name[..requirement.Operation.Name.IndexOf('`')]}<{value.Name}>";
            }

            return null;
        }

        private static bool HasAxes(Type type) {
            return !type.ContainsGenericParameters && ValueAxes.GetNames(type).Count > 0;
        }
    }
}