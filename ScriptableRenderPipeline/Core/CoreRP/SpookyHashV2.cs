using System;
using System.Diagnostics;
using System.Text.RegularExpressions;

namespace UnityEngine.Experimental.Rendering
{
    // From
    // SpookyHash.cs
    //
    // Author:
    //     Jon Hanna <jon@hackcraft.net>
    //
    // © 2014 Jon Hanna
    //
    // Licensed under the EUPL, Version 1.1 only (the “Licence”).
    // You may not use, modify or distribute this work except in compliance with the Licence.
    // You may obtain a copy of the Licence at:
    // <http://joinup.ec.europa.eu/software/page/eupl/licence-eupl>
    // A copy is also distributed with this source code.
    // Unless required by applicable law or agreed to in writing, software distributed under the
    // Licence is distributed on an “AS IS” basis, without warranties or conditions of any kind.

    // Based on Bob Jenkins’ SpookyHash version 2. <http://burtleburtle.net/bob/hash/spooky.html>
    // Described by Jenkins as “public domain” which may or may not be legally possible for the
    // work of a living person in your jurisdiction. If not, it may be reasonably inferred that
    // permission is given by him to port the algorithm into other languages, as per here.

    // Modified on 13/03/2018

    public static class SpookyHashV2
    {
        internal const ulong SpookyConst = 0xDEADBEEFDEADBEEF;
        const int NumVars = 12;
        const int BlockSize = NumVars * 8;
        const int BufSize = 2 * BlockSize;
        static readonly bool AllowUnalignedRead = AttemptDetectAllowUnalignedRead();
        static bool AttemptDetectAllowUnalignedRead()
        {
            switch (Environment.GetEnvironmentVariable("PROCESSOR_ARCHITECTURE"))
            {
                case "x86":
                case "AMD64": // Known to tolerate unaligned-reads well.
                    return true;
            }

            // Analysis disable EmptyGeneralCatchClause
            try
            {
                return FindAlignSafetyFromUname();
            }
            catch
            {
                return false;
            }
        }

        static bool FindAlignSafetyFromUname()
        {
            var startInfo = new ProcessStartInfo("uname", "-p");
            startInfo.CreateNoWindow = true;
            startInfo.ErrorDialog = false;
            startInfo.LoadUserProfile = false;
            startInfo.RedirectStandardOutput = true;
            startInfo.UseShellExecute = false;
            try
            {
                using (var proc = new Process())
                {
                    proc.StartInfo = startInfo;
                    proc.Start();
                    using (var output = proc.StandardOutput)
                    {
                        string line = output.ReadLine();
                        if (line != null)
                        {
                            string trimmed = line.Trim();
                            if (trimmed.Length != 0)
                                switch (trimmed)
                                {
                                    case "amd64":
                                    case "i386":
                                    case "x86_64":
                                    case "x64":
                                        return true; // Known to tolerate unaligned-reads well.
                                }
                        }
                    }
                }
            }
            catch
            {
                // We don't care why we failed, as there are many possible reasons, and they all amount
                // to our not having an answer. Just eat the exception.
            }
            startInfo.Arguments = "-m";
            try
            {
                using (var proc = new Process())
                {
                    proc.StartInfo = startInfo;
                    proc.Start();
                    using (var output = proc.StandardOutput)
                    {
                        string line = output.ReadLine();
                        if (line != null)
                        {
                            string trimmed = line.Trim();
                            if (trimmed.Length != 0)
                                switch (trimmed)
                                {
                                    case "amd64":
                                    case "i386":
                                    case "i686":
                                    case "i686-64":
                                    case "i86pc":
                                    case "x86_64":
                                    case "x64":
                                        return true; // Known to tolerate unaligned-reads well.
                                    default:
                                        return new Regex(@"i\d86").IsMatch(trimmed);
                                }
                        }
                    }
                }
            }
            catch
            {
                // Again, just eat the exception.
            }

            // Analysis restore EmptyGeneralCatchClause
            return false;
        }

        public static unsafe void Hash128(void* message, int length, ulong* hash1, ulong* hash2)
        {
            if ((int)message == 0)
            {
                *hash1 = 0;
                *hash2 = 0;
                return;
            }
            if (length < BufSize)
            {
                Short(message, length, hash1, hash2, false);
                return;
            }
            ulong h0, h1, h2, h3, h4, h5, h6, h7, h8, h9, h10, h11;

            h0 = h3 = h6 = h9 = *hash1;
            h1 = h4 = h7 = h10 = *hash2;
            h2 = h5 = h8 = h11 = SpookyConst;

            var p64 = (ulong*)message;

            ulong* end = p64 + ((length / BlockSize) * NumVars);
            ulong* buf = stackalloc ulong[NumVars];
            if (AllowUnalignedRead || (((long)message) & 7) == 0)
            {
                while (p64 < end)
                {
                    h0 += p64[0]; h2 ^= h10; h11 ^= h0; h0 = h0 << 11 | h0 >> -11; h11 += h1;
                    h1 += p64[1]; h3 ^= h11; h0 ^= h1; h1 = h1 << 32 | h1 >> 32; h0 += h2;
                    h2 += p64[2]; h4 ^= h0; h1 ^= h2; h2 = h2 << 43 | h2 >> -43; h1 += h3;
                    h3 += p64[3]; h5 ^= h1; h2 ^= h3; h3 = h3 << 31 | h3 >> -31; h2 += h4;
                    h4 += p64[4]; h6 ^= h2; h3 ^= h4; h4 = h4 << 17 | h4 >> -17; h3 += h5;
                    h5 += p64[5]; h7 ^= h3; h4 ^= h5; h5 = h5 << 28 | h5 >> -28; h4 += h6;
                    h6 += p64[6]; h8 ^= h4; h5 ^= h6; h6 = h6 << 39 | h6 >> -39; h5 += h7;
                    h7 += p64[7]; h9 ^= h5; h6 ^= h7; h7 = h7 << 57 | h7 >> -57; h6 += h8;
                    h8 += p64[8]; h10 ^= h6; h7 ^= h8; h8 = h8 << 55 | h8 >> -55; h7 += h9;
                    h9 += p64[9]; h11 ^= h7; h8 ^= h9; h9 = h9 << 54 | h9 >> -54; h8 += h10;
                    h10 += p64[10]; h0 ^= h8; h9 ^= h10; h10 = h10 << 22 | h10 >> -22; h9 += h11;
                    h11 += p64[11]; h1 ^= h9; h10 ^= h11; h11 = h11 << 46 | h11 >> -46; h10 += h0;
                    p64 += NumVars;
                }
            }
            else
                while (p64 < end)
                {
                    memcpy(buf, p64, BlockSize);

                    h0 += buf[0]; h2 ^= h10; h11 ^= h0; h0 = h0 << 11 | h0 >> -11; h11 += h1;
                    h1 += buf[1]; h3 ^= h11; h0 ^= h1; h1 = h1 << 32 | h1 >> 32; h0 += h2;
                    h2 += buf[2]; h4 ^= h0; h1 ^= h2; h2 = h2 << 43 | h2 >> -43; h1 += h3;
                    h3 += buf[3]; h5 ^= h1; h2 ^= h3; h3 = h3 << 31 | h3 >> -31; h2 += h4;
                    h4 += buf[4]; h6 ^= h2; h3 ^= h4; h4 = h4 << 17 | h4 >> -17; h3 += h5;
                    h5 += buf[5]; h7 ^= h3; h4 ^= h5; h5 = h5 << 28 | h5 >> -28; h4 += h6;
                    h6 += buf[6]; h8 ^= h4; h5 ^= h6; h6 = h6 << 39 | h6 >> -39; h5 += h7;
                    h7 += buf[7]; h9 ^= h5; h6 ^= h7; h7 = h7 << 57 | h7 >> -57; h6 += h8;
                    h8 += buf[8]; h10 ^= h6; h7 ^= h8; h8 = h8 << 55 | h8 >> -55; h7 += h9;
                    h9 += buf[9]; h11 ^= h7; h8 ^= h9; h9 = h9 << 54 | h9 >> -54; h8 += h10;
                    h10 += buf[10]; h0 ^= h8; h9 ^= h10; h10 = h10 << 22 | h10 >> -22; h9 += h11;
                    h11 += buf[11]; h1 ^= h9; h10 ^= h11; h11 = h11 << 46 | h11 >> -46; h10 += h0;
                    p64 += NumVars;
                }
            int remainder = length - (int)((byte*)end - (byte*)message);

            if (remainder != 0)
                memcpy(buf, end, remainder);
            memzero(((byte*)buf) + remainder, BlockSize - remainder);
            ((byte*)buf)[BlockSize - 1] = (byte)remainder;

            h0 += buf[0]; h1 += buf[1]; h2 += buf[2]; h3 += buf[3];
            h4 += buf[4]; h5 += buf[5]; h6 += buf[6]; h7 += buf[7];
            h8 += buf[8]; h9 += buf[9]; h10 += buf[10]; h11 += buf[11];
            h11 += h1; h2 ^= h11; h1 = h1 << 44 | h1 >> -44;
            h0 += h2; h3 ^= h0; h2 = h2 << 15 | h2 >> -15;
            h1 += h3; h4 ^= h1; h3 = h3 << 34 | h3 >> -34;
            h2 += h4; h5 ^= h2; h4 = h4 << 21 | h4 >> -21;
            h3 += h5; h6 ^= h3; h5 = h5 << 38 | h5 >> -38;
            h4 += h6; h7 ^= h4; h6 = h6 << 33 | h6 >> -33;
            h5 += h7; h8 ^= h5; h7 = h7 << 10 | h7 >> -10;
            h6 += h8; h9 ^= h6; h8 = h8 << 13 | h8 >> -13;
            h7 += h9; h10 ^= h7; h9 = h9 << 38 | h9 >> -38;
            h8 += h10; h11 ^= h8; h10 = h10 << 53 | h10 >> -53;
            h9 += h11; h0 ^= h9; h11 = h11 << 42 | h11 >> -42;
            h10 += h0; h1 ^= h10; h0 = h0 << 54 | h0 >> -54;
            h11 += h1; h2 ^= h11; h1 = h1 << 44 | h1 >> -44;
            h0 += h2; h3 ^= h0; h2 = h2 << 15 | h2 >> -15;
            h1 += h3; h4 ^= h1; h3 = h3 << 34 | h3 >> -34;
            h2 += h4; h5 ^= h2; h4 = h4 << 21 | h4 >> -21;
            h3 += h5; h6 ^= h3; h5 = h5 << 38 | h5 >> -38;
            h4 += h6; h7 ^= h4; h6 = h6 << 33 | h6 >> -33;
            h5 += h7; h8 ^= h5; h7 = h7 << 10 | h7 >> -10;
            h6 += h8; h9 ^= h6; h8 = h8 << 13 | h8 >> -13;
            h7 += h9; h10 ^= h7; h9 = h9 << 38 | h9 >> -38;
            h8 += h10; h11 ^= h8; h10 = h10 << 53 | h10 >> -53;
            h9 += h11; h0 ^= h9; h11 = h11 << 42 | h11 >> -42;
            h10 += h0; h1 ^= h10; h0 = h0 << 54 | h0 >> -54;
            h11 += h1; h2 ^= h11; h1 = h1 << 44 | h1 >> -44;
            h0 += h2; h3 ^= h0; h2 = h2 << 15 | h2 >> -15;
            h1 += h3; h4 ^= h1; h3 = h3 << 34 | h3 >> -34;
            h2 += h4; h5 ^= h2; h4 = h4 << 21 | h4 >> -21;
            h3 += h5; h6 ^= h3; h5 = h5 << 38 | h5 >> -38;
            h4 += h6; h7 ^= h4; h6 = h6 << 33 | h6 >> -33;
            h5 += h7; h8 ^= h5; h7 = h7 << 10 | h7 >> -10;
            h6 += h8; h9 ^= h6; h8 = h8 << 13 | h8 >> -13;
            h7 += h9; h10 ^= h7; h9 = h9 << 38 | h9 >> -38;
            h8 += h10; h11 ^= h8; h10 = h10 << 53 | h10 >> -53;
            h9 += h11; h0 ^= h9;
            h10 += h0; h1 ^= h10; h0 = h0 << 54 | h0 >> -54;
            *hash2 = h1;
            *hash1 = h0;
        }

        static unsafe void Short(void* message, int length, ulong* hash1, ulong* hash2, bool skipTest)
        {
            ulong* p64;
            if (!skipTest && !AllowUnalignedRead && length != 0 && (((long)message) & 7) != 0)
            {
                ulong* buf = stackalloc ulong[2 * NumVars];
                memcpy(buf, message, length);
                Short(buf, length, hash1, hash2, true);
                return;
            }
            p64 = (ulong*)message;

            int remainder = length & 31;
            ulong a = *hash1;
            ulong b = *hash2;
            ulong c = SpookyConst;
            ulong d = SpookyConst;

            if (length > 15)
            {
                ulong* end = p64 + ((length / 32) * 4);
                for (; p64 < end; p64 += 4)
                {
                    c += p64[0];
                    d += p64[1];
                    c = c << 50 | c >> -50; c += d; a ^= c;
                    d = d << 52 | d >> -52; d += a; b ^= d;
                    a = a << 30 | a >> -30; a += b; c ^= a;
                    b = b << 41 | b >> -41; b += c; d ^= b;
                    c = c << 54 | c >> -54; c += d; a ^= c;
                    d = d << 48 | d >> -48; d += a; b ^= d;
                    a = a << 38 | a >> -38; a += b; c ^= a;
                    b = b << 37 | b >> -37; b += c; d ^= b;
                    c = c << 62 | c >> -62; c += d; a ^= c;
                    d = d << 34 | d >> -34; d += a; b ^= d;
                    a = a << 5 | a >> -5; a += b; c ^= a;
                    b = b << 36 | b >> -36; b += c; d ^= b;
                    a += p64[2];
                    b += p64[3];
                }
                if (remainder >= 16)
                {
                    c += p64[0];
                    d += p64[1];
                    c = c << 50 | c >> -50; c += d; a ^= c;
                    d = d << 52 | d >> -52; d += a; b ^= d;
                    a = a << 30 | a >> -30; a += b; c ^= a;
                    b = b << 41 | b >> -41; b += c; d ^= b;
                    c = c << 54 | c >> -54; c += d; a ^= c;
                    d = d << 48 | d >> -48; d += a; b ^= d;
                    a = a << 38 | a >> -38; a += b; c ^= a;
                    b = b << 37 | b >> -37; b += c; d ^= b;
                    c = c << 62 | c >> -62; c += d; a ^= c;
                    d = d << 34 | d >> -34; d += a; b ^= d;
                    a = a << 5 | a >> -5; a += b; c ^= a;
                    b = b << 36 | b >> -36; b += c; d ^= b;
                    p64 += 2;
                    remainder -= 16;
                }
            }
            d += ((ulong)length) << 56;
            switch (remainder)
            {
                case 15:
                    d += ((ulong)((byte*)p64)[14]) << 48;
                    goto case 14;
                case 14:
                    d += ((ulong)((byte*)p64)[13]) << 40;
                    goto case 13;
                case 13:
                    d += ((ulong)((byte*)p64)[12]) << 32;
                    goto case 12;
                case 12:
                    d += ((uint*)p64)[2];
                    c += p64[0];
                    break;
                case 11:
                    d += ((ulong)((byte*)p64)[10]) << 16;
                    goto case 10;
                case 10:
                    d += ((ulong)((byte*)p64)[9]) << 8;
                    goto case 9;
                case 9:
                    d += (ulong)((byte*)p64)[8];
                    goto case 8;
                case 8:
                    c += p64[0];
                    break;
                case 7:
                    c += ((ulong)((byte*)p64)[6]) << 48;
                    goto case 6;
                case 6:
                    c += ((ulong)((byte*)p64)[5]) << 40;
                    goto case 5;
                case 5:
                    c += ((ulong)((byte*)p64)[4]) << 32;
                    goto case 4;
                case 4:
                    c += ((uint*)p64)[0];
                    break;
                case 3:
                    c += ((ulong)((byte*)p64)[2]) << 16;
                    goto case 2;
                case 2:
                    c += ((ulong)((byte*)p64)[1]) << 8;
                    goto case 1;
                case 1:
                    c += (ulong)((byte*)p64)[0];
                    break;
                case 0:
                    c += SpookyConst;
                    d += SpookyConst;
                    break;
            }
            d ^= c; c = c << 15 | c >> -15; d += c;
            a ^= d; d = d << 52 | d >> -52; a += d;
            b ^= a; a = a << 26 | a >> -26; b += a;
            c ^= b; b = b << 51 | b >> -51; c += b;
            d ^= c; c = c << 28 | c >> -28; d += c;
            a ^= d; d = d << 9 | d >> -9; a += d;
            b ^= a; a = a << 47 | a >> -47; b += a;
            c ^= b; b = b << 54 | b >> -54; c += b;
            d ^= c; c = c << 32 | c >> -32; d += c;
            a ^= d; d = d << 25 | d >> -25; a += d;
            b ^= a; a = a << 63 | a >> -63; b += a;
            *hash2 = b;
            *hash1 = a;
        }

        static unsafe void memcpy(void* destPtr, void* srcPtr, int size)
        {
            var dest = (ulong*)destPtr;
            var src = (ulong*)srcPtr;
            for (ulong i = 0, c = (ulong)size / sizeof(long); i < c; ++i)
                dest[i] = src[i];
        }

        static unsafe void memzero(void* destPtr, int length)
        {
            var dest = (ulong*)destPtr;
            for (ulong i = 0, c = (ulong)length / sizeof(long); i < c; ++i)
                dest[i] = 0;
        }
    }
}
