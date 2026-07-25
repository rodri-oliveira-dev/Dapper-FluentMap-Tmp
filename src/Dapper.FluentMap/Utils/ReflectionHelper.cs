using System;
using System.Linq.Expressions;
using System.Reflection;

namespace Dapper.FluentMap.Utils
{
    /// <summary>
    /// Provides helper methods for reflection operations.
    /// </summary>
    public static class ReflectionHelper
    {
        /// <summary>
        /// Returns the <see cref="T:System.Reflection.MemberInfo"/> for the specified lamba expression.
        /// </summary>
        /// <param name="lambda">A lamba expression containing a MemberExpression.</param>
        /// <returns>A <see cref="MemberInfo"/> object for the member in the specified lambda expression.</returns>
        public static MemberInfo GetMemberInfo(LambdaExpression lambda)
        {
            if (lambda == null)
            {
                throw new ArgumentNullException(nameof(lambda));
            }

            Expression expr = lambda;
            while (true)
            {
                switch (expr.NodeType)
                {
                    case ExpressionType.Lambda:
                        expr = ((LambdaExpression)expr).Body;
                        break;

                    case ExpressionType.Convert:
                        expr = ((UnaryExpression)expr).Operand;
                        break;

                    case ExpressionType.MemberAccess:
                        var memberExpression = (MemberExpression)expr;
                        var member = memberExpression.Member;

                        if (member is PropertyInfo propertyInfo)
                        {
                            return propertyInfo;
                        }

                        throw new ArgumentException($"Expression '{lambda}' refers to member '{member.Name}', which is not a property.", nameof(lambda));

                    default:
                        throw new ArgumentException($"Expression '{lambda}' must resolve to a property.", nameof(lambda));
                }
            }
        }
    }
}
