//extern alias Object;
#region Copyright
/*
This file came from Managed Media Aggregation, You can always find the latest version @ https://net7mma.codeplex.com/
  
 Julius.Friedman@gmail.com / (SR. Software Engineer ASTI Transportation Inc. http://www.asti-trans.com)

Permission is hereby granted, free of charge, 
 * to any person obtaining a copy of this software and associated documentation files (the "Software"), 
 * to deal in the Software without restriction, 
 * including without limitation the rights to :
 * use, 
 * copy, 
 * modify, 
 * merge, 
 * publish, 
 * distribute, 
 * sublicense, 
 * and/or sell copies of the Software, 
 * and to permit persons to whom the Software is furnished to do so, subject to the following conditions:
 * 
 * 
 * JuliusFriedman@gmail.com should be contacted for further details.

The above copyright notice and this permission notice shall be included in all copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, 
 * FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. 
 * 
 * IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, 
 * DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, 
 * TORT OR OTHERWISE, 
 * ARISING FROM, 
 * OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
 * 
 * v//
 */
#endregion

namespace Media.Concepts.Classes.T
{
    #region Internals

    /// <summary>
    /// A class which own an <see cref="IntPtr"/> and is also an implementation of <see cref="Media.Concepts.Classes.I.IPtr"/>
    /// </summary>
    internal class T : Media.Concepts.Classes.I.IPtr
    {
        internal System.IntPtr Reference;

        System.IntPtr I.IPtr.IntPtr
        {
            get { return Reference; }
        }
    }

    /// <summary>
    /// The base class of some generic type
    /// </summary>
    /// <typeparam name="T">The element type</typeparam>
    internal abstract class GenericBase<T> : Media.Common.Classes.Class<T>, Media.Common.Interfaces.IGeneric<T>, Common.Interfaces.InterClass
    {
        /// <summary>
        /// A <see cref="GenericBase"/> which is constantly <see cref="null"/>
        /// </summary>
        public const GenericBase<T> Nil = null;

        /// <summary>
        /// The <see cref="System.Type"/> of <see cref="Element"/>
        /// </summary>
        public static readonly System.Type ElementType = typeof(T);

        /// <summary>
        /// The element
        /// </summary>
        public T Element { get; internal protected set; }

        /// <summary>
        /// `this`
        /// </summary>
        Common.Classes.Class Common.Interfaces.InterClass.Class
        {
            get { return this; }
        }
    }

    internal unsafe class TypeReference<Type> : T
        where Type : System.Type
    {
        internal TypeReference(Type type)
        {
            System.TypedReference typedRefeence = __makeref(type);

            Reference = (System.IntPtr)(&typedRefeence);
        }

        internal  TypeReference(string type)
        {
            System.TypedReference typedRefeence = __makeref(type);

            Reference = (System.IntPtr)(&typedRefeence);
        }
    }

    internal class TypeInformation
    {
        T Reference;

        public TypeInformation(T reference) { Reference = reference; }

        bool IsTypedInformation
        {
            get
            {
                return this is TypedInformation<System.Type>;
            }
        }

        bool IsTypeLoaded
        {
            get
            {
                unsafe
                {
                    return object.ReferenceEquals(__refvalue(*(System.TypedReference*)(Reference.Reference), System.Type), null).Equals(false);
                }
            }
        }

        bool IsStringType
        {
            get
            {
                unsafe
                {
                    return __reftype(*(System.TypedReference*)(Reference.Reference)).Equals(typeof(string));
                }
            }
        }

        bool TryGetString(out string value)
        {
            if (IsStringType.Equals(false))
            {
                value = null;

                return false;
            }

            unsafe
            {
                value = __refvalue( *(System.TypedReference*)(Reference.Reference),string);

                return true;
            }
        }

        bool TryGetType(out System.Type value)
        {
            if (IsStringType)
            {
                string typeName;

                if (TryGetString(out typeName).Equals(false))
                {
                    value = null;

                    return false;
                }

                value = System.Type.GetType(typeName);

                return true;
            }

            unsafe
            {
                value = __refvalue( *(System.TypedReference*)(Reference.Reference), System.Type);

                return true;
            }
        }
    }

    internal class TypedInformation<T> : TypeInformation
        where T : System.Type
    {
        TypeReference<T> TypeReference;

        public TypedInformation(T t)
            : base(new Classes.T.T()
            {
                Reference = Unsafe.AddressOf(ref t)
            })
        {

            TypeReference = new TypeReference<T>(t);
        }


        public TypedInformation(string type)
            : base(new Classes.T.T()
            {
                Reference = System.IntPtr.Zero
            })
        {
            TypeReference = new TypeReference<T>(type);
        }

    }

    #endregion

    /// <summary>
    /// An interface which defines a 'Constaint' and is also an <see cref="Common.Interfaces.Interface"/>
    /// </summary>
    public interface IConstraint : Common.Interfaces.Interface { }

    /// <summary>
    /// A class which is a <see cref="Common.Classes.ClassInterface"/> but also explicitly implements <see cref="IConstraint"/>
    /// </summary>
    public abstract class Constraint : Common.Classes.Abstraction, IConstraint { }

    /// <summary>
    /// A generic <see cref="Constraint"/> which is also an <see cref="Abstraction"/>
    /// </summary>
    /// <typeparam name="T">The constrained type</typeparam>
    /// <remarks>
    /// There is no polymorphic inheritance in C#, thus this class cannot also inherit Class or Class{T} directly but does so via <see cref="Abstraction"/>
    /// </remarks>
    public abstract class Constraint<T> : Constraint { }

    /// <summary>
    /// Can be used to ensure a type is assignable from another type.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public sealed class ConstrainedType<T> : Constraint<T>
    {
        /// <summary>
        /// 
        /// </summary>
        public System.Type Type { get; private set; }

        /// <summary>
        /// 
        /// </summary>

        public readonly System.Type ElementType = typeof(T);

        /// <summary>
        /// 
        /// </summary>
        /// <param name="type"></param>
        public ConstrainedType(System.Type type)
        {
            EnsureIsAssignableType(type);
        }

        //-- IConstraint?

        /// <summary>
        /// 
        /// </summary>
        /// <param name="type"></param>
        public void EnsureIsAssignableType(System.Type type)
        {
            if (ElementType.IsAssignableFrom(type).Equals(false) &&
               type.IsAssignableFrom(ElementType).Equals(false))
            {

                //Todo, delegate additional check here if desired.

                Common.TaggedExceptionExtensions.RaiseTaggedException(this, "Type Constaint Failure",
                    new Common.TaggedException<System.Type>(type, "Type is not assignable from constaint type."));
            }

            Type = type;
        }
    }

    /// <summary>
    /// Represents a class which provides the ability to manage <see cref="System.Type"/> instances
    /// </summary>
    /// <remarks>
    /// Based on some of the work here
    /// http://stackoverflow.com/questions/2969368/is-this-possible-c-sharp-collection-of-type-with-constrains-or-collection-of-g
    /// </remarks>
    public class TypeManager : Media.Common.SuppressedFinalizerDisposable
    {
        /// <summary>
        /// constantly <see cref="null"/>
        /// </summary>
        const System.Type NilType = null;

        /// <summary>
        /// Only accesible from this type or reflection.
        /// </summary>
        readonly System.Collections.Generic.Dictionary<System.Type, System.Tuple<System.Reflection.ConstructorInfo[], System.Reflection.ParameterInfo[][]>> m_SupportedTypes = new System.Collections.Generic.Dictionary<System.Type, System.Tuple<System.Reflection.ConstructorInfo[], System.Reflection.ParameterInfo[][]>>();

        /// <summary>
        /// The <see cref="System.Types"/> which are supported.
        /// </summary>
        public System.Collections.Generic.IEnumerable<System.Type> SupportedTypes { get { return m_SupportedTypes.Keys; } }

        public TypeManager(bool shouldDispose = true) : base(shouldDispose) { }

        void Probe(System.Type type, System.Reflection.BindingFlags bindingFlags = System.Reflection.BindingFlags.Public)
        {
            if (object.ReferenceEquals(type, NilType)) return;

            System.Exception any;

            System.Reflection.ConstructorInfo[] constructors = type.GetConstructors(bindingFlags);

            int register;

            if (Media.Common.Extensions.Array.ArrayExtensions.IsNullOrEmpty(constructors, out register)
                ||
                register <= 0) return;

            System.Reflection.ParameterInfo[][] parameters = new System.Reflection.ParameterInfo[register][];

            register = -1;

            foreach (System.Reflection.ConstructorInfo constructor in constructors)
            {
                //Todo, range check elimination
                parameters[++register] = constructor.GetParameters();
            }

            //There is no parameter info...
            if (register < 0) return;

            var probe = new System.Tuple<System.Reflection.ConstructorInfo[], System.Reflection.ParameterInfo[][]>(constructors, parameters);

            if (Media.Common.Extensions.Generic.Dictionary.DictionaryExtensions.TryAdd(m_SupportedTypes, ref type, ref probe, out any).Equals(false))
            {
                //The type was already probed
            }
        }

        /// <summary>
        /// Adds the parameterized type to be managed
        /// </summary>
        /// <typeparam name="T">The type to be managed</typeparam>
        /// <returns>True upon success, throws an exception upon failure.</returns>
        public bool AddSupportedType<T>() where T : Constraint<T> //Constraint, etc.
        {
            System.Type Type = NilType;

            try
            {
                Type = typeof(T);

                //Todo, verify some type constrains if required.

                //if (Type.IsAbstract) throw new System.InvalidOperationException("The parameterized type is abstract.");

                return true;
            }
            catch { throw; }
            finally { Probe(Type); }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="supportedTypeIndex"></param>
        /// <param name="version"></param>
        /// <param name="parameters"></param>
        /// <returns></returns>
        /// <remarks>
        /// If we do not know the type, but somehow know an index to type and the version and the parameters...
        /// </remarks>
        public object Create(ref int supportedTypeIndex, ref int version, object[] parameters)
        {
            //If a positive index and version was provided then assume the call is for this implementation
            if (supportedTypeIndex >= 0 && version >= 0)
            {
                int register = -1;

                System.Collections.Generic.KeyValuePair<System.Type, System.Tuple<System.Reflection.ConstructorInfo[], System.Reflection.ParameterInfo[][]>> reference;

                if (register < 0) return NilType;

                using (var enumerator = m_SupportedTypes.GetEnumerator())
                {
                    while (enumerator.MoveNext())
                    {
                        reference = enumerator.Current;

                        if ((++register).Equals(supportedTypeIndex))
                        {
                            if (Media.Common.Extensions.Array.ArrayExtensions.IsNullOrEmpty(reference.Value.Item2, out register).Equals(false) ||
                                version > register) break;

                            return reference.Value.Item1[version].Invoke(parameters);
                        }
                    }
                }
            }

            throw new System.NotImplementedException();

            //Could be stored in * < 0

            //System.Type reference = System.Type.GetTypeFromHandle(new System.RuntimeTypeHandle(new System.IntPtr(supportedTypeIndex)));
        }

        // if we know instance type\subtype (eg interface) and know an index
        public T Create<T>(ref int supportedTypeIndex, ref int version, object[] paramerters)
        {
            T typed = default(T);

            object untyped = Create(ref supportedTypeIndex, ref version, paramerters);

            if ((untyped is T).Equals(false)) Common.TaggedExceptionExtensions.RaiseTaggedException(this as IConstraint, "IConstraint Violation, allocation available in InnerException.Tag.", new Common.TaggedException<object>(untyped));

            //typed = Unsafe.ReinterpretCast<object, T>(untyped);

            //typed = CommonIntermediateLanguage.As<object, T>(untyped);

            typed = (T)(untyped);

            return typed;
        }
    }
}
