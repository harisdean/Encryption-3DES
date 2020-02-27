﻿using System;
using System.IO;
using System.Security.Cryptography;
using System.Text.Encodings;
using System.Text;
using System.IO.Enumeration;
using System.Security.Cryptography.X509Certificates;
using System.Configuration;
using Microsoft.Extensions.Configuration;

namespace _3DESFile
{
    class Program
    {
        //public delegate void EncryptionFile(string inputFile, string outputFile);
        public delegate void EncryptionMethod(String inName, String outName, byte[] tdesKey);
        public static AppSettings appSettings = new AppSettings();

        static void Main(string[] args)
        {
            Console.WriteLine("3DES Encrypt/Decrypt Program.");

            CmdParams cmdParams = ReadCommandLine(args);
            StringBuilder msgResult = GenerateFile(cmdParams);

            Console.WriteLine(msgResult);
        }

        static CmdParams ReadCommandLine(string[] args)
        {
            Console.WriteLine("Argument length: " + args.Length);
            Console.WriteLine("Supplied Arguments are:");

            foreach (Object obj in args)
            {
                Console.WriteLine(obj);
            }

            CmdParams cmdParams = new CmdParams();
            cmdParams.cmd = args[0];
            cmdParams.inputFile = args[1];
            cmdParams.outputFile = args[2];

            return cmdParams;
        }

        static StringBuilder GenerateFile(CmdParams cmdParams)
        {
            StringBuilder msg = new StringBuilder();
            try
            {
                //Delegate function for either Encrypt or Decrypt method to be called.
                EncryptionMethod encryptionMethod = null;

                if (cmdParams.cmd == "-e")
                    encryptionMethod = EncryptData;
                else if (cmdParams.cmd == "-d")
                    encryptionMethod = DecryptData;
                else // "-t" for test or both
                {
                    //EncryptFile(cmdParams.inputFile, cmdParams.outputFile);
                    //DecryptFile(cmdParams.inputFile, cmdParams.outputFile);
                }

                EncryptingFile(cmdParams.inputFile, cmdParams.outputFile, encryptionMethod);
            }
            catch (Exception ex)
            {
                msg.Append(string.Format("{0} {1}", "Unsuccessful.", ex.Message));
                return msg;
            }

            msg.AppendLine("Successfully processed the request");
            msg.AppendLine(string.Format("{0}{1}", "/in/", cmdParams.inputFile));
            msg.AppendLine(string.Format("{0}{1}", "/out/", cmdParams.outputFile));

            return msg;
        }

        static void EncryptingFile(string inputFile, string outputFile, EncryptionMethod encryptionMethod)
        {
            var appDataDir = Environment.CurrentDirectory.ToString();

            var fileIn = Path.Combine(appDataDir, "in", inputFile);

            Console.WriteLine(fileIn);
            Console.WriteLine(string.Format("Input file exists? {0}", File.Exists(fileIn)));

            var fileOut = Path.Combine(appDataDir, "out", outputFile);

            //EncryptData(fileIn, fileOut, GetKeyBytes());
            encryptionMethod(fileIn, fileOut, GetKeyBytes());
        }

        /// <summary>
        /// Read appsettings.json for Key
        /// </summary>
        /// <returns></returns>
        private static byte[] GetKeyBytes()
        {
            var builder = new ConfigurationBuilder()
               .SetBasePath(Directory.GetCurrentDirectory())
               .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true);
            var configuration = builder.Build();
            ConfigurationBinder.Bind(configuration.GetSection("AppSettings"), appSettings);

            return UTF8Encoding.UTF8.GetBytes(appSettings.EncodedKey);
        }

        private static void EncryptData(String inName, String outName, byte[] tdesKey)
        {
            TripleDESCryptoServiceProvider tdes = new TripleDESCryptoServiceProvider();
            tdes.Mode = CipherMode.ECB;
            tdes.Key = tdesKey;
            tdes.Padding = PaddingMode.PKCS7;

            Console.WriteLine("Encrypting...");

            //Read from the input file, then encrypt and write to the output file.
            string[] lines = File.ReadAllLines(inName, Encoding.UTF8);
            using FileStream fs = File.OpenWrite(outName);

            foreach (var line in lines)
            {
                //Console.WriteLine(line);
                string encrypted = Encrypt(line, tdes);
                //Console.WriteLine(encrypted);

                byte[] bytes = Encoding.UTF8.GetBytes(encrypted);
                fs.Write(bytes, 0, bytes.Length);
                byte[] nl = Encoding.UTF8.GetBytes(Environment.NewLine);
                fs.Write(nl, 0, nl.Length);
            }
        }

        public static string Encrypt(string input, TripleDESCryptoServiceProvider tripleDES)
        {
            byte[] inputArray = UTF8Encoding.UTF8.GetBytes(input);
            ICryptoTransform cTransform = tripleDES.CreateEncryptor();
            byte[] resultArray = cTransform.TransformFinalBlock(inputArray, 0, inputArray.Length);

            return Convert.ToBase64String(resultArray, 0, resultArray.Length);
        }

        private static void DecryptData(String inName, String outName, byte[] tdesKey)
        {
            TripleDESCryptoServiceProvider tdes = new TripleDESCryptoServiceProvider();
            tdes.Mode = CipherMode.ECB;
            tdes.Key = tdesKey;
            tdes.Padding = PaddingMode.PKCS7;

            Console.WriteLine("Decrypting...");

            //Read from the input file, then decrypt and write to the output file.
            string[] lines = File.ReadAllLines(inName, Encoding.UTF8);
            using FileStream fs = File.OpenWrite(outName);

            foreach (var line in lines)
            {
                //Console.WriteLine(line);
                byte[] decrypted = Decrypt(line, tdes);
                //Console.WriteLine(UTF8Encoding.UTF8.GetString(decrypted));

                fs.Write(decrypted, 0, decrypted.Length);
                byte[] nl = Encoding.UTF8.GetBytes(Environment.NewLine);
                fs.Write(nl, 0, nl.Length);
            }
        }

        public static byte[] Decrypt(string input, TripleDESCryptoServiceProvider tripleDES)
        {
            byte[] inputArray = Convert.FromBase64String(input);
            ICryptoTransform cTransform = tripleDES.CreateDecryptor();
            byte[] resultArray = cTransform.TransformFinalBlock(inputArray, 0, inputArray.Length);

            return resultArray;
        }
    }
}
