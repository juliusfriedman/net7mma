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

//Should be in Container.Common?

//Needs to be the same as the RFC
//https://tools.ietf.org/html/rfc2361


namespace Media.Codecs.Audio
{
    /// <summary>
    /// Identifies various formats for Audio Encodings
    /// </summary>
    public enum WaveFormatId : ushort
    {
        /// <summary>UNKNOWN,	Microsoft Corporation</summary>
        Unknown = 0x0000,
        /// <summary>PCM		Microsoft Corporation</summary>
        Pcm = 0x0001,
        /// <summary>ADPCM		Microsoft Corporation</summary>
        Adpcm = 0x0002,
        /// <summary>IEEE_FLOAT Microsoft Corporation</summary>
        IeeeFloat = 0x0003,
        /// <summary>VSELP		Compaq Computer Corp.</summary>
        Vselp = 0x0004,
        /// <summary>IBM_CVSD	IBM Corporation</summary>
        IbmCvsd = 0x0005,
        /// <summary>ALAW		Microsoft Corporation</summary>
        ALaw = 0x0006,
        /// <summary>MULAW		Microsoft Corporation</summary>
        MuLaw = 0x0007,
        /// <summary>DTS		Microsoft Corporation</summary>
        Dts = 0x0008,
        /// <summary>DRM		Microsoft Corporation</summary>
        Drm = 0x0009,
        /// <summary>OKI	OKI</summary>
        OkiAdpcm = 0x0010,
        /// <summary>DVI	Intel Corporation</summary>
        DviAdpcm = 0x0011,
        /// <summary>IMA  Intel Corporation</summary>
        ImaAdpcm = DviAdpcm,
        /// <summary>MEDIASPACE Videologic</summary>
        MediaspaceAdpcm = 0x0012,
        /// <summary>SIERRA Sierra Semiconductor Corp </summary>
        SierraAdpcm = 0x0013,
        /// <summary>G723 Antex Electronics Corporation </summary>
        G723Adpcm = 0x0014,
        /// <summary>DIGISTD DSP Solutions, Inc.</summary>
        DigiStd = 0x0015,
        /// <summary>DIGIFIX DSP Solutions, Inc.</summary>
        DigiFix = 0x0016,
        /// <summary>DIALOGIC_OKI Dialogic Corporation</summary>
        DialogicOkiAdpcm = 0x0017,
        /// <summary>MEDIAVISION Media Vision, Inc.</summary>
        MediaVisionAdpcm = 0x0018,
        /// <summary>CU_CODEC Hewlett-Packard Company </summary>
        CUCodec = 0x0019,
        /// <summary>YAMAHA Yamaha Corporation of America</summary>
        YamahaAdpcm = 0x0020,
        /// <summary>SONARC Speech Compression</summary>
        SonarC = 0x0021,
        /// <summary>DSPGROUP_TRUESPEECH DSP Group, Inc </summary>
        DspGroupTrueSpeech = 0x0022,
        /// <summary>ECHOSC1 Echo Speech Corporation</summary>
        EchoSpeechCorporation1 = 0x0023,
        /// <summary>AUDIOFILE_AF36, Virtual Music, Inc.</summary>
        AudioFileAf36 = 0x0024,
        /// <summary>APTX Audio Processing Technology</summary>
        Aptx = 0x0025,
        /// <summary>AUDIOFILE_AF10, Virtual Music, Inc.</summary>
        AudioFileAf10 = 0x0026,
        /// <summary>PROSODY_1612, Aculab plc</summary>
        Prosody1612 = 0x0027,
        /// <summary>LRC, Merging Technologies S.A. </summary>
        Lrc = 0x0028,
        /// <summary>DOLBY_AC2, Dolby Laboratories</summary>
        DolbyAc2 = 0x0030,
        /// <summary>GSM610, Microsoft Corporation</summary>
        Gsm610 = 0x0031,
        /// <summary>MSNAUDIO, Microsoft Corporation</summary>
        MsnAudio = 0x0032,
        /// <summary>ANTEXE, Antex Electronics Corporation</summary>
        AntexAdpcme = 0x0033,
        /// <summary>CONTROL_RES_VQLPC, Control Resources Limited </summary>
        ControlResVqlpc = 0x0034,
        /// <summary>DIGIREAL, DSP Solutions, Inc. </summary>
        DigiReal = 0x0035,
        /// <summary>DIGIADPCM, DSP Solutions, Inc.</summary>
        DigiAdpcm = 0x0036,
        /// <summary>CONTROL_RES_CR10, Control Resources Limited</summary>
        ControlResCr10 = 0x0037,
        /// <summary>Natural MicroSystems </summary>
        NMS_VBXADPCM = 0x0038,
        /// <summary>Crystal Semiconductor IMA ADPCM </summary>
        CS_IMAADPCM = 0x0039, // 
        /// <summary>Echo Speech Corporation </summary>
        ECHOSC3 = 0x003A, // 
        /// <summary>Rockwell International </summary>
        ROCKWELL = 0x003B, // 
        /// <summary>Rockwell International </summary>
        ROCKWELL_DIGITALK = 0x003C, // Rockwell International 
        /// <summary>Xebec Multimedia Solutions Limited </summary>
        XEBEC = 0x003D, // 
        /// <summary>Antex Electronics Corporation </summary>
        G721 = 0x0040, // 
        /// <summary>Antex Electronics Corporation </summary>
        G728 = 0x0041, // 
        /// <summary></summary>
        MSG723 = 0x0042, // Microsoft Corporation 
        /// <summary></summary>
        Mpeg = 0x0050, // MPEG, Microsoft Corporation 
        /// <summary></summary>
        RT24 = 0x0052, // InSoft, Inc. 
        /// <summary></summary>
        PAC = 0x0053, // InSoft, Inc. 
        /// <summary></summary>
        MpegLayer3 = 0x0055, // MPEGLAYER3, ISO/MPEG Layer3 Format Tag 
        /// <summary></summary>
        LUCENT_G723 = 0x0059, // Lucent Technologies 
        /// <summary></summary>
        CIRRUS = 0x0060, // Cirrus Logic 
        /// <summary></summary>
        ESPCM = 0x0061, // ESS Technology 
        /// <summary></summary>
        VOXWARE = 0x0062, // Voxware Inc 
        /// <summary></summary>
        CANOPUS_ATRAC = 0x0063, // Canopus, co., Ltd. 
        /// <summary></summary>
        G726 = 0x0064, // APICOM 
        /// <summary></summary>
        G722 = 0x0065, // APICOM 
        /// <summary></summary>
        DSAT_DISPLAY = 0x0067, // Microsoft Corporation 
        /// <summary></summary>
        VOXWARE_BYTE_ALIGNED = 0x0069, // Voxware Inc 
        /// <summary></summary>
        VOXWARE_AC8 = 0x0070, // Voxware Inc 
        /// <summary></summary>
        VOXWARE_AC10 = 0x0071, // Voxware Inc 
        /// <summary></summary>
        VOXWARE_AC16 = 0x0072, // Voxware Inc 
        /// <summary></summary>
        VOXWARE_AC20 = 0x0073, // Voxware Inc 
        /// <summary></summary>
        VOXWARE_RT24 = 0x0074, // Voxware Inc 
        /// <summary></summary>
        VOXWARE_RT29 = 0x0075, // Voxware Inc 
        /// <summary></summary>
        VOXWARE_RT29HW = 0x0076, // Voxware Inc 
        /// <summary></summary>
        VOXWARE_VR12 = 0x0077, // Voxware Inc 
        /// <summary></summary>
        VOXWARE_VR18 = 0x0078, // Voxware Inc 
        /// <summary></summary>
        VOXWARE_TQ40 = 0x0079, // Voxware Inc 
        /// <summary></summary>
        SOFTSOUND = 0x0080, // Softsound, Ltd. 
        /// <summary></summary>
        VOXWARE_TQ60 = 0x0081, // Voxware Inc 
        /// <summary></summary>
        MSRT24 = 0x0082, // Microsoft Corporation 
        /// <summary></summary>
        G729A = 0x0083, // AT&T Labs, Inc. 
        /// <summary></summary>
        MVI_MVI2 = 0x0084, // Motion Pixels 
        /// <summary></summary>
        DF_G726 = 0x0085, // DataFusion Systems (Pty) (Ltd) 
        /// <summary></summary>
        DF_GSM610 = 0x0086, // DataFusion Systems (Pty) (Ltd) 
        /// <summary></summary>
        ISIAUDIO = 0x0088, // Iterated Systems, Inc. 
        /// <summary></summary>
        ONLIVE = 0x0089, // OnLive! Technologies, Inc. 
        /// <summary></summary>
        SBC24 = 0x0091, // Siemens Business Communications Sys 
        /// <summary></summary>
        DOLBY_AC3_SPDIF = 0x0092, // Sonic Foundry 
        /// <summary></summary>
        MEDIASONIC_G723 = 0x0093, // MediaSonic 
        /// <summary></summary>
        PROSODY_8KBPS = 0x0094, // Aculab plc 
        /// <summary></summary>
        ZYXEL = 0x0097, // ZyXEL Communications, Inc. 
        /// <summary></summary>
        PHILIPS_LPCBB = 0x0098, // Philips Speech Processing 
        /// <summary></summary>
        PACKED = 0x0099, // Studer Professional Audio AG 
        /// <summary></summary>
        MALDEN_PHONYTALK = 0x00A0, // Malden Electronics Ltd. 
        /// <summary>GSM</summary>
        Gsm = 0x00A1,
        /// <summary>G729</summary>
        G729 = 0x00A2,
        /// <summary>G723</summary>
        G723 = 0x00A3,
        /// <summary>ACELP</summary>
        Acelp = 0x00A4,
        /// <summary></summary>
        RHETOREX = 0x0100, // Rhetorex Inc. 
        /// <summary></summary>
        IRAT = 0x0101, // BeCubed Software Inc. 
        /// <summary></summary>
        VIVO_G723 = 0x0111, // Vivo Software 
        /// <summary></summary>
        VIVO_SIREN = 0x0112, // Vivo Software 
        /// <summary></summary>
        DIGITAL_G723 = 0x0123, // Digital Equipment Corporation 
        /// <summary></summary>
        SANYO_LD = 0x0125, // Sanyo Electric Co., Ltd. 
        /// <summary></summary>
        SIPROLAB_ACEPLNET = 0x0130, // Sipro Lab Telecom Inc. 
        /// <summary></summary>
        SIPROLAB_ACELP4800 = 0x0131, // Sipro Lab Telecom Inc. 
        /// <summary></summary>
        SIPROLAB_ACELP8V3 = 0x0132, // Sipro Lab Telecom Inc. 
        /// <summary></summary>
        SIPROLAB_G729 = 0x0133, // Sipro Lab Telecom Inc. 
        /// <summary></summary>
        SIPROLAB_G729A = 0x0134, // Sipro Lab Telecom Inc. 
        /// <summary></summary>
        SIPROLAB_KELVIN = 0x0135, // Sipro Lab Telecom Inc. 
        /// <summary></summary>
        G726ADPCM = 0x0140, // Dictaphone Corporation 
        /// <summary></summary>
        QUALCOMM_PUREVOICE = 0x0150, // Qualcomm, Inc. 
        /// <summary></summary>
        QUALCOMM_HALFRATE = 0x0151, // Qualcomm, Inc. 
        /// <summary></summary>
        TUBGSM = 0x0155, // Ring Zero Systems, Inc. 
        /// <summary></summary>
        MSAUDIO1 = 0x0160, // Microsoft Corporation 		
        /// <summary>
        /// WMAUDIO2, Microsoft Corporation
        /// </summary>
        WMAUDIO2 = 0x0161,
        /// <summary>
        /// WMAUDIO3, Microsoft Corporation
        /// </summary>
        WMAUDIO3 = 0x0162,
        /// <summary></summary>
        UNISYS_NAP = 0x0170, // Unisys Corp. 
        /// <summary></summary>
        UNISYS_NAP_ULAW = 0x0171, // Unisys Corp. 
        /// <summary></summary>
        UNISYS_NAP_ALAW = 0x0172, // Unisys Corp. 
        /// <summary></summary>
        UNISYS_NAP_16K = 0x0173, // Unisys Corp. 
        /// <summary></summary>
        CREATIVE = 0x0200, // Creative Labs, Inc 
        /// <summary></summary>
        CREATIVE_FASTSPEECH8 = 0x0202, // Creative Labs, Inc 
        /// <summary></summary>
        CREATIVE_FASTSPEECH10 = 0x0203, // Creative Labs, Inc 
        /// <summary></summary>
        UHER = 0x0210, // UHER informatic GmbH 
        /// <summary></summary>
        QUARTERDECK = 0x0220, // Quarterdeck Corporation 
        /// <summary></summary>
        ILINK_VC = 0x0230, // I-link Worldwide 
        /// <summary></summary>
        RAW_SPORT = 0x0240, // Aureal Semiconductor 
        /// <summary></summary>
        ESST_AC3 = 0x0241, // ESS Technology, Inc. 
        /// <summary></summary>
        IPI_HSX = 0x0250, // Interactive Products, Inc. 
        /// <summary></summary>
        IPI_RPELP = 0x0251, // Interactive Products, Inc. 
        /// <summary></summary>
        CS2 = 0x0260, // Consistent Software 
        /// <summary></summary>
        SONY_SCX = 0x0270, // Sony Corp. 
        /// <summary></summary>
        FM_TOWNS_SND = 0x0300, // Fujitsu Corp. 
        /// <summary></summary>
        BTV_DIGITAL = 0x0400, // Brooktree Corporation 
        /// <summary></summary>
        QDESIGN_MUSIC = 0x0450, // QDesign Corporation 
        /// <summary></summary>
        VME_VMPCM = 0x0680, // AT&T Labs, Inc. 
        /// <summary></summary>
        TPC = 0x0681, // AT&T Labs, Inc. 
        /// <summary></summary>
        OLIGSM = 0x1000, // Ing C. Olivetti & C., S.p.A. 
        /// <summary></summary>
        OLIADPCM = 0x1001, // Ing C. Olivetti & C., S.p.A. 
        /// <summary></summary>
        OLICELP = 0x1002, // Ing C. Olivetti & C., S.p.A. 
        /// <summary></summary>
        OLISBC = 0x1003, // Ing C. Olivetti & C., S.p.A. 
        /// <summary></summary>
        OLIOPR = 0x1004, // Ing C. Olivetti & C., S.p.A. 
        /// <summary></summary>
        LH_CODEC = 0x1100, // Lernout & Hauspie 
        /// <summary></summary>
        NORRIS = 0x1400, // Norris Communications, Inc. 
        /// <summary></summary>
        SOUNDSPACE_MUSICOMPRESS = 0x1500, // AT&T Labs, Inc. 
        /// <summary></summary>
        DVM = 0x2000, // FAST Multimedia AG 
        /// <summary>EXTENSIBLE</summary>
        Extensible = 0xFFFE, // Microsoft 
        /// <summary></summary>
        DEVELOPMENT = 0xFFFF,

        // others - not from MS headers
        /// <summary>VORBIS1 "Og" Original stream compatible</summary>
        Vorbis1 = 0x674f,
        /// <summary>VORBIS2 "Pg" Have independent header</summary>
        Vorbis2 = 0x6750,
        /// <summary>VORBIS3 "Qg" Have no codebook header</summary>
        Vorbis3 = 0x6751,
        /// <summary>VORBIS1P "og" Original stream compatible</summary>
        Vorbis1P = 0x676f,
        /// <summary>VORBIS2P "pg" Have independent headere</summary>
        Vorbis2P = 0x6770,
        /// <summary>VORBIS3P "qg" Have no codebook header</summary>
        Vorbis3P = 0x6771,
    }
}
