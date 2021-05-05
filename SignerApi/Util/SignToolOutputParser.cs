using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using NETCore.Encrypt;
using Newtonsoft.Json;
using SignerApi.Models;

namespace SignerApi.Util
{
    public static class SignToolOutputParser
    {

        public enum SignToolOperation
        {
            Verify,
            Sign
        }

        public static ApiActivity parseSignToolOutput(SignToolOperation op, ApiActivity ac, string stdOut, string stdErr)
        {

            if (stdOut != null && stdOut.Length > 0)
            {
                //try to parse everything possible
                string patIssuedTo = @"Issued to: (.*?)$";
                Match m2 = Regex.Match(stdOut, patIssuedTo, RegexOptions.Multiline);
                ac.CertIssuedTo = m2.Groups[1].Value.Trim();

                string patIssuedBy = @"Issued by: (.*?)$";
                Match m3 = Regex.Match(stdOut, patIssuedBy, RegexOptions.Multiline);
                ac.CertIssuedBy = m3.Groups[1].Value.Trim();

                string patExpire = @"Expires: (.*?)$";
                Match m4 = Regex.Match(stdOut, patExpire, RegexOptions.Multiline);
                ac.CertExpire = m4.Groups[1].Value.Trim();

                string patCertHash = @"\s+.*?[H|h]ash: (\w+)";
                Match mCertHash = Regex.Match(stdOut, patCertHash, RegexOptions.Multiline);
                ac.CertHash = mCertHash.Groups[1].Value.Trim();

                string patFileHash = @"[H|h]ash of file.*?: (\w+)";
                Match mFileHash = Regex.Match(stdOut, patFileHash, RegexOptions.Multiline);
                ac.FileHash = mFileHash.Groups[1].Value.Trim();

                // if file hash was not parsed from output of signtool.exe, create our own hash
                if (ac.FileHash == "")
                {
                    using (FileStream fs = System.IO.File.OpenRead(ac.SystemOfficeFilename))
                    {
                        var sha = new SHA256Managed();
                        byte[] checksum = sha.ComputeHash(fs);
                        ac.FileHash = BitConverter.ToString(checksum).Replace("-", String.Empty); 
                    }
                }


                //decide if error or success
                // note: error is raised if trust chain cannot be verified. This is a normal behavior, due the fact
                // that certificates were not installed in windows cert. store, but only uploaded for checking once.
                // So if cert data can be parsed from file this is success, although stderr is populated!

                if (op == SignToolOperation.Verify)
                {
                    string patSigIdx = @"Signature Index: \d.*$";
                    Match m5 = Regex.Match(stdOut, patSigIdx, RegexOptions.Multiline);
                    if (m5.Success) // signature could be read from file successfully!
                    {
                        ac.Status = ApiActivity.ApiStatus.Ready;
                        ac.Message = ac.Message = $"File {ac.UserOfficeFilename} verified successfully.";
                        return ac;
                    }
                }
                if (op == SignToolOperation.Sign)
                {
                    string patSuccSigned = @"Successfully signed:.*$";
                    Match m6 = Regex.Match(stdOut, patSuccSigned, RegexOptions.Multiline);
                    if (m6.Success) 
                    {
                        ac.Status = ApiActivity.ApiStatus.Ready;
                        ac.Message = $"File {ac.UserOfficeFilename} signed successfully.";
                        return ac;
                    }
                }

                // if filehash was not on stdout, calculate seperately
                

            }
            if (stdErr != null && stdErr.Length > 0)
            {
                ac.Status = ApiActivity.ApiStatus.Error;
                ac.Message = stdErr.Trim();
            }

            return ac;
        }

    }
}
