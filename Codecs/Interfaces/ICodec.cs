using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Media.Codec.Interfaces
{
    /// <summary>
    /// Represents a Codec's information and capabilities.
    /// </summary>
    public interface ICodec
    {
        /// <summary>
        /// Gets the Guid which uniquely identifies the codec.
        /// </summary>
        Guid Id { get; }

        /// <summary>
        /// Gets the string which corresponds to the name of the codec.
        /// </summary>
        string Name { get; }

        /// <summary>
        /// The types of media supported by the codec.
        /// </summary>
        MediaType MediaTypes { get; }

        /// <summary>
        /// Indicates if the Codec can encode data.
        /// </summary>
        bool CanEncode { get; }

        /// <summary>
        /// Indicates if the Codec can decode dta.
        /// </summary>
        bool CanDecode { get; }

        //(Try)CreateEncoder maybe better suited with signature (bool, out IEncoder) ...options
        IEncoder Encoder { get; }

        //(Try)CreateDecoder maybe better suitedwith signature (bool, out IDecoder) ...options
        IDecoder Decoder { get; }
    }
}
