
namespace SMBLibrary.SMB1
{
    /// <summary>
    /// This is the Function field in NTTransactRequest
    /// </summary>
    public enum NTTransactSubcommandName : ushort
    {
        NT_TRANSACT_CREATE = 0x0001,
        NT_TRANSACT_IOCTL = 0x0002,
        NT_TRANSACT_SET_SECURITY_DESC = 0x0003,
        NT_TRANSACT_NOTIFY_CHANGE = 0x0004,
        // NT_TRANSACT_RENAME = 0x0005,
        NT_TRANSACT_QUERY_SECURITY_DESC = 0x0006,
    }
}