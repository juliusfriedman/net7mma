using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System;

#region References

//http://blog.functionalfun.net/2009/10/getting-methodinfo-of-generic-method.html
//http://snipplr.com/view/29683/converting-methodinfo-into-a-delegate-instance-to-improve-performance/

#endregion

namespace Media.Common.Extensions.ExpressionExtensions
{
    /// <summary>
    /// Provides useful methods for working with <see cref="Expression"/>
    /// </summary>
    public static class SymbolExtensions
    {
        #region Support

        static Func<T, object, object> MagicMethod<T>(MethodInfo method) where T : class
        {
            // First fetch the generic form
            MethodInfo genericHelper = typeof(SymbolExtensions).GetMethod("MagicMethodHelper",
                BindingFlags.Static | BindingFlags.NonPublic);

            // Now supply the type arguments
            MethodInfo constructedHelper = genericHelper.MakeGenericMethod
                (typeof(T), method.GetParameters()[0].ParameterType, method.ReturnType);

            // Now call it. The null argument is because it's a static method.
            object ret = constructedHelper.Invoke(null, new object[] { method });

            // Cast the result to the right kind of delegate and return it
            return (Func<T, object, object>)ret;
        }

        //<= 2.0

        //static Func<TTarget, object, object> MagicMethodHelper<TTarget, TParam, TReturn>(MethodInfo method)
        //    where TTarget : class
        //{
        //    // Convert the slow MethodInfo into a fast, strongly typed, open delegate
        //    Func<TTarget, TParam, TReturn> func = (Func<TTarget, TParam, TReturn>)System.Delegate.CreateDelegate
        //        (typeof(Func<TTarget, TParam, TReturn>), method);

        //    // Now create a more weakly typed delegate which will call the strongly typed one
        //    Func<TTarget, object, object> ret = (TTarget target, object param) => func(target, (TParam)param);
        //    return ret;
        //}

        //static Func<T, object, object> MagicMethod<T>(MethodInfo method)
        //{
        //    System.Reflection.ParameterInfo parameter = method.GetParameters().Single();
        //    ParameterExpression instance = Expression.Parameter(typeof(T), "instance");
        //    ParameterExpression argument = Expression.Parameter(typeof(object), "argument");
        //    MethodCallExpression methodCall = Expression.Call(instance, method,
        //        Expression.Convert(argument, parameter.ParameterType));

        //    return Expression.Lambda<Func<T, object, object>>(Expression.Convert(methodCall, typeof(object)), instance, argument).Compile();
        //}

        #endregion

        //Dependency ....
        static readonly Type TypedConstantExpressionType;// = Type.GetType("System.Linq.Expressions.TypedConstantExpression, System.Core, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089");

        static readonly Type InstanceMethodCallExpressionNType;

        static readonly PropertyInfo TypedConstantExpressionValueProperty;

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.Synchronized | System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        static SymbolExtensions()
        {
            //Ensure not already ran.
            if (false.Equals(TypedConstantExpressionType == null)
                | false.Equals(InstanceMethodCallExpressionNType == null)
                | false.Equals(TypedConstantExpressionValueProperty == null)) return;

            //For example
            //Expression<Action> ConstantExpression = () => Common.Binary.Nihil.Equals(null);

            //Rather than take a reference to the assembly achieve the same thing by using reflection a single time on what should be equal to a TypedConstantExpression.

            Expression<Action> TypedConstantExpression = () => typeof(Nullable<>).GetType();

            MethodCallExpression methodCallExpression = TypedConstantExpression.Body as MethodCallExpression;

            //InstanceMethodCallExpressionN
            InstanceMethodCallExpressionNType = methodCallExpression.GetType();

            //The body as a MethodCall reveals the Object which is really a TypedConstantExpression
            methodCallExpression = TypedConstantExpression.Body as MethodCallExpression;

            //Store the Type of that object.
            TypedConstantExpressionType = methodCallExpression.Object.GetType();

            //Store the type
            TypedConstantExpressionValueProperty = IntrospectionExtensions.GetTypeInfo(TypedConstantExpressionType).GetProperty("Value");

            TypedConstantExpression = null;

            methodCallExpression = null;
        }

        /// <summary>
        /// Creates a MethodCallExpression which can be used to refer to the MethodInfo given
        /// </summary>
        /// <param name="method"></param>
        /// <returns></returns>
        public static MethodCallExpression CreateMethod(MethodInfo method)
        {
            if (method == null)
            {
                throw new ArgumentNullException("method");
            }

            //Return the MethodCallExpression (parameters are included)
            return Expression.Call(null, method, method.GetParameters()
                                   .Select(p => Expression.Parameter(p.ParameterType, p.Name))
                                   .ToArray());

            //Called like so
            //return Expression.Lambda(callExpression, parameterExpressions).Compile();
        }

        /// <summary>
        /// Given a lambda expression that expressed a new object, returns the <see cref="System.Reflection.TypeInfo"/> of what type was expected to be allocated
        /// </summary>
        /// <param name="expression"></param>
        /// <returns></returns>
        public static TypeInfo GetTypeInfo(Expression<Action> expression) //Expression<Action> allows the syntax () => where Expression would require a Delgate.
        {
            Expression body = expression.Body;

            if (body is NewExpression)
            {
                NewExpression newExpression = expression.Body as NewExpression;

                return IntrospectionExtensions.GetTypeInfo((expression.Body as NewExpression).Constructor.DeclaringType);
            }
            else if (body is MemberExpression)
            {
                MemberExpression memberExpression = body as MemberExpression;

                return IntrospectionExtensions.GetTypeInfo(memberExpression.Member.DeclaringType);
            }
            else if (body is MethodCallExpression)
            {
                MethodCallExpression methodCallExpression = expression.Body as MethodCallExpression;

                if (methodCallExpression.Object is MemberExpression)
                {
                    return IntrospectionExtensions.GetTypeInfo((methodCallExpression.Object as MemberExpression).Member.DeclaringType);
                }
                else if (methodCallExpression.Object is ConstantExpression)
                {
                    return IntrospectionExtensions.GetTypeInfo((Type)(methodCallExpression.Object as ConstantExpression).Value);
                }

                //Actually a RuntimeType from a TypedConstantExpression...
                return IntrospectionExtensions.GetTypeInfo((Type)TypedConstantExpressionValueProperty.GetMethod.Invoke(methodCallExpression.Object, null));
            }

            throw new System.NotSupportedException("Please create an issue for your use case.");
        }

        /// <summary>
        /// Given a lambda expression that expressed a property, returns the <see cref="System.Reflection.MethodInfo"/> of the get method of the Property was expressed.
        /// </summary>
        /// <param name="expression">The expression.</param>
        /// <returns></returns>
        public static MethodInfo LoadGetter(Expression<Action> expression)
        {
            return GetPropertyInfo((LambdaExpression)expression).GetMethod;
        }

        /// <summary>
        /// Given a lambda expression that expressed a property, returns the <see cref="System.Reflection.MethodInfo"/> of the set method of the Property was expressed.
        /// </summary>
        /// <param name="expression">The expression.</param>
        /// <returns></returns>
        public static MethodInfo LoadSetter(Expression<Action> expression)
        {
            return GetPropertyInfo((LambdaExpression)expression).SetMethod;
        }

        /// <summary>
        /// Given a lambda expression that expressed a property, returns the <see cref="System.Reflection.PropertyInfo"/> of what member was expressed.
        /// </summary>
        /// <param name="expression">The expression.</param>
        /// <returns></returns>
        public static PropertyInfo GetPropertyInfo(LambdaExpression expression)
        {
            return (PropertyInfo)GetMemberInfo(expression);
        }

        /// <summary>
        /// Given a lambda expression that expressed a field, returns the <see cref="System.Reflection.Field"/> of what member was expressed.
        /// </summary>
        /// <param name="expression">The expression.</param>
        /// <returns></returns>
        public static FieldInfo GetFieldInfo(LambdaExpression expression)
        {
            return (FieldInfo)GetMemberInfo(expression);
        }

        /// <summary>
        /// Given a lambda expression that expressed a member, returns the <see cref="System.Reflection.MemberInfo"/>  of what member was expressed.
        /// </summary>
        /// <param name="expression">The expression.</param>
        /// <returns></returns>
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        public static MemberInfo GetMemberInfo(LambdaExpression expression)
        {
            MemberExpression outermostExpression = ((expression.Body as MethodCallExpression).Object as MemberExpression);

            if (expression == null) throw new ArgumentException("Invalid Expression. Should be a MemberExpression");

            return outermostExpression.Member;
        }

        /// <summary>
        /// Given a lambda expression that calls a method, returns the method info.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="expression">The expression.</param>
        /// <returns></returns>
        public static MethodInfo GetMethodInfo(Expression<Action> expression)
        {
            return GetMethodInfo((LambdaExpression)expression);
        }

        /// <summary>
        /// Given a lambda expression that calls a method, returns the method info.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="expression">The expression.</param>
        /// <returns></returns>
        public static MethodInfo GetMethodInfo<T>(Expression<Action<T>> expression)
        {
            return GetMethodInfo((LambdaExpression)expression);
        }

        /// <summary>
        /// Given a lambda expression that calls a method, returns the method info.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="expression">The expression.</param>
        /// <returns></returns>
        public static MethodInfo GetMethodInfo<T, TResult>(Expression<Func<T, TResult>> expression)
        {
            return GetMethodInfo((LambdaExpression)expression);
        }

        /// <summary>
        /// Given a lambda expression that calls a method, returns the method info.
        /// </summary>
        /// <param name="expression">The expression.</param>
        /// <returns></returns>
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        public static MethodInfo GetMethodInfo(LambdaExpression expression)
        {
            MethodCallExpression outermostExpression = expression.Body as MethodCallExpression;

            if (outermostExpression == null) throw new ArgumentException("Invalid Expression. Expression should consist of a Method call only.");

            return outermostExpression.Method;
        }
    }
}

namespace Media.UnitTests
{
    /// <summary>
    /// Provides tests which ensure the logic of the GoodbyeReport class is correct
    /// </summary>
    internal class SymbolExtensionsUnitTests
    {

        #region Nested Types (For testing)

        public class MyTestClass
        {
            string m_Test;

            public string Test
            {
                get { return m_Test; }
                set { m_Test = value; }
            }
        }

        public class MyTestClass<T> : MyTestClass
        {
            T Backing;

            public T Property
            {
                [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
                get { return Backing; }
                [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
                set { Backing = value; }
            }

            public T AnotherProperty
            {
                [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
                get;
                [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
                set;
            }

            [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoOptimization)]
            public T Method() { return default(T); }
        }

        #endregion

        public void TestExpressionExtensions()
        {
            MyTestClass testClass = new MyTestClass();

            //Todo ... show how to allow to TypedReference use inside the body
            System.TypedReference member = __makeref(testClass.Test);

            System.Type type = typeof(System.Reflection.PropertyInfo);

            System.Reflection.MethodInfo setter = Media.Common.Extensions.ExpressionExtensions.SymbolExtensions.LoadSetter(() => testClass.Test.Equals(null));

            System.Console.WriteLine("setter Name: " + setter.Name);

            System.Console.WriteLine(".Test => " + testClass.Test);

            setter.Invoke(testClass, new[] { "Test" });

            if (false == (string.Compare(testClass.Test, "Test") == 0)) throw new System.Exception("Did not invoke setter");

            System.Console.WriteLine(".Test => " + testClass.Test);

            testClass = new MyTestClass<int>();

            MyTestClass<int> casted = testClass as MyTestClass<int>;

            System.Console.WriteLine("<int>.Test => " + testClass.Test);

            setter.Invoke(testClass, new[] { "Test" });

            if (false == (string.Compare(testClass.Test, "Test") == 0)) throw new System.Exception("Did not invoke setter");

            System.Console.WriteLine(".Test => " + testClass.Test);

            System.Console.WriteLine("<int>.Test => " + ((MyTestClass<int>)testClass).Test);

            System.Console.WriteLine("<int>.Property => " + ((MyTestClass<int>)testClass).Property);

            setter = Media.Common.Extensions.ExpressionExtensions.SymbolExtensions.LoadSetter(() => ((MyTestClass<int>)testClass).Property.Equals(null));

            System.Console.WriteLine("setter Name: " + setter.Name);

            setter.Invoke(testClass, new object[] { 1 });

            if (false == (casted.Property == 1)) throw new System.Exception("Did not invoke setter");

            System.Console.WriteLine("<int>.Property => " + ((MyTestClass<int>)testClass).Property);

            System.Console.WriteLine("<int>.Property => " + casted.Property);

            setter = Media.Common.Extensions.ExpressionExtensions.SymbolExtensions.LoadSetter(() => casted.AnotherProperty.Equals(null));

            System.Console.WriteLine("<int>.AnotherProperty => " + ((MyTestClass<int>)testClass).AnotherProperty);

            System.Console.WriteLine("<int>.AnotherProperty => " + casted.AnotherProperty);

            setter.Invoke(testClass, new object[] { 2 });

            if (false == (casted.AnotherProperty == 2)) throw new System.Exception("Did not invoke setter");

            System.Console.WriteLine("<int>.AnotherProperty => " + ((MyTestClass<int>)testClass).AnotherProperty);

            System.Console.WriteLine("<int>.AnotherProperty => " + casted.AnotherProperty);

            System.Reflection.TypeInfo typeInfo = Media.Common.Extensions.ExpressionExtensions.SymbolExtensions.GetTypeInfo(() => new MyTestClass<int>());

            if (typeInfo.GetGenericArguments()[0] != typeof(int)) throw new System.Exception("Not correct type");

            System.Console.WriteLine("TypeInfo.Name " + typeInfo.Name);

            System.Console.WriteLine("TypeInfo.MetadataToken " + typeInfo.MetadataToken);

            if (typeInfo.GetGenericArguments()[0] != typeof(int)) throw new System.Exception("Not correct type");

            typeInfo = Media.Common.Extensions.ExpressionExtensions.SymbolExtensions.GetTypeInfo(() => (typeof(MyTestClass<int>)).GetType());

            if (typeInfo.GetGenericArguments()[0] != typeof(int)) throw new System.Exception("Not correct type");

            System.Type unboundedType = typeof(MyTestClass<>);

            typeInfo = Media.Common.Extensions.ExpressionExtensions.SymbolExtensions.GetTypeInfo(() => (unboundedType).GetType());

            System.Console.WriteLine("TypeInfo.Name " + typeInfo.Name);

            System.Console.WriteLine("TypeInfo.MetadataToken " + typeInfo.MetadataToken);

            System.Reflection.MethodInfo methodInfo = Media.Common.Extensions.ExpressionExtensions.SymbolExtensions.GetMethodInfo(() => ((MyTestClass<int>)null).Method());

            if (false.Equals(methodInfo.DeclaringType.Equals(typeof(MyTestClass<int>)))) throw new System.Exception("Not correct type");

            System.Console.WriteLine("GetMethodInfo.Name " + methodInfo.Name);

            System.Console.WriteLine("GetMethodInfo.MetadataToken " + methodInfo.MetadataToken);
        }
    }
}