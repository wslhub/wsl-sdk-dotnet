# WSL SDK for .NET Standard/.NET Core

This project contains a WSL API wrapper for Windows developers who wants to integrate WSL features into existing Windows applications. You can enumerate, query, executing WSL commands via C# classes.

## Code Example

```csharp
using Wslhub.Sdk;

// Assert WSL installation status
Wsl.AssertWslSupported();

// Enumerate distro list
var distros = Wsl.GetDistroListFromRegistry();

// Query distro informations
var queryResults = Wsl.GetDistroQueryResult();
```

## Upcoming Features

- Executing WSL commands via C# API
