# APSI Library for .Net (APSINet)

## Introduction

APSINet is a set of C# wrappers for the [APSI](https://github.com/microsoft/APSI) library. It contains a wrapper for the Server and the Client parts of APSI. Unlike the original APSI, it does not contain any networking implementation. The library transforms queries and query responses into byte buffers that the user should transmit between Client and Server in the most appropriate manner for their particular use-case.

## Getting Started

Currently compiling APSINet is only supported in Windows. You will need Visual Studio 2019 or newer to compile APSINet.

### Setting up vcpkg
APSINet depends on APSI, which depends on several C++ libraries to compile. You will need to clone the [vcpkg repository](https://github.com/microsoft/vcpkg) into a local directory. After the code has been cloned, please initialize vcpkg by running:

``` bootstrap-vcpkg.bat ```

After bootstrapping is complete, please run the following command to install the required dependencies:

``` vcpkg install --triplet=x64-windows-static-md apsi[hexl,log4cplus] ```

After installation is complete, please setup an environment variable called `VCPKGDIR` pointing to the directory where `vcpkg` was cloned. The Microsoft Visual Studio solution will use this variable to fine the required dependencies for APSI.


## Building APSINet
To build APSINet, simply open the provided Microsoft Visual Studio solution file called `APSILibrary.sln`.

## Contribute
For contributing to APSINet, please see [CONTRIBUTING.md](CONTRIBUTING.md).