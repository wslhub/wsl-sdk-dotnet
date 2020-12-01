# WSL SDK for .NET Standard/.NET Core

[![NuGet version (Wslhub.Sdk)](https://img.shields.io/nuget/v/Wslhub.Sdk.svg?style=flat-square)](https://www.nuget.org/packages/Wslhub.Sdk/)

This project contains a WSL API wrapper for Windows developers who wants to integrate WSL features into existing Windows applications. You can enumerate, query, executing WSL commands via C# classes.

## How to use

You will need the below items to use the WSL APIs.

- Add an application manifest which describes your application is compatible with the Windows 10 (GUID: `8e0f7a12-bfb3-4fe8-b9a5-48fd50a15a9a`) and the `requestedExecutionLevel` tag with `asInvoker` level.
- Place the code `Wsl.InitializeSecurityModel()` at the top of your application's `Main()` method.

## Limitation

Due to the limitation of COM security model requirements of WSL APIs, you can not run this SDK within the Visual Studio Test Explorer or LINQPAD.

## Code Example

```csharp
using Wslhub.Sdk;

// Place the code `Wsl.InitializeSecurityModel()` at the top of your application's `Main()` method.
Wsl.InitializeSecurity();

// Assert WSL installation status
Wsl.AssertWslSupported();

// Enumerate distro list
var distros = Wsl.GetDistroListFromRegistry();

// Query distro informations
var queryResults = Wsl.GetDistroQueryResult();

// Run a command
var result = Wsl.RunWslCommand(distroName, "cat /etc/passwd");
```

## Upcoming Features

- Aware default distro
- Executing WSL commands via C# API (supporting more longer CLOBs)
