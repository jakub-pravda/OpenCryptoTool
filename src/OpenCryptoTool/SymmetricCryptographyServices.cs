using OpenCryptoTool.Helpers;
using OpenCryptoTool.Models;
using OpenCryptoTool.Providers;
using Serilog;
using System;
using System.Security.Cryptography;

namespace OpenCryptoTool
{
    /// <summary>
    ///     Symmetric cryptography services.
    /// </summary>
    public static class SymmetricCryptographyServices
    {
        /// <summary>
        ///     Encryption services processor.
        /// </summary>
        /// <param name="cliInput">CLI options.</param>
        public static SymmetricCryptographyCliOutput ProcessOperation(ISymmetricCryptographyCliInput cliInput)
        {
            Log.Information($"Command for {cliInput.CipherType.CryptographyStandard.ToString()} de/encryption successfully parsed.");

            // warning when IV is entered by user and ECB cipher mode is used
            if (!string.IsNullOrEmpty(cliInput.InitializationVector) && cliInput.CipherType.CipherMode == CipherMode.ECB)
            {
                Log.Information("Initialization vector is not valid for ECB cipher mode and will be ignored.");
            }

            SymmetricCryptographyCliOutput cliOutput = null;
            switch (cliInput.CipherType.CryptographyStandard)
            {
                case (CryptographyStandard.Aes):
                    cliOutput = AesCryptography(cliInput);
                    break;

                default:
                    break;
            }

            return cliOutput;
        }

        /// <summary>
        ///     AES cryptography processor.
        /// </summary>
        /// <param name="cliObject"></param>
        public static SymmetricCryptographyCliOutput AesCryptography(ISymmetricCryptographyCliInput cliObject)
        {
            if (cliObject.Encryption)
            {
                // encryption
                return AesEncryption(cliObject); // TODO opravit... en/decryption should be enum
            }
            else
            {
                // decryption
                return AesDecryption(cliObject);
            }
        }

        /// <summary>
        ///     AES Encryption service.
        /// </summary>
        /// <param name="toEncryption">CLI input model.</param>
        /// <returns>CLI output model with information about decryption.</returns>
        public static SymmetricCryptographyCliOutput AesEncryption(ISymmetricCryptographyCliInput toEncryption)
        {
            Log.Information($"New aes encryption request => {toEncryption.CipherType}");

            var aesProvider = new AesProvider();
            byte[] key = new byte[0];
            byte[] IV = new byte[0];

            // check if user provide encryption key
            if (!string.IsNullOrEmpty(toEncryption.Key))
            {
                Log.Information("Working with the provided encryption key.");

                key = Convert.FromBase64String(toEncryption.Key);
            }
            else
            {
                Log.Information("Generating new encryption key.");

                key = aesProvider.GenerateKey(toEncryption.CipherType.KeySize);
            }

            // check if user provide IV
            if (!string.IsNullOrEmpty(toEncryption.InitializationVector) && toEncryption.CipherType.CipherMode != CipherMode.ECB)
            {
                Log.Information("Working with the provided initialization vector.");

                Console.WriteLine("Using same initialization vector for more then one encryption is not recommended!");
                IV = Convert.FromBase64String(toEncryption.InitializationVector);
            }
            else
            {
                Log.Information("Generating new initialization vector.");

                IV = aesProvider.GenerateInitializationVector();
            }

            var encrypted = aesProvider.Encrypt(toEncryption.Content, key, IV, toEncryption.CipherType.CipherMode);

            Log.Information("Successfully encrypted.");

            if (toEncryption.CipherType.CipherMode == CipherMode.ECB)
            {
                return new SymmetricCryptographyCliOutput(key, encrypted, toEncryption.CipherType);
            }
            else
            {
                return new SymmetricCryptographyCliOutput(IV, key, encrypted, toEncryption.CipherType);
            }
        }

        /// <summary>
        ///     AES Decryption service.
        /// </summary>
        /// <param name="toDecryption">CLI input model.</param>
        /// <returns>CLI output model with information about decryption.</returns>
        public static SymmetricCryptographyCliOutput AesDecryption(ISymmetricCryptographyCliInput toDecryption)
        {
            Log.Information($"New aes decryption request => {toDecryption.CipherType}");

            if (string.IsNullOrEmpty(toDecryption.Content))
            {
                Log.Information("Data which should be decrypted missing - asking user for input.");

                toDecryption.Content = CLIHelpers.InformationProvider("Enter entrycpted phrase");
            }

            if (string.IsNullOrEmpty(toDecryption.Key))
            {
                Log.Information("The encryption key is missing - asking user for input.");

                toDecryption.Key = CLIHelpers.InformationProvider("Enter encryption key");
            }

            if (string.IsNullOrEmpty(toDecryption.InitializationVector) && toDecryption.CipherType.CipherMode != CipherMode.ECB)
            {
                Log.Information("The initialization vector is missing - asking user for input");

                toDecryption.InitializationVector = CLIHelpers.InformationProvider("Enter initialization vector");
            }

            AesProvider aesProvider;
            if (toDecryption.CipherType.CipherMode == CipherMode.ECB)
            {
                // ignore IV when ECB cipher mode is used
                aesProvider = new AesProvider(toDecryption.Key, toDecryption.CipherType.CipherMode); 
            }
            else
            {
                aesProvider = new AesProvider(toDecryption.Key, toDecryption.InitializationVector, toDecryption.CipherType.CipherMode);
            }

            var decrypted = aesProvider.Decrypt(toDecryption.Content);

            Log.Information("Successfully decrypted.");

            return new SymmetricCryptographyCliOutput(decrypted);
        }
    }

    /// <summary>
    ///     Returned data format options.
    /// </summary>
    public enum EncryptedTextReturnOptions
    {
        Base64String
    }
}