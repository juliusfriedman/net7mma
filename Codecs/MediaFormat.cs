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

namespace Media.Codec
{
    /// <summary>
    /// A representation of information which is relevent to a format
    /// </summary>
    public class MediaFormat
    {
        #region Statics

        /// <summary>
        /// Creates a MediaFormat which is the reverse of another MediaFormat.
        /// </summary>
        /// <param name="other"></param>
        /// <returns></returns>
        public static MediaFormat Reverse(MediaFormat other)
        {
            return new MediaFormat(other.MediaType, other.ByteOrder, other.DataLayout, System.Linq.Enumerable.Reverse<MediaComponent>(other.Components));
        }

        /// <summary>
        /// Creates a MediaFormat which is the same as the other but has a planar layout
        /// </summary>
        /// <param name="other"></param>
        /// <returns></returns>
        public static MediaFormat Planar(MediaFormat other)
        {
            if (other.DataLayout == DataLayout.Planar) return other;

            return new MediaFormat(other.MediaType, other.ByteOrder, DataLayout.Planar, other.Components);
        }

        /// <summary>
        /// Creates a MediaFormat which is the same as the other but has a Packed layout
        /// </summary>
        /// <param name="other"></param>
        /// <returns></returns>
        public static MediaFormat Packed(MediaFormat other)
        {
            if (other.DataLayout == DataLayout.Packed) return other;

            return new MediaFormat(other.MediaType, other.ByteOrder, DataLayout.Packed, other.Components);
        }

        /// <summary>
        /// Creates a MediaFormat which is the same as the other but has a SemiPlanar layout
        /// </summary>
        /// <param name="other"></param>
        /// <returns></returns>
        public static MediaFormat SemiPlanar(MediaFormat other)
        {
            if (other.DataLayout == DataLayout.SemiPlanar) return other;

            return new MediaFormat(other.MediaType, other.ByteOrder, DataLayout.SemiPlanar, other.Components);
        }

        #endregion

        #region Fields

        /// <summary>
        /// The type of media in which the component is used.
        /// </summary>
        public readonly MediaType MediaType;

        /// <summary>
        /// The size in bits of all the components in the format.
        /// </summary>
        public readonly int Size;

        /// <summary>
        /// The array of components in the format which also represents the component order.
        /// </summary>
        public readonly MediaComponent[] Components;

        /// <summary>
        /// The byte order of the data within the format.
        /// </summary>
        public readonly Common.Binary.ByteOrder ByteOrder;

        /// <summary>
        /// The layout of the data within the format.
        /// </summary>
        public readonly DataLayout DataLayout;

        #endregion

        #region Constructor

        /// <summary>
        /// Creates a format in which there are equal amount of bits per component.
        /// </summary>
        /// <param name="mediaType"></param>
        /// <param name="components"></param>
        /// <param name="bitsPerComponent"></param>
        /// <param name="shouldDispose"></param>
        public MediaFormat(MediaType mediaType, Common.Binary.ByteOrder byteOrder, DataLayout dataLayout, int components, int bitsPerComponent, byte[] componentIds)
        {
            //Assign the media type
            MediaType = mediaType;

            if (byteOrder == Common.Binary.ByteOrder.Unknown) throw new System.ArgumentException("byteOrder", "Cannot be Unknown");
            ByteOrder = byteOrder;

            //Validate the datalayout
            if (dataLayout == Media.Codec.DataLayout.Unknown) throw new System.ArgumentException("dataLayout", "Cannot be Unknown");
            DataLayout = dataLayout;

            //Validate the amount of components
            if (components < 1) throw new System.ArgumentException("components", "Must be greater than 0.");

            //Create the array
            Components = new MediaComponent[components];

            long length;

            //Validate the length of the id array
            if (Common.Extensions.Array.ArrayExtensions.IsNullOrEmpty(componentIds, out length) || length < components) throw new System.ArgumentException("componentIds", "Must have the amount of elements indicated by 'components'");

            //Calculate the size in bits of all components
            Size = bitsPerComponent * components;

            //Creates each component
            for (int i = 0; i < components; ++i) Components[i] = new MediaComponent( componentIds != null ? componentIds[i] : (byte)i, bitsPerComponent);
        }

        public MediaFormat(MediaType mediaType, Common.Binary.ByteOrder byteOrder, DataLayout dataLayout, int components, int[] componentSizes, byte[] componentIds)
        {
            //Assign the media type
            MediaType = mediaType;

            if (byteOrder == Common.Binary.ByteOrder.Unknown) throw new System.ArgumentException("byteOrder", "Cannot be Unknown");
            ByteOrder = byteOrder;

            //Validate the datalayout
            if (dataLayout == Media.Codec.DataLayout.Unknown) throw new System.ArgumentException("dataLayout", "Cannot be Unknown");
            DataLayout = dataLayout;

            //Validate the amount of components
            if (components < 1) throw new System.ArgumentException("components", "Must be greater than 0.");

            //Create the array
            Components = new MediaComponent[components];

            long length;

            //Validate the sizes array
            if (Common.Extensions.Array.ArrayExtensions.IsNullOrEmpty(componentSizes, out length) || length < components) throw new System.ArgumentException("componentSizes", "Must have the amount of elements indicated by 'components'");

            //Validate the length of the id array
            if (Common.Extensions.Array.ArrayExtensions.IsNullOrEmpty(componentIds, out length) || length < components) throw new System.ArgumentException("componentIds", "Must have the amount of elements indicated by 'components'");

            //Creates each component
            for (int i = 0; i < components; ++i)
            {
                length = componentSizes[i];

                int ilen = (int)length;

                Components[i] = new MediaComponent(componentIds[i], ilen);

                Size += ilen;
            }
        }

        public MediaFormat(MediaType mediaType, Common.Binary.ByteOrder byteOrder, DataLayout dataLayout, System.Collections.Generic.IEnumerable<MediaComponent> components)
        {
            //Assign the media type
            MediaType = mediaType;

            if (byteOrder == Common.Binary.ByteOrder.Unknown) throw new System.ArgumentException("byteOrder", "Cannot be Unknown");
            ByteOrder = byteOrder;

            //Validate the dataLayout
            if (dataLayout == Media.Codec.DataLayout.Unknown) throw new System.ArgumentException("dataLayout", "Cannot be Unknown");
            DataLayout = dataLayout;

            if (components == null) throw new System.ArgumentNullException("components");

            //Assign the components
            Components = System.Linq.Enumerable.ToArray<MediaComponent>(components);

            //Validate the amount of components
            if (Components.Length < 1) throw new System.ArgumentException("components", "Must be greater than 0.");

            //Calulcate the size
            foreach (MediaComponent mc in Components) Size += mc.Size;
        }

        public MediaFormat(MediaFormat other, Common.Binary.ByteOrder byteOrder, DataLayout dataLayout, params MediaComponent[] additionalComponents)
            :this(other.MediaType, byteOrder, dataLayout, System.Linq.Enumerable.Concat(other.Components, additionalComponents ?? System.Linq.Enumerable.Empty<MediaComponent>()))
        {
            
        }

        public MediaFormat(MediaFormat other)
            : this(other.MediaType, other.ByteOrder, other.DataLayout, other.Components)
        {

        }

        #endregion

        #region Properties

        /// <summary>
        /// Indicates if there are an even amount of bits in the format.
        /// </summary>
        public bool IsAligned { get { return Size % Common.Binary.BitsPerByte == 0; } }

        /// <summary>
        /// Gets the component at the given index.
        /// </summary>
        /// <param name="index">The index</param>
        /// <returns>The MediaComponent at the given index.</returns>
        public MediaComponent this[int index]
        {
            get { return Components[index]; }
        }

        /// <summary>
        /// Calculates the length in bytes of all components.
        /// </summary>
        public int Length { get { return Common.Binary.BitsToBytes(Size); } }

        /// <summary>
        /// Indicates if any components appear more than once in a data line of the format.
        /// </summary>
        public bool IsInterleaved
        {
            get
            {
                System.Collections.Generic.HashSet<byte> idSet = new System.Collections.Generic.HashSet<byte>();

                //Iterate each component
                for (int i = 0; i < Components.Length; ++i)
                {
                    //If the component's id was already contained then the format is interleaved
                    if (false == idSet.Add(Components[i].Id)) return true;
                }

                //Each component appears only once, the format is not interleaved
                return false;
            }
        }

        /// <summary>
        /// Gets an array containing all the components id's, the array also represents the component order.
        /// </summary>
        public byte[] Ids
        {
            get
            {
                int componentsLength = Components.Length;

                byte[] result = new byte[componentsLength];

                for (int i = 0; i < componentsLength; ++i) result[i] = Components[i].Id;

                return result;
            }
        }

        #endregion

        #region Methods

        public int IndexOf(MediaComponent component)
        {
            if (component == null) throw new System.ArgumentNullException();

            return System.Array.IndexOf(Components, component);
        }

        public MediaComponent GetComponentById(byte id)
        {
            foreach (MediaComponent mc in Components)
            {
                if (mc.Id == id) return mc;
            }

            return null;
        }

        public int BitsInFormatBefore(int componentIndex, bool inclusive = false)
        {
            int bits = 0;

            if (false == inclusive) ++componentIndex;

            for (int i = 0; i < componentIndex; ++i)
            {
                bits += Components[i].Size;
            }

            return bits;
        }

        public int BytesInFormatBefore(int componentIndex, bool inclusive = false)
        {
            return Common.Binary.BitsToBytes(BitsInFormatBefore(componentIndex, inclusive));
        }

        public int BitsInFormatAfter(int componentIndex, bool inclusive = false)
        {
            int bits = 0;

            if (false == inclusive) componentIndex++;

            for (int e = Components.Length; componentIndex < e; ++componentIndex)
            {
                bits += Components[componentIndex].Size;
            }

            return bits;
        }

        public int BytesInFormatAfter(int componentIndex, bool inclusive = false)
        {
            return Common.Binary.BitsToBytes(BitsInFormatAfter(componentIndex, inclusive));
        }

        //BitsSizeWithoutComponents

        #endregion
    }
}
