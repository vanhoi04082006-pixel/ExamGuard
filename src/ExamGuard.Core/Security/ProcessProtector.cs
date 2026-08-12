using System.Runtime.InteropServices;

namespace ExamGuard.Core.Security;

/// <summary>
/// Makes the process "unkillable" from Task Manager / taskkill by rewriting the
/// process's own DACL so that EVERYONE is denied the rights needed to
/// terminate, suspend, inject into or write to the process. Self-termination
/// (ExitProcess) is NOT affected, so the password-gated Exit still works.
/// An administrator can still recover by resetting the DACL (SeDebugPrivilege).
/// </summary>
public static class ProcessProtector
{
    private const int SE_KERNEL_OBJECT = 6;
    private const uint DACL_SECURITY_INFORMATION = 0x04;

    private const uint ACCESS_DENY = 3; // DENY_ACCESS
    private const int TRUSTEE_IS_SID = 0;
    private const int TRUSTEE_IS_UNKNOWN = 0;

    private const uint PROCESS_TERMINATE = 0x0001;
    private const uint PROCESS_CREATE_THREAD = 0x0002;
    private const uint PROCESS_VM_OPERATION = 0x0008;
    private const uint PROCESS_VM_WRITE = 0x0020;
    private const uint PROCESS_SUSPEND_RESUME = 0x0800;

    private const uint READ_CONTROL = 0x00020000;
    private const uint PROCESS_QUERY_LIMITED_INFORMATION = 0x1000;
    private const uint PROCESS_ALL_ACCESS = 0x1F0FFF;

    private const uint ACCESS_GRANT = 1; // GRANT_ACCESS

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    private struct TRUSTEE
    {
        public IntPtr pMultipleTrustee;
        public int MultipleTrusteeOperation;
        public int TrusteeForm;
        public int TrusteeType;
        public IntPtr ptstrName;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct EXPLICIT_ACCESS
    {
        public uint grfAccessPermissions;
        public uint grfAccessMode;
        public uint grfInheritance;
        public TRUSTEE Trustee;
    }

    [DllImport("advapi32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    private static extern uint SetEntriesInAcl(
        uint cCountOfExplicitEntries,
        ref EXPLICIT_ACCESS pListOfExplicitEntries,
        IntPtr OldAcl,
        out IntPtr NewAcl);

    [DllImport("advapi32.dll", SetLastError = true)]
    private static extern uint SetSecurityInfo(
        IntPtr handle,
        int ObjectType,
        uint SecurityInfo,
        IntPtr psidOwner,
        IntPtr psidGroup,
        IntPtr pDacl,
        IntPtr pSacl);

    [DllImport("advapi32.dll", SetLastError = true)]
    private static extern uint GetSecurityInfo(
        IntPtr handle,
        int ObjectType,
        uint SecurityInfo,
        out IntPtr ppsidOwner,
        out IntPtr ppsidGroup,
        out IntPtr ppDacl,
        out IntPtr ppSacl,
        out IntPtr ppSecurityDescriptor);

    [DllImport("advapi32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    private static extern bool ConvertStringSidToSid(string StringSid, out IntPtr Sid);

    [DllImport("kernel32.dll")]
    private static extern IntPtr LocalFree(IntPtr hMem);

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern IntPtr OpenProcess(uint dwDesiredAccess, bool bInheritHandle, uint dwProcessId);

    [DllImport("kernel32.dll")]
    private static extern bool CloseHandle(IntPtr hObject);

    [DllImport("kernel32.dll")]
    private static extern uint GetCurrentProcessId();

    /// <summary>
    /// Denies termination/suspension/injection rights to everyone.
    /// A fresh DACL is built with the DENY ACE placed first (before any ALLOW),
    /// because Windows grants the first matching ACE and a trailing deny is
    /// beaten by an earlier "allow everyone full access" ACE (the default DACL
    /// grants full control to SYSTEM/Administrators).
    /// </summary>
    public static bool EnableUnkillable()
    {
        // PROCESS_ALL_ACCESS is always granted to our own process, and avoids
        // ACCESS_DENIED from SetSecurityInfo's post-change access check.
        IntPtr hProcess = OpenProcess(PROCESS_ALL_ACCESS, false, GetCurrentProcessId());
        if (hProcess == IntPtr.Zero)
            return false;
        try
        {
            if (!ConvertStringSidToSid("S-1-1-0", out IntPtr everyoneSid)) // Everyone
                return false;

            try
            {
                var trustee = new TRUSTEE
                {
                    pMultipleTrustee = IntPtr.Zero,
                    MultipleTrusteeOperation = 0,
                    TrusteeForm = TRUSTEE_IS_SID,
                    TrusteeType = TRUSTEE_IS_UNKNOWN,
                    ptstrName = everyoneSid
                };

                // ACE 0 (FIRST): deny Everyone the rights needed to kill/inject us.
                // ACE 1: allow Everyone read/query so tools can still inspect us.
                var entries = new[]
                {
                    new EXPLICIT_ACCESS
                    {
                        grfAccessPermissions = PROCESS_TERMINATE
                            | PROCESS_SUSPEND_RESUME
                            | PROCESS_CREATE_THREAD
                            | PROCESS_VM_OPERATION
                            | PROCESS_VM_WRITE,
                        grfAccessMode = ACCESS_DENY,
                        grfInheritance = 0,
                        Trustee = trustee
                    },
                    new EXPLICIT_ACCESS
                    {
                        grfAccessPermissions = READ_CONTROL | PROCESS_QUERY_LIMITED_INFORMATION,
                        grfAccessMode = ACCESS_GRANT,
                        grfInheritance = 0,
                        Trustee = trustee
                    }
                };

                // OldAcl = NULL: start from an empty ACL so the deny lands first.
                uint res = SetEntriesInAcl((uint)entries.Length, ref entries[0], IntPtr.Zero, out IntPtr newDacl);
                if (res != 0)
                    return false;

                try
                {
                    return SetSecurityInfo(hProcess, SE_KERNEL_OBJECT, DACL_SECURITY_INFORMATION,
                        IntPtr.Zero, IntPtr.Zero, newDacl, IntPtr.Zero) == 0;
                }
                finally
                {
                    LocalFree(newDacl);
                }
            }
            finally
            {
                LocalFree(everyoneSid);
            }
        }
        finally
        {
            CloseHandle(hProcess);
        }
    }
}
