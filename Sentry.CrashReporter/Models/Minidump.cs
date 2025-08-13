// https://formats.kaitai.io/windows_minidump/csharp.html
// License: CC0-1.0
// ReSharper disable All

using System.Text;
using Kaitai;

namespace Sentry.CrashReporter.Models;

#pragma warning disable CS8618, CS8625, CS8123

/// <summary>
///     Windows MiniDump (MDMP) file provides a concise way to store process
///     core dumps, which is useful for debugging. Given its small size,
///     modularity, some cross-platform features and native support in some
///     debuggers, it is particularly useful for crash reporting, and is
///     used for that purpose in Windows and Google Chrome projects.
///     The file itself is a container, which contains a number of typed
///     &quot;streams&quot;, which contain some data according to its type attribute.
/// </summary>
/// <remarks>
///     Reference:
///     <a href="https://learn.microsoft.com/en-us/windows/win32/api/minidumpapiset/ns-minidumpapiset-minidump_header">Source</a>
/// </remarks>
public class Minidump : KaitaiStruct
{
    public static Minidump FromFile(string fileName)
    {
        return new Minidump(new KaitaiStream(fileName));
    }

    public static Minidump FromBytes(byte[] bytes)
    {
        return new Minidump(new KaitaiStream(bytes));
    }

    public enum StreamTypes
    {
        Unused = 0,
        Reserved0 = 1,
        Reserved1 = 2,
        ThreadList = 3,
        ModuleList = 4,
        MemoryList = 5,
        Exception = 6,
        SystemInfo = 7,
        ThreadExList = 8,
        Memory64List = 9,
        CommentA = 10,
        CommentW = 11,
        HandleData = 12,
        FunctionTable = 13,
        UnloadedModuleList = 14,
        MiscInfo = 15,
        MemoryInfoList = 16,
        ThreadInfoList = 17,
        HandleOperationList = 18,
        Token = 19,
        JavaScriptData = 20,
        SystemMemoryInfo = 21,
        ProcessVmCounters = 22,
        IptTrace = 23,
        ThreadNames = 24,
        CeNull = 32768,
        CeSystemInfo = 32769,
        CeException = 32770,
        CeModuleList = 32771,
        CeProcessList = 32772,
        CeThreadList = 32773,
        CeThreadContextList = 32774,
        CeThreadCallStackList = 32775,
        CeMemoryVirtualList = 32776,
        CeMemoryPhysicalList = 32777,
        CeBucketParameters = 32778,
        CeProcessModuleMap = 32779,
        CeDiagnosisList = 32780,
        MdCrashpadInfoStream = 1129316353,
        MdRawBreakpadInfo = 1197932545,
        MdRawAssertionInfo = 1197932546,
        MdLinuxCpuInfo = 1197932547,
        MdLinuxProcStatus = 1197932548,
        MdLinuxLsbRelease = 1197932549,
        MdLinuxCmdLine = 1197932550,
        MdLinuxEnviron = 1197932551,
        MdLinuxAuxv = 1197932552,
        MdLinuxMaps = 1197932553,
        MdLinuxDsoDebug = 1197932554
    }

    public Minidump(KaitaiStream p__io, KaitaiStruct p__parent = null, Minidump p__root = null) :
        base(p__io)
    {
        M_Parent = p__parent;
        M_Root = p__root ?? this;
        f_streams = false;
        _read();
    }

    private void _read()
    {
        Magic1 = m_io.ReadBytes(4);
        if (!(KaitaiStream.ByteArrayCompare(Magic1, new byte[] { 77, 68, 77, 80 }) == 0))
        {
            throw new ValidationNotEqualError(new byte[] { 77, 68, 77, 80 }, Magic1, M_Io, "/seq/0");
        }

        Magic2 = m_io.ReadBytes(2);
        if (!(KaitaiStream.ByteArrayCompare(Magic2, new byte[] { 147, 167 }) == 0))
        {
            throw new ValidationNotEqualError(new byte[] { 147, 167 }, Magic2, M_Io, "/seq/1");
        }

        Version = m_io.ReadU2le();
        NumStreams = m_io.ReadU4le();
        OfsStreams = m_io.ReadU4le();
        Checksum = m_io.ReadU4le();
        Timestamp = m_io.ReadU4le();
        Flags = m_io.ReadU8le();
    }

    /// <remarks>
    ///     Reference:
    ///     <a href="https://learn.microsoft.com/en-us/windows/win32/api/minidumpapiset/ns-minidumpapiset-minidump_thread_list">Source</a>
    /// </remarks>
    public class ThreadList : KaitaiStruct
    {
        public static ThreadList FromFile(string fileName)
        {
            return new ThreadList(new KaitaiStream(fileName));
        }

        public ThreadList(KaitaiStream p__io, Dir p__parent = null, Minidump p__root = null) : base(p__io)
        {
            M_Parent = p__parent;
            M_Root = p__root;
            _read();
        }

        private void _read()
        {
            NumThreads = m_io.ReadU4le();
            Threads = new List<Thread>();
            for (var i = 0; i < NumThreads; i++)
            {
                Threads.Add(new Thread(m_io, this, M_Root));
            }
        }

        public uint NumThreads { get; private set; }

        public List<Thread> Threads { get; private set; }

        public Minidump M_Root { get; }

        public Dir M_Parent { get; }
    }

    /// <remarks>
    ///     Reference:
    ///     <a
    ///         href="https://learn.microsoft.com/en-us/windows/win32/api/minidumpapiset/ns-minidumpapiset-minidump_location_descriptor">
    ///         Source
    ///     </a>
    /// </remarks>
    public class LocationDescriptor : KaitaiStruct
    {
        public static LocationDescriptor FromFile(string fileName)
        {
            return new LocationDescriptor(new KaitaiStream(fileName));
        }

        public LocationDescriptor(KaitaiStream p__io, KaitaiStruct p__parent = null, Minidump p__root = null) :
            base(p__io)
        {
            M_Parent = p__parent;
            M_Root = p__root;
            f_data = false;
            _read();
        }

        private void _read()
        {
            LenData = m_io.ReadU4le();
            OfsData = m_io.ReadU4le();
        }

        private bool f_data;
        private byte[] _data;

        public byte[] Data
        {
            get
            {
                if (f_data)
                {
                    return _data;
                }

                var io = M_Root.M_Io;
                var _pos = io.Pos;
                io.Seek(OfsData);
                _data = io.ReadBytes(LenData);
                io.Seek(_pos);
                f_data = true;
                return _data;
            }
        }

        public uint LenData { get; private set; }

        public uint OfsData { get; private set; }

        public Minidump M_Root { get; }

        public KaitaiStruct M_Parent { get; }
    }

    /// <summary>
    ///     Specific string serialization scheme used in MiniDump format is
    ///     actually a simple 32-bit length-prefixed UTF-16 string.
    /// </summary>
    /// <remarks>
    ///     Reference:
    ///     <a href="https://learn.microsoft.com/en-us/windows/win32/api/minidumpapiset/ns-minidumpapiset-minidump_string">Source</a>
    /// </remarks>
    public class MinidumpString : KaitaiStruct
    {
        public static MinidumpString FromFile(string fileName)
        {
            return new MinidumpString(new KaitaiStream(fileName));
        }

        public MinidumpString(KaitaiStream p__io, SystemInfo p__parent = null, Minidump p__root = null) :
            base(p__io)
        {
            M_Parent = p__parent;
            M_Root = p__root;
            _read();
        }

        private void _read()
        {
            LenStr = m_io.ReadU4le();
            Str = Encoding.GetEncoding("UTF-16LE").GetString(m_io.ReadBytes(LenStr));
        }

        public uint LenStr { get; private set; }

        public string Str { get; private set; }

        public Minidump M_Root { get; }

        public SystemInfo M_Parent { get; }
    }

    /// <summary>
    ///     &quot;System info&quot; stream provides basic information about the
    ///     hardware and operating system which produces this dump.
    /// </summary>
    /// <remarks>
    ///     Reference:
    ///     <a href="https://learn.microsoft.com/en-us/windows/win32/api/minidumpapiset/ns-minidumpapiset-minidump_system_info">Source</a>
    /// </remarks>
    public class SystemInfo : KaitaiStruct
    {
        public static SystemInfo FromFile(string fileName)
        {
            return new SystemInfo(new KaitaiStream(fileName));
        }


        public enum CpuArchs
        {
            Intel = 0,
            Arm = 5,
            Ia64 = 6,
            Amd64 = 9,
            Unknown = 65535
        }

        public SystemInfo(KaitaiStream p__io, Dir p__parent = null, Minidump p__root = null) : base(p__io)
        {
            M_Parent = p__parent;
            M_Root = p__root;
            f_servicePack = false;
            _read();
        }

        private void _read()
        {
            CpuArch = (CpuArchs)m_io.ReadU2le();
            CpuLevel = m_io.ReadU2le();
            CpuRevision = m_io.ReadU2le();
            NumCpus = m_io.ReadU1();
            OsType = m_io.ReadU1();
            OsVerMajor = m_io.ReadU4le();
            OsVerMinor = m_io.ReadU4le();
            OsBuild = m_io.ReadU4le();
            OsPlatform = m_io.ReadU4le();
            OfsServicePack = m_io.ReadU4le();
            OsSuiteMask = m_io.ReadU2le();
            Reserved2 = m_io.ReadU2le();
        }

        private bool f_servicePack;
        private MinidumpString _servicePack;

        public MinidumpString ServicePack
        {
            get
            {
                if (f_servicePack)
                {
                    return _servicePack;
                }

                if (OfsServicePack > 0)
                {
                    var io = M_Root.M_Io;
                    var _pos = io.Pos;
                    io.Seek(OfsServicePack);
                    _servicePack = new MinidumpString(io, this, M_Root);
                    io.Seek(_pos);
                    f_servicePack = true;
                }

                return _servicePack;
            }
        }

        public CpuArchs CpuArch { get; private set; }

        public ushort CpuLevel { get; private set; }

        public ushort CpuRevision { get; private set; }

        public byte NumCpus { get; private set; }

        public byte OsType { get; private set; }

        public uint OsVerMajor { get; private set; }

        public uint OsVerMinor { get; private set; }

        public uint OsBuild { get; private set; }

        public uint OsPlatform { get; private set; }

        public uint OfsServicePack { get; private set; }

        public ushort OsSuiteMask { get; private set; }

        public ushort Reserved2 { get; private set; }

        public Minidump M_Root { get; }

        public Dir M_Parent { get; }
    }

    /// <remarks>
    ///     Reference:
    ///     <a href="https://learn.microsoft.com/en-us/windows/win32/api/minidumpapiset/ns-minidumpapiset-minidump_exception">Source</a>
    /// </remarks>
    public class ExceptionRecord : KaitaiStruct
    {
        public static ExceptionRecord FromFile(string fileName)
        {
            return new ExceptionRecord(new KaitaiStream(fileName));
        }

        public ExceptionRecord(KaitaiStream p__io, ExceptionStream p__parent = null, Minidump p__root = null) :
            base(p__io)
        {
            M_Parent = p__parent;
            M_Root = p__root;
            _read();
        }

        private void _read()
        {
            Code = m_io.ReadU4le();
            Flags = m_io.ReadU4le();
            InnerException = m_io.ReadU8le();
            Addr = m_io.ReadU8le();
            NumParams = m_io.ReadU4le();
            Reserved = m_io.ReadU4le();
            Params = new List<ulong>();
            for (var i = 0; i < 15; i++)
            {
                Params.Add(m_io.ReadU8le());
            }
        }

        public uint Code { get; private set; }

        public uint Flags { get; private set; }

        public ulong InnerException { get; private set; }

        /// <summary>
        ///     Memory address where exception has occurred
        /// </summary>
        public ulong Addr { get; private set; }

        public uint NumParams { get; private set; }

        public uint Reserved { get; private set; }

        /// <summary>
        ///     Additional parameters passed along with exception raise
        ///     function (for WinAPI, that is `RaiseException`). Meaning is
        ///     exception-specific. Given that this type is originally
        ///     defined by a C structure, it is described there as array of
        ///     fixed number of elements (`EXCEPTION_MAXIMUM_PARAMETERS` =
        ///     15), but in reality only first `num_params` would be used.
        /// </summary>
        public List<ulong> Params { get; private set; }

        public Minidump M_Root { get; }

        public ExceptionStream M_Parent { get; }
    }

    /// <remarks>
    ///     Reference:
    ///     <a href="https://learn.microsoft.com/en-us/windows/win32/api/minidumpapiset/ns-minidumpapiset-minidump_misc_info">Source</a>
    /// </remarks>
    public class MiscInfo : KaitaiStruct
    {
        public static MiscInfo FromFile(string fileName)
        {
            return new MiscInfo(new KaitaiStream(fileName));
        }

        public MiscInfo(KaitaiStream p__io, Dir p__parent = null, Minidump p__root = null) : base(p__io)
        {
            M_Parent = p__parent;
            M_Root = p__root;
            _read();
        }

        private void _read()
        {
            LenInfo = m_io.ReadU4le();
            Flags1 = m_io.ReadU4le();
            ProcessId = m_io.ReadU4le();
            ProcessCreateTime = m_io.ReadU4le();
            ProcessUserTime = m_io.ReadU4le();
            ProcessKernelTime = m_io.ReadU4le();
            CpuMaxMhz = m_io.ReadU4le();
            CpuCurMhz = m_io.ReadU4le();
            CpuLimitMhz = m_io.ReadU4le();
            CpuMaxIdleState = m_io.ReadU4le();
            CpuCurIdleState = m_io.ReadU4le();
        }

        public uint LenInfo { get; private set; }

        public uint Flags1 { get; private set; }

        public uint ProcessId { get; private set; }

        public uint ProcessCreateTime { get; private set; }

        public uint ProcessUserTime { get; private set; }

        public uint ProcessKernelTime { get; private set; }

        public uint CpuMaxMhz { get; private set; }

        public uint CpuCurMhz { get; private set; }

        public uint CpuLimitMhz { get; private set; }

        public uint CpuMaxIdleState { get; private set; }

        public uint CpuCurIdleState { get; private set; }

        public Minidump M_Root { get; }

        public Dir M_Parent { get; }
    }

    /// <remarks>
    ///     Reference:
    ///     <a href="https://learn.microsoft.com/en-us/windows/win32/api/minidumpapiset/ns-minidumpapiset-minidump_directory">Source</a>
    /// </remarks>
    public class Dir : KaitaiStruct
    {
        public static Dir FromFile(string fileName)
        {
            return new Dir(new KaitaiStream(fileName));
        }

        public Dir(KaitaiStream p__io, Minidump p__parent = null, Minidump p__root = null) : base(p__io)
        {
            M_Parent = p__parent;
            M_Root = p__root;
            f_data = false;
            _read();
        }

        private void _read()
        {
            StreamType = (StreamTypes)m_io.ReadU4le();
            LenData = m_io.ReadU4le();
            OfsData = m_io.ReadU4le();
        }

        private bool f_data;
        private object _data;

        public object Data
        {
            get
            {
                if (f_data)
                {
                    return _data;
                }

                var _pos = m_io.Pos;
                m_io.Seek(OfsData);
                switch (StreamType)
                {
                    case StreamTypes.MemoryList:
                    {
                        M_RawData = m_io.ReadBytes(LenData);
                        var io___raw_data = new KaitaiStream(M_RawData);
                        _data = new MemoryList(io___raw_data, this, M_Root);
                        break;
                    }
                    case StreamTypes.MiscInfo:
                    {
                        M_RawData = m_io.ReadBytes(LenData);
                        var io___raw_data = new KaitaiStream(M_RawData);
                        _data = new MiscInfo(io___raw_data, this, M_Root);
                        break;
                    }
                    case StreamTypes.ThreadList:
                    {
                        M_RawData = m_io.ReadBytes(LenData);
                        var io___raw_data = new KaitaiStream(M_RawData);
                        _data = new ThreadList(io___raw_data, this, M_Root);
                        break;
                    }
                    case StreamTypes.Exception:
                    {
                        M_RawData = m_io.ReadBytes(LenData);
                        var io___raw_data = new KaitaiStream(M_RawData);
                        _data = new ExceptionStream(io___raw_data, this, M_Root);
                        break;
                    }
                    case StreamTypes.SystemInfo:
                    {
                        M_RawData = m_io.ReadBytes(LenData);
                        var io___raw_data = new KaitaiStream(M_RawData);
                        _data = new SystemInfo(io___raw_data, this, M_Root);
                        break;
                    }
                    default:
                    {
                        _data = m_io.ReadBytes(LenData);
                        break;
                    }
                }

                m_io.Seek(_pos);
                f_data = true;
                return _data;
            }
        }

        public StreamTypes StreamType { get; private set; }

        /// <remarks>
        ///     Reference:
        ///     <a
        ///         href="https://learn.microsoft.com/en-us/windows/win32/api/minidumpapiset/ns-minidumpapiset-minidump_location_descriptor">
        ///         Source
        ///     </a>
        /// </remarks>
        public uint LenData { get; private set; }

        public uint OfsData { get; private set; }

        public Minidump M_Root { get; }

        public Minidump M_Parent { get; }

        public byte[] M_RawData { get; private set; }
    }

    /// <remarks>
    ///     Reference:
    ///     <a href="https://learn.microsoft.com/en-us/windows/win32/api/minidumpapiset/ns-minidumpapiset-minidump_thread">Source</a>
    /// </remarks>
    public class Thread : KaitaiStruct
    {
        public static Thread FromFile(string fileName)
        {
            return new Thread(new KaitaiStream(fileName));
        }

        public Thread(KaitaiStream p__io, ThreadList p__parent = null, Minidump p__root = null) : base(p__io)
        {
            M_Parent = p__parent;
            M_Root = p__root;
            _read();
        }

        private void _read()
        {
            ThreadId = m_io.ReadU4le();
            SuspendCount = m_io.ReadU4le();
            PriorityClass = m_io.ReadU4le();
            Priority = m_io.ReadU4le();
            Teb = m_io.ReadU8le();
            Stack = new MemoryDescriptor(m_io, this, M_Root);
            ThreadContext = new LocationDescriptor(m_io, this, M_Root);
        }

        public uint ThreadId { get; private set; }

        public uint SuspendCount { get; private set; }

        public uint PriorityClass { get; private set; }

        public uint Priority { get; private set; }

        /// <summary>
        ///     Thread Environment Block
        /// </summary>
        public ulong Teb { get; private set; }

        public MemoryDescriptor Stack { get; private set; }

        public LocationDescriptor ThreadContext { get; private set; }

        public Minidump M_Root { get; }

        public ThreadList M_Parent { get; }
    }

    /// <remarks>
    ///     Reference:
    ///     <a
    ///         href="https://learn.microsoft.com/en-us/windows/win32/api/minidumpapiset/ns-minidumpapiset-minidump_memory64_list">
    ///         Source
    ///     </a>
    /// </remarks>
    public class MemoryList : KaitaiStruct
    {
        public static MemoryList FromFile(string fileName)
        {
            return new MemoryList(new KaitaiStream(fileName));
        }

        public MemoryList(KaitaiStream p__io, Dir p__parent = null, Minidump p__root = null) : base(p__io)
        {
            M_Parent = p__parent;
            M_Root = p__root;
            _read();
        }

        private void _read()
        {
            NumMemRanges = m_io.ReadU4le();
            MemRanges = new List<MemoryDescriptor>();
            for (var i = 0; i < NumMemRanges; i++)
            {
                MemRanges.Add(new MemoryDescriptor(m_io, this, M_Root));
            }
        }

        public uint NumMemRanges { get; private set; }

        public List<MemoryDescriptor> MemRanges { get; private set; }

        public Minidump M_Root { get; }

        public Dir M_Parent { get; }
    }

    /// <remarks>
    ///     Reference:
    ///     <a
    ///         href="https://learn.microsoft.com/en-us/windows/win32/api/minidumpapiset/ns-minidumpapiset-minidump_memory_descriptor">
    ///         Source
    ///     </a>
    /// </remarks>
    public class MemoryDescriptor : KaitaiStruct
    {
        public static MemoryDescriptor FromFile(string fileName)
        {
            return new MemoryDescriptor(new KaitaiStream(fileName));
        }

        public MemoryDescriptor(KaitaiStream p__io, KaitaiStruct p__parent = null, Minidump p__root = null) :
            base(p__io)
        {
            M_Parent = p__parent;
            M_Root = p__root;
            _read();
        }

        private void _read()
        {
            AddrMemoryRange = m_io.ReadU8le();
            Memory = new LocationDescriptor(m_io, this, M_Root);
        }

        public ulong AddrMemoryRange { get; private set; }

        public LocationDescriptor Memory { get; private set; }

        public Minidump M_Root { get; }

        public KaitaiStruct M_Parent { get; }
    }

    /// <remarks>
    ///     Reference:
    ///     <a
    ///         href="https://learn.microsoft.com/en-us/windows/win32/api/minidumpapiset/ns-minidumpapiset-minidump_exception_stream">
    ///         Source
    ///     </a>
    /// </remarks>
    public class ExceptionStream : KaitaiStruct
    {
        public static ExceptionStream FromFile(string fileName)
        {
            return new ExceptionStream(new KaitaiStream(fileName));
        }

        public ExceptionStream(KaitaiStream p__io, Dir p__parent = null, Minidump p__root = null) : base(p__io)
        {
            M_Parent = p__parent;
            M_Root = p__root;
            _read();
        }

        private void _read()
        {
            ThreadId = m_io.ReadU4le();
            Reserved = m_io.ReadU4le();
            ExceptionRec = new ExceptionRecord(m_io, this, M_Root);
            ThreadContext = new LocationDescriptor(m_io, this, M_Root);
        }

        public uint ThreadId { get; private set; }

        public uint Reserved { get; private set; }

        public ExceptionRecord ExceptionRec { get; private set; }

        public LocationDescriptor ThreadContext { get; private set; }

        public Minidump M_Root { get; }

        public Dir M_Parent { get; }
    }

    private bool f_streams;
    private List<Dir> _streams;

    public List<Dir> Streams
    {
        get
        {
            if (f_streams)
            {
                return _streams;
            }

            var _pos = m_io.Pos;
            m_io.Seek(OfsStreams);
            _streams = new List<Dir>();
            for (var i = 0; i < NumStreams; i++)
            {
                _streams.Add(new Dir(m_io, this, M_Root));
            }

            m_io.Seek(_pos);
            f_streams = true;
            return _streams;
        }
    }

    public byte[] Magic1 { get; private set; }

    public byte[] Magic2 { get; private set; }

    public ushort Version { get; private set; }

    public uint NumStreams { get; private set; }

    public uint OfsStreams { get; private set; }

    public uint Checksum { get; private set; }

    public uint Timestamp { get; private set; }

    public ulong Flags { get; private set; }

    public Minidump M_Root { get; }

    public KaitaiStruct M_Parent { get; }
}

#pragma warning restore CS8618, CS8625, CS8123
