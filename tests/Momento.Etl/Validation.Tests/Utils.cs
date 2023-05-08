using System;
using System.Collections.Generic;
using System.Text;
using Momento.Etl.Model;
using Momento.Etl.Validation;
using Xunit;

namespace Momento.Etl.Validation.Tests;

internal static class Utils
{
    public static string RepeatChar(char c, int number)
    {
        StringBuilder sb = new();
        for (int i = 0; i < number; i++)
        {
            sb.Append(c);
        }
        return sb.ToString();
    }
}
