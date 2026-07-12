// A simple PowerShell script to generate a BCrypt hash by calling the .NET project
// We'll use the BCrypt.Net-Next nuget package via a temp dotnet app

$tempDir = "C:\Users\sanjeevi\Downloads\Fintech\HashGen"
New-Item -ItemType Directory -Force -Path $tempDir | Out-Null

# Create project file
@'
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <OutputType>Exe</OutputType>
    <TargetFramework>net8.0</TargetFramework>
  </PropertyGroup>
  <ItemGroup>
    <PackageReference Include="BCrypt.Net-Next" Version="4.0.3" />
  </ItemGroup>
</Project>
'@ | Out-File -Encoding utf8 "$tempDir\HashGen.csproj"

# Create Program.cs
@'
using BCrypt.Net;
string password = "Admin@123";
string hash = BCrypt.Net.BCrypt.HashPassword(password, 11);
Console.WriteLine(hash);
'@ | Out-File -Encoding utf8 "$tempDir\Program.cs"

Set-Location $tempDir
dotnet run 2>&1
