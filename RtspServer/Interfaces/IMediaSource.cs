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

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Media.Rtsp.Server
{
    public interface IMediaSource : IMedia
    {
        /*
         
         WTG IETF.... 

            http://tools.ietf.org/search/draft-ietf-mmusic-rfc2326bis-39#page-11

            'sink' is used exactly 3 times in the same paragraph, additionaly it is bound by no definition given prior.

            Besides the one I am sure we are all familiar with a `sink` may also define a one-way channel of data transfer but which way? 
         * It probably SHOULD be out, but could be in as well.. but that would be a source now wouldn't it?
         * 
         * E.g. Does a `Sink` emit data, data which can be put into the sink from anywhere including another sink?
         * 
         * E.g. a `Source` must only receive data then...
         */

        // Sink, Source - These are terms usually used in eletronics where a source supplies current and a sink receives current.

        //DataReceived

        //PacketReceived

        //EndPoint?
    }
}
