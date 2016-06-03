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

namespace Media.Concepts.Classes
{
    public static class CentralProcessingUnit
    {
        #region References

        //https://git.kernel.org/cgit/linux/kernel/git/stable/linux-stable.git/tree/arch/x86/include/asm/cpufeature.h?id=refs/tags/v4.1.3        

        //http://wiki.osdev.org/X86-64

        #endregion

        //Todo, provide a was to associate from VendorString to this enum with parse.

        /// <summary>
        /// 
        /// </summary>
        public enum Vendor : byte
        {
            Unknown,
            Centaur,
            Advanced,
            Intel,
            MicroDevices,
            Motorola,    
            VIA,
            Cyrix = VIA,
            Transmeta,
            NationalSemiConductor,
            NSC = NationalSemiConductor,
            KVM,
            MSVM,
            XenHVM,
            NexGen,
            Rise,
            SiS,
            UMC,
            Vortex,
            AMD = Advanced | MicroDevices,
            AdvancedMicroDevices = AMD,
            ReducedInstructionSet,
            ARM = Advanced | ReducedInstructionSet,
            AdvancedReducedInstructionSet = ARM,
            Microsoft,
            Parallels,
            VMware,
            Xen
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public static Vendor GetVendor(string vendorString)
        {
            switch (Concepts.Hardware.Intrinsics.CpuId.GetVendorString())
            {
                //case null:
                //case "":
                default: return Vendor.Unknown;
                // Actual Hardware
                case Concepts.Hardware.Intrinsics.CpuId.VendorStrings.GenuineIntel: return Vendor.Intel;
                case Concepts.Hardware.Intrinsics.CpuId.VendorStrings.AMDisbetter_: //early engineering samples of AMD K5 processor
                case Concepts.Hardware.Intrinsics.CpuId.VendorStrings.AuthenticAMD: return Vendor.AMD;
                case Concepts.Hardware.Intrinsics.CpuId.VendorStrings.CentaurHauls: return Vendor.Centaur;
                case Concepts.Hardware.Intrinsics.CpuId.VendorStrings.CyrixInstead: return Vendor.Cyrix;
                case Concepts.Hardware.Intrinsics.CpuId.VendorStrings.TransmetaCPU: //Transmets
                case Concepts.Hardware.Intrinsics.CpuId.VendorStrings.GenuineTMx86: return Vendor.Transmeta;
                case Concepts.Hardware.Intrinsics.CpuId.VendorStrings.Geode_by_NSC: return Vendor.NationalSemiConductor;
                case Concepts.Hardware.Intrinsics.CpuId.VendorStrings.NexGen: return Vendor.NexGen;
                case Concepts.Hardware.Intrinsics.CpuId.VendorStrings.Vortext86_SoC: return Vendor.Vortex;
                //Virtual Machines
                case Concepts.Hardware.Intrinsics.CpuId.VendorStrings.KVMKVMKVM: return Vendor.KVM;
                case Concepts.Hardware.Intrinsics.CpuId.VendorStrings.Microsoft_Hv: return Vendor.Microsoft;
                case Concepts.Hardware.Intrinsics.CpuId.VendorStrings._lrpepyh_vr: return Vendor.Parallels;
                case Concepts.Hardware.Intrinsics.CpuId.VendorStrings.VMwareVMware: return Vendor.VMware;
                case Concepts.Hardware.Intrinsics.CpuId.VendorStrings.XenVMMXenVMM: return Vendor.Xen;
            }
        }


        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        public static long GetTimestampCounter()
        {
            //Determine the best method and return the result
            throw new System.NotImplementedException();
        }

        //Mode

        //CurrentMode

        //SetMode(Mode)

        //EnterLongMode => SetMode(Long)

        //ChangeByteOrder

        //...

    }
}
