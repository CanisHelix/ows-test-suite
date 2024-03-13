<h1 align="center">OWS Test Suite</h1>

A project used to perform functional tests against MSSQL/MySQL and Postgres database functions for OWS.

_[Contibutors](#license-contribution) and PRs are welcome_.

# Introduction

**Dependencies**

- [Open World Server](https://github.com/Dartanlla/OWS/)
- Open World Server Database
    - Customer with GUID of `00000000-0000-0000-0000-000000000001` having the following:
        - Default Data from Init scripts
        - Default Data from HubWorld MMO Init scripts

# Installation

- Copy `appsettings.json.example` as `appsettings.json`
- Update all connection strings
- Install [.NET 8 SDK](https://dotnet.microsoft.com/en-us/download/dotnet/thank-you/sdk-8.0.201-windows-x64-installer)
- Load project in Visual Studio/Rider
- Run tests

## License & Contribution

OWS Test Suite is licensed under the MIT License, see [LICENSE.md](LICENSE.md) for more information. Other developers are encouraged to fork the repository, open issues & pull requests to help the development.

# Special Thanks

[Dartanlla](https://github.com/Dartanlla)
