using System;
using System.Collections.Generic;

namespace TED.Utils
{
    /// <summary>
    /// Provides functionality for replacing tokens in a string.
    /// </summary>
    internal class Tokenizer
    {
        /// <summary>
        /// A dictionary that maps token names to their corresponding values.
        /// </summary>
        static Dictionary<string, Func<string>> TokenLookup = new Dictionary<string, Func<string>>()
        {
            { "@userName", MachineInfoTokenProvider.GetUserName },
            { "@machineName", MachineInfoTokenProvider.GetMachineName },
            { "@machineSerial", MachineInfoTokenProvider.GetMachineSerial },
            { "@manufacturer", MachineInfoTokenProvider.GetManufacturer },
            { "@model", MachineInfoTokenProvider.GetModel },
            { "@ipAddress", MachineInfoTokenProvider.GetPrimaryIpAddress },
            { "@macAddress", MachineInfoTokenProvider.GetPrimaryMacAddress },
            { "@osVersion", MachineInfoTokenProvider.GetOsVersion },
            { "@osName", MachineInfoTokenProvider.GetOsName }
        };

        /// <summary>
        /// Replaces tokens in the input string with their corresponding values.
        /// </summary>
        /// <param name="input">The string that may contain tokens to be replaced.</param>
        /// <returns>The input string with all tokens replaced by their corresponding values.</returns>
        public static string ReplaceTokens(string input)
        {
            foreach (var kvp in TokenLookup)
            {
                var token = kvp.Key;
                var value = kvp.Value();

                input = input.Replace(token, value);
            }

            return input;
        }
    }

}
