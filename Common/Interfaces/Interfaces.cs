using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Media.Common.Interfaces
{
    /// <summary>
    /// 
    /// </summary>
    public interface IGeneric : Interface { }

    /// <summary>
    /// 
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public interface IGeneric<T> { }

    /// <summary>
    /// An interface which contains a <see cref="Class"/>
    /// </summary>
    public interface InterClass
    {
        Media.Common.Classes.Class Class { get; }
    }

    /// <summary>
    /// An interface which contains a <see cref="Struct"/>
    /// </summary>
    public interface InterStruct
    {
        Media.Common.Structures.Struct Struct { get; }
    }
}
