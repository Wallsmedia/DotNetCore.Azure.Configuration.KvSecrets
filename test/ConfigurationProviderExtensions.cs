// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.Extensions.Configuration;
using System;

namespace AspNetCore.Azure.Configuration.KvSecrets.Tests;

public static class ConfigurationProviderExtensions
{
    public static string Get(this IConfigurationProvider provider, string key)
    {
        string value;

        if (!provider.TryGet(key, out value))
        {
            throw new InvalidOperationException("Key not found");
        }

        return value;
    }
}