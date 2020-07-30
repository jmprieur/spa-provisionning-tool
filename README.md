# spa-provisionning-tool
Provisions or updates a Microsoft identity platform Single Page Application (SPA)

 ## Pre-requisites
 
 This tool leverages .NET Core 3.1, which is a cross platform runtime (Windows, Linux, Mac). 
 
 If your computer does not have .NET Core yet, install it from [Download .NET Core 3.1](https://dotnet.microsoft.com/download/dotnet-core/3.1), for instance you can use the .NET Core Runtime 3.1.6
 
 ## Running the tool
 
 ### Example of end to end
 
 Register a SPA application named "My first app from provisionning tool" with a SPA redirect URI https://localhost:12345
 
 ```Shell
 SPA add --app-Name "My first app from provisionning tool" --spa-redirect-uri https://localhost:12345  [--tenant-id<TenantId>]
 ```
 
 Convert an application with a SPA redirect URI to a Web redirect URI
 ```Shell
 spa updateToSpa --client-id 8657ba26-dde3-4534-b5a8-8104e73dcbaf
 ```
 
 Convert an application with a Web redirect URI to a SPA redirect URI
 ```Shell
 spa updateToWeb --client-id 8657ba26-dde3-4534-b5a8-8104e73dcbaf
 ```
 
