using System;
using System.IO;
using System.Text;
using System.Runtime.InteropServices;

public static class Task
{
    public static string Execute(string FileName = "")
    {
        try
        {
            StringBuilder output = new StringBuilder();

            if (string.IsNullOrEmpty(FileName.Trim()))
            {
                string roamingDir = System.Environment.GetFolderPath(System.Environment.SpecialFolder.ApplicationData);
                string defaultJpnIHDSPath = Path.Combine(roamingDir, "Microsoft\\InputMethod\\Shared\\JpnIHDS.dat");
                FileName = defaultJpnIHDSPath;
            }

            if (!File.Exists(FileName))
            {
                return $"Failed to IMEHistory: File not exists {FileName}";
            }

            using (var reader = new BinaryReader(File.OpenRead(FileName)))
            {
                var header = JPNIHDS.ReadFrom<JPNIHDS.IMPPRED_HEADER>(reader);

                for (int i = 0; i < header.dwEntryCount; i++)
                {
                    var sentence = JPNIHDS.ReadFrom<JPNIHDS.IMPPRED_SENTENCE>(reader);

                    DateTime dt = DateTime.FromFileTime((long)sentence.Timestamp);

                    for (int j = 0; j < sentence.bSubEntryCount; j++)
                    {
                        var substitution = JPNIHDS.ReadFrom<JPNIHDS.IMPPRED_SUBSTITUTION>(reader);
                        byte[] buf;

                        buf = reader.ReadBytes(substitution.nInputLen * 2);
                        string inputText = System.Text.Encoding.Unicode.GetString(buf);

                        buf = reader.ReadBytes(substitution.nResultLen * 2);
                        string resultText = System.Text.Encoding.Unicode.GetString(buf);

                        string text;
                        if (substitution.nResultLen == 0)
                            text = inputText;   
                        else
                            text = resultText;

                        if (text.Length != 0)
                        {
                            string dtStr = dt.ToString("yyyy/MM/dd hh:mm:ss");
                            string line = $"{dtStr} {text}";
                            output.AppendLine(line);
                        }                        
                    }
                }
            }
            return output.ToString();
        }
        catch (Exception e) { return e.GetType().FullName + ": " + e.Message + Environment.NewLine + e.StackTrace; }
    }
}
static class JPNIHDS {

    [StructLayout(LayoutKind.Explicit)]
    public struct IMPPRED_HEADER
    {
        [FieldOffset(0)]
        public UInt64 Timestamp;
        [FieldOffset(8)]
        public UInt32  dwAllocatedSize;
        [FieldOffset(12)]
        public UInt32 dwFlags;
        [FieldOffset(16)]
        public UInt32 dwEntryCount;
        [FieldOffset(20)]
        public UInt32 dwConst_0x20;
        [FieldOffset(24)]
        public UInt32 dwUnk1;
        [FieldOffset(28)]
        public UInt32 dwUsedSize;
    }

    [StructLayout(LayoutKind.Explicit)]
    public struct IMPPRED_SENTENCE
    {
        [FieldOffset(0)]
        public UInt64 Timestamp;
        [FieldOffset(8)]
        public UInt16 dwSubSize;
        [FieldOffset(10)]
        public UInt16 wConst_0x10;
        [FieldOffset(12)]
        public byte bConst_0x01;
        [FieldOffset(13)]
        public byte bSubEntryCount;
        [FieldOffset(14)]
        public UInt16 wConst_0x00;
    }

    [StructLayout(LayoutKind.Explicit)]
    public struct IMPPRED_SUBSTITUTION
    {
        [FieldOffset(0)]
        public UInt16 dwSizeOfThisStruct;
        [FieldOffset(2)]
        public byte nResultLen;
        [FieldOffset(3)]
        public byte nInputLen;
        [FieldOffset(4)]
        public UInt32 dwFlags;
    }

    public static T ReadFrom<T>(BinaryReader reader) where T : struct
    {
        int size = Marshal.SizeOf(typeof(T));
        IntPtr ptr = IntPtr.Zero;
        
        try
        {
            ptr = Marshal.AllocHGlobal(size);
            Marshal.Copy(reader.ReadBytes(size), 0, ptr, size);
            return (T)Marshal.PtrToStructure(ptr, typeof(T));
        }
        finally
        {
            if (ptr != IntPtr.Zero)
            {
                Marshal.FreeHGlobal(ptr);
            }
        }
    }
}