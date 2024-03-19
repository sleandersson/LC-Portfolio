using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;

public static class ApiKeyManager
{
    public static string GetApiKey(string keyName)
    {
        // Simply fetch and return the API key from an environment variable
        return Environment.GetEnvironmentVariable(keyName);
    }
}

