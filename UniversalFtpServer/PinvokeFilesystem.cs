using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Diagnostics;
using System.Runtime.InteropServices.ComTypes;
using FileAttributes = System.IO.FileAttributes;

namespace UniversalFtpServer
{


	public class MonitoredFolderItem
	{

		public bool IsDir { get; set; }
		public string Name { get; set; }
		public DateTimeOffset DateCreated { get; set; }

		public string ParentFolderPath { get; set; }
		public string DefaultAppName { get; set; }

		public DateTimeOffset DateModified { get; set; }

		public System.IO.FileAttributes attributes { get; set; }

		public DateTimeOffset ItemDate { get; set; }

		public ulong Size { get; set; }

		/// <summary>True if the properties have been populated at least once</summary>
		public bool IsPropertiesPopulated { get; set; }




		// NOTE: This constructor was commented out when the PInvoke method was abandoned
		//internal MonitoredFolderItem(MonitoredFolderItemType type, string name)
		public MonitoredFolderItem(string name)
		{
			Name = name;
		}


	}

	class PinvokeFilesystem
    {
		//start of block for retrival of drives
		//import dll and function from dll
		[DllImport("api-ms-win-core-file-l1-1-0.dll", SetLastError = true, CharSet = CharSet.Unicode)]
        static extern UInt32 GetLogicalDrives();

		//create Enum for drive letters
		[Flags]
		enum driveletters
		{
			A = 1,
			B = 2,
			C = 4,
			D = 8,
			E = 16,
			F = 32,
			G = 64,
			H = 128,
			I = 256,
			J = 512,
			K = 1024,
			L = 2048,
			M = 4096,
			N = 8192,
			O = 16384,
			P = 32768,
			Q = 65536,
			R = 131072,
			S = 262144,
			T = 524288,
			U = 1048576,
			V = 2097152,
			W = 4194304,
			X = 8388608,
			Y = 16777216,
			Z = 33554432
		}

		//create function to get all drives
		public static List<String> GetDrives()
		{
			//create empty list of drives
			List<String> Drives = new List<string>();

			//get all logical drives and parse it via the enum
			driveletters DriveLetters = (driveletters)GetLogicalDrives();

			//loop through all drive letters
			foreach (driveletters value in driveletters.GetValues(DriveLetters.GetType()))
				//check if the enum of the drive letter is present
				if (DriveLetters.HasFlag(value))
				{
					//add the drive letter to the list
					Drives.Add(value.ToString());
				}
			//return the complete list of drives
			return Drives;
		}
		//end block for retrival of drives

		//imports

		enum FINDEX_INFO_LEVELS
		{
			FindExInfoStandard = 0,
			FindExInfoBasic = 1
		}

		enum FINDEX_SEARCH_OPS
		{
			FindExSearchNameMatch = 0,
			FindExSearchLimitToDirectories = 1,
			FindExSearchLimitToDevices = 2
		}

		[StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
		struct WIN32_FIND_DATA
		{
			public uint itemAttributes;
			public System.Runtime.InteropServices.ComTypes.FILETIME creationTime;
			public System.Runtime.InteropServices.ComTypes.FILETIME lastAccessTime;
			public System.Runtime.InteropServices.ComTypes.FILETIME lastWriteTime;
			public uint fileSizeHigh;
			public uint fileSizeLow;
			public uint reserved0;
			public uint reserved1;
			[MarshalAs(UnmanagedType.ByValTStr, SizeConst = 260)]
			public string itemName;
			[MarshalAs(UnmanagedType.ByValTStr, SizeConst = 14)]
			public string alternateFileName;
		}


		[DllImport("api-ms-win-core-file-fromapp-l1-1-0.dll", SetLastError = true, CharSet = CharSet.Unicode)]
		static extern IntPtr FindFirstFileExFromApp(
			string lpFileName,
			FINDEX_INFO_LEVELS fInfoLevelId,
			out WIN32_FIND_DATA lpFindFileData,
			FINDEX_SEARCH_OPS fSearchOp,
			IntPtr lpSearchFilter,
			int dwAdditionalFlags);

		const int FIND_FIRST_EX_LARGE_FETCH = 2;

		[DllImport("api-ms-win-core-file-l1-1-0.dll", SetLastError = true, CharSet = CharSet.Unicode)]
		static extern bool FindNextFile(IntPtr hFindFile, out WIN32_FIND_DATA lpFindFileData);

		[DllImport("api-ms-win-core-file-l1-1-0.dll")]
		static extern bool FindClose(IntPtr hFindFile);

		public enum GET_FILEEX_INFO_LEVELS
		{
			GetFileExInfoStandard,
		}

		[StructLayout(LayoutKind.Sequential)]
		public struct WIN32_FILE_ATTRIBUTE_DATA
		{
			public System.IO.FileAttributes dwFileAttributes;
			public FILETIME ftCreationTime;
			public FILETIME ftLastAccessTime;
			public FILETIME ftLastWriteTime;
			public uint nFileSizeHigh;
			public uint nFileSizeLow;
		}

		[DllImport("api-ms-win-core-file-fromapp-l1-1-0.dll", SetLastError = true, CharSet = CharSet.Auto)]
		[return: MarshalAs(UnmanagedType.Bool)]
		public static extern bool GetFileAttributesExFromApp(string lpFileName, GET_FILEEX_INFO_LEVELS fInfoLevelId, out WIN32_FILE_ATTRIBUTE_DATA lpFileInformation);

		[DllImport("api-ms-win-core-file-fromapp-l1-1-0.dll", SetLastError = true, CharSet = CharSet.Auto)]
		[return: MarshalAs(UnmanagedType.Bool)]
		public static extern bool CreateDirectoryFromApp(string lpPathName, IntPtr SecurityAttributes);

		[DllImport("api-ms-win-core-file-fromapp-l1-1-0.dll", CharSet = CharSet.Auto, CallingConvention = CallingConvention.StdCall, SetLastError = true)]
		public static extern bool DeleteFileFromApp(string lpFileName);


		[DllImport("api-ms-win-core-file-fromapp-l1-1-0.dll", CharSet = CharSet.Auto, CallingConvention = CallingConvention.StdCall, SetLastError = true)]
		public static extern bool RemoveDirectoryFromApp(string lpPathName);

		[DllImport("api-ms-win-core-file-fromapp-l1-1-0.dll", CharSet = CharSet.Auto, CallingConvention = CallingConvention.StdCall, SetLastError = true)]
		public static extern bool CopyFileFromApp(string lpExistingFileName, string lpNewFileName, bool bFailIfExists);

		public const uint GENERIC_READ = 0x80000000;
		public const uint GENERIC_WRITE = 0x40000000;

		public const uint FILE_SHARE_READ = 0x00000001;
		public const uint FILE_SHARE_WRITE = 0x00000002;
		public const uint FILE_SHARE_DELETE = 0x00000004;

		public const uint CREATE_ALWAYS = 2;
		public const uint CREATE_NEW = 1;
		public const uint OPEN_ALWAYS = 4;
		public const uint OPEN_EXISTING = 3;
		public const uint TRUNCATE_EXISTING = 5;

		public enum File_Attributes : uint
		{
			Readonly = 0x00000001,
			Hidden = 0x00000002,
			System = 0x00000004,
			Directory = 0x00000010,
			Archive = 0x00000020,
			Device = 0x00000040,
			Normal = 0x00000080,
			Temporary = 0x00000100,
			SparseFile = 0x00000200,
			ReparsePoint = 0x00000400,
			Compressed = 0x00000800,
			Offline = 0x00001000,
			NotContentIndexed = 0x00002000,
			Encrypted = 0x00004000,
			Write_Through = 0x80000000,
			Overlapped = 0x40000000,
			NoBuffering = 0x20000000,
			RandomAccess = 0x10000000,
			SequentialScan = 0x08000000,
			DeleteOnClose = 0x04000000,
			BackupSemantics = 0x02000000,
			PosixSemantics = 0x01000000,
			OpenReparsePoint = 0x00200000,
			OpenNoRecall = 0x00100000,
			FirstPipeInstance = 0x00080000
		}

		[DllImport("api-ms-win-core-file-fromapp-l1-1-0.dll", CharSet = CharSet.Auto, CallingConvention = CallingConvention.StdCall, SetLastError = true)]
		public static extern IntPtr CreateFileFromApp(
			string lpFileName,
			uint dwDesiredAccess,
			uint dwShareMode,
			IntPtr SecurityAttributes,
			uint dwCreationDisposition,
			uint dwFlagsAndAttributes,
			IntPtr hTemplateFile
		);

		//function
		public static List<MonitoredFolderItem> GetItems(string path)
		{
			var result = new List<MonitoredFolderItem>();

			WIN32_FIND_DATA findDataResult;
			FINDEX_INFO_LEVELS findInfoLevel = FINDEX_INFO_LEVELS.FindExInfoBasic;

			int additionalFlags = 0;
			if (Environment.OSVersion.Version.Major >= 6)
			{
				findInfoLevel = FINDEX_INFO_LEVELS.FindExInfoBasic;
				additionalFlags = FIND_FIRST_EX_LARGE_FETCH;
			}

			IntPtr hFile = FindFirstFileExFromApp((@"\\?\" + path + "\\*.*"), findInfoLevel, out findDataResult, FINDEX_SEARCH_OPS.FindExSearchNameMatch, IntPtr.Zero, additionalFlags);
			var count = 0;
			if (hFile.ToInt64() != -1)
			{
				do
				{
					if (IsSystemItem(findDataResult.itemName))
						continue;

					// NOTE: This section was originally commented out when the PInvoke method was abandoned
					MonitoredFolderItem fileitem = new MonitoredFolderItem(findDataResult.itemName);

					//set attributes
					fileitem.attributes = (FileAttributes)findDataResult.itemAttributes;

					//set parent folder
					fileitem.ParentFolderPath = path;

					// May need to swap these around if the date is fucked
					long datecreatedoffset = findDataResult.creationTime.dwHighDateTime;
					datecreatedoffset = (datecreatedoffset << 32);
					datecreatedoffset = datecreatedoffset | (long)(uint)findDataResult.creationTime.dwLowDateTime;
					fileitem.DateCreated = System.DateTimeOffset.FromFileTime(datecreatedoffset);

					//set modified time
					long datemodifiedoffset = findDataResult.lastWriteTime.dwHighDateTime;
					datemodifiedoffset = (datemodifiedoffset << 32);
					datemodifiedoffset = datemodifiedoffset | (long)(uint)findDataResult.lastWriteTime.dwLowDateTime;
					fileitem.DateModified = System.DateTimeOffset.FromFileTime(datemodifiedoffset);

					//set the size
					fileitem.Size = (ulong)findDataResult.fileSizeHigh << 32 | (ulong)(uint)findDataResult.fileSizeLow;

					if (((FileAttributes)findDataResult.itemAttributes & FileAttributes.Directory) == FileAttributes.Directory)
					{
						fileitem.IsDir = true;
					}
					else
					{
						fileitem.IsDir = false;
					}
					result.Add(fileitem);

					++count;
				} while (FindNextFile(hFile, out findDataResult));

				FindClose(hFile);
			}

			return result;
		}

		//return just folder names, seperate methods as more of it is stripped out
		public static List<string> GetNames(string path)
		{
			var result = new List<string>();

			WIN32_FIND_DATA findDataResult;
			FINDEX_INFO_LEVELS findInfoLevel = FINDEX_INFO_LEVELS.FindExInfoBasic;

			int additionalFlags = 0;
			if (Environment.OSVersion.Version.Major >= 6)
			{
				findInfoLevel = FINDEX_INFO_LEVELS.FindExInfoBasic;
				additionalFlags = FIND_FIRST_EX_LARGE_FETCH;
			}

			IntPtr hFile = FindFirstFileExFromApp((@"\\?\" + path + "\\*.*"), findInfoLevel, out findDataResult, FINDEX_SEARCH_OPS.FindExSearchNameMatch, IntPtr.Zero, additionalFlags);
			var count = 0;
			if (hFile.ToInt64() != -1)
			{
				do
				{
					if (IsSystemItem(findDataResult.itemName))
						continue;

					// NOTE: This section was originally commented out when the PInvoke method was abandoned
					result.Add(findDataResult.itemName);

					++count;
				} while (FindNextFile(hFile, out findDataResult));

				FindClose(hFile);
			}

			return result;
		}

		//return just folder names, seperate methods as more of it is stripped out
		public static List<MonitoredFolderItem> GetMinInfo(string path)
		{
			var result = new List<MonitoredFolderItem>();

			WIN32_FIND_DATA findDataResult;
			FINDEX_INFO_LEVELS findInfoLevel = FINDEX_INFO_LEVELS.FindExInfoBasic;

			int additionalFlags = 0;
			if (Environment.OSVersion.Version.Major >= 6)
			{
				findInfoLevel = FINDEX_INFO_LEVELS.FindExInfoBasic;
				additionalFlags = FIND_FIRST_EX_LARGE_FETCH;
			}

			IntPtr hFile = FindFirstFileExFromApp((@"\\?\" + path + "\\*.*"), findInfoLevel, out findDataResult, FINDEX_SEARCH_OPS.FindExSearchNameMatch, IntPtr.Zero, additionalFlags);
			var count = 0;
			if (hFile.ToInt64() != -1)
			{
				do
				{
					if (IsSystemItem(findDataResult.itemName))
						continue;

					// NOTE: This section was originally commented out when the PInvoke method was abandoned
					MonitoredFolderItem fileitem = new MonitoredFolderItem(findDataResult.itemName);
					result.Add(fileitem);
					//set attributes
					fileitem.attributes = (FileAttributes)findDataResult.itemAttributes;

					//set parent folder
					fileitem.ParentFolderPath = path;
					++count;
				} while (FindNextFile(hFile, out findDataResult));

				FindClose(hFile);
			}

			return result;
		}

		/// local function
		private static bool IsSystemItem(string itemName)
		{
			if
			(
				itemName == "." ||
				itemName == ".."
			)
				return true;
			return false;
		}
	}
}
